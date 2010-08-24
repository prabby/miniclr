using System;
using Ws.Services.Transport.HTTP;
using Ws.Services.Soap;
using Ws.Services.Utilities;
using Ws.Services.WsaAddressing;
using Ws.Services.Xml;
using System.Xml;
using Ws.Services;
using Microsoft.SPOT;
using Ws.Services.Mtom;
using System.Ext;

namespace Dpws.Client.Transport
{
    /// <summary>
    /// Class used to send and receive http messages. Dpws clients use this class to send and receive messages
    /// to and from a devices hosted services.
    /// </summary>
    public class DpwsHttpClient
    {
        private WsHttpClient m_httpClient = null;

        /// <summary>
        /// Creates an instance of an DpwsHttpClient class.
        /// </summary>
        public DpwsHttpClient()
        {
            m_httpClient = new WsHttpClient();
        }

        /// <summary>
        /// Use to get or set the time in milliseconds this client will wait for a remote endpoint connection.
        /// The defaule value is 45000 or 45 seconds.
        /// </summary>
        public int SendTimeOut { get { return m_httpClient.SendTimeOut; } set { m_httpClient.SendTimeOut = value; } }

        /// <summary>
        /// Use to get or set the time in milliseconds that this client will wait for a response from a remote endpoint.
        /// The default value is 45000 or 45 seconds.
        /// </summary>
        public int ReceiveTimeout { get { return m_httpClient.ReceiveTimeout; } set { m_httpClient.ReceiveTimeout = value; } }

        /// <summary>
        /// Send an Http request containing an mtom message to an endpoint and waits for a response.
        /// </summary>
        /// <param name="bodyParts">A reference to the WsMtomBodyParts collection used to generate a mime multipart message.</param>
        /// <param name="endpointAddress">A string containing the endpoint address of a service that will receive
        /// <param name="isOneway">True = don't wait for response, false means wait for a response.</param>
        /// <param name="isChuncked">If true true the message will be chunk encoded.</param>
        /// <returns>
        /// A DpwSoapResponse object containing a WsWsaHeader and an XmlReader or null if no response is received
        /// or parsing fails.
        /// </returns>
        public DpwsSoapResponse SendRequest(ref WsMtomBodyParts bodyParts, string endpointAddress, bool isOneWay, bool isChuncked)
        {
            WsMtomParams mtomParams = new WsMtomParams();
            if (bodyParts.Boundary == null)
                bodyParts.Boundary = Guid.NewGuid().ToString() + '-' + Guid.NewGuid().ToString().Substring(0, 33);
            mtomParams.start = bodyParts.Start;
            mtomParams.boundary = bodyParts.Boundary;
            WsMtom mtom = new WsMtom();
            byte[] message = mtom.CreateMessage(bodyParts);
            WsMessage response = SendRequest(message, endpointAddress, isOneWay, isChuncked, mtomParams);
            if (isOneWay)
                return null;

            XmlReader reader;
            WsWsaHeader header;
            try
            {
                reader = WsSoapMessageParser.ParseSoapMessage(response.Message, out header);
                bodyParts = response.BodyParts;
            }
            catch
            {
                System.Ext.Console.Write("ParseSoapMessage failed.");
                return null;
            }

            return new DpwsSoapResponse(header, reader);
        }

        /// <summary>
        /// Method used to send a soap request over http to a service endpoint.
        /// </summary>
        /// <param name="soapMessage">A byte array contining a soap request message.</param>
        /// <param name="endpointAddress">A string containing the endpoint address of a service that will receive
        /// the request. This must be a transport address in the format http://ip_address:port/service_address.</param>
        /// <param name="isOneway">True = don't wait for response, false means wait for a response.</param>
        /// <param name="isChuncked">If true true the message will be chunk encoded.</param>
        /// <returns>
        /// A DpwSoapResponse object containing a WsWsaHeader and a XmlReader or null if no response is received
        /// or parsing fails.
        /// </returns>
        public DpwsSoapResponse SendRequest(byte[] soapMessage, string endpointAddress, bool isOneway, bool isChuncked)
        {
            System.Ext.Console.Write(new string(System.Text.UTF8Encoding.UTF8.GetChars(soapMessage)));

            WsMessage response = SendRequest(soapMessage, endpointAddress, isOneway, isChuncked, null);
            if (isOneway)
                return null;

            XmlReader reader;
            WsWsaHeader header;

            try
            {
                reader = WsSoapMessageParser.ParseSoapMessage(response.Message, out header);
            }
            catch
            {
                System.Ext.Console.Write("ParseSoapMessage failed.");
                return null;
            }

            return new DpwsSoapResponse(header, reader);
        }

        /// <summary>
        /// Method used to send a soap request over http to a service endpoint.
        /// </summary>
        /// <param name="soapMessage">A byte array contining a soap request message.</param>
        /// <param name="endpointAddress">A string containing the endpoint address of a service that will receive
        /// the request. This must be a transport address in the format http://ip_address:port/service_address.</param>
        /// <param name="isOneway">True = don't wait for response, false means wait for a response.</param>
        /// <param name="isChuncked">If true true the message will be chunk encoded.</param>
        /// <param name="mtomParams">If not null contains parameters required to fix up the http header for mime multipart.</param>
        /// <returns>WsMessage object containing the soap response returned from a service endpoint.</returns>
        private WsMessage SendRequest(byte[] soapMessage, string endpointAddress, bool isOneway, bool isChuncked, WsMtomParams mtomParams)
        {
            if (soapMessage == null)
                throw new ArgumentNullException("DpwsHttpClient.SendRequest - soapMessage must not be null.");
            if (endpointAddress == null)
                throw new ArgumentNullException("DpwsHttpClient.SendRequest - endpointAddress must not be null.");

            // Send the request
            WsMessage response = m_httpClient.SendRequest(soapMessage, endpointAddress, isOneway, isChuncked, mtomParams);

            return response;
        }
    }

    /// <summary>
    /// Class contains a header and envelope object parsed from a soap response message.
    /// This object is returned by the SendRequest method.
    /// </summary>
    public class DpwsSoapResponse
    {
        public readonly WsWsaHeader Header;
        public readonly XmlReader Reader;

        /// <summary>
        /// Creates an instance of a DpwsSoapResponse class initialized with a header and envelope objects.
        /// </summary>
        public DpwsSoapResponse(WsWsaHeader header, XmlReader reader)
        {
            Header = header;
            Reader = reader;
        }
    }
}


