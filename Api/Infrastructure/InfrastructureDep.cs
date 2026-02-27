using Application.Abstractions.Repositories.Chat;
using Application.Abstractions.Repositories.ChatSnapshot;
using Application.Abstractions.Repositories.Messages;
using Application.Abstractions.Repositories.User;
using Application.Abstractions.Services.Authentication;
using Application.Abstractions.Services.Background;
using Application.Abstractions.Services.Publisher;
using Application.Abstractions.Services.Security;
using Domain.OptionsConfiguration;
using Infrastructure.MongoDb.Configurations;
using Infrastructure.Repositories.GenaricRepo;
using Infrastructure.Repositories.Implementation.Chats;
using Infrastructure.Repositories.Implementation.ChatSnapshot;
using Infrastructure.Repositories.Implementation.Messages;
using Infrastructure.Repositories.Implementation.User;
using Infrastructure.Services.Authentication;
using Infrastructure.Services.Background;
using Infrastructure.Services.Publisher;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Services.Security;
using System.Text;

namespace Infrastructure
{
    public static class InfrastructureDep
    {
        public static IServiceCollection AddDbInjection(this IServiceCollection services, IConfiguration configuration)
        {

            var mongoSettings = configuration.GetSection("MongoSettings");
            var connectionString = mongoSettings["ConnectionString"];
            var databaseName = mongoSettings["DatabaseName"];

            services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
            services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>()
            .GetDatabase(databaseName));

            MongoMappingsInitializer.RegisterAll();
            
         
            
            return services;

        }
        //Add Auth Injection

        public static IServiceCollection AddAuthentcationDep(this IServiceCollection services, IConfiguration configuration)
        {

            //Mapping OptionsJWT class to appsettings.json file
            services.Configure<OptionsJWT>(configuration.GetSection("JWT"));
            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateActor = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = configuration["JWT:Audience"],
                    ValidIssuer = configuration["JWT:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"]))
                };

                opt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/chatHub")))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            services.Configure<SecurityStampValidatorOptions>(options =>
            {
                // hna ana bolh ay t3del 7sl fe ale permission yzhr fe s3tha m4 lazm arstr ale app
                options.ValidationInterval = TimeSpan.Zero;
            });


            return services;
        }
        public static IServiceCollection AddInfraRepoDep(this IServiceCollection services)
        {
            services.AddScoped<IMessagesCommandRepository, MessagesCommandRepository>();
            services.AddScoped<IMessagesQueriesRepository, MessagesQueriesRepository>();
            services.AddScoped<IUserQueriesRepository, UserQueriesRepository>();
            services.AddScoped<IChatSnapshotQuerieRepository, ChatSnapshotQuerieRepository>();
            services.AddScoped<IChatSnapshotCommandRepository, ChatSnapshotCommandRepository>();
            services.AddScoped(typeof(IGenaricRepository<>), typeof(GenaricRepository<>));
            services.AddScoped<IMessagePublisher, RabbitMqPublisher>();

            return services;
        }
        public static IServiceCollection AddInfraDep(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<IChatQueriesRepository, ChatQueriesRepository>();
            services.AddScoped<IChatCommandRepository, ChatCommandRepository>();
            services.AddScoped<IAuthenticationServices, AuthenticationServices>();
            services.AddScoped<ISecurityServices, SecurityServices>();
            services.AddSingleton(typeof(IBackgroundQueue<>), typeof(BackgroundQueueService<>));


            return services;
        }

    }
}
