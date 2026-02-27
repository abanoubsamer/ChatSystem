using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Connection.Abstraction
{
    public interface IPresenceRepository
    {
        Task SetLastSeenAsync(string userId, DateTime timestamp, CancellationToken ct = default);
        Task<DateTime?> GetLastSeenAsync(string userId, CancellationToken ct = default);
        Task RemoveAsync(string userId, CancellationToken ct = default);
    }
}
