using Application.Abstractions.Connection;
using Application.Abstractions.Handler.Methods;
using Contracts.Call.Signals;
using System.Net.WebSockets;

namespace Application.Handlers.Call
{
    public class JoinGroupMethodHandler : BaseMethodHandler<JoinGroupSignal>
    {
        public override string MethodName => "join_group";

        private readonly IConnectionServices _connectionServices;

        public JoinGroupMethodHandler(IConnectionServices connectionServices)
        {
            _connectionServices = connectionServices;
        }

        protected override async Task HandleAsync(string userId, JoinGroupSignal request, WebSocket socket)
        {
            _connectionServices.AddUserToGroup(userId, request.GroupId);
            await Task.CompletedTask;
        }
    }
}
