using Application.Dtos.Ack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Grain
{
    public interface IChatGrain : IGrainWithStringKey
    {
        // لما member يعمل ack
        Task ReceiveAckAsync(string memberId, string msgId, AckType type);

        // لما member يدخل الـ chat
        Task MemberJoinedAsync(string memberId);

        // لما member يخرج
        Task MemberLeftAsync(string memberId);

        // لما message تتبعت
        Task MessageSentAsync(string msgId, int totalReceivers);
    }
}
