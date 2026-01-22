using AvinyaAICRM.Infrastructure.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Auth
{
    public interface IJwtTokenGenerator
    {
        Task<string> GenerateToken(AppUser user);
    }
}
