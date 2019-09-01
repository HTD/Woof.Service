﻿using System;
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

        /// <summary>
        /// Gets the resource managers assigned to the service in constructor.
        /// </summary>
        public ResourceManager[] Resources { get; }

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
        /// <param name="resources">One or more <see cref="ResourceManager"/> instance.</param>
        protected ServiceBaseEx(params ResourceManager[] resources) : this() => Resources = resources;

        /// <summary>
        /// Tries to get a string from the configured resources.
        /// If not found, the message identifier itself is returned.
        /// </summary>
        /// <param name="messageId">Message identifier (resource name).</param>
        /// <returns>Resource string or message identifier.</returns>
        private string GetResourceString(string messageId) {
            foreach (var res in Resources) if (res.GetString(messageId) is string s) return s;
            return messageId;
        }

        /// <summary>
        /// Signals operation result, warning or error in service EventLog entry.
        /// </summary>
        /// <param name="severity">Event severity, one of 'I' for info, 'W' for warning, 'E' for error.</param>
        /// <param name="eventId">Unique Id for service, appears in EventLog. Good practice: 1000-1999 for informations, 2000-2999 for warnings, 3000-... for errors.</param>
        /// <param name="args">Optional arguments for message interpolation.</param>
        public void Signal(char severity, int eventId, params object[] args) {
            var messageId = $"{severity}{eventId}";
            var message = GetResourceString(messageId);
            var entryType = severity == 'E' ? EventLogEntryType.Error : (severity == 'W' ? EventLogEntryType.Warning : EventLogEntryType.Information);
            if (message.Contains("{0}") && args != null && args.Length > 0) message = string.Format(message, args);
            EventLog.WriteEvent(new EventDefinition { Id = eventId, Message = message, Type = entryType });
        }

        /// <summary>
        /// Writes an arbitrary event to the service's event log.
        /// </summary>
        /// <param name="severity">Event severity, one of 'I' for info, 'W' for warning, 'E' for error.</param>
        /// <param name="eventId">Event identifier. Set to zero if unsure.</param>
        /// <param name="message">Event message text.</param>
        public void WriteEvent(char severity, int eventId, string message) {
            var entryType = severity == 'E' ? EventLogEntryType.Error : (severity == 'W' ? EventLogEntryType.Warning : EventLogEntryType.Information);
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

    /// <summary>
    /// Event arguments for service signal events.
    /// </summary>
    public class SignalEventArgs : EventArgs {

        /// <summary>
        /// Gets the event severity, one of 'I' for info, 'W' for warning, 'E' for error.
        /// </summary>
        public char Severity { get; }

        /// <summary>
        /// Gets the event identifier.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the optional parameters to replace placeholders in predefined localized messages.
        /// </summary>
        public object[] Parameters { get; }

        /// <summary>
        /// Creates new arguments for the log signal event.
        /// </summary>
        /// <param name="severity">Event severity, one of 'I' for info, 'W' for warning, 'E' for error.</param>
        /// <param name="id">Event identifier. Should match the predefined event identifier.</param>
        /// <param name="parameters">Optional parameters to replace placeholders in predefined localized messages.</param>
        public SignalEventArgs(char severity, int id, params object[] parameters) {
            Severity = severity;
            Id = id;
            Parameters = parameters;
        }

    }

}