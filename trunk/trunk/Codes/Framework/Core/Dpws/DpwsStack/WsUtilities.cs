using System;
using System.Collections;
using System.Text;
using System.Net;
using Ws.Services.WsaAddressing;

using Microsoft.SPOT.Net.NetworkInformation;
using System.IO;
using System.Xml;
using Ws.Services.Faults;
using System.Ext.Xml;
using Ws.Services.Xml;
using System.Ext;

namespace Ws.Services
{
    /// <summary>
    /// A collection of common namespaces required by soap based standards and specifications
    /// encapsulated by the DPWS specification.
    /// </summary>
    public static class WsWellKnownUri
    {
        public const String SoapNamespaceUri = "http://www.w3.org/2003/05/soap-envelope";
        public const String WsaNamespaceUri_2004_08 = "http://schemas.xmlsoap.org/ws/2004/08/addressing";
        public const String WsaNamespaceUri_2005_08 = "http://www.w3.org/2005/08/addressing";
        public const String XopNamespaceUri = "http://www.w3.org/2004/08/xop/include";
        public const String WsdpNamespaceUri = "http://schemas.xmlsoap.org/ws/2006/02/devprof";
        public const String WseNamespaceUri = "http://schemas.xmlsoap.org/ws/2004/08/eventing";
        public const String WsxNamespaceUri = "http://schemas.xmlsoap.org/ws/2004/09/mex";
        private static String m_wsdNamespaceUri = "http://schemas.xmlsoap.org/ws/2005/04/discovery";
        public const String WstNamespaceUri = "http://schemas.xmlsoap.org/ws/2004/09/transfer";
        public const String SchemaNamespaceUri = "http://www.w3.org/2001/XMLSchema-instance";

        public const String WsaAnonymousUri = WsaNamespaceUri_2005_08 + "/anonymous";
        public const String WsaAnonymousRoleUri = WsaNamespaceUri_2005_08 + "/role/anonymous";
        public static String WsdNamespaceUri { get { return m_wsdNamespaceUri; } set { m_wsdNamespaceUri = value; } }
    }

    internal static class XmlReaderHelper
    {
        public static void SkipAllSiblings(XmlReader reader)
        {
            reader.MoveToContent();

            Microsoft.SPOT.Debug.Assert(reader.NodeType == XmlNodeType.Element || reader.NodeType == XmlNodeType.EndElement);

            // We don't care about the rest, skip it
            if (reader.NodeType == XmlNodeType.Element)
            {
                int targetDepth = reader.Depth - 1;
                while (reader.Read() && reader.Depth > targetDepth) ;

                Microsoft.SPOT.Debug.Assert(reader.NodeType == XmlNodeType.EndElement);
            }
        }

#if DEBUG
        public static bool HasReadCompleteNode(int oldDepth, XmlReader reader)
        {
            reader.MoveToContent();

            return (reader.NodeType == XmlNodeType.Element && reader.Depth == oldDepth) ||
                   (reader.NodeType == XmlNodeType.EndElement && reader.Depth == oldDepth - 1);
        }

#endif
    }

    internal static class WsSoapMessageParser
    {
        public static XmlReader ParseSoapMessage(byte[] soapMessage, out WsWsaHeader header)
        {
            MemoryStream requestStream = new MemoryStream(soapMessage);
            XmlReader reader = XmlReader.Create(requestStream);
            header = new WsWsaHeader();

            try
            {
                reader.ReadStartElement("Envelope", WsWellKnownUri.SoapNamespaceUri);
#if DEBUG
                int depth = reader.Depth;
#endif
                header.ParseHeader(reader);
#if DEBUG
                Microsoft.SPOT.Debug.Assert(XmlReaderHelper.HasReadCompleteNode(depth, reader));
#endif
                reader.ReadStartElement("Body", WsWellKnownUri.SoapNamespaceUri);

            }
            catch (XmlException e)
            {
                reader.Close();
                throw new WsFaultException(header, WsFaultType.XmlException, e.ToString());
            }

            return reader;
        }
    }

