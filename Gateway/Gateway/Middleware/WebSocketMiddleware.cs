using Application.Abstractions.Handler.GatewayWebSocket.Ingress;

using System.Security.Claims;


namespace AppGateway.Middleware
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;

        public WebSocketMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
        {
            _next = next;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/ws"))
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                if (context.User?.Identity?.IsAuthenticated != true)
                {
                    context.Response.StatusCode = 401;
                    return;
                }

                var socket = await context.WebSockets.AcceptWebSocketAsync();
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

                using var scope = _serviceProvider.CreateScope();
                var gateway = scope.ServiceProvider.GetRequiredService<IGatewayIngressHandler>();

                await gateway.HandleAsync(userId, socket, context.RequestAborted);
            }
            else
            {
                await _next(context);
            }
        }
    }
}
