using System;
using System.Collections;
using System.Text;
using System.IO;
using System.Xml;
using Ws.Services.WsaAddressing;

using System.Ext;
using System.Ext.Xml;
using Ws.Services.Utilities;
using Ws.Services.Soap;

namespace Ws.Services.Faults
{

    public class WsFaultException : Exception
    {
        String _message;

        public WsFaultException(WsWsaHeader header, WsFaultType faultType)
            : this(header, faultType, null)
        {
        }

        public WsFaultException(WsWsaHeader header, WsFaultType faultType, String message)
        {
            this.Header = header;
            this.FaultType = faultType;
            _message = message;
        }

        public readonly WsWsaHeader Header;

        public readonly WsFaultType FaultType;

        public override string Message
        {
            get
            {
                if (_message == null)
                {
                    return base.Message;
                }
                else
                {
                    return _message;
                }
            }
        }
    }

    /// <summary>
    /// Enumeration used to represent fault types.
    /// </summary>
    public enum WsFaultType
    {
        // Exception Fault Types

        /// <summary>
        /// Fault sent to indicate a general purpose exception has been thrown.
        /// </summary>
        Exception,
        /// <summary>
        /// Fault sent to indicate an ArgumentException has been thrown.
        /// </summary>
        ArgumentException,
        /// <summary>
        /// Fault sent to indicate an ArgumentNullException has been thrown.
        /// </summary>
        ArgumentNullException,
        /// <summary>
        /// Fault sent to indicate an InvalidOperationException has been thrown.
        /// </summary>
        InvalidOperationException,
        /// <summary>
        /// Fault sent to indicate an XmlException has been thrown.
        /// </summary>
        XmlException,

        // Ws-Addressing Fault Types

        /// <summary>
        /// Fault sent when the message information header cannot be processed.
        /// </summary>
        WsaInvalidMessageInformationHeader,
        /// <summary>
        /// Fault sent when the a required message information header is missing.
        /// </summary>
        WsaMessageInformationHeaderRequired,
        /// <summary>
        /// Fault sent when the endpoint specified in a message information header cannot be found.
        /// </summary>
        WsaDestinationUnreachable,
        /// <summary>
        /// Fault sent when the action property is not supported at the specified endpoint.
        /// </summary>
        WsaActionNotSupported,
        /// <summary>
        /// Fault sent when the endpoint is unable to process the message at this time.
        /// </summary>
        WsaEndpointUnavailable,

        // Ws-Eventing Fault Types

        /// <summary>
        /// Fault sent when a Subscribe request specifies an unsupported delivery mode for an event source.
        /// </summary>
        WseDeliverModeRequestedUnavailable,
        /// <summary>
        /// Fault sent when a Subscribe request contains an expiration value of 0.
        /// </summary>
        WseInvalidExpirationTime,
        /// <summary>
        /// Fault when a Subscrube request contains an unsupported expiration type.
        /// </summary>
        WseUnsupportedExpirationType,
        /// <summary>
        /// Fault sent when a Subscribe request contains a filter and the event source does not support filtering.
        /// </summary>
        WseFilteringNotSupported,
        /// <summary>
        /// Fault sent when a Subscribe request contains an unsupported filter dialect.
        /// </summary>
        WseFilteringRequestedUnavailable,
        /// <summary>
        /// Fault sent when an event source is unable to process a subscribe request for local reasons.
        /// </summary>
        WseEventSourceUnableToProcess,
        /// <summary>
        /// Fault sent when an event source is unable to renew an event subscription.
        /// </summary>
        WseUnableToRenew,
        /// <summary>
        /// Fault sent when an event subscription request has an invalid or unsupported message format.
        /// </summary>
        WseInvalidMessage,
    }

    internal static class WsFault
    {
        internal static WsMessage GenerateFaultResponse(WsFaultException e)
        {
            return GenerateFaultResponse(e.Header, e.FaultType, e.Message);
        }

