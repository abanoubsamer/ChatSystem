using Application.Future.Snapshot.Queries.Response;
using Core.Basic;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Future.Snapshot.Queries.Models
{
    public class SyncChatSnapshotModel:IRequest<Response<List<GetChatsSnapshotResponse>>>
    {
        public DateTime _LastSeenVersion { get; set; }
        public string _UserId { get; set; }
        public SyncChatSnapshotModel(DateTime lastseenVersion, string userId)
        {
            _LastSeenVersion = lastseenVersion;
            _UserId = userId;
        }
    }
}
