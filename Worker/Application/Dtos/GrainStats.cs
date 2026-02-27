using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos
{
    [GenerateSerializer]  // ✅
    public sealed record GrainStats(
     [property: Id(0)] string ChatId,
     [property: Id(1)] long ProcessedCount,
     [property: Id(2)] long BatchCount,
     [property: Id(3)] long GlobalAckCount,
     [property: Id(4)] long UptimeMs,
     [property: Id(5)] int MemberCount,
     [property: Id(6)] int PendingCount
 );
}
