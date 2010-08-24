using System;
using System.Collections;
using System.Xml;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Dpws.Client.Discovery;
using Dpws.Client.Eventing;
using Ws.Services;
using Ws.Services.Faults;
using Ws.Services.Mtom;
using Ws.Services.Soap;
using Ws.Services.Transport;
using Ws.Services.Transport.HTTP;
using Ws.Services.Transport.UDP;
using Ws.Services.Utilities;
using Ws.Services.WsaAddressing;
using Ws.Services.Xml;

using System.Ext;
using System.Ext.Xml;
using Microsoft.SPOT;

namespace Dpws.Client
{
    /// <summary>
    /// Base class used by a dpws client applicaiton or control developer to implement a dpws device client.
    /// Dpws client implementations must derive from this class.
    /// </summary>
    public class DpwsClient : IWsServiceEndpoint, IDisposable
    {
        private object m_threadLock = new object();
        private DpwsDiscoveryClient m_discoClient = null;
        private string m_endpointAddress = null;
        private DpwsEventingClient m_eventClient = new DpwsEventingClient();
        private DpwsMexClient m_mexClient = new DpwsMexClient();
        private WsServiceOperations m_eventCallbacks = new WsServiceOperations();
        private string m_transportAddress = null;
        private bool m_blockingCall = true;
        private WsMtomBodyParts m_bodyParts = null;
        private WsMessageType m_messageType = WsMessageType.Soap;
        private static WsServiceEndpoints m_discoServiceEndpoints = new WsServiceEndpoints();
        private static WsServiceEndpoints m_callbackServiceEndpoints = new WsServiceEndpoints();
        private WsUdpServiceHost m_udpSeviceHost = null;
        private WsHttpServiceHost m_httpServiceHost = null;
        private int m_port = 15373;
        private bool m_ignoreRequestFromThisIP = true;

        /// <summary>
        /// Creates an instance of a DpwsClient class.
        /// </summary>
        public DpwsClient()
        {
            Init();
        }

        /// <summary>
        /// Creates an instance of a DpwsClient class with a specified eventing callback port number.
        /// </summary>
        public DpwsClient(int port)
        {
            m_port = port;
            Init();
        }

        /// <summary>
        /// This class is an internal callback method that provides DPWS compliant Hello discovery client services.
        /// An instance of this class will be created in the Init function. The stack runs a single service that
        /// handles Hello and Bye services.
        /// </summary>
        internal DpwsDiscoClientService ClientDiscoveryService = null;

        /// <summary>
        /// Use to get an instance of a Dpws eventing client.
        /// </summary>
        /// <remarks>Use this property to access WS-Discovery methods.</remarks>
        public DpwsEventingClient EventingClient { get { return m_eventClient; } }

        /// <summary>
        /// Property containing an instance of a Dpws Discovery client.
        /// </summary>
        public DpwsDiscoveryClient DiscoveryClient { get { return m_discoClient; } }

        /// <summary>
        /// Event property used to create and event handler for Dpws Hello messages.
        /// </summary>
        public event HelloEventHandler HelloEvent;

        /// <summary>
        /// Event property used to create an event handler for Dpws End messages.
        /// </summary>
        public event ByeEventHandler ByeEvent;

        /// <summary>
        /// Event property used to create an event handler for Ws-Eventing, SubscriptionEnd messages.
        /// </summary>
        public event SubscriptionEndEventHandler SubscriptionEndEvent;

        /// <summary>
        /// Property containing an instance of a Dpws Metadata client.
        /// </summary>
        /// <remarks>Use this property to access WS-Metadata exchange methods.</remarks>
        public DpwsMexClient MexClient { get { return m_mexClient; } }

        /// <summary>
        /// Property containing a collection of event sink callback methods.
        /// </summary>
        /// <remarks>Use this colleciotn to add or remove event operations (event handlers) for this client.</remarks>
        public WsServiceOperations ServiceOperations { get { return m_eventCallbacks; } }

        /// <summary>
        /// Internal callback used by DpwsDiscoClient to update DpwsDiscoService when the ws-discovery version changes.
        /// The method deletes the current DpwsDiscoService from the listening services collection and ads a new one
        /// that when created will have the new disco versino information.
        /// </summary>
        /// <param name="discoVersion">A DiscoVersion object containing the new ws-discovery version information.</param>
        internal void UpdateDiscoVersion()
        {
            m_discoServiceEndpoints.Remove(ClientDiscoveryService);
            ClientDiscoveryService = new DpwsDiscoClientService(this);
            m_discoServiceEndpoints.Add(ClientDiscoveryService);
        }
                
