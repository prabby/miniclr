using System;
using System.Collections;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using System.IO;
using System.Threading;
using Dpws.Device;
using Dpws.Device.Services;
using Ws.Services.Faults;
using Ws.Services.Transport.HTTP;
using Ws.Services.Utilities;
using Ws.Services.WsaAddressing;

using System.Ext;
using System.Ext.Xml;
using Ws.Services;
using Ws.Services.Xml;
using Microsoft.SPOT;

namespace Dpws.Device.Services
{
    /// <summary>
    /// Use to raise a hosted service event to active event sinks.
    /// </summary>
    /// <remarks>
    /// The device stack uses this class to manage event subscriptions. A device developer uses this class to
    /// send an event message to clients that have an active event subscription with an event source.
    /// </remarks>
    public class DpwsWseSubscriptionMgr
    {
        WsHttpClient httpClient = new WsHttpClient();
        
        /// <summary>
        /// Creates an instance of the Subscription manager class.
        /// </summary>
        /// <remarks>
        /// The device manages an instance of this class on behalf of the device. A device developer
        /// must use the static Device.SubScriptionMgr.FireEvent method to fire events from an event
        /// source.
        /// </remarks>
        public DpwsWseSubscriptionMgr()
        {
        }

        /// <summary>
        /// Use this method to raise an event.
        /// </summary>
        /// <param name="hostedService">The hosted service that contains the event source.</param>
        /// <param name="eventSource">The event source that define the event.</param>
        /// <param name="eventMessage">The event message buffer.</param>
        /// <remarks>
        /// A device developer is responsible for building the event message buffer sent
        /// to clients that have an active event subscription with an event source. The subscription manager
        /// uses the hosted service parameter to access various properties of the service. The event source
        /// parameter is used to access the event sinks collection of the event source. Note: This method
        /// requires special provisions in order to properly build event message headers for each event sink.
        /// In order to send an event to a listening client the soap.header.To property must be changed for
        /// each listening client. In the future custom attribute support will solve this problem. For now
        /// however a search and replace mechanism is used to modifiy the header.To property. When a device
        /// developer builds the event message buffer they must use the search string WSDNOTIFYTOADDRESS for
        /// the To header property.
        /// </remarks>
        public void FireEvent(DpwsHostedService hostedService, DpwsWseEventSource eventSource, WsWsaHeader eventHeader, String eventMessageBody)
        {
            // Find the specified event source
            if (eventSource == null)
                throw new ArgumentNullException("FireEvent could not locate specified hosted service");
            DpwsWseEventSinks eventSinks = eventSource.EventSinks;

            // if there are event sources display message
            int count = eventSinks.Count;

            if (count > 0)
            {
                System.Ext.Console.Write("");
                System.Ext.Console.Write("Firing " + eventSource.Name);
                System.Ext.Console.Write("");
            }

            // Loop through event sinks and send the event message
            for (int i = 0; i < count; i++)
            {
                DpwsWseEventSink eventSink = eventSinks[i];

                // Try to send event. If attempt fails delete the subscription/eventSink
                try
                {
                    MemoryStream soapStream = new MemoryStream();
                    XmlWriter xmlWriter = XmlWriter.Create(soapStream);

                    WsWsaHeader header = new WsWsaHeader(eventHeader.Action, eventHeader.RelatesTo, eventSink.NotifyTo.Address.AbsoluteUri,
                        null, null, eventSink.NotifyTo.RefParameters);

                    WsSoapMessageWriter.WriteSoapMessageStart(xmlWriter, WsSoapMessageWriter.Prefixes.Wse, null, header, null);

                    if (eventMessageBody != null)
                        xmlWriter.WriteRaw(eventMessageBody);

                    WsSoapMessageWriter.WriteSoapMessageEnd(xmlWriter);

                    xmlWriter.Flush();
                    xmlWriter.Close();

                    SendEvent(soapStream.ToArray(), eventSink.NotifyTo.Address.AbsoluteUri);
                }
                catch (Exception e)
                {
                    System.Ext.Console.Write("");
                    System.Ext.Console.Write("FireEvent failed. Deleting EventSink! NotifyToAddress = " + eventSink.NotifyTo.Address + " Exception: " + e.Message);
                    System.Ext.Console.Write("");

                    // Send oneway subscription end message
                    try
                    {
                        SendSubscriptionEnd(eventSink, "DeliveryFailure", hostedService.ServiceID);
                    }
                    catch { }

                    // Remove event sink from event source list
                    eventSinks.Remove(eventSink);
                }
            }
        }

