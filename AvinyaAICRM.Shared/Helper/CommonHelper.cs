using AvinyaAICRM.Domain;
using AvinyaAICRM.Domain.Enums;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Shared.Helper
{
    public static class CommonHelper
    {
        public static ResponseModel ExceptionMessage(Exception exception)
        {
            return ResponseMessage(ResponseType.Exception.ToString(), exception.Message.ToString(), string.Empty, null);
        }

        public static ResponseModel SuccessResponseMessage(string message, dynamic? data)
        {
            return ResponseMessage(ResponseType.Success.ToString(), message, string.Empty, data);
        }

        public static ResponseModel CreatedResponseMessage(string module, dynamic? data)
        {
            return ResponseMessage(ResponseType.Created.ToString(), string.Empty, module, data);
        }

        public static ResponseModel UpdatedResponseMessage(string module, dynamic? data)
        {
            return ResponseMessage(ResponseType.Updated.ToString(), string.Empty, module, data);
        }

        public static ResponseModel DeletedResponseMessage(string module, dynamic? data)
        {
            return ResponseMessage(ResponseType.Deleted.ToString(), string.Empty, module, data);
        }

        public static ResponseModel GetResponseMessage(dynamic? data)
        {
            return ResponseMessage(ResponseType.Success.ToString(), string.Empty, string.Empty, data);
        }

        public static ResponseModel BadRequestResponseMessage(string message)
        {
            return ResponseMessage(ResponseType.BadRequest.ToString(), message, string.Empty, null);
        }

        public static ResponseModel BadRequestResponseMessage(dynamic? data)
        {
            return ResponseMessage(ResponseType.BadRequest.ToString(), string.Empty, string.Empty, data);
        }

        public static ResponseModel InvalidParameterResponseMessage(string parameterName)
        {
            return ResponseMessage(ResponseType.BadRequest.ToString(), string.Format(MessageResources.InvalidParameter, parameterName), string.Empty, null);
        }

        public static ResponseModel ResponseMessage(string response, string module)
        {
            module = module.Replace("[dbo].", "").Replace("[", "").Replace("]", "");
            return ResponseMessage(response, string.Empty, module, null);
        }

        public static ResponseModel ResponseMessage(string response, dynamic? data)
        {
            return ResponseMessage(response, string.Empty, string.Empty, data);
        }

        public static ResponseModel ResponseMessage(string response, string message, dynamic? data)
        {
            return ResponseMessage(response, message, string.Empty, data);
        }

        public static ResponseModel UnauthorizedResponseMessage(string response, string message)
        {
            return ResponseMessage(response, message, string.Empty, null);
        }

        public static ResponseModel ForbiddenResponseMessage(string statusName)
        {
            return ResponseMessage(ResponseType.Forbidden.ToString(), string.Format(MessageResources.Status, statusName), string.Empty, null);
        }

        private static ResponseModel ResponseMessage(string response, string message, string module, dynamic? data)
        {
            var enumValue = (ResponseType)Enum.Parse(typeof(ResponseType), response);
            int responseCode = ResponseTypeMapper.GetResponseCode(enumValue);

            switch (enumValue)
            {
                case ResponseType.Created:
                    message = string.Format(MessageResources.Created, module);
                    break;

                case ResponseType.Updated:
                    message = string.Format(MessageResources.Updated, module);
                    break;

                case ResponseType.Deleted:
                    message = string.Format(MessageResources.Removed, module);
                    break;
            }

            return new ResponseModel() { StatusCode = responseCode, StatusMessage = message, Data = data };
        }

    }
}