    internal static class WsSoapMessageWriter
    {
        [Flags]
        internal enum Prefixes
        {
            None = 0x00,
            Wsdp = 0x02,
            Wse = 0x04,
            Wsx = 0x08,
            Wsd = 0x10
        }

        internal class AppSequence
        {
            public String InstanceId;
            public String SequenceId;
            public String MessageNumber;

            public AppSequence(String instanceId, String sequenceId, String messageNumber)
            {
                this.InstanceId = instanceId;
                this.SequenceId = sequenceId;
                this.MessageNumber = messageNumber;
            }
        }

        public static String WriteSoapMessageStart(XmlWriter writer, Prefixes prefixes, WsXmlNamespaces additionalPrefixes,
            WsWsaHeader header, AppSequence appSequence)
        {
            String messageId = "urn:uuid:" + Guid.NewGuid();

            String xml =
                "<?xml version='1.0' encoding='UTF-8'?>" +
                "<soap:Envelope xmlns:soap='" + WsWellKnownUri.SoapNamespaceUri + "' " +
                "xmlns:wsa='" + WsWellKnownUri.WsaNamespaceUri_2005_08 + "' ";

            if ((prefixes & Prefixes.Wsdp) != Prefixes.None)
            {
                xml += "xmlns:wsdp='" + WsWellKnownUri.WsdpNamespaceUri + "' ";
            }

            if ((prefixes & Prefixes.Wse) != Prefixes.None)
            {
                xml += "xmlns:wse='" + WsWellKnownUri.WseNamespaceUri + "' ";
            }

            if ((prefixes & Prefixes.Wsx) != Prefixes.None)
            {
                xml += "xmlns:wsx='" + WsWellKnownUri.WsxNamespaceUri + "' ";
            }

            if ((prefixes & Prefixes.Wsd) != Prefixes.None || appSequence != null)
            {
                xml += "xmlns:wsd='" + WsWellKnownUri.WsdNamespaceUri + "' ";
            }

            if (additionalPrefixes != null)
            {
                int count = additionalPrefixes.Count;
                WsXmlNamespace current;
                for (int i = 0; i < count; i++)
                {
                    current = additionalPrefixes[i];
                    xml += "xmlns:" + current.Prefix + "='" + current.NamespaceURI + "' ";
                }
            }

            xml += ">" +
                "<soap:Header>" +
                    "<wsa:To soap:mustUnderstand=\"1\">" + header.To + "</wsa:To>" +
                    "<wsa:Action soap:mustUnderstand=\"1\">" + header.Action + "</wsa:Action>" +
                    "<wsa:MessageID>" + messageId + "</wsa:MessageID>";

            if (header.RelatesTo != null)
            {
                xml += "<wsa:RelatesTo>" + header.RelatesTo + "</wsa:RelatesTo>";
            }

            if (header.From != null)
            {
                xml += "<wsa:From><wsa:Address>" + header.From.Address.AbsoluteUri + "</wsa:Address></wsa:From>";
            }

            if (header.ReplyTo != null)
            {
                xml += "<wsa:ReplyTo><wsa:Address>" + header.ReplyTo.Address.AbsoluteUri + "</wsa:Address></wsa:ReplyTo>";
            }

            if (appSequence != null)
            {
                xml += "<wsd:AppSequence InstanceId='" + appSequence.InstanceId + "' " +
                    "SequenceId='" + appSequence.SequenceId + "' " +
                    "MessageNumber='" + appSequence.MessageNumber + "'/>";
            }

            writer.WriteRaw(xml);

            if (header.Any != null)
            {
                header.Any.WriteTo(writer);
            }

            writer.WriteRaw("</soap:Header><soap:Body>");

            return messageId;
        }

        public static void WriteSoapMessageEnd(XmlWriter writer)
        {
            writer.WriteRaw("</soap:Body></soap:Envelope>");
        }

    }
}

