namespace SharpBoi.Database.Models
{
    public class ChatApiConfig
    {
        public int Id { get; set; }
        public string ApiKey { get; set; }
        public string Prompt { get; set; }
        public int WordCount { get; set; }
        public string Model { get; set; }
    }
}
