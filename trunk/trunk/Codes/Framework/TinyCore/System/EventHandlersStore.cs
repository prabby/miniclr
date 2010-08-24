using System;
using System.Collections;

namespace Microsoft.SPOT
{

    /// <summary>
    ///     Container for the event handlers
    /// </summary>
    /// <remarks>
    ///     EventHandlersStore is a hashtable
    ///     of handlers for a given
    ///     EventPrivateKey or RoutedEvent
    /// </remarks>
    public class EventHandlersStore
    {
        #region Construction

        /// <summary>
        ///     Constructor for EventHandlersStore
        /// </summary>
        public EventHandlersStore()
        {
            _keys = new ArrayList();
            _values = new ArrayList();
        }

        #endregion Construction

        #region Operations

        // Returns Handlers for the given key
        internal ArrayList this[RoutedEvent key]
        {
            get
            {
                //Replace with HashTable
                int index = _keys.IndexOf(key);

                if (index >= 0)
                {
                    return ((ArrayList)_values[index]);
                }

                return null;
            }
        }

        /// <summary>
        ///     Adds a routed event handler for the given
        ///     RoutedEvent to the store
        /// </summary>
        public void AddRoutedEventHandler(
            RoutedEvent routedEvent,
            Delegate handler,
            bool handledEventsToo)
        {
            if (routedEvent == null)
            {
                throw new ArgumentNullException("routedEvent");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            // Create a new RoutedEventHandler
            RoutedEventHandlerInfo routedEventHandlerInfo =
                new RoutedEventHandlerInfo(handler, handledEventsToo);

            // Get the entry corresponding to the given RoutedEvent
            ArrayList handlers = this[routedEvent];
            if (handlers == null)
            {
                _keys.Add(routedEvent);
                _values.Add(handlers = new ArrayList());
            }

            // Add the RoutedEventHandlerInfo to the list
            handlers.Add(routedEventHandlerInfo);
        }

        #endregion Operations

        #region Data

        // Map of EventPrivateKey/RoutedEvent to Delegate/FrugalObjectList<RoutedEventHandlerInfo> (respectively)
        private ArrayList _keys;
        private ArrayList _values;

        #endregion Data
    }
}


