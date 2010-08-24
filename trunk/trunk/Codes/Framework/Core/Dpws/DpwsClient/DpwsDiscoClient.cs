using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Ws.Services.Transport;
using Ws.Services.Transport.HTTP;
using Ws.Services.Utilities;
using Ws.Services.Soap;
using Ws.Services.Xml;
using Ws.Services.WsaAddressing;

using System.Ext;
using System.IO;
using System.Ext.Xml;
using System.Xml;
using Ws.Services;
using Microsoft.SPOT;

namespace Dpws.Client.Discovery
{
    /// <summary>
    /// A class used to process client Probe and Resolve request.
    /// </summary>
    public class DpwsDiscoveryClient
    {
        private const int DiscoveryPort = 3702;
        private const long DiscoveryAddress = 239L + (255L << 0x08) + (255L << 0x10) + (250L << 0x18); // 239.255.255.250

        private static IPEndPoint DiscoveryEP;
        private int m_discoResponsePort = 15357;
        private int m_receiveTimeout = 5000;
        private Random m_random;

        private const int MulticastUdpRepeat = 4;
        private const int UdpUpperDelay = 500;
        private const int UdpMinDelay = 50;
        private const int UdpMaxDelay = 250;
        private static DiscoveryVersion m_discoVersion = new DiscoveryVersion11();
        private DpwsClient m_parent = null;

        /// <summary>
        /// Creates and instance of the DpwsDiscoveryClient class.
        /// </summary>
        internal DpwsDiscoveryClient(DpwsClient parent)
        {
            m_parent = parent;

            DiscoveryEP = new IPEndPoint(DiscoveryAddress, DiscoveryPort);

            m_random = new Random();
        }

        /// <summary>
        /// Use to get or set the UDP discovery port that this client listens for discovery responses on.
        /// </summary>
        public int DiscoveryResponsePort { get { return m_discoResponsePort; } set { m_discoResponsePort = value; } }

        /// <summary>
        /// Use to get or set the time in milliseconds that a probe or resolve listener should wait for probe matches.
        /// </summary>
        public int ReceiveTimeout { get { return m_receiveTimeout; } set { m_receiveTimeout = value; } }

        /// <summary>
        /// Use to set the Discovery version
        /// </summary>
        public DiscoveryVersion DiscoVersion
        {
            get
            {
                return m_discoVersion;
            }
            set
            {
                m_discoVersion = value;
                m_parent.UpdateDiscoVersion();
            }
        }

        /// <summary>
        /// Sends a directed Probe request and parses ProbeMatches responses.
        /// </summary>
        /// <param name="endpointAddress">
        /// A string containing a Dpws devices transport endpoint address.
        /// For example: http://192.168.0.1:8084/3cb0d1ba-cc3a-46ce-b416-212ac2419b20
        /// </param>
        /// <param name="targetServiceAddress">
        /// A string containing the target service address to probe for.
        /// For example: urn:uuid:3cb0d1ba-cc3a-46ce-b416-212ac2419b20
        /// </param>
        /// <param name="filters">
        /// A DpwsServiceTypes object containing a collection of types a service must support to signal a match.
        /// Null = any type.
        /// </param>
        /// <remarks>
        /// A directed Probe is used to discover services on a network. The DirectedProbe method sends a
        /// HTTP request to the service specified endpointAddress parameter. A service at the endpoint that
        /// implements matching types specified in the filters parameter should respond with a ProbeMatches
        /// message. The ProbeMatches mesgage is returned as the response to the DirectedProbe request.
        /// If a null filter is supplied any Dpws complient service should reply with a ProbeMatches reponse.
        /// This method is used to directly ask for service endpoint information.
        /// </remarks>
        /// <returns>
        /// A collection of ProbeMatches objects.  A ProbeMatch object contains endpoint details used
        /// used to locate the actual service on a network and the types supported by the service.
        /// </returns>
        /// <exception cref="InvalidOperationException">If a fault response is received.</exception>
        public DpwsServiceDescriptions DirectedProbe(string endpointAddress, string targetServiceAddress, DpwsServiceTypes filters)
        {
            // Build the probe request message
            string messageID = null;
            byte[] probeRequest = BuildProbeRequest(targetServiceAddress, filters, out messageID);

            System.Ext.Console.Write("");
            System.Ext.Console.Write("Sending DirectedProbe:");
            System.Ext.Console.Write(new string(new UTF8Encoding().GetChars(probeRequest)));
            System.Ext.Console.Write("");

            // Create an http client and send the probe request. Use a WspHttpClient to get an array response.
            WsHttpClient httpClient = new WsHttpClient();
            byte[] probeResponse = httpClient.SendRequest(probeRequest, endpointAddress, false, false);

            // Build probe matches collection
            DpwsServiceDescriptions probeMatches = new DpwsServiceDescriptions();
            DpwsDiscoClientProcessor soapProcessor = new DpwsDiscoClientProcessor();
            if (probeResponse != null)
            {
                System.Ext.Console.Write("");
                System.Ext.Console.Write("ProbeMatches Response From: " + endpointAddress);
                System.Ext.Console.Write(new String(System.Text.Encoding.UTF8.GetChars(probeResponse)));

                try
                {
                    DpwsServiceDescriptions tempMatches = soapProcessor.ProcessProbeMatch(probeResponse, messageID, null, null);
                    if (tempMatches != null)
                    {
                        int count = tempMatches.Count;
                        for (int i = 0; i < count; i++)
                        {
                            probeMatches.Add(tempMatches[i]);
                        }
                    }
                }
                catch (Exception e)
                {
                    System.Ext.Console.Write("");
                    System.Ext.Console.Write(e.Message);
                    System.Ext.Console.Write("");
                }
            }

            if (probeMatches.Count == 0)
                return null;
            else
                return probeMatches;
        }

