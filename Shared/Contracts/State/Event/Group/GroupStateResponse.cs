using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.State.Event.Group
{
    public class GroupStateResponse
    {
         public string Type = "GroupStateResponse";
         public string GroupId { get; set; }
         public bool IsOnline { get; set; }
         public int CountOnlineMembers { get; set; }
         public int TotalMembers { get; set; }

    }
}
