using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore.Grains;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Dtos.Call;
using Application.Dtos.Message;
using Application.Messaging;
using Contracts.Call.Event;
using Contracts.Call.Signals;
using Microsoft.Extensions.Logging;

namespace Application.Handlers.Call
{
    public sealed class OfferMethodHandler : BaseMethodHandler<OfferSignal>
    {
        public override string MethodName => "offer";

        private readonly IOutgoingMessageService _outgoingMessage;
        private readonly IMessagePublisher _publisher;
        private readonly IGrainFactory _grainFactory;
        private readonly ILogger<OfferMethodHandler> _logger;

        public OfferMethodHandler(
            IOutgoingMessageService outgoingMessage,
            IMessagePublisher publisher,
            IGrainFactory grainFactory,
            ILogger<OfferMethodHandler> logger)
        {
            _outgoingMessage = outgoingMessage;
            _publisher = publisher;
            _grainFactory = grainFactory;
            _logger = logger;
        }

        protected override async Task HandleAsync(
            MessageContext context,
            OfferSignal request,
            CancellationToken ct = default)
        {
            // ── Step 1: Validate ──────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(request.TargetUserId))
            {
                await context.SendErrorAsync(
                    Guid.NewGuid().ToString("N"),
                    "INVALID_REQUEST",
                    "TargetUserId is required",
                    ct: ct);
                return;
            }

            if (request.TargetUserId == context.UserId)
            {
                await context.SendErrorAsync(
                    Guid.NewGuid().ToString("N"),
                    "INVALID_REQUEST",
                    "Cannot call yourself",
                    ct: ct);
                return;
            }

            var sessionId = Guid.NewGuid().ToString();

            // ── Step 2: Create Session Grain ──────────────────────────────────────
            var sessionGrain = _grainFactory.GetGrain<ICallSessionGrain>(sessionId);

            var created = await sessionGrain.CreateAsync(new SessionCallInfo
            {
                SessionId = sessionId,
                Type = SessionType.Direct,
                CreatorId = context.UserId,
                CreatedAt = DateTime.UtcNow,
                ChatId = request.ChatId,
                Participants = new List<string> { context.UserId }
            });

            // ✅ Grain create فشلت — session موجودة بالفعل
            if (!created)
            {
                _logger.LogWarning(
                    "Session {SessionId} already exists — concurrent offer? | userId={UserId}",
                    sessionId, context.UserId);

                await context.SendErrorAsync(
                    Guid.NewGuid().ToString("N"),
                    "SESSION_EXISTS",
                    "A call session already exists",
                    ct: ct);
                return;
            }

            // ── Step 3: Publish Event ─────────────────────────────────────────────
            // ✅ await مش fire-and-forget — لو فشل نعمل rollback
            try
            {
                await _publisher.PublishAsync(new SessionCreatedEvent
                {
                    SessionId = sessionId,
                    CreatorId = context.UserId,
                    Type = "direct",
                    TargetUserId = request.TargetUserId,
                    ChatId = request.ChatId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish SessionCreatedEvent | sessionId={SessionId} | userId={UserId}",
                    sessionId, context.UserId);

                // ✅ Rollback — نوقف الـ session اللي اتعملت
                await RollbackSessionAsync(sessionGrain, sessionId);

                await context.SendErrorAsync(
                    Guid.NewGuid().ToString("N"),
                    "SERVICE_UNAVAILABLE",
                    "Call service is temporarily unavailable. Please try again.",
                    ct: ct);
                return;
            }

            // ── Step 4: Forward Offer to Target ───────────────────────────────────
            // ✅ await — لو SendToUserAsync فشلت نعرف
            try
            {
                await _outgoingMessage.SendToUserAsync(
                    request.TargetUserId,
                    new OutgoingMessage(
                        request.TargetUserId,
                        new
                        {
                            SessionId = sessionId,
                            SenderId = context.UserId,
                            Sdp = request.Sdp
                        },
                        "offer"),
                    ct);
            }
            catch (Exception ex)
            {
                // ✅ Target مش online — مش error حقيقي
                // الـ session شغالة والـ ring timer هيخليها تـexpire لو مش رد
                _logger.LogWarning(ex,
                    "Could not deliver offer to target | targetUserId={TargetUserId} | sessionId={SessionId}",
                    request.TargetUserId, sessionId);
            }

            // ── Step 5: Confirm to Caller ─────────────────────────────────────────
            // ✅ نبلّغ الـ caller إن الـ offer اتبعت
            await context.SendResponseAsync(
                Guid.NewGuid().ToString("N"),
                "offer_sent",
                data: null,
                ct: ct);

            _logger.LogInformation(
                "Offer sent | sessionId={SessionId} | callerId={CallerId} | targetId={TargetId}",
                sessionId, context.UserId, request.TargetUserId);
        }

        // ─── Rollback ─────────────────────────────────────────────────────────────

        /// <summary>
        /// لو PublishAsync فشلت — نوقف الـ grain علشان ما يفضلش
        /// session معلّقة بدون backend يعرف عنها.
        /// </summary>
        private async Task RollbackSessionAsync(
            ICallSessionGrain grain,
            string sessionId)
        {
            try
            {
                await grain.EndAsync("publish_failed");

                _logger.LogInformation(
                    "Session {SessionId} rolled back successfully",
                    sessionId);
            }
            catch (Exception ex)
            {
                // ✅ Rollback نفسه فشل — نـlog بس ومنبلّغش الـ client تاني
                _logger.LogError(ex,
                    "Rollback failed for session {SessionId} — grain may be orphaned",
                    sessionId);
            }
        }
    }
}