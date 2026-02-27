using AppGateway.Middleware;
using Infrastructure;
Console.WriteLine("################# => AppGateway <= ################");
Console.WriteLine("###############################################");
var builder = WebApplication.CreateBuilder(args);


builder.Services.
    AddDbInjection(builder.Configuration)
    .AddAuthentcationDep(builder.Configuration)
    .AddMassRabbitMqDep(builder.Configuration)
    .AddInfraDep();

var app = builder.Build();

app.UseAuthentication();
app.UseWebSockets();

app.UseMiddleware<WebSocketMiddleware>();

app.Run();
