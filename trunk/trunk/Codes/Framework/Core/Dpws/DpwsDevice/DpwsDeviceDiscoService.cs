using System;
using System.Collections;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Xml;
using System.Threading;
using Dpws.Device;
using Dpws.Device.Services;
using Ws.Services;
using Ws.Services.Transport;
using Ws.Services.WsaAddressing;
using Ws.Services.Xml;

using System.Ext;
using System.Ext.Xml;

namespace Dpws.Device.Discovery
{

    // This hosted service class provides DPWS compliant Ws-Discovery Probe services.
    internal class DpwsDeviceDiscoService : DpwsHostedService
    {
        public DpwsDeviceDiscoService()
        {
            Init();
        }

        // Add Ws-Discovery namespaces, set the Discovery service address = to the device address, and adds the
        // Discovery service to the internal device hosted service collection.
        private void Init()
        {
            // Set the service type name to internal to hide from discovery services
            ServiceTypeName = "Internal";

            // Set service namespace
            ServiceNamespace = new WsXmlNamespace("wsd", WsWellKnownUri.WsdNamespaceUri);

            // Set endpoint address
            EndpointAddress = Device.DiscoVersion.WellKnownAddress;

            // Add Discovery service operations
            ServiceOperations.Add(new WsServiceOperation(WsWellKnownUri.WsdNamespaceUri, "Probe"));
            ServiceOperations.Add(new WsServiceOperation(WsWellKnownUri.WsdNamespaceUri, "Resolve"));
        }

        // ProbeMatch response stub
        public byte[] Probe(WsWsaHeader header, XmlReader reader)
        {
            // If Adhoc disco is turned off return null. Adhoc disco is optional with a Discovery Proxy
            if (Device.SupressAdhoc == true)
                return null;

            reader.ReadStartElement("Probe", WsWellKnownUri.WsdNamespaceUri);

            bool match = true;

            if (reader.IsStartElement("Types", WsWellKnownUri.WsdNamespaceUri))
            {
                // Look for specified type, send probe match if any instance of any of the listed types are found
                match = false;
                string[] typesList = reader.ReadElementString().Split(' ');

                int count = typesList.Length;
                for (int i = 0; i < count; i++)
                {
                    string type = typesList[i];
                    // Parse type
                    string namespaceUri, prefix, typeName;
                    int namespaceIndex = type.IndexOf(':');
                    if (namespaceIndex == -1)
                    {
                        namespaceUri = "";
                        typeName = type;
                    }
                    else
                    {
                        if (namespaceIndex == type.Length - 1)
                            throw new XmlException("Probe - Invalid type name: " + type);

                        prefix = type.Substring(0, namespaceIndex);
                        namespaceUri = reader.LookupNamespace(prefix);

                        if (namespaceUri == null)
                        {
                            namespaceUri = prefix;
                        }

                        typeName = type.Substring(namespaceIndex + 1);
                    }

                    // Check for the dpws standard type
                    if (namespaceUri == WsWellKnownUri.WsdpNamespaceUri && typeName == "Device")
                    {
                        match = true;
                        break;
                    }

                    // If there is a host check it
                    if (Device.Host != null)
                    {
                        if (Device.Host.ServiceNamespace.NamespaceURI == namespaceUri && Device.Host.ServiceTypeName == typeName)
                        {
                            match = true;
                            break;
                        }
                    }

                    // Check for matching service
                    int servicesCount = Device.HostedServices.Count;
                    DpwsHostedService hostedService;
                    for (i = 0; i < servicesCount; i++)
                    {
                        hostedService = (DpwsHostedService)Device.HostedServices[i];
                        // Skip internal services
                        if (hostedService.ServiceTypeName == "Internal")
                            continue;

                        if (hostedService.ServiceNamespace.NamespaceURI == namespaceUri &&
                            hostedService.ServiceTypeName == typeName)
                        {
                            match = true;
                            break;
                        }
                    }
                }
            }

            // For completeness sake
            // We don't care about the rest...
            XmlReaderHelper.SkipAllSiblings(reader);
            reader.ReadEndElement(); // Probe

            if (match == true)
            {
                return new DpwsDeviceDiscovery().ProbeMatch(header, reader);
            }

            return null;
        }

        // ResolveMatch response stub
        public byte[] Resolve(WsWsaHeader header, XmlReader reader)
        {
            // If Adhoc disco is turned off return null. Adhoc disco is optional with a Discovery Proxy
            if (Device.SupressAdhoc == true)
                return null;

            return new DpwsDeviceDiscovery().ResolveMatch(header, reader);
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
        public override string Namespace { get {  return WsWellKnownUri.WsdNamespaceUri; } }
    }

