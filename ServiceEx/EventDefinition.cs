using System.Diagnostics;

namespace Woof.ServiceEx {
    
    /// <summary>
    /// System event definition.
    /// </summary>
    public class EventDefinition {

        /// <summary>
        /// Event type.
        /// </summary>
        public EventLogEntryType Type { get; set; } = EventLogEntryType.Information;
        
        /// <summary>
        /// Event identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Event message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Creates new empty system event definition.
        /// </summary>
        public EventDefinition() { }

        /// <summary>
        /// Creates new system event definition.
        /// </summary>
        /// <param name="type">Event type.</param>
        /// <param name="id">Event identifier.</param>
        /// <param name="message">Event message or message format.</param>
        public EventDefinition(EventLogEntryType type, int id, string message) {
            Type = type;
            Id = id;
            Message = message;
        }


    }

}