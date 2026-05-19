using System;

namespace MasselGUARD.Models
{
    public enum LogLevel { Debug, Info, Ok, Warn }

    /// <summary>
    /// A single timestamped line in the activity log.
    /// The ViewModel converts these to formatted RichText runs.
    /// </summary>
    public record LogEntry(
        DateTime  Timestamp,
        LogLevel  Level,
        string    Message,
        bool      IsContinuation = false);
}
