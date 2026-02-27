using AvinyaAICRM.Application.Interfaces.Clients;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.City;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Leads;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Orders;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Permission;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Products;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Quotations;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Settings;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.State;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Tasks;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.TaxCategories;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Team;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.TeamMember;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Tenant;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.User;
using AvinyaAICRM.Application.Interfaces.ServiceInterface;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Settings;
using AvinyaAICRM.Application.Services.Settings;
using AvinyaAICRM.Infrastructure.Authorization;
using AvinyaAICRM.Infrastructure.Identity;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Infrastructure.Repositories.City;
using AvinyaAICRM.Infrastructure.Repositories.ClientRepository;
using AvinyaAICRM.Infrastructure.Repositories.ErrorLog;
using AvinyaAICRM.Infrastructure.Repositories.LeadRepository;
using AvinyaAICRM.Infrastructure.Repositories.OrderRepository;
using AvinyaAICRM.Infrastructure.Repositories.Permission;
using AvinyaAICRM.Infrastructure.Repositories.ProductRepository;
using AvinyaAICRM.Infrastructure.Repositories.QuotationRepository;
using AvinyaAICRM.Infrastructure.Repositories.Settings;
using AvinyaAICRM.Infrastructure.Repositories.State;
using AvinyaAICRM.Infrastructure.Repositories.Tasks;
using AvinyaAICRM.Infrastructure.Repositories.TaxCategoryRepository;
using AvinyaAICRM.Infrastructure.Repositories.Team;
using AvinyaAICRM.Infrastructure.Repositories.TeamMember;
using AvinyaAICRM.Infrastructure.Repositories.Tenant;
using AvinyaAICRM.Infrastructure.Repositories.User;
using AvinyaAICRM.Infrastructure.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel;
using System.Text;

namespace AvinyaAICRM.Infrastructure
{
    public static class InfrastructureDependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // ---------------- DB ----------------
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // ---------------- Identity ----------------
            services.AddIdentity<AppUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            // ---------------- JWT SETTINGS ----------------
            var jwtSettings = configuration.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            });

            // ---------------- Authentication ----------------
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(authHeader))
                        {
                            context.Token = authHeader.StartsWith("Bearer ")
                                ? authHeader.Substring(7).Trim()
                                : authHeader.Trim();
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        var message = context.Exception is SecurityTokenExpiredException
                            ? "Token expired"
                            : "Invalid token";
                        return context.Response.WriteAsync($"{{\"message\":\"{message}\"}}");
                    }
                };

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });

            // 🔥 REQUIRED FOR POLICIES
            services.AddAuthorization();

            // ---------------- Cache ----------------
            services.AddMemoryCache();

            // ---------------- Repositories ----------------
            services.AddScoped<IErrorLogRepository, ErrorLogRepository>();
            services.AddScoped<ITenantRepository, TenantRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserPermissionRepository, UserPermissionRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<ITeamRepository, TeamRepository>();
            services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<ILeadFollowupRepository, LeadFollowupRepository>();
            services.AddScoped<ILeadRepository, LeadRepository>();
            services.AddScoped<IOrderItemRepository, OrderItemRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IStatusDropDownRepository, StatusDropDownRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IQuotationItemRepository, QuotationItemRepository>();
            services.AddScoped<IQuotationRepository, QuotationRepository>();
            services.AddScoped<INumberGeneratorService, NumberGeneratorService>();
            services.AddScoped<ISettingsRepository, SettingsRepository>();
            services.AddScoped<ICityRepository, CityRepository>();
            services.AddScoped<IStateRepository, StateRepository>();
            services.AddScoped<ITaxCategoryRepository, TaxCategoryRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            // ---------------- Permission System ----------------
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

            return services;
        }
    }

}
