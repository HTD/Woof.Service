using System;
using System.Diagnostics;

namespace Woof.ServiceEx {

    /// <summary>
    /// Extends EventLog with predefined events and exception logging.
    /// </summary>
    public static class EventLogEx {

        /// <summary>
        /// Last system event message text.
        /// </summary>
        private static string LastLogMessage;

        /// <summary>
        /// Lock to allow console messages synchronization.
        /// </summary>
        private static readonly object ConsoleLock = new Object();

        /// <summary>
        /// Lock to prevent from multiple access to system log.
        /// </summary>
        private static readonly object EventLogLock = new Object();

        /// <summary>
        /// Writes event log with "spam" protection.
        /// </summary>
        /// <param name="ev">Event object.</param>
        /// <param name="data">Optional data to format message text.</param>
        public static void WriteEvent(this EventLog log, EventDefinition ev, params object[] data) {
            lock (EventLogLock) {
                var msg = data.Length < 1 ? ev.Message : String.Format(ev.Message, data);
#if NOSPAM
                if (msg != LastLogMessage) {
#endif
                LastLogMessage = msg;
                if (ServiceState.IsInTestMode) {
                    lock (ConsoleLock) {
                        switch (ev.Type) {
                            case EventLogEntryType.Information: Console.Write("II: "); break;
                            case EventLogEntryType.Warning: Console.Write("WW: "); break;
                            case EventLogEntryType.Error: Console.Write("EE: "); break;
                        }
                        Console.WriteLine(String.Format("({0}) {1}", ev.Id, msg));
                    }
                } else log.WriteEntry(msg, ev.Type, ev.Id);
#if NOSPAM
                }
#endif
            }
        }

        /// <summary>
        /// Logs managed exception as event.
        /// </summary>
        /// <param name="x">Exception.</param>
        /// <param name="id">Event identifier, default 3666.</param>
        public static void WriteException(this EventLog log, Exception x, int id = 3666) => log.WriteEvent(new EventDefinition {
            Id = id,
            Type = EventLogEntryType.Error,
            Message = String.Format(EventMessages.Exception, "0x" + x.HResult.ToString("X"), x.Message, x.StackTrace)
        });

    }

}