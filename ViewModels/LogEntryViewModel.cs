using MasselGUARD.Models;

namespace MasselGUARD.ViewModels
{
    /// <summary>
    /// Display wrapper around a LogEntry.
    /// The View binds to these for rendering in the activity log.
    /// </summary>
    public class LogEntryViewModel
    {
        public LogEntry Entry { get; }

        public string Timestamp   => Entry.Timestamp.ToString("HH:mm:ss");
        public string Message     => Entry.IsContinuation ? "  ↳ " + Entry.Message : Entry.Message;
        public string ColourKey   => Entry.Level switch
        {
            LogLevel.Ok    => "LogColorOk",
            LogLevel.Warn  => "LogColorWarn",
            LogLevel.Info  => "LogColorInfo",
            _              => "LogColorDebug",
        };
        public string TimestampColourKey => "colorLogTimestamp";

        public LogEntryViewModel(LogEntry entry) => Entry = entry;
    }
}
