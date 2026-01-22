
namespace AvinyaAICRM.Domain
{
    public class MessageResources
    {
        public static readonly string Created = "{0} created.";
        public static readonly string Updated = "{0} updated.";
        public static readonly string Removed = "{0} removed.";
        public static readonly string InvalidParameter = "Invalid {0} provided.";
        public static readonly string AuthorizationHeaderMissing = "Authorization header is missing.";
        public static readonly string TokenMissingInHeader = "Token is missing in the Authorization header.";
        public static readonly string AccessTokenExpired = "Access Token Has Expired";
        public static readonly string RefreshTokenExpired = "Refresh Token Has Expired";
        public static readonly string UserNotExists = "User not exists.";
        public static readonly string UserNotActive = "User not Active.";
        public static readonly string InvalidToken = "Invalid Token";
        public static readonly string NoDataFound = "No data found.";
        public static readonly string Status = "Your account is in {0}, please follow the procedure to activate your account.";
        public static readonly string TwillioFailureGeneralMessage = "SMS delivery failed. Check your network and recipient's number, or contact your service provider.";
        public static readonly string SendGridFailureGeneralMessage = "Email not sent. Verify address and connection, or contact your email service provider for assistance.";
        public static readonly string PushNotificationFailureGeneralMessage = "Notification not sent. Verify device registration and connection, or contact your notification service provider for assistance.";
    }
}
