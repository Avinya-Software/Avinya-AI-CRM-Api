using AvinyaAICRM.API.Filters;
using AvinyaAICRM.Application;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface;
using AvinyaAICRM.Infrastructure;
using AvinyaAICRM.Infrastructure.Repositories.ErrorLog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<IErrorLogRepository, ErrorLogRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