namespace Ws.Services.Utilities
{
    /// <summary>
    /// Class used by the Device and HostedService clsses to quickly validate a special case of UrnUuid.
    /// </summary>
    internal class WsUtilities
    {
        /// <summary>
        /// Validates a urn:uuid:Guid.
        /// </summary>
        /// <param name="uri">A uri.</param>
        /// <returns>True if this is a valid urn:uud:giud.</returns>
        public static bool ValidateUrnUuid(string uri)
        {
            // Validate UUID
            if (uri.IndexOf("urn:uuid:") != 0)
                return false;

            char[] tempUUID = uri.Substring(9).ToLower().ToCharArray();
            int length = tempUUID.Length;
            int uuidSegmentCount = 0;
            int[] delimiterIndexes = { 8, 13, 18, 23 };
            bool invalidUUID = false;
            for (int i = 0; i < length; ++i)
            {
                // Make sure these are valid hex numbers numbers
                if ((tempUUID[i] < '0' || tempUUID[i] > '9') && (tempUUID[i] < 'a' || tempUUID[i] > 'f') && tempUUID[i] != '-')
                {
                    invalidUUID = true;
                    break;
                }
                else
                {
                    // Check each segment length
                    if (tempUUID[i] == '-')
                    {
                        if (uuidSegmentCount > 3)
                        {
                            invalidUUID = true;
                            break;
                        }

                        if (i != delimiterIndexes[uuidSegmentCount])
                        {
                            invalidUUID = true;
                            break;
                        }

                        ++uuidSegmentCount;
                    }
                }
            }

            if (invalidUUID)
                return false;
            return true;
        }

    }

    /// <summary>
    /// Class used to format and parse duration time values.
    /// </summary>
    public class WsDuration
    {
        /// <summary>
        /// Creates and instance of a duration object initialized to a TimeSpan.
        /// </summary>
        /// <param name="timeSpan">A TimeSpan containing the duration value.</param>
        public WsDuration(TimeSpan timeSpan)
        {
            this.DurationInSeconds = (long)(timeSpan.Ticks * 0.0000001);
            this.Ticks = timeSpan.Ticks;
            this.DurationString = this.ToString(timeSpan);
        }

        /// <summary>
        /// Creates and instance of a duration object initialized to a number of seconds.
        /// </summary>
        /// <param name="seconds">A long containing the duration value in seconds.</param>
        public WsDuration(long seconds)
        {
            TimeSpan tempTS = new TimeSpan(seconds * 10000000);
            this.DurationInSeconds = seconds;
            this.Ticks = tempTS.Ticks;
            this.DurationString = this.ToString(tempTS);
        }

        /// <summary>
        /// Creates an instance of a duration object initialized to a formated duration string value.
        /// </summary>
        /// <param name="duration">A string containing a formated duration values (P#Y#M#DT#H#M#S). </param>
        public WsDuration(string duration)
        {
            TimeSpan tempTS = ParseDuration(duration);
            this.DurationString = duration;
            this.DurationInSeconds = (long)(tempTS.Ticks * 0.0000001);
            this.Ticks = tempTS.Ticks;
        }

        /// <summary>
        /// Use to get or set the number of seconds this duration object represents.
        /// </summary>
        public readonly long DurationInSeconds;

        /// <summary>
        /// Use to get a string containing a formated duration representing the number of seconds this duration
        /// object represents.
        /// </summary>
        public readonly String DurationString;

        /// <summary>
        /// Use to get a long containing the number of duration ticks.
        /// </summary>
        public readonly long Ticks;

