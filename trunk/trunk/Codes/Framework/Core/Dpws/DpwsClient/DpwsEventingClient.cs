using System;
using System.Collections;
using System.Text;
using System.Xml;
using Ws.Services;
using Ws.Services.WsaAddressing;
using Ws.Services.Soap;
using Ws.Services.Transport.HTTP;
using Ws.Services.Utilities;
using Ws.Services.Xml;

using System.Ext;
using System.Ext.Xml;
using Microsoft.SPOT;
using System.IO;

namespace Dpws.Client.Eventing
{
    /// <summary>
    /// Subscribe method parameter class.
    /// </summary>
    /// <remarks>This class contains proerties used when subscribing to events.</remarks>
    public class DpwsSubscribeRequest
    {
        /// <summary>
        /// Creates an instance a DpwsSubscribeRequest class initialized with the service endpoint Address,
        /// notifyTo callback endpoint address and event duration.
        /// </summary>
        /// <param name="subscriptionType">A DpwsServieType object containing a definition of the service being subscripbed to.</param>
        /// <param name="endpointAddress">A string containing an event source endpoint address.</param>
        /// <param name="notifyToAddress">A string containing the notifyTo endpoint address.</param>
        /// <param name="expires">
        /// A string containing the subscription expiration time in duration format. If
        /// null the event does not expire.
        /// </param>
        /// <param name="identifier">A WsWsaRefParamter object containing a unique identifier.
        /// This value will be included in an event messages soap header as a reference parameter.
        /// This value is not processed by a service it is intended to provide a unique identified
        /// a client can use for any purpose.</param>
        /// <remarks>
        /// This constructor sets the endTo address to null indicating that the notifyTo address should receive
        /// subscription end messages. A user ID for the event is not set by default.
        /// </remarks>
        /// <exception cref="ArgumentException">If duration format is invalid.</exception>
        public DpwsSubscribeRequest(DpwsServiceType subscriptionType, string endpointAddress, string notifyToAddress, string expires, WsXmlNode identifier)
        {
            if (endpointAddress == null)
                throw new ArgumentNullException("enpointAddress must not be null.");
            if (notifyToAddress == null)
                throw new ArgumentNullException("notifyTo must not be null.");

            this.SubscriptionType = subscriptionType;
            this.EndpointAddress = new Uri(endpointAddress);

            this.NotifyTo = new WsWsaEndpointRef(new Uri(notifyToAddress));
            this.EndTo = new WsWsaEndpointRef(new Uri(notifyToAddress));

            if (identifier != null)
            {
                this.NotifyTo.RefProperties.Add(identifier);
                this.EndTo.RefProperties.Add(identifier);
            }

            if (expires != null)
                this.Expires = new WsDuration(expires);
        }

        /// <summary>
        /// Property containing a DpwsServiceType object that defines the event service type of the subscription.
        /// </summary>
        public readonly DpwsServiceType SubscriptionType;

        /// <summary>
        /// A property containing the endpoint address of a device service that exposes the event source you want
        /// to subscribe to.
        /// </summary>
        /// <remarks>
        /// The endpoint address must contain an http Uri for the service that exposes the event. This address
        /// must contain an ip address of the device that contains the service and the service id or the service
        /// that contains the event source (i.e. http://ip_address:port/service_id_guid).
        /// </remarks>
        public readonly Uri EndpointAddress;

        /// <summary>
        /// A property containing an endpoint reference that specifies where the service is to send an event.
        /// The address field of the endpoint reference must contain an http Uri that defines an ip address and
        /// a guid that specifies a unique endpoint of an event sink that will handle the request. User defined
        /// reference properties and parameters can be added to this endpoint.
        /// </summary>
        public readonly WsWsaEndpointRef NotifyTo;

