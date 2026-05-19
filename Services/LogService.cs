using System;
using System.Collections.Generic;
using System.IO;
using MasselGUARD.Models;

namespace MasselGUARD.Services
{
    /// <summary>
    /// Manages the in-app activity log.
    /// ViewModels subscribe to EntryAdded to render entries.
    /// No UI references — produces pure LogEntry objects.
    /// </summary>
    public class LogService
    {
        private readonly List<LogEntry> _entries = new();
        private LogLevel _minLevel = LogLevel.Ok;

        /// <summary>Raised on the calling thread when a new entry is added.</summary>
        public event Action<LogEntry>? EntryAdded;

        public IReadOnlyList<LogEntry> Entries => _entries;

        public LogLevel MinLevel
        {
            get => _minLevel;
            set => _minLevel = value;
        }

        public bool IsExtended
        {
            get => _minLevel <= LogLevel.Info;
            set => _minLevel = value ? LogLevel.Debug : LogLevel.Ok;
        }

        // ── Write ─────────────────────────────────────────────────────────────
        public void Write(LogLevel level, string message, bool isContinuation = false)
        {
            if (level < _minLevel) return;
            var entry = new LogEntry(DateTime.Now, level, message, isContinuation);
            _entries.Add(entry);
            EntryAdded?.Invoke(entry);
        }

        public void Ok   (string msg) => Write(LogLevel.Ok,   msg);
        public void Warn  (string msg) => Write(LogLevel.Warn,  msg);
        public void Info  (string msg) => Write(LogLevel.Info,  msg);
        public void Debug (string msg) => Write(LogLevel.Debug, msg);

        // ── Export ────────────────────────────────────────────────────────────
        public void ExportToFile(string path)
        {
            using var sw = new StreamWriter(path, append: false,
                encoding: System.Text.Encoding.UTF8);
            foreach (var e in _entries)
                sw.WriteLine($"{e.Timestamp:HH:mm:ss}  [{e.Level,-5}]  {e.Message}");
        }

        public void Clear() => _entries.Clear();
    }
}