        /// <summary>
        /// Sends a directed Resolve request and parses ResolveMatch response.
        /// </summary>
        /// <param name="endpointAddress">
        /// A string containing a Dpws devices transport endpoint address.
        /// For example: http://192.168.0.1:8084/3cb0d1ba-cc3a-46ce-b416-212ac2419b20
        /// </param>
        /// <param name="serviceAddress">
        /// A string containing the address of a service that will handle the resolve request.
        /// For example: urn:uuid:2bcdd1ba-cc3a-46ce-b416-212ac2419b20
        /// </param>
        /// <param name="targetServiceAddress">
        /// A string containing the address of a service that can process the resolve request.
        /// For example: urn:uuid:3cb0d1ba-cc3a-46ce-b416-212ac2419b20
        /// </param>
        /// <remarks>
        /// A Resolve is used to resolve the transport address of a know service. The request contains a service
        /// address aquired from configuration or a previous Resolve or Metadata Get request.
        /// The DirectedResolve method sends a Http request to the specified endpoint address.
        /// If the endpoint contains a device with this address receives the request, it must send a unicast ResolveMatches
        /// response back to the client that made the request.  
        /// </remarks>
        /// <returns>
        /// A collection of ResolveMatches objects. A ResolveMatch object contains endpoint details used
        /// used to locate the actual service on a network and the types supported by the service. 
        /// </returns>
        public DpwsServiceDescription DirectedResolve(string endpointAddress, string serviceAddress, string targetServiceAddress)
        {
            String messageID = "";
            byte[] resolveRequest = BuildResolveRequest(targetServiceAddress, serviceAddress, ref messageID);

            if (resolveRequest == null)
                return null;

            System.Ext.Console.Write("");
            System.Ext.Console.Write("Sending Resolve:");
            System.Ext.Console.Write(new string(new UTF8Encoding().GetChars(resolveRequest)));

            // Create an http client and send the resolve request. Use a WspHttpClient to get an array response.
            WsHttpClient httpClient = new WsHttpClient();
            byte[] resolveResponse = httpClient.SendRequest(resolveRequest, endpointAddress, false, false);
            DpwsDiscoClientProcessor soapProcessor = new DpwsDiscoClientProcessor();
            DpwsServiceDescription resolveMatch = soapProcessor.ProcessResolveMatch(resolveResponse, messageID, null, null);
            return resolveMatch;
        }

        /// <summary>
        /// Sends a Probe request and parses ProbeMatches responses.
        /// </summary>
        /// <param name="filters">
        /// A DpwsServiceTypes object containing a collection of types a service must support to signal a match.
        /// Null = any type.
        /// </param>
        /// <remarks>
        /// A Probe is used to discover services on a network. The Probe method sends a UDP request to the
        /// Dpws multicast address, 239.255.255.250:3702. Any service that implements types specified in the
        /// filters parameter should respond with a ProbeMatches message. The ProbeMatches mesgage is unicast
        /// back to the that client that made the request. If a null filter is supplied any Dpws complient
        /// service should reply with a ProbeMatches reponse. Probe waits DpwsDiceoveryCleint.ReceiveTimout
        /// for probe matches.
        /// </remarks>
        /// <returns>
        /// A collection of ProbeMatches objects.  A ProbeMatch object contains endpoint details used
        /// used to locate the actual service on a network and the types supported by the service.
        /// </returns>
        public DpwsServiceDescriptions Probe(DpwsServiceTypes filters)
        {
            return Probe(filters, 0, -1);
        }

