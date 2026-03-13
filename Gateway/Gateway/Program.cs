using AppGateway;
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



builder.UseOrleans(silo =>
{
    silo.ConfigureLogging(logging =>
        logging.AddConsole().SetMinimumLevel(LogLevel.Debug));
    silo
        .UseLocalhostClustering()
        .UseMongoDBClient(
            builder.Configuration["MongoSettings:ConnectionString"]!)
        .AddMongoDBGrainStorage("ChatStore", options =>
        {
            options.DatabaseName = "ChatDb";
            options.CollectionPrefix = "Orleans_Chat_";
        });
});
builder.Services.AddHostedService<RoomGrainMigrationService>();
var app = builder.Build();

app.UseAuthentication();
app.UseWebSockets();

app.UseMiddleware<WebSocketMiddleware>();

app.Run();
