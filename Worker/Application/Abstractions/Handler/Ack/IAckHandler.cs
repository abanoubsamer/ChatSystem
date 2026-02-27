using Application.Dtos.Ack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Handler.Ack
{
    public interface IAckHandler
    {

        public string ACK { get; }

        public Task HandleAckAsync(
            string messageId,
            string SanderId,
            string chatId,
            string receiverId,
            DateTime ackAt
            , bool isSeen
           );

    }
}
