using AppGateway.Middleware;
using Infrastructure;
Console.WriteLine("################# => AppGateway <= ################");
Console.WriteLine("###############################################");
var builder = WebApplication.CreateBuilder(args);


builder.Services.
    AddDbInjection(builder.Configuration)
    .AddAuthentcationDep(builder.Configuration)
    .AddMassRabbitMqDep()
    .AddInfraDep();

var app = builder.Build();

app.UseAuthentication();
app.UseWebSockets();

app.UseMiddleware<WebSocketMiddleware>();

app.Run();
