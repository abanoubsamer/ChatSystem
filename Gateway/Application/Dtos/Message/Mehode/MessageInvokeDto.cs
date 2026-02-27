using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.Dtos.Message.Mehode
{
    public class MessageInvokeDto
    {
        [JsonPropertyName("Method")]
        public string Method { get; set; }
        [JsonPropertyName("Params")]
        public JsonElement Params { get; set; }
    }
}