        /// <summary>
        /// A property containing an endpoint reference that specifies where the service is to send subscription
        /// end events. The address field of the endpoint reference must contain an http Uri that defines an
        /// ip address and a guid that specifies a unique endpoint of an event sink that will handle the request.
        /// User defined reference properties and parameters can be added to this endpoint. If this proerty is null
        /// the service will send subscription end notifications to the notifyTo address.
        /// </summary>
        public WsWsaEndpointRef EndTo;

        /// <summary>
        /// A property contining the desired duration of a subscription. The service may assign a different value.
        /// This stack supports duration formats only.
        /// </summary>
        public readonly WsDuration Expires;
    }

    /// <summary>
    /// Dpws event subscription class.
    /// </summary>
    public class DpwsEventSubscription
    {
        /// <summary>
        /// Creates an instance of a DpwsEventSubscription class.
        /// </summary>
        internal DpwsEventSubscription(XmlReader reader)
        {
            reader.ReadStartElement("SubscribeResponse", WsWellKnownUri.WseNamespaceUri);

            if (reader.IsStartElement("SubscriptionManager", WsWellKnownUri.WseNamespaceUri))
            {
                this.SubscriptionManager = new WsWsaEndpointRef(reader);

                this.Expires = new WsDuration(reader.ReadElementString("Expires", WsWellKnownUri.WseNamespaceUri));

                WsXmlNode identifier = this.SubscriptionManager.RefParameters.GetNode("Identifier", WsWellKnownUri.WseNamespaceUri);
                this.SubscriptionID = identifier.Value;

                if (this.SubscriptionID == null)
                {
                    throw new XmlException();
                }
            }
            else
            {
                throw new XmlException();
            }
        }

        /// <summary>
        /// Property used to store the Expires duration returned from a subscribe request.
        /// The value may be different that the expiration time sent in te original request.
        /// </summary>
        public readonly WsDuration Expires;

        /// <summary>
        /// A property containing a uniue identifier assigned by the subscription manager for this event.
        /// This ID is required for later unsubscribe, renew and get status request.
        /// </summary>
        public readonly string SubscriptionID;

        /// <summary>
        /// Property used to store the endpoint reference of the subscription manager.
        /// </summary>
        public readonly WsWsaEndpointRef SubscriptionManager;
    }

    /// <summary>
    /// Class used by a Dpws client to control device event subscriptions.
    /// </summary>
    /// <remarks>
    /// A DPWS client uses this class to subscribe to, Unsubscribe from, Renew a subscription to and
    /// get the status of an event subscription.
    /// </remarks>
    public class DpwsEventingClient
    {
        /// <summary>
        /// Creates an instance of a DpwsEventingClient class.
        /// </summary>
        public DpwsEventingClient()
        {
        }

