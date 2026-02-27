using Application.Abstractions.Grain;
using Application.Abstractions.Handler.Ack;
using Application.Abstractions.Queue;
using Application.Dtos.Ack;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Handler.Ack
{
    public class DeliveryAckHandler : IAckHandler
    {
        public string ACK => "Delivery";

        private readonly IClusterClient _clusterClient;
        public DeliveryAckHandler(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }
        public async Task HandleAckAsync(string messageId, string SanderId, string chatId, string receiverId, DateTime ackAt,bool isSeen)
        {
            var grain = _clusterClient.GetGrain<IAckGrain>(chatId);
            await grain.ReceiveAsync(new AckReceived
            {
                MessageId = messageId,
                SenderId = SanderId,
                Type =   isSeen ? AckType.Seen : AckType.Delivery,
                ChatId = chatId,
                UserId = receiverId,
                Timestamp = ackAt
            });

            Console.WriteLine($"✅ ACK queued: {receiverId} → {messageId}");
        }
    }
}
