namespace SwiftFill.Services
{
    /// <summary>
    /// In-memory audit log service. Stores up to 500 recent entries.
    /// Registered as a Singleton so logs persist for the lifetime of the app process.
    /// No DB migration required — logs reset on app restart (acceptable for a live ERP).
    /// </summary>
    public class AuditLogService
    {
        private readonly LinkedList<AuditLogEntry> _entries = new();
        private readonly object _lock = new();
        private const int MaxEntries = 500;

        public void Log(string actor, string role, string action, string detail, AuditLogType type = AuditLogType.System)
        {
            var entry = new AuditLogEntry
            {
                Timestamp = DateTime.Now,
                Actor = actor,
                Role = role,
                Action = action,
                Detail = detail,
                Type = type
            };

            lock (_lock)
            {
                _entries.AddFirst(entry);
                if (_entries.Count > MaxEntries)
                    _entries.RemoveLast();
            }
        }

        /// <summary>Returns most recent Security-type events.</summary>
        public IEnumerable<AuditLogEntry> GetSecurityLogs() =>
            _entries.Where(e => e.Type == AuditLogType.Security).Take(100);

        /// <summary>Returns most recent System-type events.</summary>
        public IEnumerable<AuditLogEntry> GetSystemLogs() =>
            _entries.Where(e => e.Type == AuditLogType.System).Take(100);

        /// <summary>Returns all entries.</summary>
        public IEnumerable<AuditLogEntry> GetAllLogs() => _entries.Take(200);
    }

    public class AuditLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Actor { get; set; } = "";        // Username or "System"
        public string Role { get; set; } = "";         // Role at time of action
        public string Action { get; set; } = "";       // e.g. "Login", "ArchiveOrder"
        public string Detail { get; set; } = "";       // e.g. "Order SF-2026-12345 archived"
        public AuditLogType Type { get; set; }

        public string BadgeColor => Type switch
        {
            AuditLogType.Security => "rgba(220,53,69,0.15)",
            AuditLogType.System   => "rgba(46,160,67,0.15)",
            _ => "rgba(255,255,255,0.08)"
        };
        public string BadgeTextColor => Type switch
        {
            AuditLogType.Security => "#ff6b6b",
            AuditLogType.System   => "#6bff72",
            _ => "#aaa"
        };
        public string BadgeBorder => Type switch
        {
            AuditLogType.Security => "rgba(220,53,69,0.4)",
            AuditLogType.System   => "rgba(46,160,67,0.4)",
            _ => "rgba(255,255,255,0.15)"
        };
    }

    public enum AuditLogType { Security, System, Inventory }
}
