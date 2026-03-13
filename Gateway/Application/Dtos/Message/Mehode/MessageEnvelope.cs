using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Dtos.Message.Mehode
{
    public sealed class MessageEnvelope
    {
        public string Method { get; init; } = string.Empty;
        public JsonElement Params { get; init; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Method);
        public string? ValidationError => IsValid ? null : "Method name is required";
    }
}
