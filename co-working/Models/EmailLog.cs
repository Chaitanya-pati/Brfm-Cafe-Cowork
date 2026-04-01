namespace co_working.Models
{
    public class EmailLogEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Interest { get; set; } = "";
        public string Message { get; set; } = "";
        public string? Error { get; set; }
    }
}
