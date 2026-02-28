using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.Contact
{
    public class ContactDto
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string ContactName { get; set; }
        public string ContactEmail { get; set; }
        public string ContactAvatar { get; set; }
    }
}
