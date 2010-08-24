using System;
using Microsoft.SPOT;
using Ws.Services.Mtom;

namespace Ws.Services.Soap
{
    /// <summary>
    /// Enum used to identify a soap request/response message type.
    /// </summary>
    public enum WsMessageType
    {
        /// <summary>
        /// The type of message is soap.
        /// </summary>
        Soap = 0,

        /// <summary>
        /// The type of message is Mtom.
        /// </summary>
        Mtom = 1,
    }

    /// <summary>
    /// Class used to pass soap or mtom messages from a transport service to the message processor.
    /// </summary>
    internal class WsMessage
    {
        /// <summary>
        /// Creates an instance of a WsRequestMessage class an initializes the soap message property.
        /// </summary>
        /// <param name="soapMessage">A byte array containing a soap request message.</param>
        /// <remarks>
        /// This class is used to create a simple soap request message object. Because Mtom message are identified
        /// via transport header fields (yuck) this class is required to abstract the transport messages.
        /// The transport services create this object and pass it to a message processor.
        /// With this contructor the request message is simply stored in the SoapMessage property.
        /// /// </remarks>
        public WsMessage(byte[] soapMessage)
        {
            Debug.Assert(soapMessage != null);
            this.Message = soapMessage;
            this.MessageType = WsMessageType.Soap;
            System.Ext.Console.Write(new String(System.Text.Encoding.UTF8.GetChars(soapMessage)));
        }

        /// <summary>
        /// Creates an instance of a WsRequestMessage class, parses the message into mtom body parts and assigns
        /// the start body part to the SoapMessage property.
        /// </summary>
        /// <param name="message">A byte array containing the message received by a transport.</param>
        /// <param name="boundary">A string containing a boundary element if the transport received an mtom message.</param>
        /// <param name="start">
        /// A string containing the ID of the mtom body part that contains the soap envelope
        /// of the mtom attachment.
        /// </param>
        /// <remarks>
        /// Because Mtom message are identified via transport header fields (yuck).
        /// The transport services create this object and pass it to a message processor. If this is a normal soap request, no
        /// additional processing is required and the SoapMessage property is set to the request message. If this is an
        /// Mtom message, the transport header contains an mtom boundary element and start content id needed to parse
        /// the mtom message. The boundary element is used to identify the start and end of mtom body parts sections.
        /// The start content id is used to identify the specific mtom body part that contains a soap envelope that
        /// references the additional body part elements. If an mtom message is created the constructor parses the
        /// mtom message and creates an mtom body parts object. For mtom the constructor also sets the SoapMessage
        /// property to the body part identified by the startContentID parmeter.
        /// </remarks>
        /// <exception cref="ArgumentNullException">If the message parameter is null.</exception>
        public WsMessage(byte[] message, string boundary, string start)
        {
            Debug.Assert(message != null && boundary != null && start != null);

            // Create an mtom object and parse the transport request message.
            WsMtom mtom = new WsMtom(message);

            this.Message = mtom.ParseMessage(boundary);
            this.MessageType = WsMessageType.Mtom;
            this.BodyParts = mtom.BodyParts;

            System.Ext.Console.Write("Mtom message received. " + this.BodyParts.Count + " body parts found.");
            System.Ext.Console.Write("Soap Message:");
            System.Ext.Console.Write(new String(System.Text.Encoding.UTF8.GetChars(this.BodyParts[0].Content.ToArray())));
        }

        /// <summary>
        /// Creates an instance of a WsResponseMessage inintilized from an mtom body parts object.
        /// </summary>
        /// <param name="bodyParts">An mtom body parts object.</param>
        /// <exception cref="ArgumentNullException">If the bodyParts parameter is null.</exception>
        /// <remarks>
        /// This contructor processes an mtom body parts collection and uilds an mtom message.
        /// It also sets the Boundary and StartContenID properties from the body parts object.
        /// </remarks>
        public WsMessage(WsMtomBodyParts bodyParts)
        {
            Debug.Assert(bodyParts != null);

            this.MessageType = WsMessageType.Mtom;

            this.Message = new WsMtom().CreateMessage(bodyParts);
            this.BodyParts = bodyParts;
        }

        /// <summary>
        /// Use to get the message type.
        /// </summary>
        public readonly WsMessageType MessageType;

        /// <summary>
        /// Use to get a byte array containing the request transport message.
        /// </summary>
        public readonly byte[] Message;

        /// <summary>
        /// Use to get an mtom body parts collection.
        /// </summary>
        public readonly WsMtomBodyParts BodyParts;
    }
}


