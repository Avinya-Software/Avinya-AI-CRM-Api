using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Domain.Enums
{
    public enum ResponseType
    {
        Success,
        Created,
        Updated,
        Deleted,
        BadRequest,
        Unauthorized,
        Exception,
        Forbidden
    }

    public static class ResponseTypeMapper
    {
        private static readonly Dictionary<ResponseType, int> mapping = new()
        {
            { ResponseType.Success, 200},
            { ResponseType.Created, 201},
            { ResponseType.Updated, 200},
            { ResponseType.Deleted, 200},
            { ResponseType.BadRequest, 400},
            { ResponseType.Unauthorized, 401},
            { ResponseType.Forbidden, 403},
            { ResponseType.Exception, 500},
        };

        public static int GetResponseCode(ResponseType responseType)
        {
            return mapping[responseType];
        }
    }
}
