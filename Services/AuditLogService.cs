using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using SwiftFill.Data;
using SwiftFill.Models;
using System.ComponentModel.DataAnnotations;

namespace SwiftFill.Services
{
    /// <summary>
    /// Persistent audit log service. Stores entries in the SQL database.
    /// Registered as a Singleton, but uses IServiceScopeFactory to access Scoped DbContext.
    /// </summary>
    public class AuditLogService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public AuditLogService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public void Log(string actor, string role, string action, string detail, AuditLogType type = AuditLogType.System)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var entry = new AuditLogEntry
                {
                    Timestamp = DateTime.Now,
                    Actor = actor,
                    Role = role,
                    Action = action,
                    Detail = detail,
                    Type = type
                };

                context.AuditLogs.Add(entry);
                context.SaveChanges();
            }
        }

        /// <summary>Returns recent Security-type events from DB.</summary>
        public IEnumerable<AuditLogEntry> GetSecurityLogs()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                return context.AuditLogs
                    .Where(e => e.Type == AuditLogType.Security)
                    .OrderByDescending(e => e.Timestamp)
                    .Take(100)
                    .ToList();
            }
        }

        /// <summary>Returns recent System-type events from DB.</summary>
        public IEnumerable<AuditLogEntry> GetSystemLogs()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                return context.AuditLogs
                    .Where(e => e.Type == AuditLogType.System)
                    .OrderByDescending(e => e.Timestamp)
                    .Take(100)
                    .ToList();
            }
        }

        /// <summary>Returns all entries from DB.</summary>
        public IEnumerable<AuditLogEntry> GetAllLogs()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                return context.AuditLogs
                    .OrderByDescending(e => e.Timestamp)
                    .Take(200)
                    .ToList();
            }
        }
    }

    public class AuditLogEntry
    {
        [Key]
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Actor { get; set; } = "";        // Username or "System"

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

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

    public enum AuditLogType { Security, System, Inventory, Finance }
}
