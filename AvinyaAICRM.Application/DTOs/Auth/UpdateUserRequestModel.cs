using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Auth
{
    public class UpdateUserRequestModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public bool IsActive { get; set; }
    }

}
