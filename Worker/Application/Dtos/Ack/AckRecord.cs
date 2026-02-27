using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.Ack
{
    public sealed record AckRecord(
     string ChatId,
     string UserId,
     string MessageId,
     AckType Type,
     DateTime Timestamp
 );
}
