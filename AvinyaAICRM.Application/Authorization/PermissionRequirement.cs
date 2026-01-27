using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace AvinyaAICRM.Application.Authorization
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Module { get; }
        public string Action { get; }

        public PermissionRequirement(string module, string action)
        {
            Module = module;
            Action = action;
        }
    }
}
