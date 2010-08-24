using System;
using System.Collections;
using System.Text;
using System.Net;
using System.Threading;
using Dpws.Device.Discovery;
using Dpws.Device.Mex;
using Dpws.Device.Services;
using Ws.Services;
using Ws.Services.Transport;
using Ws.Services.Transport.HTTP;
using Ws.Services.Transport.UDP;
using Ws.Services.Utilities;

using Microsoft.SPOT.Net.NetworkInformation;
using System.Ext;
using Ws.Services.Xml;
using Ws.Services.WsaAddressing;
using Microsoft.SPOT;

namespace Dpws.Device
{

    /// <summary>
    /// Creates a device used to manage global device information, host and hosted services.
    /// </summary>
    /// <remarks>
    /// The Device class provides the base functionality of a device. It is the primary control class
    /// for a device and is the container and manager of hosted services provided by a device. This class exposes
    /// static methods and properties used to manage device information, start and stop internal services
    /// and manage hosted services.
    ///
    /// A device developer uses this class to set device specific information common to all hosted services
    /// like ThisModel, ThisDevice and endpoint informaton like the device address and device id. When
    /// developing hosted services this class provides access to information required by the service
    /// when building response messages. The device class also manages a collection of hosted services
    /// that implement the functionality of a device.
    ///
    /// Hosted services are created by device developers. These services are added to the collection
    /// of hosted services maintained by the device class. When the device services, start an instance
    /// of each hosted service in the hosted services collection is instantiated. When a device request is
    /// received the hosted services collection contains information used by the transport services
    /// to route request to a specific service.
    ///
    /// The device class also manages a set of internal hosted services that provide the boiler plate
    /// functionality of DPWS Device Profile for Web Services) like Discovery, Mex and the Event Subscription
    /// manager. As mentioned these are boiler plate services and are therefore not exposed to a device
    /// developer.
    ///
    /// The device class also manages a collection of namespaces common to all DPWS compliant devices.
    ///
    /// If a device requires Host functionality outlined in the DPWS specification a hosted service can be
    /// added to the device to provide this functionality. Special provisions are made because a devices
    /// Host service has the same endpoint as the device.
    /// </remarks>
    public static class Device
    {
        // Fields
        /// <summary>
        /// This class stores model specific information about a device.
        /// </summary>
        public static class ThisModel
        {
            /// <summary>
            /// Name of the manufacturer of this device.
            /// </summary>
            /// <remarks>Must have fewer than MAX_FIELD_SIZE characters.</remarks>
            public static string Manufacturer = string.Empty;

            /// <summary>
            /// Url to a Web site of the manufacturer of this device.
            /// </summary>
            /// <remarks>Must have fewer than MAX_URI_SIZE octets.</remarks>
            public static string ManufacturerUrl = string.Empty;

            /// <summary>
            /// User friendly name for this model of device.
            /// </summary>
            /// <remarks>Must have fewer than MAX_FIELD_SIZE characters.</remarks>
            public static string ModelName = null;

            /// <summary>
            /// Model number for this model of device.
            /// </summary>
            /// <remarks>Must have fewer than MAX_FIELD_SIZE characters.</remarks>
            public static string ModelNumber = null;

            /// <summary>
            /// URL to a Web site for this model of device.
            /// </summary>
            /// <remarks>Must have fewer than MAX_URI_SIZE octets.</remarks>
            public static string ModelUrl = null;

            /// <summary>
            /// URL to an Html page for this device.
            /// </summary>
            /// <remarks>Must have fewer than MAX_URI_SIZE octets.</remarks>
            public static string PresentationUrl = null;

            public static WsXmlNodeList Any = null;
        }

        /// <summary>
        /// This class stores device specific information about a device.
        /// </summary>
        public static class ThisDevice
        {
            /// <summary>
            /// User-friendly name for this device.
            /// </summary>
            /// <remarks>Must have fewer than MAX_FIELD_SIZE characters.</remarks>
            public static string FriendlyName = null;

