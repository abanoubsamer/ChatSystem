using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.State.Event.Group
{
    public class GetGroupState
    {
        public string Type = "GetGroupState";
        public string GroupId { get; set; }
    }
}