        /// <summary>
        /// Called by for each active event sink to send an event message to a listening client.
        /// </summary>
        /// <param name="eventMessage">Byte array containing the event message soap envelope.</param>
        /// <param name="notifyToAddress"></param>
        private void SendEvent(byte[] eventMessage, string notifyToAddress)
        {

            // Parse the http transport address
            if (notifyToAddress.IndexOf("http://") == 0)
            {
                httpClient.SendRequest(eventMessage, notifyToAddress, false, false);
            }
            else
            {
                System.Ext.Console.Write("");
                System.Ext.Console.Write("Unsupported transport address (FireEvent - notifyToAddress: " + notifyToAddress);
                System.Ext.Console.Write("");
                return;
            }
        }

        /// <summary>
        /// Used by the subscription manager to send a subscription end message to a listening client.
        /// </summary>
        /// <param name="eventSink">An event sink object containing the client endpoint information.</param>
        /// <param name="subEndType">A string that specifies the reason fot the subscription end.</param>
        /// <param name="subMangerID">A id used by the client to identify this subcription.</param>
        /// <remarks>If an error occures when sending an event message this method is called to tell
        /// the client this subscription has been expired.
        /// </remarks>
        private void SendSubscriptionEnd(DpwsWseEventSink eventSink, string subEndType, string subMangerID)
        {
            // if we weren't given an EndTo, don't send
            if (eventSink.EndTo == null)
            {
                return;
            }

            // Parse the http transport address
            if (eventSink.EndTo.Address.Scheme == "http")
            {
                WsHttpClient httpClient = new WsHttpClient();
                httpClient.SendRequest(SubscriptionEndResponse(eventSink, subEndType, subMangerID), eventSink.EndTo.Address.AbsoluteUri, true, false);
            }
            else
            {
                System.Ext.Console.Write("");
                System.Ext.Console.Write("Unsupported transport address Subscription EndTo.Address: " + eventSink.EndTo.Address);
                System.Ext.Console.Write("");
                return;
            }
        }

        /// <summary>
        /// This method build a subscription end message.
        /// </summary>
        /// <param name="eventSink">An event sink containing client endpoint information.</param>
        /// <param name="shutdownMessage">A string containing reason why the subscription is ending.</param>
        /// <param name="subMangerID">An id sent by the client that they use to reference a subscription.</param>
        /// <returns></returns>
        private byte[] SubscriptionEndResponse(DpwsWseEventSink eventSink, string shutdownMessage, string subMangerID)
        {

            MemoryStream soapStream = new MemoryStream();
            XmlWriter xmlWriter = XmlWriter.Create(soapStream);

            WsWsaHeader responseHeader = new WsWsaHeader(
                WsWellKnownUri.WseNamespaceUri + "/SubscriptionEnd",    // Action
                null,                                                   // RelatesTo
                eventSink.EndTo.Address.AbsoluteUri,                    // To
                null, null, eventSink.EndTo.RefProperties);             // ReplyTo, From, Any

            WsSoapMessageWriter.WriteSoapMessageStart(xmlWriter,
                WsSoapMessageWriter.Prefixes.Wse,                       // Prefix
                null,                                                   // Additional Prefix
                responseHeader,                                         // Header
                null);                                                  // AppSequence

            // write body
            xmlWriter.WriteStartElement("wse", "SubscriptionEnd", null);
            xmlWriter.WriteStartElement("wse", "SubscriptionManager", null);
            xmlWriter.WriteStartElement("wsa", "Address", null);
            xmlWriter.WriteString("http://" + Device.IPV4Address + ":" + Device.Port + "/" + subMangerID);
            xmlWriter.WriteEndElement(); // End Address
            xmlWriter.WriteStartElement("wsa", "ReferenceParameters", null);
            xmlWriter.WriteStartElement("wse", "Identifier", null);
            xmlWriter.WriteString(eventSink.ID);
            xmlWriter.WriteEndElement(); // End Identifier
            xmlWriter.WriteEndElement(); // End ReferenceParameters
            xmlWriter.WriteEndElement(); // End SubscriptionManager
            xmlWriter.WriteStartElement("wse", "Code", null);
            xmlWriter.WriteString("wse:" + shutdownMessage);
            xmlWriter.WriteEndElement(); // End Code
            xmlWriter.WriteEndElement(); // End SubscriptionEnd

            WsSoapMessageWriter.WriteSoapMessageEnd(xmlWriter);

            // Flush and close writer. Return stream buffer
            xmlWriter.Flush();
            xmlWriter.Close();
            return soapStream.ToArray();
        }

