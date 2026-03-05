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
    public class MediaStateChangedConsumer : IConsumer<MediaStateChangedEvent>
    {
        private readonly ICallService _callService;
        private readonly ILogger<MediaStateChangedConsumer> _logger;

        public MediaStateChangedConsumer(ICallService callService, ILogger<MediaStateChangedConsumer> logger)
        {
            _callService = callService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<MediaStateChangedEvent> context)
        {
            var evt = context.Message;

                 _logger.LogDebug(
                "Processing MediaStateChanged: {UserId} in {SessionId}, Muted: {IsMuted}",
                evt.UserId, evt.SessionId, evt.IsMuted);

            try
            {
                await _callService.UpdateMediaStateAsync(
                    evt.SessionId,
                    evt.UserId,
                    evt.IsMuted,
                    evt.IsVideoOn,
                    evt.IsScreenSharing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to update media state for {UserId}",
                    evt.UserId);
                // Media state مش critical، ممكن نسكت على الخطأ
                // throw; // Uncomment لو عايز retry
            }
        }
    }
}