        /// <summary>
        /// Use to subscribe to a devices, hosted service event sources.
        /// </summary>
        /// <param name="subscriptionRequest">
        /// A DpwsSubscriptionRequest object containing the address of the service hosting the desired event,
        /// the address where the event is sent, an optional address where subscription end messages are sent,
        /// A subscription expiration (in duration format) and an optional user defined identifier.
        /// </param>
        /// <returns>
        /// A DpwsEventSubscription containing the the subscription managers address, the time when the subscription
        /// expires (duration) and optional reference parameters and properties. Per spec the
        /// sub mananger may assign a different duration value than that specified in the request.
        /// </returns>
        /// <exception cref="ArgumentNullException">If required subscription parameters are not set.</exception>
        public DpwsEventSubscription Subscribe(DpwsSubscribeRequest subscriptionRequest)
        {
            if (subscriptionRequest.SubscriptionType == null)
                throw new ArgumentNullException("Subscribe - SubscriptionType must not be null");
            if (subscriptionRequest.SubscriptionType.TypeName == null)
                throw new ArgumentNullException("Subscribe - SubscriptionType.TypeName must not be null");
            if (subscriptionRequest.SubscriptionType.NamespaceUri == null)
                throw new ArgumentNullException("Subscribe - SubscriptionType.NamespaceUri must not be null");
            if (subscriptionRequest.EndpointAddress == null)
                throw new ArgumentNullException("Subscribe - EndpointAddress property must not be null.");
            if (subscriptionRequest.NotifyTo == null)
                throw new ArgumentNullException("Subscribe - NotifyTo must not be null.");
            if (subscriptionRequest.NotifyTo.Address == null)
                throw new ArgumentNullException("Subscribe - NotifyTo.Address must not be null.");

            // Convert the address string to a Uri
            Uri serviceUri = null;
            try
            {
                serviceUri = subscriptionRequest.EndpointAddress;
                if (serviceUri.Scheme != "http")
                {
                    System.Ext.Console.Write("");
                    System.Ext.Console.Write("Invalid endpoint address. Must be a Uri. Http Uri schemes only.");
                    System.Ext.Console.Write("");
                    return null;
                }
            }
            catch (Exception e)
            {
                System.Ext.Console.Write("");
                System.Ext.Console.Write(e.Message);
                System.Ext.Console.Write("");
                return null;
            }

            // Performance debugging
            DebugTiming timeDebuger = new DebugTiming();
            long startTime = timeDebuger.ResetStartTime("");

            // Build Subscribe Request
            MemoryStream soapStream = new MemoryStream();
            XmlWriter xmlWriter = XmlWriter.Create(soapStream);

            WsWsaHeader header = new WsWsaHeader(
                WsWellKnownUri.WseNamespaceUri + "/Subscribe",  // Action
                null,                                           // RelatesTo
                serviceUri.AbsoluteUri,                         // To
                WsWellKnownUri.WsaAnonymousUri,                 // ReplyTo
                null, null);                                    // From, Any

            WsXmlNamespaces additionalPrefixes = new WsXmlNamespaces();
            additionalPrefixes.Add(new WsXmlNamespace("myPrefix", subscriptionRequest.SubscriptionType.NamespaceUri));

            String messageID = WsSoapMessageWriter.WriteSoapMessageStart(xmlWriter,
                WsSoapMessageWriter.Prefixes.Wsd | WsSoapMessageWriter.Prefixes.Wse,
                additionalPrefixes, header, null);

            // Performance debuging
            timeDebuger.PrintElapsedTime("*****Write Header Took");

            // write body
            xmlWriter.WriteStartElement("wse", "Subscribe", null);
            
            // If EndTo is set write it
            if (subscriptionRequest.EndTo != null)
                WriteEndpointRef(ref xmlWriter, subscriptionRequest.EndTo, "EndTo");

            // Add the delivery element and NotifyTo endpoint
            xmlWriter.WriteStartElement("wse", "Delivery", null);
            xmlWriter.WriteAttributeString("Mode", "http://schemas.xmlsoap.org/ws/2004/08/eventing/DeliveryModes/Push");

            // Writer the notify to endpoint
            WriteEndpointRef(ref xmlWriter, subscriptionRequest.NotifyTo, "NotifyTo");

            xmlWriter.WriteEndElement(); // End Delivery

            // Write Expiration time
            if (subscriptionRequest.Expires != null)
            {
                xmlWriter.WriteStartElement("wse", "Expires", null);
                xmlWriter.WriteString(subscriptionRequest.Expires.DurationString);
                xmlWriter.WriteEndElement(); // End Expires
            }

            // Write Filter element specifying the event to subscribe to.
            xmlWriter.WriteStartElement("wse", "Filter", null);
            xmlWriter.WriteAttributeString("Dialect", "http://schemas.xmlsoap.org/ws/2006/02/devprof/Action");
            xmlWriter.WriteString(subscriptionRequest.SubscriptionType.NamespaceUri + "/" + subscriptionRequest.SubscriptionType.TypeName);
            xmlWriter.WriteEndElement(); // End Filter

            xmlWriter.WriteEndElement(); // End Subscribe

            WsSoapMessageWriter.WriteSoapMessageEnd(xmlWriter);

            // Performance debuging
            timeDebuger.PrintElapsedTime("*****Write Body Took");

            // Performance debuging
            timeDebuger.PrintTotalTime(startTime, "***Subscribe Message Build Took");

            // Flush and close writer
            xmlWriter.Flush();
            xmlWriter.Close();

            // Create an Http client and send Subscribe request
            WsHttpClient httpClient = new WsHttpClient();
            byte[] subscribeResponse = null;
            try
            {
                subscribeResponse = httpClient.SendRequest(soapStream.ToArray(), subscriptionRequest.EndpointAddress.AbsoluteUri, false, false);
            }
            catch (Exception e)
            {
                System.Ext.Console.Write("");
                System.Ext.Console.Write("Subscribe failed. " + e.Message);
                return null;
            }

            // If a subscribe response is received process it and return expiration time the subscription manager
            // actually assigned.
            // If a parsing fault is received print exception and go on.
            DpwsEventSubscription response = null;
            if (subscribeResponse == null)
            {
                System.Ext.Console.Write("");
                System.Ext.Console.Write("Subscribe response is null.");
                return null;
            }
            else
            {
                System.Ext.Console.Write("");
                System.Ext.Console.Write("Response From: " + subscriptionRequest.EndpointAddress.Host);
                System.Ext.Console.Write(new String(System.Text.Encoding.UTF8.GetChars(subscribeResponse)));
                try
                {
                    // It is ok for the service to return a 202 and a 0 length response
                    // if thi is the case just return null
                    if (subscribeResponse.Length == 0)
                        return null;
                    response = ProcessSubscribeResponse(subscribeResponse, messageID);
                }
                catch (Exception e)
                {
                    System.Ext.Console.Write("");
                    System.Ext.Console.Write("Subscription response parsing failed.");
                    System.Ext.Console.Write(e.Message);
                }
            }

            return response;
        }