            /// <summary>
            /// Firmware version for this device.
            /// </summary>
            /// <remarks>Must have fewer than MAX_FIELD_SIZE characters.</remarks>
            public static string FirmwareVersion = null;

            /// <summary>
            /// Manufacturer assigned serial number for this device.
            /// </summary>
            /// <remarks>Must have fewer than MAX_FIELD_SIZE characters.</remarks>
            public static string SerialNumber = null;

            public static WsXmlNodeList Any = null;
        }

        private static DpwsWseEventSinkQMgr m_eventQManager = null;
        private static int m_MetadataVersion = 1;
        private static String m_AppSequence = null;
        private static int m_MessageID = 0;
        private static int m_port = 8084;
        private static string m_address = "urn:uuid:3cb0d1ba-cc3a-46ce-b416-212ac2419b51";
        private static string m_transportAddress;
        private static object m_threadLock = new object();
        private static WsServiceEndpoints m_discoServiceEndpoints = new WsServiceEndpoints();
        private static WsServiceEndpoints m_hostedServices = new WsServiceEndpoints();
        private static WsUdpServiceHost m_udpServiceHost = null;
        private static WsHttpServiceHost m_httpServiceHost = null;
        private static bool m_ignoreRequestFromThisIP = false;
        private static int m_appMaxDelay = 500;
        private static bool isStarted = false;
        private static bool m_supressAdhoc = false;
        private static DiscoveryVersion m_discoVersion = new DiscoveryVersion11();

        /// <summary>
        /// This class is an internal hosted service that provides DPWS compliant WS-Discovery Probe and Resolve
        /// service endpoints.
        /// </summary>
        internal static DpwsDeviceDiscoService m_discoveryService = null;

        /// <summary>
        /// This class is an internal hosted service that provides DPWS compliant WS-Transfer (Metadata Exchange) services.
        /// </summary>
        internal static DpwsDeviceMexService m_discoMexService = null;

        /// <summary>
        /// Property used to determine if device services are running.
        /// </summary>
        public static bool Started { get { return isStarted; } }

        /// <summary>
        /// Collection property used to add hosted services to this device. Hosted services implement device
        /// specific services and derive from DpwsHostedService.
        /// </summary>
        public static WsServiceEndpoints HostedServices { get { return m_hostedServices; } }


        /// <summary>
        /// Property useed to set the maximum number of threads used to process Http request. Default value is 5.
        /// Use this as a tuning parameter and be conservative.
        /// </summary>
        public static int MaxServiceThreads
        {
            get
            {
                return m_httpServiceHost != null ? m_httpServiceHost.MaxThreadCount : 0;
            }

            set
            {
                if (m_httpServiceHost == null)
                    throw new NullReferenceException("Setting MaxSeviceThreads  is not permitted prior to calling Device.Start.");
                m_httpServiceHost.MaxThreadCount = value;
            }
        }

        /// <summary>
        /// Property useed to set the maximum number of threads used to process Udp (Discovery) request. Default value is 5.
        /// Use this as a tuning parameter and be conservative.
        /// </summary>
        public static int MaxDiscoveryThreads
        {
            get
            {
                return m_udpServiceHost != null ? m_udpServiceHost.MaxThreadCount : 0;
            }

            set
            {
                if (m_udpServiceHost == null)
                    throw new NullReferenceException("Setting MaxDiscoveryThreads is not permitted prior to calling Device.Start.");
                m_udpServiceHost.MaxThreadCount = value;
            }
        }

        /// <summary>
        /// Use this static property if the device requires a Host relationship, create a hosted service that
        /// implements device specific host funcitonality and assign it to the Host property.
        /// The Host, hosted service will have the same endpoint address as the device.
        /// </summary>
        public static DpwsHostedService Host = null;

        /// <summary>
        /// Field used to access an event subscription manager. The subscription manager provides event
        /// subscription functionality required to fire events from a hosted service.
        /// </summary>
        public static DpwsWseSubscriptionMgr SubscriptionManager = new DpwsWseSubscriptionMgr();

