using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs
{
        public class JwtTokenModel
        {
            [JsonProperty("token")]
            public string Token { get; set; } = string.Empty;

            [JsonProperty("refreshToken")]
            public string RefreshToken { get; set; } = string.Empty;

            [JsonProperty("userRoleId")]
            public short? UserRoleID { get; set; } = 0;

            [JsonProperty("userId")]
            public Guid UserID { get; set; } = Guid.Empty;
        }
    
}