        /// <summary>
        /// Service endpoints are stored as urn:uuid's. This method is used to convert the header.To address into
        /// urn:uuid format if it is not already.
        /// </summary>
        /// <param name="toAddress">A string containing the header.To address.</param>
        /// <returns>A string containing a urn:uuid parsed from the header.To field.</returns>
        internal string FixToAddress(string toAddress)
        {
            // Make sure that the To address is a urn:uuid, use uri for parsing (cases could be complex)
            string endpointAddress = null;
            if (toAddress.IndexOf("urn") == 0 || toAddress.IndexOf("uuid") == 0 || toAddress.IndexOf("http") == 0)
            {
                // Convert to address to Uri
                Uri toUri = new Uri(toAddress);

                // Convert the to address to a urn:uuid if it is an Http endpoint
                if (toUri.Scheme == "urn")
                    endpointAddress = toUri.AbsoluteUri;
                else if (toUri.Scheme == "uuid")
                {
                    endpointAddress = "urn:" + toUri.AbsoluteUri;
                }
                else if (toUri.Scheme == "http")
                {
                    endpointAddress = "urn:uuid:" + toUri.AbsoluteUri;
                }
                else
                    endpointAddress = toAddress;
            }
            else
                endpointAddress = "urn:uuid:" + toAddress;
            return endpointAddress;
        }

