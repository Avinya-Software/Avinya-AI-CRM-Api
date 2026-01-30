
using AvinyaAICRM.Application.Interfaces.ServiceInterface;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AI;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Auth;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Permission;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.SuperAdmin;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Tasks;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.User;
using AvinyaAICRM.Application.Services.AI;
using AvinyaAICRM.Application.Services.Auth;
using AvinyaAICRM.Application.Services.ErrorLog;
using AvinyaAICRM.Application.Services.Permission;
using AvinyaAICRM.Application.Services.Tasks;
using AvinyaAICRM.Application.Services.User;
using AvinyaAICRM.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AvinyaAICRM.Application
{
    public static class ApplicationDependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IErrorLogService, ErrorLogService>();
            services.AddScoped<ISuperAdminService, SuperAdminService>();
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<ITaskService, TaskService>();
            var solutionRoot = Directory.GetParent(Directory.GetCurrentDirectory())!.FullName;

            var modelPath = Path.Combine(
                solutionRoot,
                "AvinyaAICRM.Infrastructure",
                "AI",
                "Models",
                "intent-model.zip"
            );

            services.AddSingleton<IIntentService>(
                _ => new IntentService(modelPath)
            );

            return services;
        }
    }
}
