using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace co_working.Services
{
    public interface ISpamProtectionService
    {
        bool IsRateLimited(string ipAddress);
        bool IsSpamName(string name);
        bool IsValidInterest(string interest);
    }

    public class SpamProtectionService : ISpamProtectionService
    {
        private static readonly ConcurrentDictionary<string, List<DateTime>> _ipSubmissions = new();
        private static readonly TimeSpan _window = TimeSpan.FromHours(1);
        private const int _maxSubmissionsPerWindow = 3;

        private static readonly HashSet<string> ValidInterests = new(StringComparer.OrdinalIgnoreCase)
        {
            "hot-desk", "private-office", "cafe", "both", "other"
        };

        public bool IsRateLimited(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress)) return false;

            var now = DateTime.UtcNow;
            var entries = _ipSubmissions.GetOrAdd(ipAddress, _ => new List<DateTime>());

            lock (entries)
            {
                entries.RemoveAll(t => now - t > _window);
                if (entries.Count >= _maxSubmissionsPerWindow)
                    return true;

                entries.Add(now);
            }

            return false;
        }

        public bool IsSpamName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return true;

            name = name.Trim();

            if (name.Length > 40) return true;

            if (name.Length > 6 && !name.Contains(' '))
            {
                var vowels = Regex.Matches(name, "[aeiouAEIOU]").Count;
                var vowelRatio = (double)vowels / name.Length;
                if (vowelRatio < 0.15) return true;
            }

            if (Regex.IsMatch(name, @"[^aeiouAEIOU\s]{5,}"))
                return true;

            if (!Regex.IsMatch(name, @"^[\p{L}\s'\-\.]+$"))
                return true;

            return false;
        }

        public bool IsValidInterest(string interest)
        {
            return ValidInterests.Contains(interest);
        }
    }
}
