using Application.Future.Messages.Querey.Response;
using Core.Basic;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Future.Messages.Querey.Model
{
    public class GetMsgInfoModel:IRequest<Response<List<UserMessageReadInfoResponse>>>
    {
        public string Id { get; set; }
        public GetMsgInfoModel(string id)
        {   
            Id = id;
        }
    }
}
