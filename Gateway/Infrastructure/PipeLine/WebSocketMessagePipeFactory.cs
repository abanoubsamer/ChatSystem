using Application.Abstractions.PipeLine;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.PipeLine
{
    public sealed class WebSocketMessagePipeFactory : IMessagePipeFactory
    {
        private readonly ILogger<WebSocketMessagePipe> _logger;

        public WebSocketMessagePipeFactory(ILogger<WebSocketMessagePipe> logger)
        {
            _logger = logger;
        }

        public IMessagePipe Create(WebSocket socket)
        {
            return new WebSocketMessagePipe(socket, _logger);
        }
    }
}
