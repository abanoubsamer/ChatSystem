using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Cache
{
    public interface IChatMemberCache
    {
        ValueTask<HashSet<string>> GetMembersAsync(string chatId, CancellationToken ct = default);
        void SetMembers(string chatId, HashSet<string> members, TimeSpan? expiry = null);
        void Remove(string chatId);
    }
}
