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
            if (!context.Request.Path.StartsWithSegments("/ws"))
            {
                await _next(context);
                return;
            } 
            // ✅ Layer 1 — WebSocket request check
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
        
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

           
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync();

            await using var scope = _serviceProvider.CreateAsyncScope();
            var gateway = scope.ServiceProvider
                .GetRequiredService<IGatewayIngressHandler>();

            await gateway.HandleAsync(userId, socket, context.RequestAborted);
           
        }
       
    }
}
