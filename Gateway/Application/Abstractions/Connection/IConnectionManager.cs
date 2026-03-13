using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Connection
{
    public interface IConnectionManager : IAsyncDisposable
    {
        Task InitializeAsync(string userId, WebSocket socket, CancellationToken cancellationToken);
        Task ShutdownAsync(CancellationToken cancellationToken = default);
    }
}