        /// <summary>
        /// Sends a Probe request and parses ProbeMatches responses.
        /// </summary>
        /// <param name="filters">
        /// A DpwsServiceTypes object containing a collection of types a service must support to signal a match.
        /// Null = any type.
        /// </param>
        /// <param name="maxProbeMatches">
        /// An integer representing the maximum number of matches to reveive within the timout period. Pass 0 to receive
        /// as many matches as possible before the timeout expires.
        /// </param>
        /// <param name="timeout">
        /// An integer specifying a request timeout in milliseconds. Pass -1 to wait ReceiveTimeout.
        /// </param>
        /// <remarks>
        /// A Probe is used to discover services on a network. The Probe method sends a UDP request to the
        /// Dpws multicast address, 239.255.255.250:3702. Any service that implements types specified in the
        /// filters parameter should respond with a ProbeMatches message. The ProbeMatches mesgage is unicast
        /// back to the that client that made the request. If a null filter is supplied any Dpws complient
        /// service should reply with a ProbeMatches reponse. Probe waits DpwsDiceoveryCleint.ReceiveTimout
        /// for probe matches.
        /// </remarks>
        /// <returns>
        /// A collection of ProbeMatches objects.  A ProbeMatch object contains endpoint details used
        /// used to locate the actual service on a network and the types supported by the service.
        /// </returns>
        public DpwsServiceDescriptions Probe(DpwsServiceTypes filters, int maxProbeMatches, int timeout)
        {
            // Build the probe request message
            WsMessageCheck messageCheck = new WsMessageCheck();
            string messageID = null;
            byte[] probeRequest = BuildProbeRequest(DiscoVersion.WellKnownAddress, filters, out messageID);

            System.Ext.Console.Write("");
            System.Ext.Console.Write("Sending Probe:");
            System.Ext.Console.Write(new string(new UTF8Encoding().GetChars(probeRequest)));
            System.Ext.Console.Write("");

            // Create a new UdpClient
            Socket udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, m_discoResponsePort);
            udpClient.Bind(localEP);

            // Very important - Set default multicast interface for the underlying socket
            byte[] ipBytes = IPAddress.Parse(WsNetworkServices.GetLocalIPV4Address()).GetAddressBytes();
            long longIP = (long)((ipBytes[0] + (ipBytes[1] << 0x08) + (ipBytes[2] << 0x10) + (ipBytes[3] << 0x18)) & 0xFFFFFFFF);
            udpClient.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, (int)longIP);

            // Random back off implemented as per soap over udp specification
            // for unreliable multicast message exchange
            SendWithBackoff(probeRequest, udpClient);

            // Create probe matches collection and set expiration loop timer
            DpwsServiceDescriptions probeMatches = new DpwsServiceDescriptions();
            byte[] probeResponse = new byte[c_MaxUdpPacketSize];
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            int responseLength;

            // Multiply receive time by 10000 to convert milliseconds to 100 nano ticks
            long endTime = (long)(DateTime.Now.Ticks + (timeout < 0 ? m_receiveTimeout * 10000 : timeout * 10000));
            DpwsDiscoClientProcessor soapProcessor = new DpwsDiscoClientProcessor();

            // Build probe matches collection as long as timeout has not expired
            int noReceived = 0;

