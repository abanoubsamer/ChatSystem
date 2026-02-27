using Application;
using Infrastructure;
using Microsoft.OpenApi.Models;
Console.WriteLine("################# => API <= ################");
Console.WriteLine("###############################################");
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://fastidious-chebakia-8edf39.netlify.app", "http://localhost:5500") // السماح فقط لهذا الأصل
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
builder.Services.AddDbInjection(builder.Configuration)
    .AddAuthentcationDep(builder.Configuration)
       .AddInfraDep().AddInfraRepoDep()
            .AddApplicationDep(builder.Configuration);

builder.Services.AddSignalR();

builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(c =>
{
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Please enter JWT token with 'Bearer ' prefix.",
    });

    // Add Security Requirement for Bearer token
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowSpecificOrigin"); // تأكد من إنه مفعّل
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
