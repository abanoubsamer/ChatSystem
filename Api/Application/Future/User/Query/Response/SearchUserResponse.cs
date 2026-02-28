using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Future.User.Query.Response
{
    public class SearchUserResponse
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string ProfileImage { get; set; }
        public bool IsAlreadyContact { get; set; }
    }
}
