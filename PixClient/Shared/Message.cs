
using Newtonsoft.Json;

namespace PixClient.Shared
{
    public class Message
    {
        public required string role { get; set; }
        public required string content { get; set; }

        [JsonIgnore]
        public DateTime timestamp { get; set; }
    }
}
