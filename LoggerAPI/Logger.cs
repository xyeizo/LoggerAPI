using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static LoggerAPI.Objects_Folder.LoggerData;
namespace LoggerAPI
{
    public static class Logger
    {
        private static readonly ConcurrentQueue<(LogLevel, string)> ConsoleQueue = new ConcurrentQueue<(LogLevel, string)>();
        private static readonly ConcurrentQueue<string> LogQueue = new ConcurrentQueue<string>();
        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
        private static readonly ManualResetEventSlim ConsoleEvent = new ManualResetEventSlim(false);
        private static readonly ManualResetEventSlim FileEvent = new ManualResetEventSlim(false);
        private static bool IsWritingToFile = false;
        private static LoggerConfiguration _configuration = new LoggerConfiguration();

        public static LoggerConfiguration Configuration
        {
            get => _configuration;
            set
            {
                _configuration = value;
                if (_configuration.UseWatermark)
                    SetWatermark();
            }
        }

        static Logger()
        {
            Task.Run(() => WriteLogsToFile());
            Task.Run(() => ProcessConsoleQueue());
            if (_configuration.UseWatermark)
                SetWatermark();
        }

        private static void SetWatermark()
        {
            Console.ForegroundColor = _configuration.WatermarkColor;
            Console.WriteLine($"[{DateTime.Now}] {_configuration.WatermarkText}");
            Console.ResetColor();
        }

        public static void Log(LogLevel level, string message)
        {
            if (level < _configuration.MinimumLogLevel)
                return;

            ConsoleQueue.Enqueue((level, message));
            ConsoleEvent.Set();

            string logEntry = _configuration.UseWatermark ? $"[{DateTime.Now}] {_configuration.WatermarkText} - " : "";
            logEntry += $"[{DateTime.Now}] [{level}] {message}";
            LogQueue.Enqueue(logEntry);
            FileEvent.Set();
        }

        private static async Task ProcessConsoleQueue()
        {
            while (true)
            {
                ConsoleEvent.Wait();
                while (ConsoleQueue.TryDequeue(out (LogLevel level, string message) log))
                {
                    DisplayLogMessage(log.level, log.message);
                }
                ConsoleEvent.Reset();
            }
        }

        private static void DisplayLogMessage(LogLevel level, string message)
        {
            ConsoleColor originalColor = Console.ForegroundColor;

            if (_configuration.UseWatermark)
                Console.WriteLine($"[{DateTime.Now}] {_configuration.WatermarkText}");

            Console.ForegroundColor = ConsoleColor.DarkGray; // For timestamp
            Console.Write($"[{DateTime.Now}] ");
            Console.ForegroundColor = GetConsoleColor(level);
            Console.Write("[");
            Console.ForegroundColor = _configuration.MessageColor;
            Console.Write($"{level}");
            Console.ForegroundColor = GetConsoleColor(level);
            Console.Write("] ");
            Console.ForegroundColor = _configuration.MessageColor;
            Console.WriteLine($"{message}");
            Console.ForegroundColor = originalColor;
        }

        private static async Task WriteLogsToFile()
        {
            while (true)
            {
                FileEvent.Wait();
                await SemaphoreSlim.WaitAsync();

                try
                {
                    if (!LogQueue.IsEmpty && !IsWritingToFile)
                    {
                        IsWritingToFile = true;
                        using (StreamWriter writer = File.AppendText(_configuration.LogFilePath))
                        {
                            while (LogQueue.TryDequeue(out string logEntry))
                            {
                                await writer.WriteLineAsync(logEntry);
                            }
                        }
                        IsWritingToFile = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to write logs to file: {ex.Message}");
                }
                finally
                {
                    SemaphoreSlim.Release();
                    FileEvent.Reset();
                }
            }
        }

        private static ConsoleColor GetConsoleColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return ConsoleColor.Gray;
                case LogLevel.Debug:
                    return ConsoleColor.DarkGray;
                case LogLevel.Info:
                    return ConsoleColor.Green;
                case LogLevel.Warning:
                    return ConsoleColor.Yellow;
                case LogLevel.Error:
                    return ConsoleColor.Red;
                case LogLevel.Fatal:
                    return ConsoleColor.DarkRed;
                default:
                    return ConsoleColor.White;
            }
        }
    }

}
