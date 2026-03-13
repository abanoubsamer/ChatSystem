using Application.Dtos;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Connection.Grains
{
    public interface IUserGrain : IGrainWithStringKey
    {
        Task ConnectAsync(string connectionId);
        Task DisconnectAsync(string connectionId);
        Task<IReadOnlySet<string>> GetActiveConnectionsAsync();
        Task<int> GetConnectionCountAsync();
        Task<bool> IsOnlineAsync();
        Task<UserPresence> GetPresenceAsync();
    }

}
