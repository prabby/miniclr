using System;
using System.Collections;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Xml;
using Ws.Services.WsaAddressing;
using Ws.Services.Faults;
using Ws.Services.Mtom;
using Ws.Services.Soap;
using Ws.Services.Transport;
using Ws.Services.Utilities;

using System.Ext;
using Microsoft.SPOT;

namespace Ws.Services.Transport.HTTP
{
    /// <summary>
    /// An Http Service host listens for and processes request made to it's service endpoints.
    /// </summary>
    internal class WsHttpServiceHost
    {
        // Fields
        HttpListener m_httpListener;
        private int m_Port;
        private bool m_requestStop = false;
        private Thread m_thread = null;
        private WsServiceEndpoints m_serviceEndpoints = new WsServiceEndpoints();
        private WsThreadManager m_threadManager = new WsThreadManager(5, "Http");

        private static int m_maxReadPayload = 0x20000;

        /// <summary>
        /// Creates a http service host.
        /// </summary>
        /// <param name="port">An integer containing the port number this host will listen on.</param>
        /// <param name="serviceEndpoints">A collection of service endpoints this transport service can dispatch to.</param>
        public WsHttpServiceHost(int port, WsServiceEndpoints serviceEndpoints)
        {
            m_Port = port;
            m_serviceEndpoints = serviceEndpoints;
            m_httpListener = new HttpListener("http", m_Port);
        }

        /// <summary>
        /// Use to get or set the maximum number of processing threads for Udp request. Default is 5.
        /// </summary>
        public int MaxThreadCount { get { return m_threadManager.MaxThreadCount; } set { m_threadManager.MaxThreadCount = value; } }

        /// <summary>
        /// Property containing the maximum message size this transport service will accept.
        /// </summary>
        public static int MaxReadPayload { get { return m_maxReadPayload; } set { m_maxReadPayload = value; } }

        /// <summary>
        /// Property containing the listers port number.
        /// </summary>
        public int Port { get { return m_Port; } }

        /// <summary>
        /// Use to start the Http Server listening for request.
        /// </summary>
        public void Start()
        {
            if (m_thread == null)
            {
                m_thread = new Thread(new ThreadStart(this.Listen));
                m_thread.Start();
            }
        }

        /// <summary>
        /// Use to stop the Http service.
        /// </summary>
        public void Stop()
        {
            m_requestStop = true;
            m_httpListener.Close();
            while (m_thread.IsAlive == true)
                Thread.Sleep(100);
            m_thread = null;
        }

        /// <summary>
        /// Collection property containing service endpoints for this service host.
        /// </summary>
        public WsServiceEndpoints ServiceEndpoints { get { return m_serviceEndpoints; } set { m_serviceEndpoints = value; } }

        /// <summary>
        /// HttpServer Socket Listener
        /// </summary>
        public void Listen()
        {
            // Create listener and start listening
            int threadCount = 0;
            m_httpListener.Start();
            while (m_requestStop == false)
            {
                HttpListenerContext context;
                try
                {
                    context = m_httpListener.GetContext();
                }
                catch (SocketException e)
                {
                    // If request to stop listener flag is set or locking call is interupted return
                    // The error code 10004 comes from the socket exception.
                    if (m_requestStop == true || e.ErrorCode == 10004)
                        return;

                    // Throw on any other error
                    System.Ext.Console.Write("Accept failed! Socket Error = " + e.ErrorCode);
                    throw e;
                }

                // The context returned by m_httpListener.GetContext(); can be null in case the service was stopped.
                if (context != null)
                {
                    WsHttpMessageProcessor processor = new WsHttpMessageProcessor(m_serviceEndpoints, context);

                    if (m_threadManager.ThreadsAvailable == false)
                    {
                        processor.SendError(503, "Service Unavailable");
                        System.Ext.Console.Write("Http max thread count " + threadCount + " exceeded. Request ignored.");
                        // System.Ext.Console.Write("Sending Service Unavailble to: " + sock.RemoteEndPoint.ToString());  Igor
                    }
                    else
                    {
                        // Try to get a processing thread and process the request
                        m_threadManager.StartNewThread(processor);
                    }
                }
            }
        }
    }

    sealed class WsHttpMessageProcessor : IDisposable, IWsTransportMessageProcessor
    {
        // Fields
        HttpListenerContext m_httpContext;
        HttpListenerRequest m_httpRequest;
        HttpListenerResponse m_httpResponse;
        private bool m_chunked = false;
        private WsMtomParams m_mtomHeader = null;
        private WsServiceEndpoints m_serviceEndpoints = null;