        private void Init()
        {
            m_discoClient = new DpwsDiscoveryClient(this);

            // Set the initial endpoint address
            m_endpointAddress = "urn:uuid:" + Guid.NewGuid();

            // Add the Hello and Bye discovery disco services
            ClientDiscoveryService = new DpwsDiscoClientService(this);
            m_discoServiceEndpoints.Add(ClientDiscoveryService);

            // Start a Udp discovery service host
            m_udpSeviceHost = new WsUdpServiceHost(m_discoServiceEndpoints);
            if (!m_udpSeviceHost.IsRunning)
            {
                m_udpSeviceHost.IgnoreRequestFromThisIP = m_ignoreRequestFromThisIP;
                m_udpSeviceHost.MaxThreadCount = 5;
                m_udpSeviceHost.Start();
                System.Ext.Console.Write("Udp service host started...");
            }
            else
                m_udpSeviceHost = null;

            // Add eventing SubscriptionEnd ServiceOperations. By default Subscription End call back
            // to this client
            ServiceOperations.Add(new WsServiceOperation(WsWellKnownUri.WseNamespaceUri, "SubscriptionEnd"));

            // Add callbacks implemented by this client
            m_callbackServiceEndpoints.Add(this);

            // Start the Http service host
            m_httpServiceHost = new WsHttpServiceHost(m_port, m_callbackServiceEndpoints);
            m_httpServiceHost.MaxThreadCount = m_callbackServiceEndpoints.Count;
            m_httpServiceHost.Start();
            System.Ext.Console.Write("Http service host started...");
        }

        /// <summary>
        /// Use to Get or Set a collection used to store Mtom bodyparts.
        /// </summary>
        /// <remarks>
        /// If the transport services receive an Mtom request. This collection contains the body parts parsed
        /// from the Mtom message. If a client needs to send an Mtom request they can populate this collection
        /// and set the ResponseType property to WsResponseType.Mtom to tell the transport services to parse this
        /// collectionand return and Mtom formatted response message.
        /// </remarks>
        public WsMtomBodyParts BodyParts { get { return m_bodyParts; } set { m_bodyParts = value; } }

        /// <summary>
        /// Property used to enable of disable receiving discovery request from this ip address.
        /// </summary>
        public bool IgnoreRequestFromThisIP
        {
            get
            {
                if (m_udpSeviceHost == null)
                    return m_ignoreRequestFromThisIP;
                else
                    return m_udpSeviceHost.IgnoreRequestFromThisIP;
            }

            set
            {
                if (m_udpSeviceHost == null)
                    m_ignoreRequestFromThisIP = value;
                else
                    m_udpSeviceHost.IgnoreRequestFromThisIP = value;
            }
        }

        /// <summary>
        /// Property used to get or set the local IPV4 address.
        /// </summary>
        protected string IPV4Address { get { return WsNetworkServices.GetLocalIPV4Address(); } }

        /// <summary>
        /// A urn:uuid used by the transport services to locate this client. This property represents the endpoint
        /// reference address of a client. This stack supports urn:uuid endpoint addresses only.
        /// </summary>
        /// <remarks>
        /// This property contains a unique endpoint reference address. The message dispatcher uses this address
        /// to route messages to methods implemented in a client at this address. The combined base transport address
        /// (http://ip_address:port/) and this property equal the Header.To property of messages directed to this client.
        /// </remarks>
        public string EndpointAddress
        {
            get
            {
                lock (m_threadLock)
                {
                    return m_endpointAddress;
                }
            }

            set
            {
                lock (m_threadLock)
                {
                    if (value == null)
                        throw new ArgumentNullException("DpwsClient.EndpointAddress", "must not be null");

                    try
                    {
                        Uri tempUri = new Uri(value);
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException("DpwsClient.EndpointAddress - Address must be a vaild Uri", e);
                    }

                    m_endpointAddress = value;
                    m_transportAddress = "http://" + WsNetworkServices.GetLocalIPV4Address() + ":" + m_port + "/" + m_endpointAddress.Substring(9);
                }
            }
        }

        /// <summary>
        /// Property used to identify the type of message sent or received.
        /// </summary>
        /// <remarks>
        /// The type of messages supported by the stack are Soap, Mtom and Fault.
        /// See BodyParts description. The default value of this property is WsResponseType.Soap.
        /// </remarks>
        public WsMessageType MessageType { get { return m_messageType; } set { m_messageType = value; } }

        /// <summary>
        /// Property containing the port used to listen for events.
        /// </summary>
        public int EventingPort { get { return m_port; } }

        /// <summary>
        /// Method used by the discovery callback methods to raise a Hello event
        /// </summary>
        /// <param name="helloEventArgs">A HelloEventArgs object containing hello event details.</param>
        internal void RaiseHelloEvent(DpwsServiceDescription helloEventArgs)
        {
            if (HelloEvent != null)
            {
                HelloEvent(this, helloEventArgs);
            }
        }