        /// <summary>
        /// Property containing the maximum wait time for a probe match message response.
        /// </summary>
        /// <remarks>
        /// The default value is 500 ms. as per Dpws specification. Set this value to 0 if implementing
        /// a discovery proxy. See Ws-Discovery specification for details.
        /// </remarks>
        public static int ProbeMatchDelay { get { return m_appMaxDelay; } set { m_appMaxDelay = value; } }

        /// <summary>
        /// Set this property to true to ignore disco request from the same ip address as the device.
        /// </summary>
        public static bool IgnoreLocalClientRequest
        {
            get
            {
                if (m_udpServiceHost == null)
                    return m_ignoreRequestFromThisIP;
                else
                    return m_udpServiceHost.IgnoreRequestFromThisIP;
            }

            set
            {
                if (m_udpServiceHost == null)
                    m_ignoreRequestFromThisIP = value;
                else
                    m_udpServiceHost.IgnoreRequestFromThisIP = value;
            }
        }

        /// <summary>
        /// Initializes the device. This method builds the colletion of internal device services, sets
        /// the devices transport address and adds DPWS required namespaces to the devices namespace collection.
        /// </summary>
        private static void Init()
        {
            // Add disco services to udp service endpoints collection
            m_discoveryService = new DpwsDeviceDiscoService();
            m_discoServiceEndpoints.Add(m_discoveryService);

            // Create a new udp service host and add the discovery endpoints
            m_udpServiceHost = new WsUdpServiceHost(m_discoServiceEndpoints);
            m_udpServiceHost.IgnoreRequestFromThisIP = m_ignoreRequestFromThisIP;

            // Add metadata get service endpoint
            m_discoMexService = new DpwsDeviceMexService();
            m_hostedServices.Add(m_discoMexService);

            // Add directd probe service endpoint
            m_hostedServices.Add(m_discoveryService);

            // Create a new http service host and add hosted services endpoints
            m_httpServiceHost = new WsHttpServiceHost(m_port, m_hostedServices);

            // Set the device endpoint address
            if (m_transportAddress == null)
            {
                m_transportAddress = "http://" + IPV4Address + ":" + m_port + "/" + m_address.Substring(9);
                System.Ext.Console.Write("");
                System.Ext.Console.Write("IP Address: " + IPV4Address);
                System.Ext.Console.Write("");
            }
        }

        /// <summary>
        /// Property contining the IPV4 address of this device.
        /// </summary>
        public static string IPV4Address { get { return WsNetworkServices.GetLocalIPV4Address(); } }

