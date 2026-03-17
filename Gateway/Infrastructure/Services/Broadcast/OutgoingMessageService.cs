using Application.Abstractions.Broadcast;
using Application.Abstractions.Broadcast.Abstraction;
using Application.Dtos.Message;
using Application.Messaging;
using Application.Serialization;
using Microsoft.Extensions.Logging;

public sealed class OutgoingMessageService : IOutgoingMessageService
{
    private readonly IFanOutResolverManager _fanOutResolver;
    private readonly ILogger<OutgoingMessageService> _logger;

    public OutgoingMessageService(
        IFanOutResolverManager fanOutResolver,
        ILogger<OutgoingMessageService> logger)
    {
        _fanOutResolver = fanOutResolver;
        _logger = logger;
    }

    public async Task SendToUserAsync(
        string userId, OutgoingMessage message, CancellationToken ct = default)
    {
        var contexts = new List<MessageContext>(4);
        await _fanOutResolver.ResolveUserContextsAsync(userId, contexts, ct);

        if (contexts.Count == 0)
        {
            _logger.LogDebug("User {UserId} has no active connections", userId);
            return;
        }

        var data = MessageSerializer.Serialize(message);

        foreach (var ctx in contexts)
            await ctx.SendRawAsync(data, FrameType.Message, ct);

        _logger.LogDebug("Sent to user | userId={UserId} | contexts={Count}",
            userId, contexts.Count);
    }

    public async Task SendToRoomAsync(
        string roomId, OutgoingMessage message, CancellationToken ct = default)
    {
        var contexts = new List<MessageContext>(8);
        await _fanOutResolver.ResolveGroupContextsAsync(roomId, contexts, ct: ct);

        if (contexts.Count == 0)
        {
            _logger.LogDebug("Room {RoomId} has no active connections", roomId);
            return;
        }

        var data = MessageSerializer.Serialize(message);

        foreach (var ctx in contexts)
            await ctx.SendRawAsync(data, FrameType.Message, ct);

        _logger.LogDebug("Sent to room | roomId={RoomId} | contexts={Count}",
            roomId, contexts.Count);
    }

    public async Task SendToRoomAsync(
        string excludeUserId, string roomId,
        OutgoingMessage message, CancellationToken ct = default)
    {
        var contexts = new List<MessageContext>(8);
        await _fanOutResolver.ResolveGroupContextsAsync(roomId, contexts, excludeUserId, ct);

        if (contexts.Count == 0)
        {
            _logger.LogDebug("Room {RoomId} has no active connections (excluding {UserId})",
                roomId, excludeUserId);
            return;
        }

        var data = MessageSerializer.Serialize(message);

        foreach (var ctx in contexts)
            await ctx.SendRawAsync(data, FrameType.Message, ct);
    }

    public async Task SendToUsersAsync(
        IEnumerable<string> userIds, OutgoingMessage message, CancellationToken ct = default)
    {
        var contexts = new List<MessageContext>(8);
        await _fanOutResolver.ResolveUsersContextsAsync(userIds, contexts, ct);

        if (contexts.Count == 0)
        {
            _logger.LogDebug("No active connections for the specified users");
            return;
        }

        var data = MessageSerializer.Serialize(message);

        foreach (var ctx in contexts)
            await ctx.SendRawAsync(data, FrameType.Message, ct);

        _logger.LogDebug("Sent to users | contexts={Count}", contexts.Count);
    }
}