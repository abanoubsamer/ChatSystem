using Application.Abstractions.Cache;
using Application.Abstractions.Grain;
using Application.Abstractions.Repositories.ChatMember;
using Application.Abstractions.Repositories.MessageReceipts;
using Application.Abstractions.Services.Publisher;
using Application.Dtos;
using Application.Dtos.Ack;
using Domain.Models.Result;
using Application.Dtos.MessageReceipts.Command;
using Contracts.Message.Events;
using Domain.Models.State;
using Infrastructure.Extension;
using Infrastructure.Services.Ack;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using System.Diagnostics;

/// <summary>
/// Refactored AckGrain:
/// 1. Removed [Reentrant] - Relying on Orleans' single-threaded model.
/// 2. Removed Channel-based batching - Orleans handles message queuing.
/// 3. Direct processing for predictable performance.
/// </summary>
public sealed class AckGrain : Grain, IAckGrain
{
    private readonly IPersistentState<ChatAckState> _persistentState;
    private AckEngine? _engine;
    private readonly ILogger<AckGrain> _logger;
    private HashSet<string>? _members;
    private int _memberCount;
    private bool _initialized;
    private long _processedCount;
    private readonly Stopwatch _uptime = new();
    private readonly IMessagePublisher _publisher;
    private readonly IChatMemberCommandRepository _repository;
    private readonly IMessageReceiptsCommandRepository _Msgrepository;
    private readonly IChatMemberCache _memberCache;

    // Batching for DB updates (Message Receipts)
    private readonly List<UpdateMessageReceiptsDto> _pendingReceipts = new();
    private IDisposable? _flushTimer;

    public AckGrain(
        [PersistentState("ackState", "AckStore")] IPersistentState<ChatAckState> persistentState,
        IMessagePublisher publisher,
        IChatMemberCommandRepository repository,
        ILogger<AckGrain> logger,
        IMessageReceiptsCommandRepository Msgrepository,
        IChatMemberCache memberCache)
    {
        _logger = logger;
        _Msgrepository = Msgrepository;
        _persistentState = persistentState;
        _publisher = publisher;
        _repository = repository;
        _memberCache = memberCache;
    }

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        _uptime.Start();
        
        var chatId = this.GetPrimaryKeyString();
        LogDebug("Activating Grain", chatId);

        _members = await LoadMembersAsync(chatId, ct);
        _memberCount = _members.Count;

        _engine = new AckEngine(_persistentState.State, _memberCount);

        _flushTimer = RegisterTimer(
            async _ => await FlushInternalAsync(),
            state: null,
            dueTime: TimeSpan.FromMilliseconds(500),
            period: TimeSpan.FromMilliseconds(500)
        );

        _initialized = true;
        LogDebug($"Grain activated with {_memberCount} members", chatId);
    }

    public async ValueTask<AckResult> ReceiveAsync(AckReceived ack)
    {
        if (!_initialized) throw new InvalidOperationException("Grain not initialized");

        var sw = Stopwatch.StartNew();

        // Direct process (thread-safe due to Orleans model)
        AckResult result = ack.Type == AckType.Delivery
                    ? _engine!.UpdateDelivery(ack.UserId, ack.MessageId)
                    : _engine!.UpdateRead(ack.UserId, ack.MessageId);

        _pendingReceipts.Add(new UpdateMessageReceiptsDto
        {
            UserId = ack.UserId,
            MessageId = ack.MessageId,
            ChatId = ack.ChatId,
            Status = ack.Type,
            DeliveredAt = ack.Type == AckType.Delivery ? ack.Timestamp : null,
            ReadAt = ack.Type == AckType.Seen ? ack.Timestamp : null
        });

        _processedCount++;
        sw.Stop();

        if (result.IsGlobalChanged)
        {
            await PublishGlobalAsync(ack, result);
        }

        return result;
    }

    private async Task FlushInternalAsync()
    {
        try
        {
            if (_pendingReceipts.Count > 0)
            {
                // Bulk update DB
                await _Msgrepository.BulkUpdateMessageReceiptsAsync(new List<UpdateMessageReceiptsDto>(_pendingReceipts));
                _pendingReceipts.Clear();
            }

            if (_engine != null)
            {
                await _engine.FlushAsync(_persistentState);
            }
        }
        catch (Exception ex)
        {
            LogError($"Flush failed: {ex.Message}");
        }
    }

    private async Task PublishGlobalAsync(AckReceived ack, AckResult result)
    {
        try
        {
            await _publisher.PublishAsync(new MessageDeliveredAckEvent
            {
                ChatId = ack.ChatId,
                MessageIds = result.NewGlobalMin!,
                Type = ack.Type == AckType.Seen ? "FullSeen" : "FullDelivery",
                DeliveredAt = ack.Timestamp,
                ReceiverId = result.UserId,
                SanderId = ack.SenderId
            });
        }
        catch (Exception ex)
        {
            LogError($"Publish failed: {ex.Message}", ack.ChatId, ack.MessageId);
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    {
        _uptime.Stop();
        _flushTimer?.Dispose();

        await FlushInternalAsync();

        if (_engine != null)
        {
            _engine.Dispose();
        }

        LogDebug($"Deactivating Grain: Processed={_processedCount}, UptimeMs={_uptime.ElapsedMilliseconds}");
    }

    private async Task<HashSet<string>> LoadMembersAsync(string chatId, CancellationToken ct)
    {
        var members = await _memberCache.GetMembersAsync(chatId, ct);

        if (members.Count == 0)
        {
            members = await _repository.GetChatMembersAsync(chatId, ct);
            if (members.Count > 0)
                _memberCache.SetMembers(chatId, members, TimeSpan.FromHours(1));
        }

        return members;
    }

    public ValueTask<GlobalMinResult> GetGlobalMinsAsync()
    {
        var (d, r) = _engine?.GetGlobalMins() ?? (null, null);
        return new ValueTask<GlobalMinResult>(new GlobalMinResult(d, r));
    }

    public ValueTask<bool> IsMessageFullyAckedAsync(string messageId)
    {
        return new ValueTask<bool>(_engine?.IsFullyRead(messageId) ?? false);
    }

    public ValueTask<GrainStats> GetStatsAsync()
    {
        return new ValueTask<GrainStats>(new GrainStats(
            this.GetPrimaryKeyString(),
            _processedCount,
            0, // BatchCount is not explicitly tracked now
            0, // GlobalAckCount is not explicitly tracked now
            _uptime.ElapsedMilliseconds,
            _memberCount,
            _pendingReceipts.Count));
    }


    #region Logging Helpers
    private void LogDebug(string message, string chatId = null, string messageId = null)
    {
        _logger.LogDebug("{ChatId} {MessageId} {Message}", chatId, messageId, message);
    }

    private void LogError(string message, string chatId = null, string messageId = null)
    {
        _logger.LogError("{ChatId} {MessageId} {Message}", chatId, messageId, message);
    }
    #endregion

}
