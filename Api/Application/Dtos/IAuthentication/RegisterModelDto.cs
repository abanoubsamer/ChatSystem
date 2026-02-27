using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.IAuthentication
{
    public class RegisterModelDto
    {
        public string UserName { get; set; }
        public string Email { get; set; }

        public string? AvatarUrl { get; set; }

        public string? Bio { get; set; }
        public string Password { get; set; }
    }
}
