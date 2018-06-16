using System;
using System.Diagnostics;
using System.Resources;
using System.ServiceProcess;

namespace Woof.ServiceEx {

    /// <summary>
    /// Provides a base class for a service that will exist as part of a service application.
    /// <see cref="ServiceBaseEx"/> should be derived from when creating a new service class.
    /// </summary>
    public abstract class ServiceBaseEx : ServiceBase {

        /// <summary>
        /// Gets the service instance if exists.
        /// </summary>
        public static ServiceBaseEx Instance { get; private set; }

        public ResourceManager Resources { get; }

        /// <summary>
        /// Service class constructor
        /// </summary>
        public ServiceBaseEx() : base() {
            EventLog.Log = ServiceState.Configuration.Company;
            EventLog.Source = ServiceState.Configuration.DisplayName;
            ServiceName = ServiceState.Configuration.ServiceName;
            CanPauseAndContinue = false;
            CanShutdown = true;
            Instance = this;
        }

        /// <summary>
        /// Constructs service class assigning resources for event messages.
        /// </summary>
        /// <param name="resourceManager"><see cref="ResourceManager"/> instance.</param>
        protected ServiceBaseEx(ResourceManager resourceManager) : this() => Resources = resourceManager;

        /// <summary>
        /// Signals operation result, warning or error in service EventLog entry.
        /// </summary>
        /// <param name="type">'I' for information, 'W' for warning, 'E' for errors.</param>
        /// <param name="eventId">Unique Id for service, appears in EventLog. Good practice: 1000-1999 for informations, 2000-2999 for warnings, 3000-... for errors.</param>
        /// <param name="args">Optional arguments for message interpolation.</param>
        public void Signal(char type, int eventId, params object[] args) {
            var messageId = $"{type}{eventId}";
            var message = Resources.GetString(messageId);
            var entryType = type == 'E' ? EventLogEntryType.Error : (type == 'W' ? EventLogEntryType.Warning : EventLogEntryType.Information);
            if (args != null && args.Length > 0) message = message != null ? string.Format(message, args) : args[0].ToString();
            EventLog.WriteEvent(new EventDefinition { Id = eventId, Message = message, Type = entryType });
        }

        public void WriteEvent(char type, int eventId, string message) {
            var messageId = $"{type}{eventId}";
            var entryType = type == 'E' ? EventLogEntryType.Error : (type == 'W' ? EventLogEntryType.Warning : EventLogEntryType.Information);
            EventLog.WriteEvent(new EventDefinition { Id = eventId, Message = message, Type = entryType });
        }

        /// <summary>
        /// Public access to service START.
        /// </summary>
        /// <param name="args">Data passed by the start command.</param>
        public void Start(params string[] args) => OnStart(args);

        /// <summary>
        /// Public access to service SHUTDOWN.
        /// </summary>
        public void Shutdown() => OnShutdown();

    }

    public class SignalEventArgs : EventArgs {

        public char Severity { get; }

        public int Id { get; }

        public object[] Parameters { get; }

        public SignalEventArgs(char severity, int id, params object[] parameters) {
            Severity = severity;
            Id = id;
            Parameters = parameters;
        }

    }

}