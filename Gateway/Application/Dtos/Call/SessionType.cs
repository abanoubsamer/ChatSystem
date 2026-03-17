using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.Call
{
    [GenerateSerializer]
    public enum SessionType
    {
        Direct,
        Group
    }
}