        /// <summary>
        /// Method parses an event subscription response and returns the duratoin returned in the response.
        /// </summary>
        /// <param name="subscribeResponse">A byte array containing a subscribe response message.</param>
        /// <param name="messageID">
        /// A string containing the original message ID passed in the request. This id is used to validate
        /// that this is a response to the original request.
        /// </param>
        /// <returns>A DpwsEventSubscription object containing the expiration time actually set by a subscription manager.</returns>
        /// <exception cref="XmlException">If parsing errors are detected.</exception>
        /// <exception cref="InvalidOperationException">If a fault response is received.</exception>
        private DpwsEventSubscription ProcessSubscribeResponse(byte[] subscribeResponse, string messageID)
        {
            XmlReader reader = ProcessResponse(subscribeResponse, messageID, "SubscribeResponse");

            try
            {
                return new DpwsEventSubscription(reader);
            }
            finally
            {
                reader.Close();
            }
        }

        private XmlReader ProcessResponse(byte[] response, String messageID, String action)
        {
            WsWsaHeader header;
            XmlReader reader;

            reader = WsSoapMessageParser.ParseSoapMessage(response, out header);

            try
            {
                if (
                    (header.Action == WsWellKnownUri.WsaNamespaceUri_2004_08 + "/fault") ||
                    (header.Action == WsWellKnownUri.WsaNamespaceUri_2005_08 + "/fault")
                    )
                {
                    WsFault.ParseFaultResponseAndThrow(reader);
                }
                else if (header.Action != WsWellKnownUri.WseNamespaceUri + "/" + action)
                {
                    throw new XmlException();
                }

                // Make sure this response matches the request
                if (header.RelatesTo != messageID)
                    throw new XmlException("Invalid message ID in response. Id does not match request ID.");

                return reader;
            }
            catch
            {
                // if something's wrong, close the reader, and rethrow the exception
                reader.Close();
                throw;
            }
        }

