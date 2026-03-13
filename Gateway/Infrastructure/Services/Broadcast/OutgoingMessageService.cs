using Application.Abstractions.Broadcast;
using Application.Abstractions.Broadcast.Abstraction;
using Application.Abstractions.Connection.Abstraction;
using Application.Dtos.Message;
using Application.Messaging;
using Application.Serialization;
using Domain.Models;
using Infrastructure.Extension;
using Infrastructure.Services.Broadcast.Implementation;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Servers;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.RegularExpressions;

namespace Infrastructure.Services.Broadcast
{
    public sealed class OutgoingMessageService : IOutgoingMessageService
    {
        private readonly IBroadcastManager _broadcastManager;
        private readonly IFanOutResolverManager _fanOutResolver;
        private readonly ILogger<OutgoingMessageService> _logger;

  
        [ThreadStatic]
        private static List<MessageContext>? _contextBuffer;
        public OutgoingMessageService(
            IBroadcastManager broadcastManager,
            IFanOutResolverManager fanOutResolver,
            ILogger<OutgoingMessageService> logger)
        {
            _broadcastManager = broadcastManager;
            _fanOutResolver = fanOutResolver;
            _logger = logger;
        }

        public async Task SendToUserAsync(
           string userId,
           OutgoingMessage message,
           CancellationToken ct = default)
        {
            var contexts = GetBuffer();

            try
            {
                await _fanOutResolver.ResolveUserContextsAsync(userId, contexts, ct);

                if (contexts.Count == 0)
                {
                    _logger.LogDebug("User {UserId} has no active connections", userId);
                    return;
                }

                var data = MessageSerializer.Serialize(message);
                await _broadcastManager.BroadcastAsync(contexts, data, ct);

                _logger.LogDebug(
                    "Sent to user | userId={UserId} | contexts={Count}",
                    userId, contexts.Count);
            }
            finally
            {
                contexts.Clear();
            }
        }

        public async Task SendToRoomAsync(
            string roomId,
            OutgoingMessage message,
            CancellationToken ct = default)
        {
            var contexts = GetBuffer();

            try
            {
                await _fanOutResolver.ResolveGroupContextsAsync(roomId, contexts, ct: ct);

                if (contexts.Count == 0)
                {
                    _logger.LogDebug("Room {RoomId} has no active connections", roomId);
                    return;
                }

                var data = MessageSerializer.Serialize(message);
                await _broadcastManager.BroadcastAsync(contexts, data, ct);

                _logger.LogDebug(
                    "Sent to room | roomId={RoomId} | contexts={Count}",
                    roomId, contexts.Count);
            }
            finally
            {
                contexts.Clear();
            }
        }

        public async Task SendToRoomAsync(
            string excludeUserId,
            string roomId,
            OutgoingMessage message,
            CancellationToken ct = default)
        {
            var contexts = GetBuffer();

            try
            {
                await _fanOutResolver.ResolveGroupContextsAsync(roomId, contexts, excludeUserId, ct);

                if (contexts.Count == 0)
                {
                    _logger.LogDebug(
                        "Room {RoomId} has no active connections (excluding {UserId})",
                        roomId, excludeUserId);
                    return;
                }

                var data = MessageSerializer.Serialize(message);
                await _broadcastManager.BroadcastAsync(contexts, data, ct);
            }
            finally
            {
                contexts.Clear();
            }
        }

        public async Task SendToUsersAsync(
            IEnumerable<string> userIds,
            OutgoingMessage message,
            CancellationToken ct = default)
        {
            var contexts = GetBuffer();

            try
            {
                await _fanOutResolver.ResolveUsersContextsAsync(userIds, contexts, ct);

                if (contexts.Count == 0)
                {
                    _logger.LogDebug("No active connections for the specified users");
                    return;
                }

                var data = MessageSerializer.Serialize(message);
                await _broadcastManager.BroadcastAsync(contexts, data, ct);

                _logger.LogDebug(
                    "Sent to users | contexts={Count}",
                    contexts.Count);
            }
            finally
            {
                contexts.Clear();
            }
        }

        // ─── Private ──────────────────────────────────────────────

        private static List<MessageContext> GetBuffer()
            => _contextBuffer ??= new List<MessageContext>(32);
    }
}
