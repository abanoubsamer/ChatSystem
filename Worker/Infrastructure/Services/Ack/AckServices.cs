using Application.Abstractions.Grain;
using Application.Abstractions.Repositories.Chat;
using Application.Abstractions.Repositories.ChatMember;
using Application.Abstractions.Repositories.MessageReceipts;
using Application.Abstractions.Repositories.Messages;
using Application.Abstractions.Services.Ack;
using Application.Abstractions.Services.Chat;
using Application.Abstractions.Services.Message;
using Application.Abstractions.Services.Publisher;
using Application.Dtos.Ack;
using Application.Dtos.ChatMember.Command;
using Application.Dtos.MessageReceipts.Command;
using Contracts.Enums;
using Contracts.Message.Events;
using Domain.Models;
using Domain.Models.State;
using Infrastructure.Cache;
using Infrastructure.Repositories.Implementation.RepoMessageReceipts;
using MongoDB.Bson;
using System.Diagnostics;

namespace Infrastructure.Services.Ack
{
    public class AckServices : IAckServices
    {
            public List<Acked> CollapseAcks(List<Acked> batch)
            {
                var dict = new Dictionary<(string ReceiverId, AckType Type), Acked>();
                foreach (var ack in batch)
                {
                    var key = (ack.ReceiverId.ToString(), ack.AckType);
                    if (!dict.TryGetValue(key, out var existing) || ObjectId.Parse(ack.LastMsgId) > ObjectId.Parse(existing.LastMsgId))
                        dict[key] = ack;
                }
                return dict.Values.ToList();
            }

            public List<Acked> FilterChanged(List<Acked> collapsed, ChatWatermarkState state)
            {
                var changed = new List<Acked>();
                foreach (var ack in collapsed)
                {
                    var receiverId = ack.ReceiverId.ToString();
                    var watermarks = ack.AckType == AckType.Delivery
                        ? state.DeliveryWatermarks
                        : state.SeenWatermarks;

                    if (!watermarks.TryGetValue(receiverId, out var current) || ObjectId.Parse(ack.LastMsgId) > ObjectId.Parse(current))
                    {
                        watermarks[receiverId] = ack.LastMsgId.ToString();
                        changed.Add(ack);
                    }
                }
                return changed;
            }
       

    }
}