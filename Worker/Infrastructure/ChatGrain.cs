using Application.Abstractions.Grain;
using Application.Dtos.Ack;
using Contracts.Message.Events;
using Domain.Models.State;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class ChatGrain : Grain, IChatGrain
    {
        private readonly IPersistentState<ChatGrainState> _state;
        private readonly IPublishEndpoint _publisher;
        private IDisposable? _saveTimer;

        public ChatGrain(
            [PersistentState("chat", "MongoStore")] IPersistentState<ChatGrainState> state,
            IPublishEndpoint publisher)
        {
            _state = state;
            _publisher = publisher;
        }

        // ─── لما الـ Grain يقوم ────────────────────────────────────────
        public override Task OnActivateAsync(CancellationToken ct)
        {
            // احفظ في DB كل 30 ثانية
            _saveTimer = RegisterTimer(
                _ => _state.WriteStateAsync(),
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30));

            return base.OnActivateAsync(ct);
        }

        // ─── لما message تتبعت ────────────────────────────────────────
        // ─── Grain ────────────────────────────────────────────────────────
        public Task MessageSentAsync(string msgId, int totalReceivers)
        {
            var bitmapSize = (totalReceivers / 8) + 1;

            // ─── حدث الـ TotalMembers دايما ──────────────────────────
            _state.State.TotalMembers = totalReceivers;

            _state.State.PendingDelivery[msgId] = totalReceivers;
            _state.State.PendingSeen[msgId] = totalReceivers;
            _state.State.DeliveryBitmaps[msgId] = new byte[bitmapSize];
            _state.State.SeenBitmaps[msgId] = new byte[bitmapSize];

            return Task.CompletedTask;
        }

        // ─── لما member يعمل ack ──────────────────────────────────────
        public async Task ReceiveAckAsync(string memberId, string msgId, AckType type)
        {
            if (!_state.State.MemberIndex.TryGetValue(memberId, out var index))
            {
                index = _state.State.NextIndex++;
                _state.State.MemberIndex[memberId] = index;
            }

            var pending = type == AckType.Delivery
                ? _state.State.PendingDelivery
                : _state.State.PendingSeen;

            var bitmaps = type == AckType.Delivery
                ? _state.State.DeliveryBitmaps
                : _state.State.SeenBitmaps;

            // ─── جيب كل الـ messages اللي أصغر من أو تساوي الـ msgId ──
            var msgsToAck = pending.Keys
                .Where(m => string.Compare(m, msgId) <= 0)
                .ToList();

            foreach (var msg in msgsToAck)
            {
                var bitmap = bitmaps[msg];
                var byteIndex = index / 8;
                var bitIndex = index % 8;

                if (byteIndex >= bitmap.Length)
                {
                    var newBitmap = new byte[byteIndex + 1];
                    bitmap.CopyTo(newBitmap, 0);
                    bitmaps[msg] = newBitmap;
                    bitmap = newBitmap;
                }

                if ((bitmap[byteIndex] & (1 << bitIndex)) != 0)
                    continue; // duplicate

                bitmap[byteIndex] |= (byte)(1 << bitIndex);
                pending[msg]--;

                if (pending[msg] <= 0)
                {
                    pending.Remove(msg);
                    bitmaps.Remove(msg);

                    if (type == AckType.Delivery)
                        _state.State.MinDelivery = msg;
                    else
                        _state.State.MinSeen = msg;

                    await _state.WriteStateAsync();

                    await _publisher.Publish(new MessageDeliveredAckEvent
                    {
                        ChatId = this.GetPrimaryKeyString(),
                        MessageIds = msg,
                        ReceiverId = memberId,
                        DeliveredAt = DateTime.UtcNow,
                        Type = type == AckType.Delivery ? "Delivery" : "Seen"
                    });
                }
            }
        }

        // ─── لما member يدخل ──────────────────────────────────────────
        public async Task MemberJoinedAsync(string memberId)
        {
            if (_state.State.MemberIndex.ContainsKey(memberId)) return;

            _state.State.MemberIndex[memberId] = _state.State.NextIndex++;
            _state.State.TotalMembers++;
            await _state.WriteStateAsync();
        }

        // ─── لما member يخرج ──────────────────────────────────────────
        public async Task MemberLeftAsync(string memberId)
        {
            _state.State.MemberIndex.Remove(memberId);
            _state.State.TotalMembers--;
            await _state.WriteStateAsync();
        }

        // ─── لما الـ Grain ينام ───────────────────────────────────────
        public override async Task OnDeactivateAsync(
            DeactivationReason reason, CancellationToken ct)
        {
            await _state.WriteStateAsync();
            await base.OnDeactivateAsync(reason, ct);
        }
    }
}
