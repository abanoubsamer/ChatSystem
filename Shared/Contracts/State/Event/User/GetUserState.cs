using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.State.Event.User
{
    public class GetUserState
    {
        public string Type = "GetUserState";
        public string UserId { get; set; }
        
    }
}