        internal static WsMessage GenerateFaultResponse(WsWsaHeader header, WsFaultType faultType, String details)
        {
            String code = String.Empty;
            String subcode = String.Empty;
            String reason = String.Empty;

            switch (faultType)
            {
                case WsFaultType.ArgumentException:
                    code = "soap:Receiver";
                    subcode = "Ws:ArgumentException";
                    reason = "One of the arguments provided to a method is not valid.";
                    break;
                case WsFaultType.ArgumentNullException:
                    code = "soap:Receiver";
                    subcode = "Ws:ArgumentNullException";
                    reason = "A null reference was passed to a method that does not accept it as a valid argument.";
                    break;
                case WsFaultType.Exception:
                    code = "soap:Receiver";
                    subcode = "Ws:Exception";
                    reason = "Errors occured during application execution.";
                    break;
                case WsFaultType.InvalidOperationException:
                    code = "soap:Receiver";
                    subcode = "Ws:InvalidOperationException";
                    reason = "A method call is invalid for the object's current state.";
                    break;
                case WsFaultType.XmlException:
                    code = "soap:Receiver";
                    subcode = "Ws:XmlException";
                    reason = "Syntax errors found during parsing.";
                    break;

                case WsFaultType.WsaInvalidMessageInformationHeader:
                    code = "soap:Sender";
                    subcode = "wsa:InvalidMessageInformationHeader";
                    reason = "A message information header is not valid and cannot be processed.";
                    break;
                case WsFaultType.WsaMessageInformationHeaderRequired:
                    code = "soap:Sender";
                    subcode = "wsa:MessageInformationHeaderRequired";
                    reason = "A required message Information header, To, MessageID, or Action, is not present";
                    break;
                case WsFaultType.WsaDestinationUnreachable:
                    code = "soap:Sender";
                    subcode = "wsa:DestinationUnreachable";
                    reason = "No route can be determined to reach the destination role defined by the WS=Addressing To.";
                    break;
                case WsFaultType.WsaActionNotSupported:
                    code = "soap:Sender";
                    subcode = "wsa:ActionNotSupported";
                    reason = "The [action] cannot be processed at the receiver.";
                    break;
                case WsFaultType.WsaEndpointUnavailable:
                    code = "soap:Receiver";
                    subcode = "wsa:EndpointUnavailable";
                    reason = "The endpoint is unable to process the message at this time.";
                    break;

                case WsFaultType.WseDeliverModeRequestedUnavailable:
                    code = "soap:Sender";
                    subcode = "wse:DeliverModeRequestedUnavailable";
                    reason = "The request delivery mode is not supported.";
                    break;
                case WsFaultType.WseInvalidExpirationTime:
                    code = "soap:Sender";
                    subcode = "wse:InvalidExpirationTime";
                    reason = "The expiration time requested is invalid.";
                    break;
                case WsFaultType.WseUnsupportedExpirationType:
                    code = "soap:Sender";
                    subcode = "wse:UnsupportedExpirationType";
                    reason = "Only expiration durations are supported.";
                    break;
                case WsFaultType.WseFilteringNotSupported:
                    code = "soap:Sender";
                    subcode = "wse:FilteringNotSupported";
                    reason = "Filtering is not supported.";
                    break;
                case WsFaultType.WseFilteringRequestedUnavailable:
                    code = "soap:Sender";
                    subcode = "wse:FilteringRequestedUnavailable";
                    reason = "The requested filter dialect is not supported.";
                    break;
                case WsFaultType.WseEventSourceUnableToProcess:
                    code = "soap:Receiver";
                    subcode = "wse:EventSourceUnableToProcess";
                    reason = "No explaination yet.";
                    break;
                case WsFaultType.WseUnableToRenew:
                    code = "soap:Receiver";
                    subcode = "wse:UnableToRenew";
                    reason = "No explaination yet.";
                    break;
                case WsFaultType.WseInvalidMessage:
                    code = "soap:Sender";
                    subcode = "wse:InvalidMessage";
                    reason = "Message is not valid and cannot be processed.";
                    break;
            }

            // Create the XmlWriter
            MemoryStream soapStream = new MemoryStream();
            XmlWriter xmlWriter = XmlWriter.Create(soapStream);

            // Generate the fault Header
            WsWsaHeader faultHeader = new WsWsaHeader(
                WsWellKnownUri.WsaNamespaceUri_2005_08 + "/fault", // Action
                header.MessageID,                          // RelatesTo
                "urn:schemas-xmlsoap-org:ws:2005:08:fault",// To
                null, null, null);                           // ReplyTo, From, Any

            WsSoapMessageWriter.WriteSoapMessageStart(xmlWriter, WsSoapMessageWriter.Prefixes.Wsdp, null, faultHeader, null);

            // Generate fault Body
            xmlWriter.WriteStartElement("wsa", "Fault", null);

            xmlWriter.WriteStartElement("wsa", "Code", null);
            xmlWriter.WriteStartElement("wsa", "Value", null);
            xmlWriter.WriteString(code);
            xmlWriter.WriteEndElement(); // End Value
            xmlWriter.WriteStartElement("wsa", "Subcode", null);
            xmlWriter.WriteStartElement("wsa", "Value", null);
            xmlWriter.WriteString(subcode);
            xmlWriter.WriteEndElement(); // End Value
            xmlWriter.WriteEndElement(); // End Subcode
            xmlWriter.WriteEndElement(); // End Code

            xmlWriter.WriteStartElement("wsa", "Reason", null);
            xmlWriter.WriteStartElement("wsa", "Text", null);
            xmlWriter.WriteAttributeString("xml", "lang", null, "en");
            xmlWriter.WriteString(reason);
            xmlWriter.WriteEndElement(); // End Text
            xmlWriter.WriteEndElement(); // End Reason

            xmlWriter.WriteStartElement("wsdp", "Detail", null);
            xmlWriter.WriteString(details);
            xmlWriter.WriteEndElement(); // End Detail

            xmlWriter.WriteEndElement(); // End Fault

            WsSoapMessageWriter.WriteSoapMessageEnd(xmlWriter);

            // Flush and close writer. Return stream buffer
            xmlWriter.Flush();
            xmlWriter.Close();

            WsMessage response = new WsMessage(soapStream.ToArray());
            return response;
        }
    }
}


