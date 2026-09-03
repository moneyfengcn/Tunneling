using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Tunneling.Server.Infrastructure
{
    public class SerilogInMemorySink : ILogEventSink
    {
        private readonly MessageTemplateTextFormatter _formatter;

        public SerilogInMemorySink( )
        {
            _formatter = new MessageTemplateTextFormatter("{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message:lj}{NewLine}{Exception}", null);
        }

        public event Action<string>? LogEmitted;

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) return;

            using var sw = new StringWriter();
            _formatter.Format(logEvent, sw);
            var text = sw.ToString();

            LogEmitted?.Invoke(text);
        }
    }
}
