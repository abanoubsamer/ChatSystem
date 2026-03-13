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
using System.Threading.Channels;

[Reentrant]
public sealed class AckGrain : Grain, IAckGrain
{
    private readonly IPersistentState<ChatAckState> _persistentState;
    private AckEngine? _engine;
    private readonly Channel<AckReceived> _channel;
    private readonly ILogger<AckGrain> _logger;
    private HashSet<string>? _members;
    private int _memberCount;
    private bool _initialized;
    private long _processedCount;
    private long _deliveryGlobalCount;
    private long _readGlobalCount;
    private readonly Stopwatch _uptime = new();
    private readonly IMessagePublisher _publisher;
    private readonly IChatMemberCommandRepository _repository;
    private readonly IMessageReceiptsCommandRepository _Msgrepository;
    private readonly IChatMemberCache _memberCache;
    private const int BATCH_SIZE = 50;           // أصغر لسهولة الاختبار
    private const int BATCH_TIMEOUT_MS = 500;    // Timeout أطول للـ batch

    private readonly CancellationTokenSource _queueCts = new();

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
        _channel = Channel.CreateUnbounded<AckReceived>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
    }

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        _uptime.Start();
        
        var chatId = this.GetPrimaryKeyString();
       
        LogDebug("Activating Grain", chatId);

        _members = await LoadMembersAsync(chatId, ct);
        _memberCount = _members.Count;

        _engine = new AckEngine(_persistentState.State, _memberCount);

        RegisterTimer(
            async _ => await _engine.FlushAsync(_persistentState),
            state: null,
            dueTime: TimeSpan.FromMilliseconds(500),
            period: TimeSpan.FromMilliseconds(500)
        );

        _initialized = true;

        // Start batch processing loop with independent token
        _ = Task.Run(() => ProcessBatchesAsync(_queueCts.Token));

        LogDebug($"Grain activated with {_memberCount} members", chatId);
    }

    public ValueTask<AckResult> ReceiveAsync(AckReceived ack)
    {
        if (!_initialized) throw new InvalidOperationException("Grain not initialized");
        if (ack.ChatId != this.GetPrimaryKeyString()) throw new ArgumentException("ChatId mismatch");

        if (_channel.Writer.TryWrite(ack))
        {
            LogDebug("ACK queued in channel", ack.ChatId, ack.MessageId);
            return new ValueTask<AckResult>(new AckResult(ack.UserId, ack.MessageId, null, null, false, ack.Type));
        }
        else
        {
            LogWarning("Channel busy, processing ACK directly", ack.ChatId, ack.MessageId);
            return new ValueTask<AckResult>(ProcessDirect(ack));
        }
    }

   private async Task ProcessBatchesAsync(CancellationToken ct)
{
    try
    {
        LogPerf("Batch processing loop started");

        await foreach (var batch in _channel.Reader.ReadBatchesAsync(BATCH_SIZE, TimeSpan.FromMilliseconds(BATCH_TIMEOUT_MS), ct))
        {
            var sw = Stopwatch.StartNew();
            LogPerf($"Batch received: Count={batch.Count}");

            // 🚀 Step 1: Dedup + Merge في Dictionary واحد
            var latest = new Dictionary<string, (string MsgId, AckType Type, DateTime Ts)>(batch.Count / 2);
            
            foreach (var ack in batch)
            {
                ProcessDirect(ack);

                if (latest.TryGetValue(ack.UserId, out var curr))
                {
                    var cmp = string.CompareOrdinal(ack.MessageId, curr.MsgId);
                    
                    if (cmp > 0)
                        latest[ack.UserId] = (ack.MessageId, ack.Type, ack.Timestamp);
                    else if (cmp == 0 && ack.Type == AckType.Seen && curr.Type == AckType.Delivery)
                        latest[ack.UserId] = (ack.MessageId, AckType.Seen, ack.Timestamp);
                }
                else
                {
                    latest[ack.UserId] = (ack.MessageId, ack.Type, ack.Timestamp);
                }
            }

            // 🚀 Step 2: Build DTOs
            var receipts = new List<UpdateMessageReceiptsDto>(latest.Count);
            
            foreach (var (userId, (msgId, type, ts)) in latest)
            {
                receipts.Add(new UpdateMessageReceiptsDto
                {
                    UserId = userId,
                    MessageId = msgId,
                    ChatId = this.GetPrimaryKeyString(),
                    Status =  type,
                    DeliveredAt = type == AckType.Delivery ? ts : null,
                    ReadAt = type == AckType.Seen ? ts : null
                });
            }

             if (receipts.Count > 0)
                await _Msgrepository.BulkUpdateMessageReceiptsAsync(receipts);

                sw.Stop();
            
                LogPerf($"Batch processed: Count={batch.Count}, Unique={receipts.Count}, TotalMs={sw.ElapsedMilliseconds}");
        }
    }
    catch (OperationCanceledException)
    {
        LogWarning("Batch processing loop cancelled");
    }
    catch (Exception ex)
    {
        LogError($"Batch processing loop crashed: {ex}");
    }
}
   
    private AckResult ProcessDirect(AckReceived ack)
    {
        var sw = Stopwatch.StartNew();

        AckResult result = ack.Type == AckType.Delivery
                    ? _engine!.UpdateDelivery(ack.UserId, ack.MessageId)
                    : _engine!.UpdateRead(ack.UserId, ack.MessageId);

        
        Interlocked.Increment(ref _processedCount);

        sw.Stop();
        

        if (result.IsGlobalChanged)
        {
            if (ack.Type == AckType.Delivery) Interlocked.Increment(ref _deliveryGlobalCount);
           
            else Interlocked.Increment(ref _readGlobalCount);

            _ = PublishGlobalAsync(ack, result);
        }

        return result;
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
            LogDebug($"GlobalAckEvent published", ack.ChatId, ack.MessageId);
        }
        catch (Exception ex)
        {
            LogError($"Publish failed: {ex.Message}", ack.ChatId, ack.MessageId);
        }
    }
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    {
        _queueCts.Cancel();
        _channel.Writer.Complete();
        _uptime.Stop();

        if (_engine != null)
        {
            await _engine.EmergencyFlushAsync(_persistentState, TimeSpan.FromSeconds(5));
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
        throw new NotImplementedException();
    }

    public ValueTask<bool> IsMessageFullyAckedAsync(string messageId)
    {
        throw new NotImplementedException();
    }

    public ValueTask<GrainStats> GetStatsAsync()
    {
        throw new NotImplementedException();
    }


    #region Logging Helpers
    private void LogDebug(string message, string chatId = null, string messageId = null)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} [DEBUG]{(chatId != null ? $" Chat:{chatId}" : "")}{(messageId != null ? $" Msg:{messageId}" : "")} {message}");
        Console.ResetColor();
    }

    private void LogPerf(string message, string chatId = null, string messageId = null)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} [PERF]{(chatId != null ? $" Chat:{chatId}" : "")}{(messageId != null ? $" Msg:{messageId}" : "")} {message}");
        Console.ResetColor();
    }

    private void LogError(string message, string chatId = null, string messageId = null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} [ERROR]{(chatId != null ? $" Chat:{chatId}" : "")}{(messageId != null ? $" Msg:{messageId}" : "")} {message}");
        Console.ResetColor();
    }

    private void LogWarning(string message, string chatId = null, string messageId = null)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} [WARN]{(chatId != null ? $" Chat:{chatId}" : "")}{(messageId != null ? $" Msg:{messageId}" : "")} {message}");
        Console.ResetColor();
    }
    #endregion

}