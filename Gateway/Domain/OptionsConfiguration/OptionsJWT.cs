using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.OptionsConfiguration
{
    public class OptionsJWT
    {
        public string Issuer { get; set; }

        public string Audience { get; set; }

        public int LiveTimeHours { get; set; }

        public string SecretKey { get; set; }
    }
}
