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
    public class ParticipantLeftConsumer : IConsumer<ParticipantLeftEvent>
    {
        private readonly ICallService _callService;
        private readonly ILogger<ParticipantLeftConsumer> _logger;

        public ParticipantLeftConsumer(ICallService callService, ILogger<ParticipantLeftConsumer> logger)
        {
            _callService = callService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ParticipantLeftEvent> context)
        {
            var evt = context.Message;

            _logger.LogInformation(
                "Processing ParticipantLeft: {UserId} from {SessionId}, Reason: {Reason}",
                evt.UserId, evt.SessionId, evt.Reason);

            try
            {
                await _callService.LeaveSessionAsync(
                    evt.SessionId,
                    evt.UserId,
                    evt.Reason);

                _logger.LogInformation(
                    "User {UserId} left session {SessionId}",
                    evt.UserId, evt.SessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process leave for {UserId} from {SessionId}",
                    evt.UserId, evt.SessionId);
                throw;
            }
        }
    }
}