        /// <summary>
        /// Method used by the discovery callback methods to raise a Bye event
        /// </summary>
        /// <param name="byeEventArgs">A HelloEventArgs object containing hello event details.</param>
        internal void RaiseByeEvent(DpwsServiceDescription byeEventArgs)
        {
            ByeEvent(this, byeEventArgs);
        }

        /// <summary>
        /// Read only property containing a Dpws compliant transport address.
        /// </summary>
        /// <remarks>
        /// The only transport address format supportd by this stack is:
        /// http://(device ip address):(device port)/(device.address - urn:uuid prefix)
        /// </remarks>
        public string TransportAddress
        {
            get
            {
                lock (m_threadLock)
                {
                    return m_transportAddress;
                }
            }
        }

        /// <summary>
        /// Releases all resources used by the DpwsClients object and removes this instance from
        /// the list of stack service processes.
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Stop tranport services and releases the managed resources used by this class.
        /// </summary>
        /// <param name="disposing">True to release managed resources</param>
        internal void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Stop the transport services
                m_httpServiceHost.Stop();
                m_udpSeviceHost.Stop();

                m_threadLock = null;
                m_discoClient = null;
                m_endpointAddress = null;
                m_eventClient = null;
                m_mexClient = null;
                m_eventCallbacks = null;
                m_transportAddress = null;
                m_bodyParts = null;
                m_discoServiceEndpoints = null;
                m_callbackServiceEndpoints = null;
                m_udpSeviceHost = null;
                m_httpServiceHost = null;
            }
        }

        /// <summary>
        /// Property that determines if ProcessRequest call blocks.
        /// </summary>
        bool IWsServiceEndpoint.BlockingCall { get { return m_blockingCall; } set { m_blockingCall = value; } }

        /// <summary>
        /// A wrapper method that implements the ProcessRequest interface used to control blocking of request.
        /// The BlockingCall property is used by the stack to control single instance execution. Hello and Bye
        /// are not blocked. Event messages from a device are blocked. This means it is up to a client developer
        /// to insure long thread safety.
        /// </summary>
        /// <param name="header">A WsWsaHeader containing the header proerties of a request.</param>
        /// <param name="envelope">A WsXmlDocument containing the entire envelope of the message in a node tree.</param>
        /// <returns>A byte array containing a soap response to the request.</returns>
        byte[] IWsServiceEndpoint.ProcessRequest(WsWsaHeader header, XmlReader reader)
        {
            // This allow static services like Discovery to run unblocked
            if (m_blockingCall == true)
            {
                lock (m_threadLock)
                {
                    return ProcessRequest(header, reader);
                }
            }
            else
                return ProcessRequest(header, reader);
        }

        // Implements the IWsServiceEndpoint.ProcessRequest method.
        // Gets a service callback method based on the action property sent in a soap header and calls
        // invoke on the callback method.
        private byte[] ProcessRequest(WsWsaHeader header, XmlReader reader)
        {
            WsServiceOperation callback;

            // Look for a client event sink callback action
            if ((callback = m_eventCallbacks[header.Action]) != null)
            {
                m_messageType = WsMessageType.Soap;
                System.Reflection.MethodInfo methodInfo = this.GetType().GetMethod(callback.MethodName);
                if (methodInfo == null)
                {
                    System.Ext.Console.Write("");
                    System.Ext.Console.Write("Client event callback method for action " + header.Action + " was not found");
                    System.Ext.Console.Write("");
                }

                return (byte[])methodInfo.Invoke(this, new object[] { header, reader });
            }

            // If a valid Action is not found, fault
            throw new WsFaultException(header, WsFaultType.WsaDestinationUnreachable, "To: " + header.To + " Action: " + header.Action);
        }

        /// <summary>
        /// SubscriptionEnd stub calls the eventing client SubscriptionEnd handler
        /// </summary>
        /// <param name="header">A WsWsaHeader object containing the details soap header information.</param>
        /// <param name="envelope">A WsXmlDocument object containing the the entire soap message.</param>
        /// <returns>A byte array containig a soap response mesage.</returns>
        internal byte[] SubscriptionEnd(WsWsaHeader header, XmlReader reader)
        {
            try
            {
                SubscriptionEndEvent(this, new SubscriptionEndEventArgs(header, reader));
            }
            catch { }

            return null;
        }
    }

    /// <summary>
    /// Class used to store a services type definition.
    /// </summary>
    /// <remarks>
    /// The information in this class represents a type encapsulated by a service.
    /// Service type information returned from a probe an resolve are stored in this class.
    /// </remarks>
    public class DpwsServiceType
    {
        /// <summary>
        /// Use to get or set a service type namespace prefix.
        /// </summary>
        //public string Prefix = null;

        /// <summary>
        /// Use to get or set a service types namespace Uri.
        /// </summary>
        public readonly String NamespaceUri;

        /// <summary>
        /// Use to get or set the name of a service type.
        /// </summary>
        public readonly String TypeName;

        /// <summary>
        /// Creates an instance of a DpwsEndpointType class.
        /// </summary>
        public DpwsServiceType(String typeName, String namespaceUri)
        {
            TypeName = typeName;
            NamespaceUri = namespaceUri;
        }

        internal DpwsServiceType(String type, XmlReader context)
        {
            // Check to see if this is a fully qualified path. If so parse into namespaceUri and typeName.
            if (Uri.IsWellFormedUriString(type, UriKind.Absolute) && type.LastIndexOf("/") != -1)
            {
                int typeNameIndex = type.LastIndexOf("/");
                NamespaceUri = type.Substring(typeNameIndex);
                TypeName = type.Substring(typeNameIndex + 1);
            }
            else
            {
                int prefixIndex;
                if ((prefixIndex = type.IndexOf(':')) > 0)
                {
                    NamespaceUri = context.LookupNamespace(type.Substring(0, prefixIndex));
                    TypeName = type.Substring(prefixIndex + 1);
                }
                else
                {
                    TypeName = type;
                }
            }
        }
    }

    /// <summary>
    /// Collection class used to store a list of DpwsServiceTypes objects.
    /// </summary>
    /// <remarks>This collection is thread safe.</remarks>
    public class DpwsServiceTypes
    {

        // Fields
        private object m_threadLock = new object();
        private ArrayList m_serviceTypes = new ArrayList();

        /// <summary>
        /// Creates an instance of a DpwsServiceTypes collection class.
        /// </summary>
        /// <remarks>
        /// This collection is thread safe.
        /// </remarks>
        public DpwsServiceTypes()
        {
        }

        internal DpwsServiceTypes(XmlReader reader)
        {
            reader.ReadStartElement();
            reader.MoveToContent();

            // Set types argument. Resolve namespaces and create a DpwsServiceType object for each type.
            String[] types = reader.Value.Split(' ');
            int count = types.Length;

            for (int i = 0; i < count; i++)
            {
                m_serviceTypes.Add(new DpwsServiceType(types[i], reader));
            }

            reader.Read(); // Content
            reader.ReadEndElement();
        }

        /// <summary>
        /// Use to Get the number of DpwsServiceTypes elements actually contained in the collection.
        /// </summary>
        public int Count
        {
            get
            {
                return m_serviceTypes.Count;
            }
        }

        /// <summary>
        /// Use to Get or set the DpwsServiceType element at the specified index.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the DpwsServiceType element to get or set.
        /// </param>
        /// <returns>
        /// An intance of a DpwsServiceType element.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// If index is less than zero.-or- index is equal to or greater than the collection count.
        /// </exception>
        public DpwsServiceType this[int index]
        {
            get
            {
                return (DpwsServiceType)m_serviceTypes[index];
            }
        }

        public DpwsServiceType this[String typeName]
        {
            get
            {
                DpwsServiceType serviceType;
                lock (m_threadLock)
                {
                    int count = m_serviceTypes.Count;
                    for (int i = 0; i < count; i++)
                    {
                        serviceType = (DpwsServiceType)m_serviceTypes[i];
                        if (serviceType.TypeName == typeName)
                        {
                            return serviceType;
                        }
                    }

                    return null;
                }
            }
        }

        /// <summary>
        /// Adds a DpwsServiceType to the end of the collection.
        /// </summary>
        /// <param name="value">
        /// The DpwsServiceType element to be added to the end of the collection.
        /// The value can be null.
        /// </param>
        /// <returns>
        /// The collection index at which the DpwsServiceType has been added.
        /// </returns>
        public int Add(DpwsServiceType value)
        {
            lock (m_threadLock)
            {
                return m_serviceTypes.Add(value);
            }
        }

        /// <summary>
        /// Removes all elements from the collection.
        /// </summary>
        public void Clear()
        {
            lock (m_threadLock)
            {
                m_serviceTypes.Clear();
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific DpwsServiceType from the collection.
        /// </summary>
        /// <param name="serviceType">
        /// The DpwsServiceType to remove from the collection.
        /// The value can be null.
        /// </param>
        public void Remove(DpwsServiceType serviceType)
        {
            lock (m_threadLock)
            {
                m_serviceTypes.Remove(serviceType);
            }
        }
    }
}


