using System;
using System.Collections;
using System.Text;
using System.Threading;
using System.IO;
using System.Xml;
using Dpws.Device;
using Dpws.Device.Services;
using Ws.Services.Utilities;
using Ws.Services.WsaAddressing;

using System.Ext;
using System.Ext.Xml;
using Ws.Services;
using Ws.Services.Faults;
using Ws.Services.Xml;

namespace Dpws.Device.Discovery
{
    internal class DpwsDeviceDiscovery
    {
        public virtual byte[] ProbeMatch(WsWsaHeader header, XmlReader reader)
        {

            // Performance debugging
            DebugTiming timeDebuger = new DebugTiming();
            long startTime = timeDebuger.ResetStartTime("");

            // Build ProbeMatch
            MemoryStream soapStream = new MemoryStream();
            XmlWriter xmlWriter = XmlWriter.Create(soapStream);

            // If a Host exist write the Host namespace
            WsXmlNamespaces additionalPrefixes = null;
            if (Device.Host != null)
            {
                additionalPrefixes = new WsXmlNamespaces();
                additionalPrefixes.Add(Device.Host.ServiceNamespace);
            }

            WsWsaHeader matchHeader = new WsWsaHeader(
                WsWellKnownUri.WsdNamespaceUri + "/ProbeMatches",                       // Action
                header.MessageID,                                                       // RelatesTo
                WsWellKnownUri.WsaAnonymousUri,                                         // To
                null, null, null);                                                      // ReplyTo, From, Any

            WsSoapMessageWriter.WriteSoapMessageStart(xmlWriter,
                WsSoapMessageWriter.Prefixes.Wsd | WsSoapMessageWriter.Prefixes.Wsdp,   // Prefix
                additionalPrefixes,                                                     // Additional Prefixes
                matchHeader,                                                            // Header
                new WsSoapMessageWriter.AppSequence(Device.AppSequence, Device.SequenceID, Device.MessageID)); // AppSequence

            // Performance debuging
            timeDebuger.PrintElapsedTime("*****Write Header Took");

            // write body
            xmlWriter.WriteStartElement("wsd", "ProbeMatches", WsWellKnownUri.WsdNamespaceUri);
            xmlWriter.WriteStartElement("wsd", "ProbeMatch", WsWellKnownUri.WsdNamespaceUri);
            xmlWriter.WriteStartElement("wsa", "EndpointReference", WsWellKnownUri.WsaNamespaceUri_2005_08);
            xmlWriter.WriteStartElement("wsa", "Address", WsWellKnownUri.WsaNamespaceUri_2005_08);
            xmlWriter.WriteString(Device.EndpointAddress);
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();

            // Write hosted service types
            xmlWriter.WriteStartElement("wsd", "Types", WsWellKnownUri.WsdNamespaceUri);
            WriteDeviceServiceTypes(xmlWriter);
            xmlWriter.WriteEndElement(); // End Types

            xmlWriter.WriteStartElement("wsd", "XAddrs", WsWellKnownUri.WsdNamespaceUri);
            xmlWriter.WriteString(Device.TransportAddress);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("wsd", "MetadataVersion", WsWellKnownUri.WsdNamespaceUri);
            xmlWriter.WriteString(Device.MetadataVersion.ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();

            // Create return buffer and close writer

            // Performance debuging
            timeDebuger.PrintElapsedTime("*****Write Body Took");

            WsSoapMessageWriter.WriteSoapMessageEnd(xmlWriter);

            // Performance debuging
            timeDebuger.PrintTotalTime(startTime, "***ProbeMatch Took");

            // Flush and close writer. Return stream buffer
            xmlWriter.Flush();
            xmlWriter.Close();

            // Delay probe match as per Ws-Discovery specification
            Thread.Sleep(new Random().Next(Device.ProbeMatchDelay));

            return soapStream.ToArray();

        }

        public virtual byte[] ResolveMatch(WsWsaHeader header, XmlReader reader)
        {
            reader.ReadStartElement("Resolve", WsWellKnownUri.WsdNamespaceUri);

            if (reader.IsStartElement("EndpointReference", WsWellKnownUri.WsaNamespaceUri_2005_08) == false)
            {
                return null;
            }

            WsWsaEndpointRef epRef = new WsWsaEndpointRef(reader);

            // If the destination endpoint is ours send a resolve match else return null
            if (Device.EndpointAddress != epRef.Address.AbsoluteUri)
                return null;

            // Build ResolveMatch
            MemoryStream soapStream = new MemoryStream();
            XmlWriter xmlWriter = XmlWriter.Create(soapStream);

            // If a Host exist write the Host namespace
            WsXmlNamespaces additionalPrefixes = null;
            if (Device.Host != null)
            {
                additionalPrefixes = new WsXmlNamespaces();
                additionalPrefixes.Add(Device.Host.ServiceNamespace);
            }

            WsWsaHeader matchHeader = new WsWsaHeader(
                WsWellKnownUri.WsdNamespaceUri + "/ResolveMatches",                     // Action
                header.MessageID,                                                       // RelatesTo
                WsWellKnownUri.WsaAnonymousUri,                                         // To
                null, null, null);                                                      // ReplyTo, From, Any

            WsSoapMessageWriter.WriteSoapMessageStart(xmlWriter,
                WsSoapMessageWriter.Prefixes.Wsd | WsSoapMessageWriter.Prefixes.Wsdp,   // Prefix
                additionalPrefixes,                                                     // Additional Prefixes
                matchHeader,                                                            // Header
                new WsSoapMessageWriter.AppSequence(Device.AppSequence, Device.SequenceID, Device.MessageID)); // AppSequence

            // write body
            xmlWriter.WriteStartElement("wsd", "ResolveMatches", WsWellKnownUri.WsdNamespaceUri);
            xmlWriter.WriteStartElement("wsd", "ResolveMatch", WsWellKnownUri.WsdNamespaceUri);
            xmlWriter.WriteStartElement("wsa", "EndpointReference", WsWellKnownUri.WsaNamespaceUri_2005_08);
            xmlWriter.WriteStartElement("wsa", "Address", WsWellKnownUri.WsaNamespaceUri_2005_08);
            xmlWriter.WriteString(Device.EndpointAddress);
            xmlWriter.WriteEndElement(); // End Address
            xmlWriter.WriteEndElement(); // End EndpointReference

            // Write hosted service types
            xmlWriter.WriteStartElement("wsd", "Types", WsWellKnownUri.WsdNamespaceUri);
            WriteDeviceServiceTypes(xmlWriter);
            xmlWriter.WriteEndElement(); // End Types

            xmlWriter.WriteStartElement("wsd", "XAddrs", WsWellKnownUri.WsdNamespaceUri);
            xmlWriter.WriteString(Device.TransportAddress);
            xmlWriter.WriteEndElement(); // End XAddrs

            xmlWriter.WriteStartElement("wsd", "MetadataVersion", WsWellKnownUri.WsdNamespaceUri);
            xmlWriter.WriteString(Device.MetadataVersion.ToString());
            xmlWriter.WriteEndElement(); // End MetadataVersion

            xmlWriter.WriteEndElement(); // End ResolveMatch
            xmlWriter.WriteEndElement(); // End ResolveMatches

            WsSoapMessageWriter.WriteSoapMessageEnd(xmlWriter);

            // Flush and close writer. Return stream buffer
            xmlWriter.Flush();
            xmlWriter.Close();
            return soapStream.ToArray();

        }

        // Write Service Types list.
        private void WriteDeviceServiceTypes(XmlWriter xmlWriter)
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
        }
    }

}


