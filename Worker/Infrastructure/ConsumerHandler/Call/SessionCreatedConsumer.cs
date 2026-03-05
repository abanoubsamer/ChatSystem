using Application.Abstractions.Services.Call;
using Contracts.Call.Event;
using Infrastructure.Services.Call;
using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ConsumerHandler.Call
{
    public class SessionCreatedConsumer : IConsumer<SessionCreatedEvent>
    {
        private readonly ICallService _callService;
        private readonly ILogger<SessionCreatedConsumer> _logger;

        public SessionCreatedConsumer(ICallService callService, ILogger<SessionCreatedConsumer> logger)
        {
            _callService = callService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<SessionCreatedEvent> context)
        {
            var evt = context.Message;

            _logger.LogInformation(
                "Processing SessionCreated: {SessionId} by {CreatorId}",
                evt.SessionId, evt.CreatorId);

            try
            {
                await _callService.CreateSessionAsync(
                    evt.SessionId,
                    evt.CreatorId,
                    evt.Type,
                    evt.TargetUserId,
                    evt.ChatId);

                _logger.LogInformation(
                    "Session {SessionId} created successfully",
                    evt.SessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to create session {SessionId}",
                    evt.SessionId);
                throw; // MassTransit هيعيد المحاولة (retry)
            }
        }
    }
}
