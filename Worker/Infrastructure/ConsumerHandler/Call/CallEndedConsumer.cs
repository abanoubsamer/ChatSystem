using Application.Abstractions.Services.Call;
using Contracts.Call.Event;
using Contracts.Call.Signals;
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
    public class CallEndedConsumer : IConsumer<CallEndedEvent>
    {
        private readonly ICallService _callService;
        private readonly ILogger<CallEndedConsumer> _logger;

        public CallEndedConsumer(ICallService callService, ILogger<CallEndedConsumer> logger)
        {
            _callService = callService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CallEndedEvent> context)
        {
            var evt = context.Message;

            _logger.LogInformation(
                "Processing CallEnded: {SessionId} , Reason: {Reason}",
                evt.SessionId,  evt.Reason);
            try
            {
                await _callService.EndSessionAsync(
                    evt.SessionId,
                    evt.Reason);

                _logger.LogInformation(
                    "Session {SessionId} ended successfully",
                    evt.SessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to end session {SessionId}",
                    evt.SessionId);
                throw;
            }
        }

        public Task Consume(ConsumeContext<EndCallSignal> context)
        {
            throw new NotImplementedException();
        }
    }
}
