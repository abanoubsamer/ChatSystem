using Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Snapshot.Chat.Command
{
    public class AddSnapshotUserCommand
    {
           public string  ChatId { get; set; }
           public List<string>  MemebrId { get; set; }
           public ChatType ChatType { get; set; }
           public string DisplayName { get; set; }
           public string ProfileImage { get; set; }
         
                 
    }
}