        /// <summary>
        /// Global eventing Subscribe stub.
        /// </summary>
        /// <param name="header">Header object.</param>
        /// <param name="reader">An XmlReader positioned at the begining of the Subscribe request body element.</param>
        /// <param name="serviceEndpoints">A Collection of serviceEndpoints used to determine what services contain the event source specified in the filter.</param>
        /// <returns>Byte array containing a Subscribe response.</returns>
        internal byte[] Subscribe(WsWsaHeader header, XmlReader reader, WsServiceEndpoints serviceEndpoints)
        {
            // Parse Subscribe Request
            /////////////////////////////
            DpwsWseEventSink eventSink = new DpwsWseEventSink();
            try
            {
                reader.ReadStartElement("Subscribe", WsWellKnownUri.WseNamespaceUri);

                if (reader.IsStartElement("EndTo", WsWellKnownUri.WseNamespaceUri))
                {
                    eventSink.EndTo = new WsWsaEndpointRef(reader);
                }

                reader.ReadStartElement("Delivery", WsWellKnownUri.WseNamespaceUri);
                if (reader.IsStartElement("NotifyTo", WsWellKnownUri.WseNamespaceUri))
                {
                    eventSink.NotifyTo = new WsWsaEndpointRef(reader);
                }
                else
                {
                    throw new WsFaultException(header, WsFaultType.WseDeliverModeRequestedUnavailable);
                }

                reader.ReadEndElement();

                if (reader.IsStartElement("Expires", WsWellKnownUri.WseNamespaceUri))
                {
                    long expires = new WsDuration(reader.ReadElementString()).DurationInSeconds;

                    if (expires > 0)
                    {
                        eventSink.Expires = expires;
                    }
                    else
                    {
                        throw new WsFaultException(header, WsFaultType.WseInvalidExpirationTime);
                    }
                }
                else
                {
                    // Never Expires
                    eventSink.Expires = -1;
                }

                if (reader.IsStartElement("Filter", WsWellKnownUri.WseNamespaceUri))
                {
                    if (reader.MoveToAttribute("Dialect") == false || reader.Value != "http://schemas.xmlsoap.org/ws/2006/02/devprof/Action")
                    {
                        throw new WsFaultException(header, WsFaultType.WseFilteringRequestedUnavailable);
                    }

                    reader.MoveToElement();

                    String filters = reader.ReadElementString();

                    if (filters != String.Empty)
                    {
                        eventSink.Filters = filters.Split(' ');
                    }
                }

                XmlReaderHelper.SkipAllSiblings(reader);

                reader.ReadEndElement(); // Subscribe
            }
            catch (XmlException e)
            {
                throw new WsFaultException(header, WsFaultType.WseInvalidMessage, e.ToString());
            }

            // Parse urn:uuid from the To address
            string endpointAddress = FixToAddress(header.To);

            // Build a temporary collection of device services that match the specified endpoint address.
            WsServiceEndpoints matchingServices = new WsServiceEndpoints();
            for (int i = 0; i < serviceEndpoints.Count; ++i)
            {
                if (serviceEndpoints[i].EndpointAddress == endpointAddress)
                    matchingServices.Add(serviceEndpoints[i]);
            }

            // For each service with a matching endpoint and event sources add an event sink to the
            // event source collection
            for (int i = 0; i < matchingServices.Count; ++i)
            {
                DpwsWseEventSources eventSources = ((DpwsHostedService)matchingServices[i]).EventSources;

                // Set the EventSinkID
                eventSink.ID = "urn:uuid:" + Guid.NewGuid().ToString();

                // If subscribing to all event sources
                if (eventSink.Filters == null)
                {
                    int count = eventSources.Count;
                    for (int ii = 0; i < count; i++)
                    {
                        DpwsWseEventSource eventSource = eventSources[ii];
                        eventSink.StartTime = DateTime.Now.Ticks;
                        eventSource.EventSinks.Add(eventSink);
                    }
                }
                else
                {
                    // If subscribing to a specific event based on an event filter.
                    DpwsWseEventSource eventSource;
                    string[] filterList = eventSink.Filters;
                    int length = filterList.Length;
                    for (int ii = 0; i < length; i++)
                    {
                        if ((eventSource = eventSources[filterList[ii]]) != null)
                        {
                            eventSink.StartTime = DateTime.Now.Ticks;
                            eventSource.EventSinks.Add(eventSink);
                        }
                        else
                        {
                            throw new Exception("Event source " + filterList[ii] + " was not found.");
                        }
                    }
                }
            }

            // Generate Response
            //////////////////////////
            MemoryStream soapStream = new MemoryStream();
            XmlWriter xmlWriter = XmlWriter.Create(soapStream);

            WsWsaHeader responseHeader = new WsWsaHeader(
                WsWellKnownUri.WseNamespaceUri + "/SubscribeResponse",  // Action
                header.MessageID,                                       // RelatesTo
                header.ReplyTo.Address.AbsoluteUri,                     // To
                null, null, null);                                      // ReplyTo, From, Any

            WsSoapMessageWriter.WriteSoapMessageStart(xmlWriter,
                WsSoapMessageWriter.Prefixes.Wse,                       // Prefix
                null,                                                   // Additional Prefix
                responseHeader,                                         // Header
                new WsSoapMessageWriter.AppSequence(Device.AppSequence, Device.SequenceID, Device.MessageID)); // AppSequence

            // write body
            xmlWriter.WriteStartElement("wse", "SubscribeResponse", null);
            xmlWriter.WriteStartElement("wse", "SubscriptionManager", null);
            xmlWriter.WriteStartElement("wsa", "Address", null);
            // Create a uri. Use the path (by default will be a uuid) for the sub manager endpoint
            Uri subMgrUri = new Uri(((DpwsHostedService)matchingServices[0]).EndpointAddress);
            xmlWriter.WriteString("http://" + Device.IPV4Address + ":" + Device.Port + "/" + subMgrUri.AbsolutePath);
            xmlWriter.WriteEndElement(); // End Address
            xmlWriter.WriteStartElement("wsa", "ReferenceParameters", null);
            xmlWriter.WriteStartElement("wse", "Identifier", null);
            xmlWriter.WriteString(eventSink.ID);
            xmlWriter.WriteEndElement(); // End Identifier
            xmlWriter.WriteEndElement(); // End ReferenceParameters
            xmlWriter.WriteEndElement(); // End SubscriptionManager
            xmlWriter.WriteStartElement("wse", "Expires", null);
            xmlWriter.WriteString(new WsDuration(eventSink.Expires).DurationString);
            xmlWriter.WriteEndElement(); // End Expires
            xmlWriter.WriteEndElement(); // End SubscribeResponse

            WsSoapMessageWriter.WriteSoapMessageEnd(xmlWriter);

            // Flush and close writer. Return stream buffer
            xmlWriter.Flush();
            xmlWriter.Close();
            return soapStream.ToArray();
        }

