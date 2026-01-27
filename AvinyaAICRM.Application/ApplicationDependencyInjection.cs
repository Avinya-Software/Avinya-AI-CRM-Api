using AvinyaAICRM.Application.Interfaces.RepositoryInterface;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Tenant;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Application.Interfaces.ServiceInterface;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Auth;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Permission;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.SuperAdmin;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.User;
using AvinyaAICRM.Application.Services.Auth;
using AvinyaAICRM.Application.Services.ErrorLog;
using AvinyaAICRM.Application.Services.Permission;
using AvinyaAICRM.Application.Services.User;
using AvinyaAICRM.Infrastructure.Identity;
using AvinyaAICRM.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            return services;
        }
    }
}
