using Application.Abstractions.Cache;
using Infrastructure;
using Infrastructure.Cache;


var builder = Host.CreateApplicationBuilder(args);

Console.WriteLine("################# => Worker <= ################");
Console.WriteLine("###############################################");

builder.UseOrleans(silo =>
{
    silo
        .UseLocalhostClustering()
        .UseMongoDBClient(
            builder.Configuration["MongoSettings:ConnectionString"]!)
        .AddMongoDBGrainStorage("AckStore", options =>
        {
            options.DatabaseName = "ChatDb";
            options.CollectionPrefix = "Orleans_Ack_";
        }).UseMongoDBReminders(options =>
        {
            options.DatabaseName = "ChatDb";
            options.CollectionPrefix = "Orleans_";
        });
});

builder.Services.AddSingleton<IChatMemberCache, MemoryMemberCache>(); // ✅ Memory


builder.Services
    .AddDbInjection(builder.Configuration)
    .AddInfraRepoDep()
    .AddMassRabbitMqDep(builder.Configuration);

var host = builder.Build();
host.Run();