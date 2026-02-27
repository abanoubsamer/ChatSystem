using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.State.Event.User
{
    public class UserStateResponse
    {
        public string Type = "UserStateResponse" ;
        public string UserId { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }
    }
}
