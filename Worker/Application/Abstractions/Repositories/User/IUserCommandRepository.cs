using Contracts.Snapshot.Chat.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories.User
{
    public interface IUserCommandRepository
    {

        Task UpdateUserLastVersion(SyncUserVersionCommand syncUser);
    }
}
