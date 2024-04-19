using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoggerAPI.Objects_Folder
{
    public class LoggerData
    {
        public enum LogLevel
        {
            Trace,
            Debug,
            Info,
            Warning,
            Error,
            Fatal
        }

        public class LoggerConfiguration
        {
            public string LogFilePath { get; set; } = "logfile.txt";
            public LogLevel MinimumLogLevel { get; set; } = LogLevel.Info;
            public ConsoleColor WatermarkColor { get; set; } = ConsoleColor.Gray;
            public string WatermarkText { get; set; } = "LOG";
            public bool UseWatermark { get; set; } = false;
            public ConsoleColor MessageColor { get; set; } = ConsoleColor.White;
        }
    }
}