        /// <summary>
        /// Eventing UnSubscribe stub.
        /// </summary>
        /// <param name="header">Header object.</param>
        /// <param name="reader">An XmlReader positioned at the begining of the Unsubscribe request body element.</param>
        /// <param name="serviceEndpoints">A Collection of serviceEndpoints used to determine what services contain the specified event.</param>
        /// <returns>Byte array containing an UnSubscribe response.</returns>
        /// <remarks>This method is used by the stack framework. Do not use this method.</remarks>
        public byte[] Unsubscribe(WsWsaHeader header, XmlReader reader, WsServiceEndpoints serviceEndpoints)
        {
            // Parse Unsubscribe Request
            ///////////////////////////////
            // there's no info in Unsubscribe that we actually need, just get the identifier from header
            String eventSinkID = header.Any.GetNodeValue("Identifier", WsWellKnownUri.WseNamespaceUri);

            bool eventSourceFound = false;
            if (eventSinkID != null)
            {

                // Parse urn:uuid from the To address
                string endpointAddress = FixToAddress(header.To);

                // Iterate the list of hosted services at the specified endpoint and unsubscribe from each event source
                // that matches the eventSinkID
                for (int i = 0; i < Device.HostedServices.Count; ++i)
                {
                    if (serviceEndpoints[i].EndpointAddress == endpointAddress)
                    {

                        // Delete Subscription
                        DpwsWseEventSources eventSources = ((DpwsHostedService)Device.HostedServices[i]).EventSources;

                        // Look for matching event in hosted services event sources
                        DpwsWseEventSource eventSource;
                        DpwsWseEventSinks eventSinks;
                        DpwsWseEventSink eventSink;
                        int eventSourcesCount = eventSources.Count;
                        int eventSinksCount;
                        for (int ii = 0; ii < eventSourcesCount; ii++)
                        {
                            eventSource = eventSources[ii];
                            eventSinks = eventSource.EventSinks;
                            eventSinksCount = eventSinks.Count;
                            for (int j = 0; j < eventSinksCount; j++)
                            {
                                eventSink = eventSinks[j];
                                if (eventSink.ID == eventSinkID)
                                {
                                    eventSourceFound = true;
                                    eventSource.EventSinks.Remove(eventSink);

                                }
                            }
                        }
                    }
                }

                if (eventSourceFound)
                {
                    // Generate Response

                    MemoryStream soapStream = new MemoryStream();
                    XmlWriter xmlWriter = XmlWriter.Create(soapStream);

                    WsWsaHeader responseHeader = new WsWsaHeader(
                        WsWellKnownUri.WseNamespaceUri + "/UnsubscribeResponse",// Action
                        header.MessageID,                                       // RelatesTo
                        header.ReplyTo.Address.AbsoluteUri,                     // To
                        null, null, null);

                    WsSoapMessageWriter.WriteSoapMessageStart(xmlWriter,
                        WsSoapMessageWriter.Prefixes.Wse,                       // Prefix
                        null,                                                   // Additional Prefix
                        responseHeader,                                         // Header
                        new WsSoapMessageWriter.AppSequence(Device.AppSequence, Device.SequenceID, Device.MessageID)); // AppSequence

                    WsSoapMessageWriter.WriteSoapMessageEnd(xmlWriter);

                    // Flush and close writer. Return stream buffer
                    xmlWriter.Flush();
                    xmlWriter.Close();
                    return soapStream.ToArray();
                }
            }

            // Something went wrong
            throw new WsFaultException(header, WsFaultType.WseEventSourceUnableToProcess);
        }

