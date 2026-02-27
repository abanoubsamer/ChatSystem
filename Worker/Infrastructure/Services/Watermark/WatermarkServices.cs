using Application.Abstractions.Repositories.Chat;
using Application.Abstractions.Repositories.ChatMember;
using Application.Abstractions.Services.Watermark;
using Application.Dtos.Ack;
using Contracts.Message.Events;
using Domain.Models.State;
using MongoDB.Bson;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Watermark
{
    public class WatermarkServices: IWatermarkServices
    {
        private readonly IChatQueriesRepository _repo;
        private readonly IChatMemberCommandRepository _memberRepo;
        public WatermarkServices(IChatMemberCommandRepository memberRepo,IChatQueriesRepository repo)
        {
            _memberRepo = memberRepo;
            _repo = repo;
        }
        public async Task<ChatWatermarkState?> LoadWatermarksAsync(ObjectId chatId)
        {
            var members = await _memberRepo.GetChatMembersWatermarksAsync(chatId);
            if (members == null || !members.Any()) return null;

            var state = new ChatWatermarkState
            {
                DeliveryWatermarks = new Dictionary<string, string>(),
                SeenWatermarks = new Dictionary<string, string>()
            };

            foreach (var member in members)
            {
                if (!string.IsNullOrEmpty(member.LastDeliveredMsgId))
                    state.DeliveryWatermarks[member.UserId] = member.LastDeliveredMsgId;

                if (!string.IsNullOrEmpty(member.LastSeenMsgId))
                    state.SeenWatermarks[member.UserId] = member.LastSeenMsgId;
            }

            return state;
        }
        public async Task<List<MessageDeliveredAckEvent>> UpdateGlobalWatermarks(ChatWatermarkState state, ObjectId chatId, List<Acked> changed)
        {
            var events = new List<MessageDeliveredAckEvent>();

            foreach (var ack in changed)
            {



                var watermarks = ack.AckType == AckType.Delivery ? state.DeliveryWatermarks : state.SeenWatermarks;
                var currentMin = ack.AckType == AckType.Delivery ? state.MinLastMsgIdDelivery : state.MinLastMsgIdSeen;
                var currentOwner = ack.AckType == AckType.Delivery ? state.MinDeliveryOwnerId : state.MinSeenOwnerId;
               // events.Add(CreateAckEvent(chatId.ToString(), currentMin, ack.Timestamp, ack.LastMsgId, ack.AckType));
                // First time
                if (currentOwner == string.Empty)
                {
                    if (ack.AckType == AckType.Delivery)
                    {
                        state.MinLastMsgIdDelivery = ack.LastMsgId;
                        state.MinDeliveryOwnerId = ack.ReceiverId.ToString();
                    }
                    else
                    {
                        state.MinLastMsgIdSeen = ack.LastMsgId;
                        state.MinSeenOwnerId = ack.ReceiverId.ToString();
                    }

                    events.Add(CreateAckEvent(chatId.ToString(), ack.LastMsgId, ack.Timestamp, "GLOBAL", ack.AckType, true));
                    continue;
                }


                if (ack.ReceiverId.ToString() != currentOwner && currentOwner != ack.SanderId ) continue;


                var newMin = watermarks.Where(x => x.Key != ack.SanderId)
                    .MinBy(x => ObjectId.Parse(x.Value));


                if (newMin.Value == null || newMin.Key == null) continue;

                var newMinId = newMin.Value;
                var newOwner = newMin.Key;
                if (ObjectId.Parse(newMinId) <= ObjectId.Parse(currentMin)) continue;

                

                if (ack.AckType == AckType.Delivery)
                {
                    state.MinLastMsgIdDelivery = newMinId;
                    state.MinDeliveryOwnerId = newOwner;
                }
                else
                {
                    state.MinLastMsgIdSeen = newMinId;
                    state.MinSeenOwnerId = newOwner;
                }

                events.Add(CreateAckEvent(chatId.ToString(), newMinId, ack.Timestamp, "GLOBAL", ack.AckType, true));
            }

            return events;
        }

     
        private MessageDeliveredAckEvent CreateAckEvent(string chatId, string messageId, DateTime timestamp, string receiverId, AckType type, bool full = false)
        {
            return new MessageDeliveredAckEvent
            {
                ChatId = chatId,
                MessageIds = messageId,
                DeliveredAt = timestamp,
                ReceiverId = receiverId,
                Type = full ? (type == AckType.Delivery ? "FullDelivery" : "FullSeen")
                            : (type == AckType.Delivery ? "Delivery" : "Seen")
            };
        }
    }
}

