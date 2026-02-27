using Infrastructure;
Console.WriteLine("################# => BroadcastPreparationWorker <= ################");
Console.WriteLine("###############################################");
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbInjection(builder.Configuration)
    .AddInfraRepoDep()
    .AddMassRabbitMqDep();

var host = builder.Build();
host.Run();
