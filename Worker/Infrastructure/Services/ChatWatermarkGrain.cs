using Application.Abstractions.Grain;
using Application.Abstractions.Repositories.Chat;
using Application.Abstractions.Repositories.ChatMember;
using Application.Abstractions.Repositories.MessageReceipts;
using Application.Abstractions.Services.Ack;
using Application.Abstractions.Services.Chat;
using Application.Abstractions.Services.Member;
using Application.Abstractions.Services.MessageReceipts;
using Application.Abstractions.Services.Watermark;
using Application.Dtos.Ack;
using Application.Dtos.ChatMember.Command;
using Application.Dtos.MessageReceipts.Command;
using Contracts.Enums;
using Contracts.Message.Events;
using Domain.Models.State;
using Infrastructure.Services.Ack;
using Infrastructure.Services.Queue;
using MassTransit;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ChatWatermarkGrain : Grain, IChatWatermarkGrain
    {
        private AckBatchQueue _queue = null!;
        private AckBatchProcessor _processor = null!;

        private readonly IPersistentState<ChatWatermarkState> _state;
        private readonly IPublishEndpoint _publisher;
        private readonly IMemeberServices _memberServices;
        private readonly IMessageReceiptsServices _msgReceServices;
        private readonly IWatermarkServices _watermarkServices;
        private readonly IAckServices _ackServices;
        private readonly ILogger<ChatWatermarkGrain> _logger;

        public ChatWatermarkGrain(
            [PersistentState("watermark", "WatermarkStore")] IPersistentState<ChatWatermarkState> state,
            IMemeberServices memberServices,
            IMessageReceiptsServices msgReceServices,
            IWatermarkServices watermarkServices,
            IPublishEndpoint publisher,
            IAckServices ackServices,
            ILogger<ChatWatermarkGrain> logger)
        {
            _state = state;
            _memberServices = memberServices;
            _msgReceServices = msgReceServices;
            _watermarkServices = watermarkServices;
            _publisher = publisher;
            _ackServices = ackServices;
            _logger = logger;
        }

        public override async Task OnActivateAsync(CancellationToken ct)
        {
            var chatId = ObjectId.Parse(this.GetPrimaryKeyString());

            // Orleans بيعمل load تلقائي، بس لو فاضي حمّل من DB
            if (_state.State.DeliveryWatermarks == null || !_state.State.DeliveryWatermarks.Any())
            {
                var watermarks = await _watermarkServices.LoadWatermarksAsync(chatId);
                if (watermarks != null)
                {
                    _state.State.DeliveryWatermarks = watermarks.DeliveryWatermarks ?? new();
                    _state.State.SeenWatermarks = watermarks.SeenWatermarks ?? new();
                }
            }

            _processor = new AckBatchProcessor(
                _state, _memberServices, _msgReceServices,
                _watermarkServices, _publisher, _ackServices, _logger);

            _queue = new AckBatchQueue(
                RegisterTimer,
                batch => _processor.ProcessAsync(batch, chatId),
                _logger);
        }

        // ✅ Entry point وحيد من البره
        public Task ReceiveAckAsync(Acked ack)
            => _queue.EnqueueAsync(ack);

        public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
        {
            try
            {
                await _queue.FlushAndDisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChatId={ChatId} Failed to flush queue on deactivation",
                    this.GetPrimaryKeyString());
                // مش بنـ rethrow لأن الـ deactivation لازم يكمل
            }
        }
    }

}
