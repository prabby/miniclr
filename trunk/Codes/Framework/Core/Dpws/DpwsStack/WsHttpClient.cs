using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using System.Text;

using System.Ext;

using Ws.Services.Mtom;
using Ws.Services.Soap;

namespace Ws.Services.Transport.HTTP
{
    /// <summary>
    /// Class used to send http request/response messages.
    /// </summary>
    public class WsHttpClient
    {
        private const int c_chunkSize = 0xff;
        private const String m_linearWhiteSpace = "\x09\x20";
        private WsMtomParams m_mtomHeader = null;

        /// <summary>
        /// Creates an instance of a WsHttpClient class.
        /// </summary>
        public WsHttpClient()
        {
        }

        /// <summary>
        /// Gets or sets the time in milliseconds this client will wait to send data to the remote endpoint.
        /// The defaule value is 60000 or 1 minute.
        /// </summary>
        public int SendTimeOut = 60000;

        /// <summary>
        /// Gets or sets the time in milliseconds that this client will wait for data from the remote endpoint.
        /// </summary>
        /// <remarks>The default value is 60000 or 1 minute.</remarks>
        public int ReceiveTimeout = 60000;

        /// <summary>
        /// Gets or sets the time in milliseconds that this client will wait for a HTTP response from the remote endpoint.
        /// </summary>
        /// <remarks>The default value is 60000 or 1 minute.</remarks>
        public int RequestTimeout = 60000;

        /// <summary>
        /// Send an Http request to an endpoint and waits for a response.
        /// </summary>
        /// <param name="soapMessage">A byte array containing the soap message to be sent.</param>
        /// <param name="remoteEndpoint">A sting containing the name of a remote listening endpoint.</param>
        /// <param name="isOneWay">A parameter used to specify if this is a one way transaction.</param>
        /// <param name="isChuncked">If true true the message will be chunk encoded.</param>
        /// <returns>
        /// A byte array containing a soap response to the request. This array will be null for OneWay request.
        /// </returns>
        public byte[] SendRequest(byte[] soapMessage, string remoteEndpoint, bool isOneWay, bool isChuncked)
        {
            WsMessage response = SendRequest(soapMessage, remoteEndpoint, isOneWay, isChuncked, null);
            if (response == null)
                return null;
            return response.Message;
        }

