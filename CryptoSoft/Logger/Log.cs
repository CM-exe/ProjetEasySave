using System;

namespace EasySave.Logger {
    // Enumeration of different logging levels
    public enum LogLevel {
        Debug,         // Detailed technical information for debugging
        Information,   // General information about normal operations
        Warning,       // Warnings about non-critical issues
        Error,         // Errors that may interrupt a process
        Critical       // Serious errors that may cause application failure
    }

    // Interface representing the structure of a log entry
    public interface ILog {
        public DateTime Datetime { get; set; }             // Date and time when the log was recorded
        public string JobName { get; set; }   // Name 
        public string Source { get; set; }
        public string Destination { get; set; }
        public string TaskType { get; set; } // Type of task (e.g., copy, remove, ...)
        public double Filesize { get; set; }               // Size of the file being processed
        public double TransfertDuration { get; set; } // Duration of the transfer in seconds
        public LogLevel Level { get; set; }                // Severity level of the log
        public string Message { get; set; }
    }

    // Concrete class implementing the ILog interface
    public class Log : ILog {
        public DateTime Datetime { get; set; }
        public string JobName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty;
        public double Filesize { get; set; }
        public double TransfertDuration { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