        /// <summary>
        /// Method used to write an endpoint reference and reference parameters or properties.
        /// </summary>
        /// <param name="xmlWriter">An XmlWriter used to write the endpoint reference.</param>
        /// <param name="endpointRef">A WsWsaEndpointRef containing the information to write.</param>
        private void WriteEndpointRef(ref XmlWriter xmlWriter, WsWsaEndpointRef endpointRef, String name)
        {
            xmlWriter.WriteStartElement("wse", name, null);
            xmlWriter.WriteStartElement("wsa", "Address", null);
            xmlWriter.WriteString(endpointRef.Address.AbsoluteUri);
            xmlWriter.WriteEndElement(); // End Address

            // if ref paramters write them
            if (endpointRef.RefParameters != null && endpointRef.RefParameters.Count > 0)
            {
                xmlWriter.WriteStartElement("wsa", "ReferenceParameters", null);

                // Iterate ref parameters
                int count = endpointRef.RefParameters.Count;
                for (int i = 0; i < count; i++)
                {
                    WsXmlNode refParam = endpointRef.RefParameters[i];
                    // Write the element name
                    xmlWriter.WriteStartElement(refParam.Prefix, refParam.LocalName, refParam.NamespaceURI);
                    // Write the value
                    xmlWriter.WriteString(refParam.Value);
                    xmlWriter.WriteEndElement(); // End param element
                }

                xmlWriter.WriteEndElement(); // End ReferenceParameters
            }

            // if ref properties write them
            if (endpointRef.RefProperties != null && endpointRef.RefProperties.Count > 0)
            {
                xmlWriter.WriteStartElement("wsa", "ReferenceProperties", null);

                // Iterate ref parameters
                int count = endpointRef.RefProperties.Count;
                for (int i = 0; i < count; i++)
                {
                    WsXmlNode refProp = endpointRef.RefProperties[i];
                    // Write the element name
                    xmlWriter.WriteStartElement(refProp.Prefix, refProp.LocalName, refProp.NamespaceURI);
                    // Write the value
                    xmlWriter.WriteString(refProp.Value);
                    xmlWriter.WriteEndElement(); // End property element
                }

                xmlWriter.WriteEndElement(); // End ReferenceProperties
            }

            xmlWriter.WriteEndElement(); // End EndpointReference

            return;
        }