            while (DateTime.Now.Ticks < endTime)
            {
                if (udpClient.Available > 0)
                {
                    // If maxProbeRequest is set check count
                    if (maxProbeMatches > 0)
                    {
                        if (noReceived > maxProbeMatches)
                            break;
                    }

                    // Since MF sockets does not have an IOControl method catch 10054 to get around the problem
                    // with Upd and ICMP.
                    try
                    {
                        // Wait for response
                        responseLength = udpClient.ReceiveFrom(probeResponse, c_MaxUdpPacketSize, SocketFlags.None, ref remoteEP);
                    }
                    catch (SocketException se)
                    {
                        if (se.ErrorCode == 10054)
                            continue;
                        throw se;
                    }

                    // If we received process probe match
                    if (responseLength > 0)
                    {
                        System.Ext.Console.Write("");
                        System.Ext.Console.Write("ProbeMatches Response From: " + ((IPEndPoint)remoteEP).Address.ToString());
                        System.Ext.Console.Write(new String(System.Text.Encoding.UTF8.GetChars(probeResponse)));

                        // Process the response
                        try
                        {
                            DpwsServiceDescriptions tempMatches = soapProcessor.ProcessProbeMatch(probeResponse, messageID, (IPEndPoint)remoteEP, messageCheck);
                            if (tempMatches != null)
                            {
                                int count = tempMatches.Count;
                                for (int i = 0; i < count; i++)
                                {
                                    probeMatches.Add(tempMatches[i]);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            System.Ext.Console.Write("");
                            System.Ext.Console.Write(e.Message);
                            System.Ext.Console.Write("");
                        }

                        // Increment the number received counter
                        ++noReceived;
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }
            }

            udpClient.Close();
            udpClient = null;

            // Display results
            if (probeMatches == null)
                System.Ext.Console.Write("Probe timed out.");
            else
                System.Ext.Console.Write("Received " + probeMatches.Count + " probeMatches matches.");

            return probeMatches;
        }

        /// <summary>
        /// Builds a probe request message based on the filters parameter.
        /// </summary>
        /// <param name="serviceAddress">
        /// A string containing the target service address.
        /// For example: urn:uuid:3cb0d1ba-cc3a-46ce-b416-212ac2419b20
        /// </param>
        /// <param name="filters">
        /// A DpwsServiceTypes object containing a collection of types a service must support to signal a match.
        /// Null = any type.
        /// </param>
        /// <param name="messageID">
        /// A string used to return the messageID assigned to this message.
        /// </param>
        /// <returns>A byte array containing the probe message or null if an error occures.</returns>
        private byte[] BuildProbeRequest(string serviceAddress, DpwsServiceTypes filters, out String messageID)
        {
            // Performance debugging
            DebugTiming timeDebuger = new DebugTiming();
            long startTime = timeDebuger.ResetStartTime("");

            // Build Probe request
            MemoryStream soapStream = new MemoryStream();
            XmlWriter xmlWriter = XmlWriter.Create(soapStream);

            WsWsaHeader header = new WsWsaHeader(
                WsWellKnownUri.WsdNamespaceUri + "/Probe",      // Action
                null,                                           // RelatesTo
                serviceAddress,                                 // To
                null, null, null);                              // ReplyTo, From, Any

            // If filters are supplied, write filter namespaces if prefixed. Build filter list for use later,
            // include wsdp:device.
            WsXmlNamespaces namespaces = new WsXmlNamespaces();

            // Prefix hack for now:
            int i = 0;
            string prefix;
            string filterList = "";
            bool spaceFlag = false;
            if (filters != null)
            {
                int count = filters.Count;
                for (int j = 0; j < count; j++)
                {
                    DpwsServiceType serviceType = filters[j];
                    prefix = namespaces.LookupPrefix(serviceType.NamespaceUri);
                    if (prefix == null)
                    {
                        prefix = "MyPrefix" + (i++);
                        namespaces.Add(new WsXmlNamespace(prefix, serviceType.NamespaceUri));
                    }

                    filterList = filterList + ((spaceFlag == true) ? " " : "") + prefix + ":" + serviceType.TypeName;
                    spaceFlag = true;
                }
            }

            messageID = WsSoapMessageWriter.WriteSoapMessageStart(xmlWriter,
                WsSoapMessageWriter.Prefixes.Wsd, namespaces, header, null);

            // Performance debuging
            timeDebuger.PrintElapsedTime("*****Write Header Took");

            // write body
            xmlWriter.WriteStartElement("wsd", "Probe", null);

            // If filter is supplied add filter types tag else write blank string to probe body, force an empty tag
            if (filterList.Length != 0)
            {
                xmlWriter.WriteStartElement("wsd", "Types", null);
                xmlWriter.WriteString(filterList);
                xmlWriter.WriteEndElement(); // End Filter
            }
            else
                xmlWriter.WriteString("");

            xmlWriter.WriteEndElement(); // End Probe

            // Performance debuging
            timeDebuger.PrintElapsedTime("*****Write Body Took");

            WsSoapMessageWriter.WriteSoapMessageEnd(xmlWriter);

            // Performance debuging
            timeDebuger.PrintTotalTime(startTime, "***Probe Message Build Took");

            // Flush and close writer
            xmlWriter.Flush();
            xmlWriter.Close();

            // return the probe message
            return soapStream.ToArray();
        }

        /// <summary>
        /// Send a Resolve request to a specific service endpoint.
        /// </summary>
        /// <param name="targetServiceAddress">
        /// A string containing the target service address of a known service. For Dpws this address would
        /// represents a devices address.
        /// For example: urn:uuid:3cb0d1ba-cc3a-46ce-b416-212ac2419b20
        /// </param>
        /// <remarks>
        /// A Resolve is used to resolve the transport address of a know service. The request contains a service
        /// address aquired from configuration or a previous Probe or Metadata Get request.
        /// The Resolve method sends a UDP request to the the Dpws multicast address, 239.255.255.250:3702.
        /// If a device with this address receives the request, it must send a unicast ResolveMatches
        /// response back to the client that made the request.
        /// </remarks>
        /// <returns>
        /// A collection of ResolveMatches objects. A ResolveMatch object contains endpoint details used
        /// used to locate the actual service on a network and the types supported by the service.
        /// </returns>
        public DpwsServiceDescription Resolve(string targetServiceAddress)
        {
            return Resolve(targetServiceAddress, -1);
        }

        /// <summary>
        /// Send a Resolve request to a specific service endpoint.
        /// </summary>
        /// <param name="serviceAddress">
        /// A string containing the target service address of a known service. For Dpws this address would
        /// represents a devices address.
        /// For example: urn:uuid:3cb0d1ba-cc3a-46ce-b416-212ac2419b20
        /// </param>
        /// <param name="timeout">
        /// An integer specifying a request timeout in milliseconds. Pass -1 to wait ReceiveTimeout.
        /// </param>
        /// <remarks>
        /// A Resolve is used to resolve the transport address of a know service. The request contains a service
        /// address aquired from configuration or a previous Probe or Metadata Get request.
        /// The Resolve method sends a UDP request to the the Dpws multicast address, 239.255.255.250:3702.
        /// If a device with this address receives the request, it must send a unicast ResolveMatches
        /// response back to the client that made the request.
        /// </remarks>
        /// <returns>
        /// A collection of ResolveMatches objects. A ResolveMatch object contains endpoint details used
        /// used to locate the actual service on a network and the types supported by the service.
        /// </returns>
        public DpwsServiceDescription Resolve(string targetServiceAddress, int timeout)
        {
            String messageID = "";
            byte[] resolveRequest = BuildResolveRequest(targetServiceAddress, DiscoVersion.WellKnownAddress, ref messageID);

            if (resolveRequest == null)
                return null;

            // Send Resolve and return probe matches if received
            return SendResolveRequest(resolveRequest, messageID, timeout < 0 ? m_receiveTimeout : timeout);
        }

        /// <summary>
        /// Builds a probe request message based on the filters parameter.
        /// </summary>
        /// <param name="targetServiceAddress">
        /// A string containing the target service address of a known service to resolve.
        /// For example: urn:uuid:3cb0d1ba-cc3a-46ce-b416-212ac2419b20
        /// </param>
        /// <param name="serviceAddress">
        /// A string containing the address of a service endpoint used to process the resolve request.
        /// For example: urn:uuid:22d0d1ba-cc3a-46ce-b416-212ac2419b20
        /// </param>
        /// <param name="messageID">
        /// A string reference used to store retreive the messageID from resolve message generation. The id is
        /// used to verify probe match responses for ad-hoc operation.
        /// </param>
        /// <returns>A byte array containing the resolve message or null if an error occures.</returns>
        private byte[] BuildResolveRequest(string targetServiceAddress, string serviceAddress, ref String messageID)
        {
            // Build Resolve Request
            MemoryStream soapStream = new MemoryStream();
            XmlWriter xmlWriter = XmlWriter.Create(soapStream);

            WsWsaHeader header = new WsWsaHeader(
                WsWellKnownUri.WsdNamespaceUri + "/Resolve",    // Action
                null,                                           // RelatesTo
                serviceAddress,                                 // To
                null, null, null);                              // ReplyTo, From, Any

            messageID = WsSoapMessageWriter.WriteSoapMessageStart(xmlWriter,
                WsSoapMessageWriter.Prefixes.Wsd, null, header, null);


            // write body
            xmlWriter.WriteStartElement("wsd", "Resolve", null);
            xmlWriter.WriteStartElement("wsa", "EndpointReference", null);
            xmlWriter.WriteStartElement("wsa", "Address", null);
            xmlWriter.WriteString(targetServiceAddress);

            xmlWriter.WriteEndElement(); // End Address
            xmlWriter.WriteEndElement(); // End EndpointReference
            xmlWriter.WriteEndElement(); // End Resolve

            WsSoapMessageWriter.WriteSoapMessageEnd(xmlWriter);

            // Flush and close writer
            xmlWriter.Flush();
            xmlWriter.Close();

            // return the resolve message
            return soapStream.ToArray();
        }

        // This is the maximum size of the UDP datagram before it gets broken up
        // Since an incomplete packet would fail in the stack, there's no point to
        // read beyond the packetsize
        private const int c_MaxUdpPacketSize = 5229;

        /// <summary>
        /// Use to send a resolve request to the ws-discovery address and receive a resolve match.
        /// </summary>
        /// <param name="message">A byte array containing a the resolve message.</param>
        /// <param name="messageID">
        /// A string containing the message ID of a resolve request. This ID will be used to validate against
        /// a ResolveMatch received if it don't match, the ResolveMatch is discarded.
        /// </param>
        /// <param name="timeout">
        /// A DateTime value containing the length of time this request will wait for resolve match.
        /// until the timeout value has expired.
        /// </param>
        /// <returns>A resolve match object.</returns>
        private DpwsServiceDescription SendResolveRequest(byte[] message, string messageID, long timeout)
        {
            WsMessageCheck messageCheck = new WsMessageCheck();

            System.Ext.Console.Write("");
            System.Ext.Console.Write("Sending Resolve:");
            System.Ext.Console.Write(new string(new UTF8Encoding().GetChars(message)));

            // Create a new UdpClient
            Socket udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, m_discoResponsePort);
            udpClient.Bind(localEP);

            // Very important - Set default multicast interface for the underlying socket
            byte[] ipBytes = IPAddress.Parse(WsNetworkServices.GetLocalIPV4Address()).GetAddressBytes();
            long longIP = (long)((ipBytes[0] + (ipBytes[1] << 0x08) + (ipBytes[2] << 0x10) + (ipBytes[3] << 0x18)) & 0xFFFFFFFF);
            udpClient.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, (int)longIP);

            // Random back off implemented as per soap over udp specification
            // for unreliable multicast message exchange
            SendWithBackoff(message, udpClient);

            // Wait for resolve match as long a timeout has not expired
            DpwsServiceDescription resolveMatch = null;
            byte[] resolveResponse = new byte[c_MaxUdpPacketSize];
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            int responseLength;
            long endTime = (long)(DateTime.Now.Ticks + (timeout * 10000));
            DpwsDiscoClientProcessor soapProcessor = new DpwsDiscoClientProcessor();
            while (DateTime.Now.Ticks < endTime)
            {
                if (udpClient.Available > 0)
                {
                    // Since MF sockets does not have an IOControl method catch 10054 to get around the problem
                    // with Upd and ICMP.
                    try
                    {
                        // Wait for response
                        responseLength = udpClient.ReceiveFrom(resolveResponse, c_MaxUdpPacketSize, SocketFlags.None, ref remoteEP);
                    }
                    catch (SocketException se)
                    {
                        if (se.ErrorCode == 10054)
                            continue;
                        throw se;
                    }

                    // If we received process resolve match
                    if (responseLength > 0)
                    {
                        System.Ext.Console.Write("");
                        System.Ext.Console.Write("ResolveMatches Response From: " + ((IPEndPoint)remoteEP).Address.ToString());
                        System.Ext.Console.Write(new String(System.Text.Encoding.UTF8.GetChars(resolveResponse)));

                        try
                        {
                            resolveMatch = soapProcessor.ProcessResolveMatch(resolveResponse, messageID, (IPEndPoint)remoteEP, messageCheck);
                            if (resolveMatch != null)
                                break;
                        }
                        catch (Exception e)
                        {
                            System.Ext.Console.Write("");
                            System.Ext.Console.Write(e.Message);
                            System.Ext.Console.Write("");
                        }
                    }
                }

                Thread.Sleep(10);
            }

            udpClient.Close();
            udpClient = null;

            // Display results
            if (resolveMatch == null)
                System.Ext.Console.Write("Resolve timed out.");

            return resolveMatch;
        }

