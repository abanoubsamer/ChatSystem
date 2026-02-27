using Contracts.User.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Message.Dtos
{
    public class LastMessageDto
    {
        public string MessageId { get; set; }
        public string Text { get; set; }

        public UserDto Sender { get; set; }

        public bool isRead { get; set; }

        public DateTime SentAt { get; set; }

    }
}