        /// <summary>
        /// Event renew stub.
        /// </summary>
        /// <param name="header">Header object.</param>
        /// <param name="reader">An XmlReader positioned at the begining of the Renew request body element.</param>
        /// <param name="serviceEndpoints">A Collection of serviceEndpoints used to determine what services contain the specified event.</param>
        /// <returns>Byte array containing an Renew response.</returns>
        /// <remarks>This method is used by the stack framework. Do not use this method.</remarks>
        public byte[] Renew(WsWsaHeader header, XmlReader reader, WsServiceEndpoints serviceEndpoints)
        {
            long newExpiration = 0;
            String eventSinkId = String.Empty;

            // Parse Renew request
            ////////////////////////////
            try
            {
                reader.ReadStartElement("Renew", WsWellKnownUri.WseNamespaceUri);

                if (reader.IsStartElement("Expires", WsWellKnownUri.WseNamespaceUri))
                {
                    newExpiration = new WsDuration(reader.ReadElementString()).DurationInSeconds;

                    if (newExpiration <= 0)
                    {
                        throw new WsFaultException(header, WsFaultType.WseInvalidExpirationTime);
                    }
                }
                else
                {
                    // Never Expires
                    newExpiration = -1;
                }

                eventSinkId = header.Any.GetNodeValue("Identifier", WsWellKnownUri.WseNamespaceUri);

                if (eventSinkId == null)
                {
                    throw new XmlException();
                }
            }
            catch (XmlException e)
            {
                throw new WsFaultException(header, WsFaultType.WseInvalidMessage, e.ToString());
            }

            // Parse urn:uuid from the To address
            string endpointAddress = FixToAddress(header.To);

            // Iterate the list of hosted services at the specified endpoint and renew each subscription
            // with and event source that matches the eventSinkID
            DpwsWseEventSink eventSink;
            bool eventSinkFound = false;
            for (int i = 0; i < serviceEndpoints.Count; ++i)
            {
                if (serviceEndpoints[i].EndpointAddress == endpointAddress)
                {
                    if ((eventSink = GetEventSink(((DpwsHostedService)serviceEndpoints[i]).EventSources, eventSinkId)) != null)
                    {
                        eventSinkFound = true;

                        // Update event sink expires time
                        eventSink.Expires = newExpiration;
                    }
                }
            }

            // Generate Response
            if (eventSinkFound)
                return GetStatusResponse(header, newExpiration); // It's just like the GetStatus Response
            throw new WsFaultException(header, WsFaultType.WseEventSourceUnableToProcess, "Subscription was not found. ID=" + eventSinkId);
        }