        /// <summary>
        /// Use to unsubscribe from a devices event source.
        /// </summary>
        /// <param name="endpointAddress">
        /// A Uri containing the endpoint address of the service or subscription manager that is currently
        /// maintaining this event subscription on behalf of the device. This address is an http uri
        /// (i.e. http://ip_address:port/serviceID).
        /// </param>
        /// <param name="subscription">An event subscription returned from a previous subscribe call.
        /// The subscription contains among other things a subscription ID used by the subscription manager
        /// to identify a specific event source subscription and the endpoint address of the subscription manager.
        /// </param>
        /// <returns>True if the Unsubscribe request was successful.</returns>
        public bool Unsubscribe(Uri endpointAddress, DpwsEventSubscription subscription)
        {
            // Performance debugging
            DebugTiming timeDebuger = new DebugTiming();
            long startTime = timeDebuger.ResetStartTime("");

            // Build Unsubscribe Request
            MemoryStream soapStream = new MemoryStream();
            XmlWriter xmlWriter = XmlWriter.Create(soapStream);

            WsXmlNodeList nodeList = new WsXmlNodeList();
            nodeList.Add(new WsXmlNode(null, "identifier", WsWellKnownUri.WseNamespaceUri, subscription.SubscriptionID));

            WsWsaHeader header = new WsWsaHeader(
                WsWellKnownUri.WseNamespaceUri + "/Unsubscribe",            // Action
                null,                                                       // RelatesTo
                endpointAddress.AbsoluteUri,                                // To
                WsWellKnownUri.WsaAnonymousUri,                             // ReplyTo
                subscription.SubscriptionManager.Address.AbsoluteUri,       // From
                nodeList);                                                  // Identifier

            String messageID = WsSoapMessageWriter.WriteSoapMessageStart(xmlWriter,
                WsSoapMessageWriter.Prefixes.Wse, null, header, null);

            // Performance debuging
            timeDebuger.PrintElapsedTime("*****Write Header Took");

            // write body
            xmlWriter.WriteStartElement("wse", "Unsubscribe", null);
            xmlWriter.WriteString("");
            xmlWriter.WriteEndElement(); // End Unsubscribe

            // Performance debuging
            timeDebuger.PrintElapsedTime("*****Write Body Took");

            WsSoapMessageWriter.WriteSoapMessageEnd(xmlWriter);

            // Performance debuging
            timeDebuger.PrintTotalTime(startTime, "***Unsubscribe Message Build Took");

            // Flush and close writer
            xmlWriter.Flush();
            xmlWriter.Close();

            // Create an Http client and send Unsubscribe request
            WsHttpClient httpClient = new WsHttpClient();
            byte[] unsubscribeResponse = null;
            try
            {
                unsubscribeResponse = httpClient.SendRequest(soapStream.ToArray(), endpointAddress.ToString(), false, false);
            }
            catch (Exception e)
            {
                System.Ext.Console.Write("");
                System.Ext.Console.Write("Unsubscribe failed. " + e.Message);
                return false;
            }

            // If a unsubscribe response is received simple validate that the messageID and action are correct and
            // If a parsing fault is received print exception and go on.
            if (unsubscribeResponse == null)
            {
                System.Ext.Console.Write("");
                System.Ext.Console.Write("Unsubscribe response is null.");
                return false;
            }
            else
            {
                System.Ext.Console.Write("");
                System.Ext.Console.Write("Response From: " + endpointAddress.Host);
                System.Ext.Console.Write(new String(System.Text.Encoding.UTF8.GetChars(unsubscribeResponse)));
                try
                {
                    return ProcessUnsubscribeResponse(unsubscribeResponse, messageID);
                }
                catch (Exception e)
                {
                    System.Ext.Console.Write("");
                    System.Ext.Console.Write("Unsubscribe response parsing failed.");
                    System.Ext.Console.Write(e.Message);
                    return false;
                }
            }
        }

        /// <summary>
        /// Method parses an unsubscribe response and returns.
        /// </summary>
        /// <param name="unsubscribeResponse">A byte array containing an unsubscribe response message.</param>
        /// <param name="messageID">
        /// A string containing the original message ID passed in the request. This id is used to validate
        /// that this is a response to the original request.
        /// </param>
        /// <returns>True is parsing is successful.</returns>
        /// <exception cref="XmlException">
        /// If header or envelope parsing fails, If a required message tag is missing or an invalid or missing
        /// namespace is found.
        /// </exception>
        private bool ProcessUnsubscribeResponse(byte[] unsubscribeResponse, string messageID)
        {
            XmlReader reader = ProcessResponse(unsubscribeResponse, messageID, "UnsubscribeResponse");
            reader.Close();

            return true;
        }

