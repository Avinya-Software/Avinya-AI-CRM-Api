
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Client;
using AvinyaAICRM.Application.Interfaces.ServiceInterface;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AI;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Auth;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.City;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Leads;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Orders;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Permission;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Products;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Quotations;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Settings;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.State;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.SuperAdmin;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Tasks;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.TaxCategories;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Team;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.TeamMember;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.User;
using AvinyaAICRM.Application.Services;
using AvinyaAICRM.Application.Services.AI;
using AvinyaAICRM.Application.Services.Auth;
using AvinyaAICRM.Application.Services.City;
using AvinyaAICRM.Application.Services.Client;
using AvinyaAICRM.Application.Services.ErrorLog;
using AvinyaAICRM.Application.Services.Leads;
using AvinyaAICRM.Application.Services.Orders;
using AvinyaAICRM.Application.Services.Permission;
using AvinyaAICRM.Application.Services.Products;
using AvinyaAICRM.Application.Services.Quotations;
using AvinyaAICRM.Application.Services.Settings;
using AvinyaAICRM.Application.Services.State;
using AvinyaAICRM.Application.Services.Tasks;
using AvinyaAICRM.Application.Services.TaxCategories;
using AvinyaAICRM.Application.Services.Team;
using AvinyaAICRM.Application.Services.TeamMember;
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
            services.AddScoped<ITeamService, TeamService>();
            services.AddScoped<ITeamMemberService, TeamMemberService>();
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<ILeadFollowupService, LeadFollowupService>();
            services.AddScoped<ILeadService, LeadService>();
            services.AddScoped<IOrderItemService, OrderItemService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IStatusDropDownServices, StatusDropDownServices>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IQuotationItemService, QuotationItemService>();
            services.AddScoped<IQuotationService, QuotationService>();
            services.AddScoped<ISettingsServices, SettingsServices>();
            services.AddScoped<ICityService, CityService>();
            services.AddScoped<IStateService, StateService>();
            services.AddScoped<ITaxCategoryService, TaxCategoryService>();

            return services;
        }
    }
}