        private const int ReadPayload = 0x800;

        /// <summary>
        /// HttpProcess()
        ///     Summary:
        ///         Main Http processor class.
        /// </summary>
        /// <param name="serviceEndpoints">A collection of service endpoints.</param>
        /// <param name="s">
        /// Socket s
        /// </param>
        public WsHttpMessageProcessor(WsServiceEndpoints serviceEndpoints, HttpListenerContext httpContext)
        {
            m_httpContext = httpContext;
            m_serviceEndpoints = serviceEndpoints;
        }

        /// <summary>
        /// Releases all resources used by the HttpProcess object.
        /// </summary>
        public void Dispose()
        {
            m_mtomHeader = null;
            m_httpResponse.Close();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Http servers message processor. This method reads a message from a socket and calls downstream
        /// processes that parse the request, dispatch to a method and returns a response.
        /// </summary>
        /// <remarks>The parameters should always be set to null. See IWsTransportMessageProcessor for details.</remarks>
        public void ProcessRequest()
        {
            try
            {   // Retrieval of Request causes reading of data from network and parsing request.
                m_httpRequest = m_httpContext.Request;
                m_httpResponse = m_httpContext.Response;

                System.Ext.Console.Write("Request From: " + m_httpRequest.RemoteEndPoint.ToString());

                // Checks and process headers important for DPWS
                if (!ProcessKnownHeaders())
                {
                    return;
                }

                byte[] messageBuffer = null;

                int messageLength = (int)m_httpRequest.ContentLength64;
                if (messageLength > 0)
                {
                    // If there is content length for the message, we read it complete.
                    messageBuffer = new byte[messageLength];

                    for ( int offset = 0; offset < messageLength; )
                    {
                        int noRead = m_httpRequest.InputStream.Read(messageBuffer, offset, messageLength - offset);
                        if (noRead == 0)
                        {
                            throw new IOException("Http server got only " + offset + " bytes. Expected to read " + messageLength + " bytes.");
                        }

                        offset += noRead;
                    }
                }
                else
                {
                    // In this case the message is chunk encoded, but m_httpRequest.InputStream actually does processing.
                    // So we we read until zero bytes are read.
                    bool readComplete = false;
                    int bufferSize = ReadPayload;
                    messageBuffer = new byte[bufferSize];
                    int offset = 0;
                    while (!readComplete)
                    {
                        while (offset < ReadPayload)
                        {
                            int noRead = m_httpRequest.InputStream.Read(messageBuffer, offset, messageLength - offset);
                            // If we read zero bytes - means this is end of message. This is how InputStream.Read for chunked encoded data.
                            if (noRead == 0)
                            {
                                readComplete = true;
                                break;
                            }

                            offset += noRead;
                        }

                        // If read was not complete - increase the buffer.
                        if (!readComplete)
                        {
                            bufferSize += ReadPayload;
                            byte[] newMessageBuf = new byte[bufferSize];
                            Array.Copy(messageBuffer, newMessageBuf, offset);
                            messageBuffer = newMessageBuf;
                        }
                    }

                    m_chunked = false;
                }

                // Process the soap request.
                try
                {
                    // Message byte buffer
                    byte[] soapRequest;
                    byte[] soapResponse = null;

                    // If this is an mtom message process attachments, else process the raw message
                    if (m_mtomHeader != null)
                    {
                        // Process the message
                        WsMessage response = ProcessRequestMessage(new WsMessage(messageBuffer, m_mtomHeader.boundary, m_mtomHeader.start));
                        if (response != null)
                        {

                            soapResponse = response.Message;
                            if (response.MessageType == WsMessageType.Mtom)
                            {
                                m_mtomHeader.boundary = response.BodyParts.Boundary;
                                m_mtomHeader.start = response.BodyParts.Start;
                            }
                        }
                    }
                    else
                    {
                        // Convert the message buffer to a byte array
                        soapRequest = messageBuffer;

                        // Performance debuging
                        DebugTiming timeDebuger = new DebugTiming();
                        timeDebuger.ResetStartTime("***Request Debug timer started");

                        // Process the message
                        WsMessage response = ProcessRequestMessage(new WsMessage(soapRequest));
                        if (response != null)
                            soapResponse = response.Message;

                        // Performance debuging
                        timeDebuger.PrintElapsedTime("***ProcessMessage Took");
                    }

                    // Display remote endpoint
                    System.Ext.Console.Write("Response To: " + m_httpRequest.RemoteEndPoint.ToString());

                    // Send the response
                    SendResponse(soapResponse);
                }
                catch (Exception e)
                {
                    System.Ext.Console.Write(e.Message + " " + e.InnerException);
                    SendError(400, "Bad Request");
                }
            }
            catch
            {
                System.Ext.Console.Write("Invalid request format. Request ignored.");
                SendError(400, "Bad Request");
            }
            finally
            {
                m_httpResponse.Close();
            }
        }

        /// <summary>
        /// Parses a transport message and builds a header object and envelope document then calls processRequest
        /// on a service endpoint.
        /// </summary>
        /// <param name="soapRequest">WsRequestMessage object containing a raw soap message or mtom soap request.</param>
        /// <returns>WsResponseMessage object containing the soap response returned from a service endpoint.</returns>
        private WsMessage ProcessRequestMessage(WsMessage soapRequest)
        {
            // Parse and validate the soap message
            WsWsaHeader header;
            XmlReader reader;

            try
            {
                reader = WsSoapMessageParser.ParseSoapMessage(soapRequest.Message, out header);
            }
            catch (WsFaultException e)
            {
                return WsFault.GenerateFaultResponse(e);
            }

            try
            {
                // Now check for implementation specific service endpoints.
                IWsServiceEndpoint serviceEndpoint = null;
                string endpointAddress = null;

                // If this is Uri convert it
                if (header.To.IndexOf("urn") == 0 || header.To.IndexOf("http") == 0)
                {

                    // Convert to address to Uri
                    Uri toUri;
                    try
                    {
                        toUri = new Uri(header.To);
                    }
                    catch
                    {
                        System.Ext.Console.Write("Unsupported Header.To Uri format: " + header.To);
                        return WsFault.GenerateFaultResponse(header, WsFaultType.ArgumentException, "Unsupported Header.To Uri format");
                    }

                    // Convert the to address to a Urn:uuid if it is an Http endpoint
                    if (toUri.Scheme == "urn")
                        endpointAddress = toUri.AbsoluteUri;
                    else if (toUri.Scheme == "http")
                    {
                        endpointAddress = "urn:uuid:" + toUri.AbsoluteUri.Substring(1);
                    }
                    else
                        endpointAddress = header.To;
                }
                else
                    endpointAddress = "urn:uuid:" + header.To;

                // Look for a service at the requested endpoint that contains an operation matching the Action
                // This hack is required because service spec writers determined that more than one service type
                // can live at a single endpoint address. Why you would want to break the object model and allow
                // this feature is unknown so for now we must hack. 
                bool eventingReqFlag = true;
                for (int i = 0; i < m_serviceEndpoints.Count; ++i)
                {
                    if (m_serviceEndpoints[i].EndpointAddress == endpointAddress)
                    {
                        if (m_serviceEndpoints[i].ServiceOperations[header.Action] != null)
                        {
                            serviceEndpoint = m_serviceEndpoints[i];
                            eventingReqFlag = false;
                            break;
                        }
                    }
                }

                // Worst part of the hack: If no matching endpoint is found assume this is an event subscription
                // request and call the base eventing methods on any class. They had to be Global because of this feature.
                // Now the subscription manager must determine globally that a suitable web service is found. Yuch!!
                if (eventingReqFlag)
                {
                    serviceEndpoint = m_serviceEndpoints[0];
                }

                // If a matching service endpoint is found call operation
                if (serviceEndpoint != null)
                {
                    // If this is mtom, copy the requests body parts to the hosted services body parts
                    // prior to making the call
                    if (soapRequest.MessageType == WsMessageType.Mtom)
                        serviceEndpoint.BodyParts = soapRequest.BodyParts;

                    // Process the request
                    byte[] response;
                    try
                    {
                        response = serviceEndpoint.ProcessRequest(header, reader);
                    }
                    catch (WsFaultException e)
                    {
                        return WsFault.GenerateFaultResponse(e);
                    }
                    catch (Exception e)
                    {
                        return WsFault.GenerateFaultResponse(header, WsFaultType.Exception, e.ToString());
                    }

                    // If the message response type is Soap return a SoapMessage type
                    if (serviceEndpoint.MessageType == WsMessageType.Soap)
                    {
                        if (response == null)
                            return null;

                        return new WsMessage(response);
                    }

                    // If the response is Mtom build an mtom response message
                    else // if (serviceEndpoint.MessageType == WsMessageType.Mtom)
                    {
                        return new WsMessage(serviceEndpoint.BodyParts);
                    }
                }

                // Unreachable endpoint requested. Generate fault response
                return WsFault.GenerateFaultResponse(header, WsFaultType.WsaDestinationUnreachable, "Unknown service endpoint");
            }
            finally
            {
                reader.Close();
            }
        }

        /// <summary>
        /// Verifies values of specific headers.
        /// </summary>
        /// <returns>True if parsing is successful</returns>
        private bool ProcessKnownHeaders()
        {
            HttpStatusCode errorCode = 0;
            string errorName = "";
            // The HTTP methid should be GET or POST
            if (m_httpRequest.HttpMethod != "POST" && m_httpRequest.HttpMethod != "GET")
            {
                errorCode = HttpStatusCode.NotImplemented;
            }

            // HTTP version should be 1.1
            if (m_httpRequest.ProtocolVersion != HttpVersion.Version11)
            {
                errorCode = HttpStatusCode.BadRequest;
            }

            if (m_httpRequest.ContentLength64 > WsHttpServiceHost.MaxReadPayload)
            {
                errorCode = HttpStatusCode.Forbidden; // 403
                errorName = HttpKnownHeaderNames.ContentLength;
            }

            WebHeaderCollection webHeaders = (System.Net.WebHeaderCollection)m_httpRequest.Headers;
            string strChunked = webHeaders[HttpKnownHeaderNames.TransferEncoding];
            if (strChunked != null)
            {
                if (strChunked == "chunked")
                {
                    m_chunked = true;
                }
                else
                {
                    errorCode = HttpStatusCode.NotFound; // 404
                    errorName = HttpKnownHeaderNames.TransferEncoding;
                }
            }

            string strContentType = m_httpRequest.ContentType;
            if (strContentType != null)
            {
                strContentType = strContentType.ToLower();
                
                if (strContentType.IndexOf("multipart/related;") == 0)
                {
                    // Create the mtom header class
                    m_mtomHeader = new WsMtomParams();

                    // Parse Mtom Content-Type parameters
                    string[] fields = strContentType.Substring(18).Split(';');
                    int fieldsLen = fields.Length;
                    for (int i = 0; i < fieldsLen; ++i)
                    {
                        string[] param = fields[i].Split('=');
                        if (param.Length > 1)
                        {
                            switch (param[0].ToUpper())
                            {
                                case "BOUNDARY":
                                    if (param[1].Length > 72)
                                        throw new ArgumentException("Mime boundary element length exceeded.", "boundary");
                                    m_mtomHeader.boundary = param[1];
                                    break;
                                case "TYPE":
                                    m_mtomHeader.type = param[1];
                                    break;
                                case "START":
                                    m_mtomHeader.start = param[1];
                                    break;
                                case "START-INFO":
                                    m_mtomHeader.startInfo = param[1];
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    // Check required Mtom fields
                    if (m_mtomHeader.boundary == null || m_mtomHeader.type == null || m_mtomHeader.start == null)
                    {
                        errorCode = HttpStatusCode.NotFound;
                        errorName = HttpKnownHeaderNames.ContentType;
                    }
                }
                else if (strContentType.IndexOf("application/soap+xml") != 0)
                {
                    errorCode = HttpStatusCode.NotFound;
                    errorName = HttpKnownHeaderNames.ContentType;
                }
            }

            string strMimeVersion = webHeaders["Mime-Version"];
            if (strMimeVersion != null)
            {
                if (strMimeVersion != "1.0")
                {
                    errorCode = HttpStatusCode.NotFound;
                    errorName = "Mime-Version";
                }
            }

            if (errorCode != 0)
            {
                SendError((int)errorCode, errorName);
                return false;
            }

            return true;
        }

        /// <summary>
        /// SendError()
        ///     Summary:
        ///         Sends Http Error messages.
        ///     Arguments:
        ///         int errorCode
        /// </summary>
        public void SendError(int errorCode, string message)
        {
            try
            {
                m_httpResponse.StatusCode = errorCode;
                m_httpResponse.StatusDescription = message;
                m_httpResponse.Close();
                System.Ext.Console.Write("Http error response: " + "HTTP/1.1 " + errorCode + " " + message);
            }
            catch (Exception e)
            {
                System.Ext.Console.Write("Http error response failed send: " + e.Message);
            }
        }

        /// <summary>
        /// SendResponse()
        ///     Summary:
        ///         Sends success header and soap message.
        ///     Arguments:
        ///         byte[] soapMessage
        /// </summary>
        /// <param name="soapMessage">A byte array containing a soap message.</param>
        public void SendResponse(byte[] soapMessage)
        {
            StreamWriter streamWriter = new StreamWriter(m_httpResponse.OutputStream);
            // Write Header, if message is null write accepted
            if (soapMessage == null)
                m_httpResponse.StatusCode = 202;
            else
                m_httpResponse.StatusCode = 200;

            // Check to see it the hosted service is sending mtom
            WebHeaderCollection webHeaders = m_httpResponse.Headers;
            if (m_mtomHeader != null)
            {
                string responseLine = HttpKnownHeaderNames.ContentType + ": Multipart/Related;boundary=" +
                    m_mtomHeader.boundary +
                    ";type=\"application/xop+xml\";start=\"" +
                    m_mtomHeader.start +
                    "\";start-info=\"application/soap+xml\"";

                System.Ext.Console.Write(responseLine);
                webHeaders.Add(responseLine);

                responseLine = "Server: Microsoft-MF HTTP 1.0";
                System.Ext.Console.Write(responseLine);
                webHeaders.Add(responseLine);

                responseLine = "MIME-Version: 1.0";
                System.Ext.Console.Write(responseLine);
                webHeaders.Add(responseLine);

                responseLine = "Date: " + DateTime.Now.ToString();
                System.Ext.Console.Write(responseLine);
                webHeaders.Add(responseLine);

            }
            else
            {
                m_httpResponse.ContentType = "application/soap+xml; charset=utf-8";
                System.Ext.Console.Write(HttpKnownHeaderNames.ContentType + ": " + m_httpResponse.ContentType);
            }

            // If chunked encoding is enabled write chunked message else write Content-Length
            if (m_chunked)
            {
                string responseLine = "Transfer-Encoding: chunked";
                webHeaders.Add(responseLine);
                System.Ext.Console.Write(responseLine);

                // Chunk message
                int bufferIndex = 0;
                int chunkSize = 0;
                int defaultChunkSize = 0xff;
                byte[] displayBuffer = new byte[defaultChunkSize];
                while (bufferIndex < soapMessage.Length)
                {

                    // Calculate chunk size and write to stream
                    chunkSize = soapMessage.Length - bufferIndex < defaultChunkSize ? soapMessage.Length - bufferIndex : defaultChunkSize;
                    streamWriter.WriteLine(chunkSize.ToString("{0:X}"));
                    System.Ext.Console.Write(chunkSize.ToString("{0:X}"));

                    // Write chunk
                    streamWriter.WriteBytes(soapMessage, bufferIndex, chunkSize);
                    streamWriter.WriteLine();
                    for (int i = 0; i < chunkSize; ++i)
                        displayBuffer[i] = soapMessage[bufferIndex + i];
                    System.Ext.Console.Write(new String(System.Text.Encoding.UTF8.GetChars(displayBuffer), bufferIndex, chunkSize));

                    // Adjust buffer index
                    bufferIndex = bufferIndex + chunkSize;
                }

                // Write 0 length and blank line
                streamWriter.WriteLine("0");
                streamWriter.WriteLine();
                System.Ext.Console.Write("0");
                System.Ext.Console.Write("");

            }
            else
            {
                if (soapMessage == null)
                {
                    m_httpResponse.ContentLength64 = 0;
                }
                else
                {
                    m_httpResponse.ContentLength64 = soapMessage.Length;
                }

                System.Ext.Console.Write("Content Length: " + m_httpResponse.ContentLength64);

                // If an empty message is returned (i.e. oneway request response) don't send
                if (soapMessage != null && soapMessage.Length > 0)
                {
                    if (m_mtomHeader == null)
                        // Display soap message
                        System.Ext.Console.Write(new String(System.Text.Encoding.UTF8.GetChars(soapMessage)));
                    else
                        System.Ext.Console.Write("Mtom message sent.");

                    // Write soap message
                    streamWriter.WriteBytes(soapMessage, 0, soapMessage.Length);
                }
            }

            // Flush the stream and return
            streamWriter.Flush();
            m_httpResponse.Close();
            return;
        }
    }
}


