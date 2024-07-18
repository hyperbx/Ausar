using Ausar.Collections;
using Ausar.Logger.Enums;
using Ausar.Logger.Interfaces;

namespace Ausar.Logger.Handlers
{
    internal class FrontendLogger : ILogger
    {
        public static StackList<Log> Logs = new(100);

        public void Log(object in_message, ELogLevel in_logLevel, string in_caller)
        {
            WriteLine(string.IsNullOrEmpty(in_caller) ? in_message : $"[{in_caller}] {in_message}", in_logLevel);
        }

        public void Write(object in_message, ELogLevel in_logLevel)
        {
            if (!App.IsFrontendDebug)
                return;

            var log = new Log(in_message.ToString(), in_logLevel);

            for (int i = 0; i < Logs.Count; i++)
            {
                if (Logs[i].Equals(log))
                {
                    Logs[i].RepeatCount++;
                    return;
                }
            }

            try
            {
                App.Current.Dispatcher.Invoke(() => Logs.Add(log));
            }
            catch (TaskCanceledException)
            {
                // ignored...
            }
        }

        public void WriteLine(object in_message, ELogLevel in_logLevel)
        {
            Write(in_message.ToString().Replace("\n", "\r\n") + "\r\n", in_logLevel);
        }
    }
}
