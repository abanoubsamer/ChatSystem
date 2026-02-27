using Application.Future.Chat.Querey.Response;
using Core.Basic;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Future.Chat.Querey.Model
{
    public class GetChatInfoModel:IRequest<Response<GetChatInfoResponse>>
    {
        public string ChatId { get; set; }
        public GetChatInfoModel(string chatid)
        {
            ChatId = chatid;
        }
    }
}
