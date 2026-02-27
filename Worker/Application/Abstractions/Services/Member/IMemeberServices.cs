using Application.Dtos.Ack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Services.Member
{
    public interface IMemeberServices
    {
        public Task UpdateChatMembersAsync(List<Acked> items);
    }
}
