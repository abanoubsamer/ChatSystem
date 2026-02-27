using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Dtos.User
{
    public class userDto
    {

        public string UserId { get; set; }

   
        public string UserName { get; set; }
        // public string? ProfileImage { get; set; }
    }
}