        /// <summary>
        /// Use to renew an existing subscription.
        /// </summary>
        /// <param name="endpointAddress">
        /// A Uri containing the endpoint address of the service or subscription manager that is currently
        /// maintaining this event subscription on behalf of the device. This address is an http uri
        /// (i.e. http://ip_address:port/serviceID).
        /// </param>
        /// <param name="subscriptionID">
        /// A subscription ID returned from a previous subscribe response. The device uses this ID
        /// to identify a specific event source subscription.
        /// </param>
        /// <param name="expires">
        /// A WsDuration object indicating the new duration time for this event subscription, null = infinite.
        /// </param>
        /// <returns>
        /// A WsDuration object containing the new Expires time actually asigned by the device service to this
        /// event subscription, null = infinite.
        /// </returns>
        public WsDuration Renew(Uri endpointAddress, String subscriptionID, WsDuration expires)
        {
            // Performance debugging
            DebugTiming timeDebuger = new DebugTiming();
            long startTime = timeDebuger.ResetStartTime("");

            // Build Renew request
            MemoryStream soapStream = new MemoryStream();
            XmlWriter xmlWriter = XmlWriter.Create(soapStream);

            WsXmlNodeList nodeList = new WsXmlNodeList();
            nodeList.Add(new WsXmlNode("wse", "Identifier", null, subscriptionID));

            WsWsaHeader header = new WsWsaHeader(
                WsWellKnownUri.WseNamespaceUri + "/Renew",  // Action
                null,                                       // RelatesTo
                endpointAddress.AbsoluteUri,                // To
                null, null, nodeList);                      // ReplyTo, From, Any

            String messageID = WsSoapMessageWriter.WriteSoapMessageStart(xmlWriter,
                WsSoapMessageWriter.Prefixes.Wse, null, header, null);

            // Performance debuging
            timeDebuger.PrintElapsedTime("*****Write Header Took");

            // write body
            xmlWriter.WriteStartElement("wse", "Renew", null);
            xmlWriter.WriteStartElement("wse", "Expires", null);
            xmlWriter.WriteString(expires.DurationString);
            xmlWriter.WriteEndElement(); // End Expires
            xmlWriter.WriteEndElement(); // End Renew

            WsSoapMessageWriter.WriteSoapMessageEnd(xmlWriter);

            // Performance debuging
            timeDebuger.PrintElapsedTime("*****Write Body Took");

            // Performance debuging
            timeDebuger.PrintTotalTime(startTime, "***Renew Message Build Took");

            // Flush and close writer
            xmlWriter.Flush();
            xmlWriter.Close();

            // Create an Http client and send Unsubscribe request
            WsHttpClient httpClient = new WsHttpClient();
            byte[] renewResponse = null;
            try
            {
                renewResponse = httpClient.SendRequest(soapStream.ToArray(), endpointAddress.ToString(), false, false);
            }
            catch (Exception e)
            {
                System.Ext.Console.Write("");
                System.Ext.Console.Write("Renew failed. " + e.Message);
                return null;
            }

            // If a renew response is received validate the messageID and action and get the new expiration time.
            // If a parsing fault is received print exception and go on.
            if (renewResponse == null)
            {
                System.Ext.Console.Write("");
                System.Ext.Console.Write("Renew response is null.");
                return null;
            }
            else
            {
                System.Ext.Console.Write("");
                System.Ext.Console.Write("Response From: " + endpointAddress.Host);
                System.Ext.Console.Write(new String(System.Text.Encoding.UTF8.GetChars(renewResponse)));
                try
                {
                    return ProcessRenewResponse(renewResponse, messageID);
                }
                catch (Exception e)
                {
                    System.Ext.Console.Write("");
                    System.Ext.Console.Write("Unsubscribe response parsing failed.");
                    System.Ext.Console.Write(e.Message);
                }
            }

            return null;
        }

        /// <summary>
        /// Method parses a subscription renewal response and returns.
        /// </summary>
        /// <param name="renewResponse">A byte array containing a renew response message.</param>
        /// <param name="messageID">
        /// A string containing the original message ID passed in the request. This id is used to validate
        /// that this is a response to the original request.
        /// </param>
        /// <returns>A WsDuration object containing the new expiration time for the event.</returns>
        /// <exception cref="XmlException">
        /// If header or envelope parsing fails, If a required message tag is missing or an invalid or missing
        /// namespace is found.
        /// </exception>
        private WsDuration ProcessRenewResponse(byte[] renewResponse, string messageID)
        {
            XmlReader reader = ProcessResponse(renewResponse, messageID, "RenewResponse");

            try
            {
                reader.ReadStartElement("RenewResponse", WsWellKnownUri.WseNamespaceUri);

                WsDuration expires = null;

                if (reader.IsStartElement("Expires", WsWellKnownUri.WseNamespaceUri))
                {
                    expires = new WsDuration(reader.ReadElementString());
                }

                return expires;
            }
            finally
            {
                reader.Close();
            }
        }

