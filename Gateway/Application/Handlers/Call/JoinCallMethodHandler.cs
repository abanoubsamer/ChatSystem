using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore;
using Application.Abstractions.Connection;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Abstractions.Session;
using Contracts.Call.Event;
using Contracts.Call.Session;
using Contracts.Call.Signals;
using System.Net.WebSockets;

namespace Application.Handlers.Call
{
    public class JoinCallMethodHandler : BaseMethodHandler<JoinGroupSignal>
    {
        public override string MethodName => "join_call";

        private readonly IBroadcastServices _broadcastServices;
        private readonly IConnectionServices _connectionServices;
        private readonly ICallSessionStore _sessionStore;
        private readonly IMessagePublisher _publisher;
        private readonly IRingTimeoutService _ringTimeout;

        public JoinCallMethodHandler(
            IBroadcastServices broadcastServices,
            ICallSessionStore sessionStore,
            IConnectionServices connectionServices,
            IMessagePublisher publisher,
            IRingTimeoutService ringTimeout)
        {
            _sessionStore = sessionStore;
            _broadcastServices = broadcastServices;
            _publisher = publisher;
            _connectionServices = connectionServices;
            _ringTimeout = ringTimeout;
        }

        protected override async Task HandleAsync(string userId, JoinGroupSignal request, WebSocket socket)
        {
            var session = await _sessionStore.GetAsync(request.SessionId);
          
            // ── Guard: session must exist ────────────────────────────────
            if (session == null)
            {
                await _broadcastServices.SendMessageToUserAsync(userId, new
                {
                    Method = "join_failed",
                    Params = new
                    {
                        SessionId = request.SessionId,
                        Reason = "session_not_found",
                        Message = "This call no longer exists."
                    }
                });
                return;
            }

            // ── Add user to WebSocket group ──────────────────────────────
            _connectionServices.AddUserToGroup(userId, request.SessionId);

            if (!session.Participants.Contains(userId))
            {
                session.Participants.Add(userId);
                await _sessionStore.SetAsync(session.SessionId, session);
            }

            // ══════════════════════════════════════════════════════════════
            // ✅ FEATURE 2: Cancel ring timer — someone answered!
            // ══════════════════════════════════════════════════════════════
            _ringTimeout.CancelRingTimer(request.SessionId);

            // ── First joiner: notify creator the call was answered ────────
            if (session.Participants.Count == 2)
            {
                await _broadcastServices.SendMessageToUserAsync(session.CreatorId, new
                {
                    Method = "call_answered",           // renamed from group_call_created for clarity
                    Params = new
                    {
                        SessionId = request.SessionId,
                        FirstJoinerId = userId
                    }
                });
            }

            // ── Publish participant joined event ─────────────────────────
            await _publisher.PublishAsync(new ParticipantJoinedEvent
            {
                SessionId = request.SessionId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            });

            // ── Notify everyone else in the group ────────────────────────
            var existingMembers = _connectionServices.GetUsersInGroup(request.SessionId)
                                                     .Where(m => m != userId)
                                                     .ToList();

            await _broadcastServices.SendMessageToGroupAsync(userId, request.SessionId, new
            {
                Method = "user_joined_call",
                Params = new
                {
                    SessionId = request.SessionId,
                    UserId = userId,
                    ExistingMembers = existingMembers
                }
            });
        }
    }
}