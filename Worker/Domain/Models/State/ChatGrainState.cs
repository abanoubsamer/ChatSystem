using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.State
{
   
    [GenerateSerializer]
    public class ChatGrainState
    {
        // عدد اللي لسه مستنيين لكل message
        [Id(0)] public Dictionary<string, int> PendingDelivery { get; set; } = new();
        [Id(1)] public Dictionary<string, int> PendingSeen { get; set; } = new();

        // Bitmap: مين عمل ack (125 byte لكل 1000 member)
        [Id(2)] public Dictionary<string, byte[]> DeliveryBitmaps { get; set; } = new();
        [Id(3)] public Dictionary<string, byte[]> SeenBitmaps { get; set; } = new();

        // member → index بتاعه في الـ Bitmap
        [Id(4)] public Dictionary<string, int> MemberIndex { get; set; } = new();

        [Id(5)] public int TotalMembers { get; set; }
        [Id(6)] public int NextIndex { get; set; } = 0;

        // الـ min الحالي
        [Id(7)] public string MinDelivery { get; set; } = string.Empty;
        [Id(8)] public string MinSeen { get; set; } = string.Empty;
    }
}