        /// <summary>
        /// Send an Http request to an endpoint and waits for a response.
        /// </summary>
        /// <param name="soapMessage">A byte array containing the soap message to be sent.</param>
        /// <param name="remoteEndpoint">A sting containing the name of a remote listening endpoint.</param>
        /// <param name="isOneWay">A parameter used to specify if this is a one way transaction.</param>
        /// <param name="isChuncked">If true true the message will be chunk encoded.</param>
        /// <param name="mtomParams">If not null contains parameters required to fix up the http header for mime multipart.</param>
        /// <returns>WsMessage object containing the soap response returned from a service endpoint.</returns>
        internal WsMessage SendRequest(byte[] soapMessage, string remoteEndpoint, bool isOneWay, bool isChuncked, WsMtomParams mtomParams)
        {
            System.Ext.Console.Write("Executing Send Request");

            HttpWebRequest request = HttpWebRequest.Create(remoteEndpoint) as HttpWebRequest;

            request.Timeout = RequestTimeout;
            request.ReadWriteTimeout = (ReceiveTimeout > SendTimeOut) ? ReceiveTimeout : SendTimeOut;
            
            // Post method
            request.Method = "POST";

            // If the message is Mtom
            if (mtomParams != null)
            {
                request.Headers.Add("Mime-Version", "1.0");
                request.ContentType = "Multipart/Related;boundary=" +
                    mtomParams.boundary +
                    ";type=\"application/xop+xml\";start=\"" +
                    mtomParams.start +
                    "\";start-info=\"application/soap+xml\"";
                request.Headers.Add("Content-Description", "WSDAPI MIME multipart");
            }
            else
            {
                request.ContentType = "application/soap+xml; charset=utf-8";
            }

            request.UserAgent = "MFWsAPI";
            request.KeepAlive = !isOneWay;

            request.Headers.Add("Cache-Control", "no-cache");
            request.Headers.Add("Pragma", "no-cache");

            // Not chunked. We know the full length of the content and send in one chunk.
            if (!isChuncked)
            {
                request.ContentLength = soapMessage.Length;

            }
            else // Set chunked property or content length on request.
            {
                System.Ext.Console.Write("Not supported");

            }

            // Now sending the message. GetRequestStream actually sends the headers.
            
            using(Stream reqStream = request.GetRequestStream())
            {
                // Write soap message
                reqStream.Write(soapMessage, 0, soapMessage.Length);
                // Flush the stream and force a write
                reqStream.Flush();
            
                // Reset the encoding flags
                m_mtomHeader = null;

                if (!isOneWay)
                {   // Get response, check the fields

                    HttpWebResponse resp = request.GetResponse() as HttpWebResponse;
                   
                    if (resp == null)
                    {
                        throw new WebException("No response was received on the HTTP channel", WebExceptionStatus.ReceiveFailure);
                    }
                    
                    try                    
                    {
                        if (resp.ProtocolVersion != HttpVersion.Version11)
                        {
                            throw new IOException("Invalid http version in response line.");
                        }

                        if (resp.StatusCode != HttpStatusCode.OK && resp.StatusCode != HttpStatusCode.Accepted)
                        {
                            throw new IOException("Bad status code in response: " + resp.StatusCode);
                        }

                        if (resp.ContentLength > 0)
                        {
                            
                            // Return the soap response.
                            byte[] soapResponse = new byte[resp.ContentLength];
                            Stream respStream = resp.GetResponseStream();

                            // Now need to read all data. We read in the loop until resp.ContentLength or zero bytes read.
                            // Zero bytes read means there was error on server and it did not send all data.
                            int respLength = (int)resp.ContentLength;
                            for (int totalBytesRead = 0; totalBytesRead < respLength; )
                            {
                                int bytesRead = respStream.Read(soapResponse, totalBytesRead, (int)resp.ContentLength - totalBytesRead);
                                // If nothing is read - means server closed connection or timeout. In this case no retry.
                                if (bytesRead == 0)
                                {
                                    break;
                                }

                                // Adds number of bytes read on this iteration.
                                totalBytesRead += bytesRead;
                            }

                            // If this is Mtom process the header
                            if (resp.Headers[HttpKnownHeaderNames.ContentType].ToLower().IndexOf("multipart/related;") != -1)
                            {
                                m_mtomHeader = ProcessMtomHeader(resp.Headers[HttpKnownHeaderNames.ContentType]);
                            }

                            WsMessage message;
                            if (m_mtomHeader != null)
                                message = new WsMessage(soapResponse, m_mtomHeader.boundary, m_mtomHeader.start);
                            else
                                message = new WsMessage(soapResponse);

                            return message;
                        }
                        //
                        // ContentLenght == 0 is OK
                        //
                        else if(resp.ContentLength < 0)
                        {
                            throw new ProtocolViolationException("Invalid http header, content length: " + resp.ContentLength);
                        }
                    }
                    finally
                    {
                        if(resp != null)
                        {
                            resp.Dispose();
                        }
                    }
                }
            }

            return null;
        }

        WsMtomParams ProcessMtomHeader(string mtomHeaderValue)
        {
            // Create the mtom header class
            WsMtomParams mtomHeader = new WsMtomParams();

            // Parse Mtom Content-Type parameters
            string[] fields = mtomHeaderValue.Substring(18).Split(';');
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
                            mtomHeader.boundary = param[1];
                            break;
                        case "TYPE":
                            mtomHeader.type = param[1];
                            break;
                        case "START":
                            mtomHeader.start = param[1];
                            break;
                        case "START-INFO":
                            mtomHeader.startInfo = param[1];
                            break;
                        default:
                            break;
                    }
                }
            }

            // Check required Mtom fields
            if (mtomHeader.boundary == null || mtomHeader.type == null || mtomHeader.start == null)
            {
                throw new ArgumentException("Bad content-type http response header. ErrorCode: 404");
            }

            return mtomHeader;
        }
    }
}


