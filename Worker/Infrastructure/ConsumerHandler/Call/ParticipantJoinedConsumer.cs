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
    public class ParticipantJoinedConsumer : IConsumer<ParticipantJoinedEvent>
    {
        private readonly ICallService _callService;
        private readonly ILogger<ParticipantJoinedConsumer> _logger;

        public ParticipantJoinedConsumer(ICallService callService, ILogger<ParticipantJoinedConsumer> logger)
        {
            _callService = callService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ParticipantJoinedEvent> context)
        {
            var evt = context.Message;

            _logger.LogInformation(
                "Processing ParticipantJoined: {UserId} in {SessionId}",
                evt.UserId, evt.SessionId);

            try
            {
                await _callService.JoinSessionAsync(evt.SessionId, evt.UserId);

                _logger.LogInformation(
                    "User {UserId} joined session {SessionId}",
                    evt.UserId, evt.SessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process join for {UserId} in {SessionId}",
                    evt.UserId, evt.SessionId);
                throw;
            }
        }
    }
}
