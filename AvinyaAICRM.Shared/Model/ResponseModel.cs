using Newtonsoft.Json;

namespace AvinyaAICRM.Shared.Model
{
    public class ResponseModel
    {
        [JsonProperty("statusCode")]
        public int StatusCode { get; set; } = 0;

        [JsonProperty("statusMessage")]
        public string StatusMessage { get; set; } = string.Empty;

        [JsonProperty("data")]
        public dynamic? Data { get; set; } = null;

        public ResponseModel()
        { }

        public ResponseModel(int statusCode, string statusMessage)
        {
            StatusCode = statusCode;
            StatusMessage = statusMessage;
        }
    }
}