    /// <summary>
    /// Discovery client used to send managed Hello and Bye messages
    /// </summary>
    public class ManagedDiscovery
    {
        /// <summary>
        /// Method used to send an http Hello message to specified endpoint. Use for managed discovery.
        /// </summary>
        /// <param name="endpointAddress">A string containing the endpoint address of a listening client.</param>
        public void DirectedHello(string endpointAddress)
        {
            byte[] greetingsMessage = DpwsDiscoGreeting.BuildHelloMessage(endpointAddress, null, null);
            Ws.Services.Transport.HTTP.WsHttpClient httpClient = new Ws.Services.Transport.HTTP.WsHttpClient();
            httpClient.SendRequest(greetingsMessage, endpointAddress, true, false);
            return;
        }

        /// <summary>
        /// Method used to send an http Bye message to specified endpoint. Use for managed discovery.
        /// </summary>
        /// <param name="endpointAddress">A string containing the endpoint address of a listening client.</param>
        public void DirectedBye(string endpointAddress)
        {
            byte[] greetingsMessage = DpwsDiscoGreeting.BuildByeMessage(endpointAddress, null, null);
            Ws.Services.Transport.HTTP.WsHttpClient httpClient = new Ws.Services.Transport.HTTP.WsHttpClient();
            httpClient.SendRequest(greetingsMessage, endpointAddress, true, false);
            return;
        }
    }
    
    /// <summary>
    /// Discovery client used to send Hello and Bye messages
    /// </summary>
    internal static class DpwsDiscoGreeting
    {
        private static IPEndPoint DiscoveryEP;

        private const long DiscoveryAddress = 239L + (255L << 0x08) + (255L << 0x10) + (250L << 0x18);
        private const int DiscoveryPort = 3702;
        private const int MulticastUdpRepeat = 4;
        private const int UdpUpperDelay = 500;
        private const int UdpMinDelay = 50;
        private const int UdpMaxDelay = 250;

        /// <summary>
        /// Method used to send Hello and Bye messages
        /// </summary>
        /// <param name="greetingType">An integer representing the type of greeting 0 = Hello, 1 = Bye.</param>
        public static void SendGreetingMessage(bool isHello)
        {
            // If Adhoc disco is turned off return. Adhoc disco is optional with a Discovery Proxy
            if (Device.SupressAdhoc == true)
                return;
            
            byte[] greetingsMessage = null;
            if (isHello)
                greetingsMessage = BuildHelloMessage(Device.DiscoVersion.WellKnownAddress, null, null);
            else
                greetingsMessage = BuildByeMessage(Device.DiscoVersion.WellKnownAddress, null, null);

            // Create a UdpClient used to send Hello and Bye messages
            Socket udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // Very important - Set default multicast interface for the underlying socket
            byte[] ipBytes = IPAddress.Parse(WsNetworkServices.GetLocalIPV4Address()).GetAddressBytes();
            long longIP = (long)((ipBytes[0] + (ipBytes[1] << 0x08) + (ipBytes[2] << 0x10) + (ipBytes[3] << 0x18)) & 0xFFFFFFFF);
            udpClient.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, (int)longIP);

            if (DiscoveryEP == null)
            {
                DiscoveryEP = new IPEndPoint(new IPAddress(DiscoveryAddress), DiscoveryPort);
            }

            // Random back off implemented as per soap over udp specification
            // for unreliable multicast message exchange
            Random rand = new Random();
            int backoff = 0;
            for (int i = 0; i < MulticastUdpRepeat; ++i)
            {
                if (i == 0)
                {
                    backoff = rand.Next(UdpMaxDelay - UdpMinDelay) + UdpMinDelay;
                }
                else
                {
                    backoff = backoff * 2;
                    backoff = backoff > UdpUpperDelay ? UdpUpperDelay : backoff;
                }

                Thread.Sleep(backoff);
                udpClient.SendTo(greetingsMessage, greetingsMessage.Length, SocketFlags.None, DiscoveryEP);
            }

            udpClient.Close();
        }