        /// <summary>
        /// Event GetStatus stub.
        /// </summary>
        /// <param name="header">Header object.</param>
        /// <param name="reader">An XmlReader positioned at the begining of the GetStatus request body element.</param>
        /// <param name="serviceEndpoints">A Collection of serviceEndpoints used to determine what services contain the specified event.</param>
        /// <returns>Byte array containing an GetStatus response.</returns>
        /// <remarks>This method is used by the stack framework. Do not use this method.</remarks>
        public byte[] GetStatus(WsWsaHeader header, XmlReader reader, WsServiceEndpoints serviceEndpoints)
        {
            // Parse GetStatus Request
            ///////////////////////////////
            // there's no info in GetStatus that we actually need, just get the identifier from header
            String eventSinkID = header.Any.GetNodeValue("Identifier", WsWellKnownUri.WseNamespaceUri);

            // Iterate the list of hosted services at the specified endpoint and get the status of the first
            // subscription matching the eventSink. Not pretty but eventing and shared service endpoints don't
            // fit together
            if (eventSinkID != null)
            {
                // Parse urn:uuid from the To address
                string endpointAddress = FixToAddress(header.To);

                for (int i = 0; i < serviceEndpoints.Count; ++i)
                {
                    if (serviceEndpoints[i].EndpointAddress == endpointAddress)
                    {
                        DpwsWseEventSink eventSink;
                        if ((eventSink = GetEventSink(((DpwsHostedService)serviceEndpoints[i]).EventSources, eventSinkID)) != null)
                        {
                            long timeRemaining = DateTime.Now.Ticks - (eventSink.StartTime + eventSink.Expires);
                            timeRemaining = timeRemaining < 0 ? 0 : timeRemaining;

                            return GetStatusResponse(header, timeRemaining);
                        }
                    }
                }
            }

            // Something went wrong
            throw new WsFaultException(header, WsFaultType.WseEventSourceUnableToProcess);
        }

        /// <summary>
        /// Iterates a collection of event sources an looks for an event sink by ID.
        /// </summary>
        /// <param name="eventSources">A collection of event sources.</param>
        /// <param name="eventSinkID">An event sink ID.</param>
        /// <returns>An event sink object if found otherwise null.</returns>
        private DpwsWseEventSink GetEventSink(DpwsWseEventSources eventSources, string eventSinkID)
        {
            DpwsWseEventSink eventSink;
            // Look for matching event in hosted services event sources
            int count = eventSources.Count;
            for (int i = 0; i < count; i++)
            {
                if ((eventSink = eventSources[i].EventSinks[eventSinkID]) != null)
                {
                    return eventSink;
                }
            }

            return null;
        }

        private byte[] GetStatusResponse(WsWsaHeader header, long newDuration)
        {
            MemoryStream soapStream = new MemoryStream();
            XmlWriter xmlWriter = XmlWriter.Create(soapStream);

            WsWsaHeader responseHeader = new WsWsaHeader(
                WsWellKnownUri.WseNamespaceUri + "/RenewResponse",      // Action
                header.MessageID,                                       // RelatesTo
                header.ReplyTo.Address.AbsoluteUri,                     // To
                null, null, null);                                      // ReplyTo, From, Any

            WsSoapMessageWriter.WriteSoapMessageStart(xmlWriter,
                WsSoapMessageWriter.Prefixes.Wse,                       // Prefix
                null,                                                   // Additional Prefix
                responseHeader,                                         // Header
                new WsSoapMessageWriter.AppSequence(Device.AppSequence, Device.SequenceID, Device.MessageID)); // AppSequence

            // write body
            xmlWriter.WriteStartElement("wse", "Expires", null);
            xmlWriter.WriteString(new WsDuration(newDuration).DurationString);
            xmlWriter.WriteEndElement(); // End Expires

            WsSoapMessageWriter.WriteSoapMessageEnd(xmlWriter);

            // Flush and close writer. Return stream buffer
            xmlWriter.Flush();
            xmlWriter.Close();
            return soapStream.ToArray();
        }
    }
}