        /// <summary>
        /// Use to get the status of an event subscription.
        /// </summary>
        /// <param name="endpointAddress">
        /// A Uri containing the endpoint address of the service or subscription manager that is currently
        /// maintaining this event subscription on behalf of the device. This address is an http uri
        /// (i.e. http://ip_address:port/serviceID).
        /// </param>
        /// <param name="subscriptionID">
        /// A subscription ID returned from a previous subscribe response. The device uses this ID
        /// to identify a specific event source subscription.
        /// </param>
        /// <returns>
        /// A WsDuration object containing the remaining subscription time for this event subscription, null = infinite.
        /// </returns>
        public WsDuration GetStatus(Uri endpointAddress, string subscriptionID)
        {
            // Performance debugging
            DebugTiming timeDebuger = new DebugTiming();
            long startTime = timeDebuger.ResetStartTime("");

            // Build Renew request
            MemoryStream soapStream = new MemoryStream();
            XmlWriter xmlWriter = XmlWriter.Create(soapStream);

            WsXmlNodeList nodeList = new WsXmlNodeList();
            nodeList.Add(new WsXmlNode("wse", "Identifier", null, subscriptionID));

            WsWsaHeader header = new WsWsaHeader(
                WsWellKnownUri.WseNamespaceUri + "/GetStatus",  // Action
                null,                                           // RelatesTo
                endpointAddress.AbsoluteUri,                    // To
                null, null, nodeList);                          // ReplyTo, From, Any

            String messageID = WsSoapMessageWriter.WriteSoapMessageStart(xmlWriter,
                WsSoapMessageWriter.Prefixes.Wse, null, header, null);

            // Performance debuging
            timeDebuger.PrintElapsedTime("*****Write Header Took");

            // write body
            xmlWriter.WriteStartElement("soap", "Body", null);
            xmlWriter.WriteStartElement("wse", "GetStatus", null);
            xmlWriter.WriteString("");
            xmlWriter.WriteEndElement(); // End GetStatus

            // Performance debuging
            timeDebuger.PrintElapsedTime("*****Write Body Took");

            WsSoapMessageWriter.WriteSoapMessageEnd(xmlWriter);

            // Performance debuging
            timeDebuger.PrintTotalTime(startTime, "***Renew Message Build Took");

            // Flush and close writer
            xmlWriter.Flush();
            xmlWriter.Close();

            // Create an Http client and send GetStatus request
            WsHttpClient httpClient = new WsHttpClient();
            byte[] getStatusResponse = null;
            try
            {
                getStatusResponse = httpClient.SendRequest(soapStream.ToArray(), endpointAddress.ToString(), false, false);
            }
            catch (Exception e)
            {
                System.Ext.Console.Write("");
                System.Ext.Console.Write("GetStatus failed. " + e.Message);
                return null;
            }

            // If a GetStatus response is received validate the messageID and action and get the remaining
            // event subscription time. If a fault is received print exception and go on.
            if (getStatusResponse == null)
            {
                System.Ext.Console.Write("");
                System.Ext.Console.Write("Renew response is null.");
                return null;
            }
            else
            {
                System.Ext.Console.Write("");
                System.Ext.Console.Write("Response From: " + endpointAddress.Host);
                System.Ext.Console.Write(new String(System.Text.Encoding.UTF8.GetChars(getStatusResponse)));

                // Note: Since the response is the same for GetStatus ans it is for Renew reuse the
                // Renew response parser.
                try
                {
                    return ProcessRenewResponse(getStatusResponse, messageID);
                }
                catch (Exception e)
                {
                    System.Ext.Console.Write("");
                    System.Ext.Console.Write("Unsubscribe response parsing failed.");
                    System.Ext.Console.Write(e.Message);
                }
            }

            return null;
        }
    }
}