        internal static byte[] BuildHelloMessage(string endpointAddress, WsWsaHeader header, XmlReader reader)
        {
            MemoryStream soapStream = new MemoryStream();
            XmlWriter xmlWriter = XmlWriter.Create(soapStream);

            WsXmlNamespaces additionalPrefixes = null;
            // If a Host exist write the Host namespace
            if (Device.Host != null)
            {
                additionalPrefixes = new WsXmlNamespaces();
                additionalPrefixes.Add(Device.Host.ServiceNamespace);
            }

            WsWsaHeader helloHeader = new WsWsaHeader(
                WsWellKnownUri.WsdNamespaceUri + "/Hello",                              // Action
                null,                                                                   // RelatesTo
                endpointAddress,                                                        // To
                null, null, null);                                                      // ReplyTo, From, Any

            WsSoapMessageWriter.WriteSoapMessageStart(xmlWriter,
                WsSoapMessageWriter.Prefixes.Wsd | WsSoapMessageWriter.Prefixes.Wsdp,   // Prefix
                additionalPrefixes,                                                     // Additional Prefixes
                helloHeader,                                                            // Header
                new WsSoapMessageWriter.AppSequence(Device.AppSequence, Device.SequenceID, Device.MessageID)); // AppSequence

            // write body
            xmlWriter.WriteStartElement("wsd", "Hello", WsWellKnownUri.WsdNamespaceUri);
            xmlWriter.WriteStartElement("wsa", "EndpointReference", WsWellKnownUri.WsaNamespaceUri_2005_08);
            xmlWriter.WriteStartElement("wsa", "Address", WsWellKnownUri.WsaNamespaceUri_2005_08);
            xmlWriter.WriteString(Device.EndpointAddress);
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();

            // Write hosted service types
            xmlWriter.WriteStartElement("wsd", "Types", WsWellKnownUri.WsdNamespaceUri);
            WriteDeviceServiceTypes(xmlWriter, false);
            xmlWriter.WriteEndElement(); // End Types

            xmlWriter.WriteStartElement("wsd", "XAddrs", WsWellKnownUri.WsdNamespaceUri);
            xmlWriter.WriteString(Device.TransportAddress);
            xmlWriter.WriteEndElement();
            xmlWriter.WriteStartElement("wsd", "MetadataVersion", WsWellKnownUri.WsdNamespaceUri);
            xmlWriter.WriteString(Device.MetadataVersion.ToString());
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();

            // Flush and close writer. Return stream buffer
            WsSoapMessageWriter.WriteSoapMessageEnd(xmlWriter);

            xmlWriter.Flush();
            xmlWriter.Close();
            return soapStream.ToArray();
        }

        // Write Service Types list. Optionally add hosted service to the list
        private static void WriteDeviceServiceTypes(XmlWriter xmlWriter, bool includeHostedServices)
        {
            // Write the default wsdp:Device type
            xmlWriter.WriteString("wsdp:Device");

            // If device has a Host service add the Host service type
            if (Device.Host != null)
            {
                DpwsHostedService hostedService = Device.Host;

                // Add a space delimiter
                xmlWriter.WriteString(" ");

                string serviceType = (string)hostedService.ServiceNamespace.Prefix + ":" + hostedService.ServiceTypeName;
                xmlWriter.WriteString(serviceType);
            }

            // Add hosted service to list if required
            if (includeHostedServices)
                WriteHostedServiceTypes(xmlWriter);

        }

        // Write Hosted Service Types
        private static void WriteHostedServiceTypes(XmlWriter xmlWriter)
        {

            // Step through list of hosted services and add types to list
            int count = Device.HostedServices.Count;
            DpwsHostedService hostedService;
            String typesString = string.Empty;
            for (int i = 0; i < count; i++)
            {
                hostedService = (DpwsHostedService)Device.HostedServices[i];

                // Skip internal services
                if (hostedService.ServiceTypeName == "Internal")
                    continue;
                typesString += hostedService.ServiceNamespace.Prefix + ":" + hostedService.ServiceTypeName + " ";
            }

            xmlWriter.WriteString(typesString);
        }

        // Build ws-discovery bye message
        internal static byte[] BuildByeMessage(string endpointAddress, WsWsaHeader header, XmlReader reader)
        {
            MemoryStream soapStream = new MemoryStream();
            XmlWriter xmlWriter = XmlWriter.Create(soapStream);

            WsWsaHeader byeHeader = new WsWsaHeader(
                WsWellKnownUri.WsdNamespaceUri + "/Bye",            // Action
                null,                                               // RelatesTo
                endpointAddress,                                    // To
                null, null, null);                                  // ReplyTo, From, Any

            WsSoapMessageWriter.WriteSoapMessageStart(xmlWriter,
                WsSoapMessageWriter.Prefixes.Wsd,                   // Prefix
                null,                                               // Additional Prefix
                byeHeader,                                          // Header
                new WsSoapMessageWriter.AppSequence(Device.AppSequence, Device.SequenceID, Device.MessageID)); // AppSequence

            // write body
            xmlWriter.WriteStartElement("wsd", "Bye", WsWellKnownUri.WsdNamespaceUri);
            xmlWriter.WriteStartElement("wsa", "EndpointReference", WsWellKnownUri.WsaNamespaceUri_2005_08);
            xmlWriter.WriteStartElement("wsa", "Address", WsWellKnownUri.WsaNamespaceUri_2005_08);
            xmlWriter.WriteString(Device.EndpointAddress);
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();
            xmlWriter.WriteStartElement("wsd", "XAddrs", WsWellKnownUri.WsdNamespaceUri);
            xmlWriter.WriteString(Device.TransportAddress);
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();

            WsSoapMessageWriter.WriteSoapMessageEnd(xmlWriter);

            // Flush and close writer. Return stream buffer
            xmlWriter.Flush();
            xmlWriter.Close();
            return soapStream.ToArray();
        }
    }
}


