using AvinyaAICRM.API.Filters;
using AvinyaAICRM.Application;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AI;
using AvinyaAICRM.Application.Services.AI;
using AvinyaAICRM.Infrastructure;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IIntentService>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();

    var modelPath = Path.Combine(
        env.WebRootPath,
        "AI",
        "Model",
        "intent-model.zip"
    );

    if (!File.Exists(modelPath))
        throw new Exception($"Intent model not found: {modelPath}");

    return new IntentService(modelPath);
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// ---------------- Swagger ----------------
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AvinyaAICRM API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

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
            Array.Empty<string>()
        }
    });
});


var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseStaticFiles();
/* 🔥 MUST COME FIRST */
app.UseAuthentication();

/* 🔥 THEN AUTHORIZATION */
app.UseAuthorization();

app.MapControllers();
app.Run();
