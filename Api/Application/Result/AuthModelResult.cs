using System;
using System.Collections.Generic;
using System.Text;

namespace Services.Result
{
    public class AuthModelResult
    {
        public string Message { get; set; } 
        public string UserId { get; set; }

        public string UserName { get; set; }

        public string Token { get; set; }

        public bool IsAuthenticated { get; set; }

        public bool IsExpired { get; set; }


        public DateTime ExpireDate { get; set; }

        public List<string> Roles { get; set; }
        
        public string Email { get; set; }

        public string? RefreshToken { get; set; }

        public DateTime ExpireRefreshToken { get; set; }

    }
}
