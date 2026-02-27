using Application.Abstractions.Services.Ack;
using Application.Abstractions.Services.Member;
using Application.Abstractions.Services.MessageReceipts;
using Application.Abstractions.Services.Watermark;
using Application.Dtos.Ack;
using Domain.Models.State;
using MassTransit;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Ack
{
    public class AckBatchProcessor
    {
        private readonly IPersistentState<ChatWatermarkState> _state;
        private readonly IPublishEndpoint _publisher;
        private readonly IMemeberServices _memberServices;
        private readonly IMessageReceiptsServices _msgReceServices;
        private readonly IWatermarkServices _watermarkServices;
        private readonly IAckServices _ackServices;
        private readonly ILogger _logger;

        public AckBatchProcessor(
            IPersistentState<ChatWatermarkState> state,
            IMemeberServices memberServices,
            IMessageReceiptsServices msgReceServices,
            IWatermarkServices watermarkServices,
            IPublishEndpoint publisher,
            IAckServices ackServices,
            ILogger logger)
        {
            _state = state;
            _memberServices = memberServices;
            _msgReceServices = msgReceServices;
            _watermarkServices = watermarkServices;
            _publisher = publisher;
            _ackServices = ackServices;
            _logger = logger;
        }

        public async Task ProcessAsync(List<Acked> batch, ObjectId chatId)
        {
            if (batch.Count == 0) return;

            var swTotal = Stopwatch.StartNew();
            try
            {
                // Collapse + Filter
                var collapsed = _ackServices.CollapseAcks(batch);
                var changed = _ackServices.FilterChanged(collapsed, _state.State);

                if (!changed.Any()) return;

                // DB writes
                await Task.WhenAll(
                    _msgReceServices.UpdateMessageReceiptsAsync(changed),
                    _memberServices.UpdateChatMembersAsync(changed)
                );

                // Watermarks + State
                var events = await _watermarkServices.UpdateGlobalWatermarks(
                    _state.State, chatId, changed);

                await _state.WriteStateAsync();

                // Publish events
                await Task.WhenAll(events.Select(evt => _publisher.Publish(evt)));

                swTotal.Stop();
                _logger.LogInformation(
                    "ChatId={ChatId} Batch={Total} Changed={Changed} Time={Time}ms",
                    chatId, batch.Count, changed.Count, swTotal.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "ChatId={ChatId} Failed to process batch of {Count}",
                    chatId, batch.Count);
                throw;
            }
        }
    }
}