        /// <summary>
        /// Creates a xml schema duration from a TimeSpan
        /// </summary>
        /// <param name="timeSpan">A TimeSpan containing the time value used to create the duration.</param>
        /// <returns>A string contining the duration in Xml Schema format.</returns>
        public string ToString(TimeSpan timeSpan)
        {
            string dur;
            if (timeSpan.Ticks < 0)
            {
                dur = "-P";
                timeSpan = timeSpan.Negate();
            }
            else
                dur = "P";

            int years = timeSpan.Days / 365;
            int days = timeSpan.Days - years * 365;
            dur += (years > 0 ? years + "Y" : "");
            dur += (days > 0 ? days + "D" : "");
            dur += (timeSpan.Hours > 0 || timeSpan.Minutes > 0 || timeSpan.Seconds > 0 || timeSpan.Milliseconds > 0 ? "T" : "");
            dur += (timeSpan.Hours > 0 ? timeSpan.Hours + "H" : "");
            dur += (timeSpan.Minutes > 0 ? timeSpan.Minutes + "M" : "");
            dur += (timeSpan.Seconds > 0 ? timeSpan.Seconds.ToString() : "");
            dur += (timeSpan.Milliseconds > 0 ? (timeSpan.Milliseconds * 0.001).ToString().Substring(1) + "S" : (timeSpan.Seconds > 0 ? "S" : ""));
            return dur;
        }

        /// <summary>
        /// Parses a duration string and assigns duration properties.
        /// </summary>
        /// <param name="duration">A string containing a valid duration value.</param>
        /// <remarks>A valid duration string conforms to the format: P#Y#M#DT#H#M#S.</remarks>
        private TimeSpan ParseDuration(string duration)
        {
            int multiplier = duration[0] == '-' ? -1 : 1;
            int start = multiplier == -1 ? 1 : 0;

            // Check for mandatory start sentinal
            if (duration[start] != 'P')
                throw new ArgumentException("Invalid duration format.");
            ++start;

            string sentinals = "PYMDTHMS";
            int[] durationBuf = { 0, 0, 0, 0, 0, 0, 0, 0 };
            string fieldValue;
            double seconds = 0;
            int fldIndex;
            int lastFldIndex = 0;
            for (int i = start; i < duration.Length; ++i)
            {
                char curChar = duration[i];

                if (curChar == '.')
                {
                    for (i = i + 1; i < duration.Length; ++i)
                    {
                        curChar = duration[i];
                        if (curChar == 'S')
                            break;
                    }

                    if (curChar != 'S')
                        throw new ArgumentException("Invalid duration format.");
                }

                if (curChar < '0' || curChar > '9')
                {
                    if ((fldIndex = sentinals.Substring(lastFldIndex).IndexOf(curChar)) == -1)
                        throw new ArgumentException("Invalid duration format.");
                    fldIndex += lastFldIndex;
                    lastFldIndex = fldIndex;

                    // Skip T sentinal
                    if (sentinals[fldIndex] == 'T')
                    {
                        start = i + 1;
                        continue;
                    }

                    // Check for blank fields
                    if (i - start < 1)
                        throw new ArgumentException("Invalid duration format.");

                    fieldValue = duration.Substring(start, i - start);
                    if (fldIndex == 7)
                        seconds = Convert.ToDouble(fieldValue);
                    else
                        durationBuf[fldIndex] = Convert.ToInt32(fieldValue);

                    start = i + 1;
                }
            }

            // Assign duration properties
            // days = years * 365 days + months * 31 days + days;
            int days = durationBuf[1] * 365 + durationBuf[2] * 31 + durationBuf[3];

            // Note: Adding 0.0001 temporarily fixes a double/rounding problem
            int milliseconds = (int)(((seconds - (int)seconds) + 0.0001) * 1000.00);
            if ((ulong)((((long)days * 86400L + (long)durationBuf[5] * 3600L + (long)durationBuf[6] * 60L + (long)(seconds)) * 10000000L) + (long)(milliseconds * 10000L)) > long.MaxValue)
                throw new ArgumentOutOfRangeException("Durations value exceeds TimeSpan.MaxValue.");
            TimeSpan tempTs = new TimeSpan(days, durationBuf[5], durationBuf[6], (int)seconds, milliseconds);
            return multiplier == -1 ? tempTs.Negate() : tempTs;
        }
    }
}