        /// <summary>
        /// Property containing the http listener port number.
        /// </summary>
        public static int Port
        {
            get { return m_port; }
            set
            {
                if (isStarted == false)
                {
                    m_port = value;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        /// <summary>
        /// Method used to Start the stack transport services.
        /// </summary>
        /// <remarks>
        /// The Start Method initialises device specific services and calls the stack services to start
        /// the Http and UDP transport services. It also creates and starts an instance of the
        /// Event Queue manager service that manages event subscription expirations.
        /// </remarks>
        public static void Start()
        {
            isStarted = true;

            // Initialize the device collections
            System.Ext.Console.Write("");
            System.Ext.Console.Write("Initializing device.....");
            Init();

            // Start Udp service host
            m_udpServiceHost.Start();
            System.Ext.Console.Write("Udp service host started...");

            // Start Http service host
            m_httpServiceHost.Start();
            System.Ext.Console.Write("Http service host started...");

            // Start the event subscription manager
            m_eventQManager = new DpwsWseEventSinkQMgr();
            m_eventQManager.Start();
            System.Ext.Console.Write("Event subscription manager started...");

            // Send hello greeting
            DpwsDiscoGreeting.SendGreetingMessage(true);
        }

        /// <summary>
        /// Static method used to stop stack transport services.
        /// </summary>
        /// <remarks>
        /// This method stops the underlying stack transport services and the devices event queue manager.
        /// </remarks>
        public static void Stop()
        {
            m_eventQManager.Stop();
            m_udpServiceHost.Stop();
            m_httpServiceHost.Stop();

            // Send bye
            DpwsDiscoGreeting.SendGreetingMessage(false);

            isStarted = false;
        }

        /// <summary>
        /// Static property used to Get or Set a devices DPWS compliant Metadata version.
        /// </summary>
        /// <remarks>
        /// If a device changes any of it's relationship metadata, it Must increment the Metadata Version exposed
        /// in Hello, ProbeMatch and ResolveMatch soap messages. The device metadata relationship is considered to
        /// be static per metadata version.
        /// </remarks>
        public static int MetadataVersion
        {
            get
            {
                return m_MetadataVersion;
            }

            set
            {
                m_MetadataVersion = value;
            }
        }

        /// <summary>
        /// Static property containing a Dpws compliant endpoint address.
        /// </summary>
        /// <remarks>
        /// This stack supports urn:uuid:(guid) format addresses only.
        /// </remarks>
        public static string EndpointAddress
        {
            get
            {
                return m_address;
            }

            set
            {
                string localIpAddress = WsNetworkServices.GetLocalIPV4Address();
                string port = m_port.ToString();

                lock (m_threadLock)
                {
                    if (!WsUtilities.ValidateUrnUuid(value))
                        throw new ArgumentException("Device.Address Must be a valid urn:uuid");

                    m_address = value;
                    m_transportAddress = "http://" + localIpAddress + ":" + port + "/" + m_address.Substring(9);
                }
            }
        }

        /// <summary>
        /// Static property used to Get a devices DPWS compliant transport address.
        /// </summary>
        /// <remarks>
        /// The transport address format supportd by this stack is:
        /// http://(device ip address):(device port)/(device.address - urn:uuid prefix)
        /// </remarks>
        internal static string TransportAddress
        {
            get
            {
                return m_transportAddress;
            }
        }

        /// <summary>
        /// Static property used to Get the required WS-Discovery App Sequence value.
        /// </summary>
        /// <remarks>
        /// This is a soap header value and is required by WS-Discovery to facilitate ordering of Hello and
        /// Bye messages. This stack uses the App Sequence request and response messages. In addition to the
        /// Ws-Discovery Hello/Bye requirements this stack includes the AppSequence element in all WS-Discovery
        /// messages.
        /// </remarks>
        internal static String AppSequence
        {
            get
            {
                lock (m_threadLock)
                {
                    return m_AppSequence == null ? SetAppSequence() : m_AppSequence;
                }
            }
        }

        /// <summary>
        /// Static property used to Get or Set the DPWS compliant MessageID value.
        /// </summary>
        /// <remarks>
        /// This is a soap header value and is required in DPWS compliant request and response messages.
        /// This stack uses this property to match a response with a request.
        /// This property is thread safe.
        /// </remarks>
        internal static String MessageID
        {
            get
            {
                lock (m_threadLock)
                {
                    return (++m_MessageID).ToString();
                }
            }
        }

        internal static String SequenceID = "urn:uuid:c883e4a8-9af4-4bf4-aaaf-06394151d6c0";

        /// <summary>
        /// Initializes the App Sequence value.
        /// </summary>
        /// <returns>An In32 containing the app sequence number.</returns>
        private static String SetAppSequence()
        {
            DateTime gmt1970 = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            long gmtTicks = gmt1970.Ticks;
            return m_AppSequence = ((int)((DateTime.UtcNow.Ticks - gmtTicks) / 10000000L)).ToString();
        }

        /// <summary>
        /// Static property used to turn adhoc discovery on or off. Adhoc disco is optional with a Discovery Proxy.
        /// </summary>
        public static bool SupressAdhoc { get { return m_supressAdhoc; } set { m_supressAdhoc = value; } }

        /// <summary>
        /// Use to set the Discovery version
        /// </summary>
        public static DiscoveryVersion DiscoVersion
        {
            get
            {
                return m_discoVersion;
            }
            set
            {
                if (isStarted == true)
                    throw new Exception("Cannot set DiscoveryVersion property while services are running.");
                m_discoVersion = value;
            }
        }
    
    }
}

