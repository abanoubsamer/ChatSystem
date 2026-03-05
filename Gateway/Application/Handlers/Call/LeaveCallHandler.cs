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
    public class LeaveCallHandler : BaseMethodHandler<LeaveCallSignal>
    {
        public override string MethodName => "leave_call";

        private readonly IBroadcastServices _broadcastServices;
        private readonly IConnectionServices _connectionServices;
        private readonly IMessagePublisher _eventPublisher;
        private readonly ICallSessionStore _sessionStore;
        private readonly IRingTimeoutService _ringTimeout;

        public LeaveCallHandler(
            IBroadcastServices broadcastServices,
            ICallSessionStore sessionStore,
            IConnectionServices connectionServices,
            IMessagePublisher eventPublisher,
            IRingTimeoutService ringTimeout)
        {
            _sessionStore = sessionStore;
            _broadcastServices = broadcastServices;
            _connectionServices = connectionServices;
            _eventPublisher = eventPublisher;
            _ringTimeout = ringTimeout;
        }

        protected override async Task HandleAsync(string userId, LeaveCallSignal request, WebSocket socket)
        {
            var session = await _sessionStore.GetAsync(request.SessionId);

            if (session == null) return;

            // Remove user from WebSocket group
            _connectionServices.RemoveUserFromGroup(userId, request.SessionId);

            var remaining = _connectionServices.GetGroupCount(request.SessionId);

            if (session.Type == SessionType.Direct)
            {
               
                await EndSessionAsync(session, "peer_left");
            }
            else
            {
                if (remaining == 0)
                {
                    await EndSessionAsync(session, "last_left");
                }
                else
                {
                    await _eventPublisher.PublishAsync(new ParticipantLeftEvent
                    {
                        SessionId = request.SessionId,
                        UserId = userId,
                        RemainingCount = remaining
                    });

                    await _broadcastServices.SendMessageToGroupAsync(userId, request.SessionId, new
                    {
                        Method = "user_left",
                        UserId = userId,
                        Remaining = remaining
                    });
                }
            }
        }

        private async Task EndSessionAsync(SessionCallInfo session, string reason)
        {
            // ✅ Cancel ring timer if still active (creator left before anyone joined)
            _ringTimeout.CancelRingTimer(session.SessionId);

            // Remove session from store
            await _sessionStore.RemoveAsync(session.SessionId);

            // ✅ Remove ChatId → SessionId index so new calls can be created
            if (!string.IsNullOrEmpty(session.ChatId))
            {
                await _sessionStore.RemoveActiveChatSessionAsync(session.ChatId);
            }

            // Publish call ended event
            await _eventPublisher.PublishAsync(new CallEndedEvent
            {
                SessionId = session.SessionId,
                Timestamp = DateTime.UtcNow,
                Reason = reason
            });

            // Notify all remaining participants
            await _broadcastServices.SendMessageToGroupAsync(null, session.SessionId, new
            {
                Method = "call_ended",
                Reason = reason,
                SessionId = session.SessionId
            });

            // Cleanup WebSocket group
            _connectionServices.RemoveGroup(session.SessionId);
        }
    }
}