        private void SendWithBackoff(byte[] message, Socket udpClient)
        {
            int backoff = 0;
            int messageLen = message.Length;
            for (int i = 0; i < MulticastUdpRepeat; ++i)
            {
                if (i == 0)
                {
                    backoff = m_random.Next(UdpMaxDelay - UdpMinDelay) + UdpMinDelay;
                }
                else
                {
                    backoff = backoff * 2;
                    backoff = backoff > UdpUpperDelay ? UdpUpperDelay : backoff;
                }

                Thread.Sleep(backoff);
                udpClient.SendTo(message, messageLen, SocketFlags.None, DiscoveryEP);
            }
        }
    }

    /// <summary>
    /// Abstract base Ws-Discovery version class
    /// </summary>
    public abstract class DiscoveryVersion
    {
        /// <summary>
        /// Use to get or set the Ws-Discovery version number.
        /// </summary>
        public abstract double Version { get; }

        /// <summary>
        /// Use to get or set the Ws-Discovery Well Know Address.
        /// </summary>
        public abstract string WellKnownAddress { get; }

        /// <summary>
        /// Use to get or set the Ws-Discovery Well Know Address.
        /// </summary>
        public abstract string Namespace { get; }
    }

    /// <summary>
    /// Derived class used to store Ws-Discovery version 1.0
    /// </summary>
    public class DiscoveryVersion10 : DiscoveryVersion
    {

        public DiscoveryVersion10()
        {
            WsWellKnownUri.WsdNamespaceUri = "http://schemas.xmlsoap.org/ws/2005/04/discovery";
        }

        /// <summary>
        /// Get the Ws-Discovery version number.
        /// </summary>
        public override double Version { get { return 1.0; } }

        /// <summary>
        /// Use to get the Ws-Discovery Well Know Address.
        /// </summary>
        public override string WellKnownAddress { get { return "urn:schemas-xmlsoap-org:ws:2005:04:discovery"; } }
        public override string Namespace { get { return WsWellKnownUri.WsdNamespaceUri; } }
    }

    /// <summary>
    /// Derived class used to store Ws-Discovery version 1.1
    /// </summary>
    public class DiscoveryVersion11 : DiscoveryVersion
    {

        public DiscoveryVersion11()
        {
            WsWellKnownUri.WsdNamespaceUri = "http://docs.oasis-open.org/ws-dd/ns/discovery/2009/01";
        }

        /// <summary>
        /// Get the Ws-Discovery version number.
        /// </summary>
        public override double Version { get { return 1.0; } }

        /// <summary>
        /// Use to get the Ws-Discovery Well Know Address.
        /// </summary>
        public override string WellKnownAddress { get { return "urn:docs-oasis-open-org:ws-dd:ns:discovery:2009:01"; } }
        public override string Namespace { get { return WsWellKnownUri.WsdNamespaceUri; } }
    }

    /// <summary>
    /// Base class used by DpwsProbeMatch and DpwsResolve match classes.
    /// </summary>
    /// <remarks>
    /// This base class contains target service endpoint properties that identify
    /// a services supported types and endpoint address.
    /// </remarks>
    public class DpwsServiceDescription
    {
        internal enum ServiceDescriptionType
        {
            Hello,
            Bye,
            ProbeMatch,
            ResolveMatch
        }

        internal DpwsServiceDescription(XmlReader reader, ServiceDescriptionType type)
        {
            reader.ReadStartElement(); // ProbeMatch / ResolveMatch / Hello / Bye

            // Endpoint Reference
            if (
                (reader.IsStartElement("EndpointReference", WsWellKnownUri.WsaNamespaceUri_2004_08) == false) && 
                (reader.IsStartElement("EndpointReference", WsWellKnownUri.WsaNamespaceUri_2005_08) == false) 
               )
            {
                throw new XmlException();
            }

            this.Endpoint = new WsWsaEndpointRef(reader);

            // Types
            if (reader.IsStartElement("Types", WsWellKnownUri.WsdNamespaceUri))
            {
                this.ServiceTypes = new DpwsServiceTypes(reader);
            }

            // Optional Scopes??
            if (reader.IsStartElement("Scopes", WsWellKnownUri.WsdNamespaceUri))
            {
                reader.Skip();
            }

            // XAddrs
            if (reader.IsStartElement("XAddrs", WsWellKnownUri.WsdNamespaceUri))
            {
                this.XAddrs = reader.ReadElementString().Split(' ');
                int count = XAddrs.Length;

                for (int i = 0; i < count; i++)
                {
                    // validate all XAddrs for fully qualified paths
                    if (Uri.IsWellFormedUriString(XAddrs[i], UriKind.Absolute) == false)
                    {
                        throw new XmlException();
                    }
                }
            }
            else if (type == ServiceDescriptionType.ResolveMatch) // for ResolveMatch, XAddrs is required
            {
                throw new XmlException();
            }

            // MetadataVersion
            if (reader.IsStartElement("MetadataVersion", WsWellKnownUri.WsdNamespaceUri))
            {
                this.MetadataVersion = reader.ReadElementString();
            }
            else if (type != ServiceDescriptionType.Bye) // for Hello, ProbeMatch and ResolveMatch, MetadataVersion is required
            {
                throw new XmlException();
            }

            XmlReaderHelper.SkipAllSiblings(reader); // xs:any
            reader.ReadEndElement(); // ProbeMatch / ResolveMatch / Hello / Bye
        }

        /// <summary>
        /// Creates an instance of a DpwsServiceDescription class initialized with a service endpoint and a list of
        /// supported service types.
        /// </summary>
        /// <param name="endpoint">A WsWsaEndpointRef object containing a Dpws Device servie endpoint.</param>
        /// <param name="serviceTypes">A string array containing a list of service types supporte by a Dpws devie service endpoint.</param>
        public DpwsServiceDescription(WsWsaEndpointRef endpoint, DpwsServiceTypes serviceTypes)
        {
            this.Endpoint = endpoint;
            this.ServiceTypes = serviceTypes;
        }

        /// <summary>
        /// Use to get a WsWsaEndpointRef contining the endpoint of a service that supports
        /// types requested in a probe match.
        /// </summary>
        public readonly WsWsaEndpointRef Endpoint;

        /// <summary>
        /// Use to get the metadata version of the probe match object.
        /// </summary>
        public readonly String MetadataVersion;

        /// <summary>
        /// Use to get a list of types supported by a device.
        /// </summary>
        public readonly DpwsServiceTypes ServiceTypes;

        /// <summary>
        /// Use to get an array containing transport specific endpoint addresses representing a
        /// services physical endpoint address. This is an optional ws-discovery parameter.
        /// </summary>
        public readonly String[] XAddrs;
    }

    /// <summary>
    /// A base collection class used to store DpwsServiceDescription objects.
    /// </summary>
    /// <remarks>
    /// DpwsProbeMatches and DpwsResolveMatches derive from this base collection.
    /// This class is thread safe.
    /// </remarks>
    public class DpwsServiceDescriptions
    {
        private object m_threadLock = new object();
        private ArrayList m_serviceDescriptions = new ArrayList();

        /// <summary>
        /// Creates an instance of a DpwsServiceDescriptions collection.
        /// </summary>
        internal DpwsServiceDescriptions()
        {
        }

        internal DpwsServiceDescriptions(XmlReader reader, DpwsServiceDescription.ServiceDescriptionType type)
        {
            Microsoft.SPOT.Debug.Assert(type == DpwsServiceDescription.ServiceDescriptionType.ProbeMatch ||
                type == DpwsServiceDescription.ServiceDescriptionType.ResolveMatch);

            String collectionName, itemName;
            if (type == DpwsServiceDescription.ServiceDescriptionType.ProbeMatch)
            {
                collectionName = "ProbeMatches";
                itemName = "ProbeMatch";
            }
            else
            {
                collectionName = "ResolveMatches";
                itemName = "ResolveMatch";
            }

            reader.ReadStartElement(collectionName, WsWellKnownUri.WsdNamespaceUri);

            while (reader.IsStartElement(itemName, WsWellKnownUri.WsdNamespaceUri))
            {
#if DEBUG
                int depth = reader.Depth;
#endif
                m_serviceDescriptions.Add(new DpwsServiceDescription(reader, type));
#if DEBUG
                Microsoft.SPOT.Debug.Assert(XmlReaderHelper.HasReadCompleteNode(depth, reader));
#endif
            }

            if (type == DpwsServiceDescription.ServiceDescriptionType.ResolveMatch && m_serviceDescriptions.Count > 1)
            {
                // Per schema, there can only be 1 resolve match
                throw new XmlException();
            }

            XmlReaderHelper.SkipAllSiblings(reader); // xs:any
            reader.ReadEndElement(); // collectionName
        }

        /// <summary>
        /// Use to Get the number of DpwsServiceDescription elements actually contained in the collection.
        /// </summary>
        public int Count
        {
            get
            {
                return m_serviceDescriptions.Count;
            }
        }

        /// <summary>
        /// Use to Get or set the DpwsServiceDescription element at the specified index.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the DpwsServiceDescription element to get or set.
        /// </param>
        /// <returns>
        /// An intance of a DpwsServiceDescription element.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// If index is less than zero.-or- index is equal to or greater than the collection count.
        /// </exception>
        public DpwsServiceDescription this[int index]
        {
            get
            {
                return (DpwsServiceDescription)m_serviceDescriptions[index];
            }
        }

        public DpwsServiceDescription this[String endpointAddress]
        {
            get
            {
                DpwsServiceDescription serviceDescription;
                int count = m_serviceDescriptions.Count;
                for (int i = 0; i < count; i++)
                {
                    serviceDescription = (DpwsServiceDescription)m_serviceDescriptions[i];
                    if (serviceDescription.Endpoint.Address.AbsoluteUri == endpointAddress)
                    {
                        return serviceDescription;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Adds a DpwsServiceDescription to the end of the collection.
        /// </summary>
        /// <param name="value">
        /// The DpwsServiceDescription element to be added to the end of the collection.
        /// The value can be null.
        /// </param>
        /// <returns>
        /// The collection index at which the DpwsServiceDescription has been added.
        /// </returns>
        public int Add(DpwsServiceDescription value)
        {
            lock (m_threadLock)
            {
                return m_serviceDescriptions.Add(value);
            }
        }

        /// <summary>
        /// Removes all elements from the collection.
        /// </summary>
        public void Clear()
        {
            lock (m_threadLock)
            {
                m_serviceDescriptions.Clear();
            }
        }

        /// <summary>
        /// Determines whether an instance of a specified DpwsServiceDescription is in the collection.
        /// </summary>
        /// <param name="item">
        /// The DpwsServiceDescription to locate in the collection. The value can be null.
        /// </param>
        /// <returns>
        /// True if DpwsServiceDescription is found in the collection; otherwise, false.
        /// </returns>
        public bool Contains(DpwsServiceDescription item)
        {
            lock (m_threadLock)
            {
                return m_serviceDescriptions.Contains(item);
            }
        }

        /// <summary>
        /// Searches for a DpwsServiceDescription by endpoint address and returns the zero-based index
        /// of the first occurrence within the entire collection.
        /// </summary>
        /// <param name="endpointAddress">
        /// The endpoint address of the DpwsServiceDescription to locate in the collection.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence of DpwsServiceDescription within the entire collection,
        /// if found; otherwise, -1.
        /// </returns>
        public int IndexOf(string endpointAddress)
        {
            lock (m_threadLock)
            {
                for (int i = 0; i < m_serviceDescriptions.Count; i++)
                {
                    if (((DpwsServiceDescription)m_serviceDescriptions[i]).Endpoint.Address.AbsoluteUri == endpointAddress)
                        return i;
                }

                return -1;
            }
        }
    }
}


