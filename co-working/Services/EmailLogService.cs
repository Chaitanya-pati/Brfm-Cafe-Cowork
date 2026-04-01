using System.Text.Json;
using co_working.Models;

namespace co_working.Services
{
    public interface IEmailLogService
    {
        Task AppendAsync(EmailLogEntry entry);
        Task<List<EmailLogEntry>> GetAllAsync();
    }

    public class EmailLogService : IEmailLogService
    {
        private readonly string _logPath;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private static readonly JsonSerializerOptions _json = new()
        {
            WriteIndented = true
        };

        public EmailLogService(IWebHostEnvironment env)
        {
            var dataDir = Path.Combine(env.ContentRootPath, "Data");
            Directory.CreateDirectory(dataDir);
            _logPath = Path.Combine(dataDir, "email-logs.json");
        }

        public async Task AppendAsync(EmailLogEntry entry)
        {
            await _lock.WaitAsync();
            try
            {
                var entries = await ReadAsync();
                entries.Add(entry);
                var json = JsonSerializer.Serialize(entries, _json);
                await File.WriteAllTextAsync(_logPath, json);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<EmailLogEntry>> GetAllAsync()
        {
            await _lock.WaitAsync();
            try
            {
                return await ReadAsync();
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<List<EmailLogEntry>> ReadAsync()
        {
            if (!File.Exists(_logPath))
                return new List<EmailLogEntry>();

            var json = await File.ReadAllTextAsync(_logPath);
            if (string.IsNullOrWhiteSpace(json))
                return new List<EmailLogEntry>();

            return JsonSerializer.Deserialize<List<EmailLogEntry>>(json) ?? new List<EmailLogEntry>();
        }
    }
}
