using Application.Result;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Result
{
    public class LoginResult : Result<string>
    {
       public AppUser User { get; set; }
    }
}
