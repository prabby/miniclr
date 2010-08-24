using System;
using System.Collections;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml;
using System.IO;
using Ws.Services.Faults;
using Ws.Services.Soap;
using Ws.Services.Transport;
using Ws.Services.Utilities;
using Ws.Services.WsaAddressing;

using System.Ext;
using Microsoft.SPOT;

namespace Ws.Services.Transport.UDP
{
    /// <summary>
    /// Udp service host listens for and processes Udp request made to it's service endpoints.
    /// </summary>
    internal class WsUdpServiceHost
    {
        // Fields
        private Socket m_udpReceiveClient;
        private static Socket m_udpSendClient;
        private static object m_udpSendLock = new object();
        private static bool m_servicesRunning = false;

        private const int c_discoveryPort = 3702;

        private bool m_requestStop = false;
        private Thread m_thread = null;
        private WsThreadManager m_threadManager = new WsThreadManager(5, "Udp");
        private WsServiceEndpoints m_serviceEndpoints;

        /// <summary>
        /// Creates and instance of a WsUdpServiceHost class.
        /// </summary>
        /// <param name="serviceEndpoints">A collection of service endpoints this transport service can dispatch to.</param>
        public WsUdpServiceHost(WsServiceEndpoints serviceEndpoints)
        {
            m_serviceEndpoints = serviceEndpoints;

            // Create a UdpClient used to receive multicast messages. Bind and join mutilcast group, set send timeout
            // Set the client (socket) to reuse addresses. Set the receive buffer size to 65K. This will limit the
            // memory use if threads start getting over worked.
            m_udpReceiveClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, c_discoveryPort);
            m_udpReceiveClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            m_udpReceiveClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 0x5000);
            m_udpReceiveClient.Bind(localEP);
            // Join Multicast Group
            byte[] multicastOpt = new byte[] { 239, 255, 255, 250,   // WsDiscovery Multicast Address: 239.255.255.250
                                                 0,   0,   0,   0 }; // IPAddress.Any: 0.0.0.0
            m_udpReceiveClient.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOpt);

            // Create a UdpClient used to send request responses. Set SendTimeout.
            m_udpSendClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            m_udpSendClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        }

        /// <summary>
        /// Property controls whether discovery request originating from this IP will be ignored.
        /// </summary>
        public bool IgnoreRequestFromThisIP;

        // This is the maximum size of the UDP datagram before it gets broken up
        // Since an incomplete packet would fail in the stack, there's no point to
        // read beyond the packetsize
        private const int c_MaxUdpPacketSize = 5229;

        /// <summary>
        /// Listens for Udp request on 239.255.255.250:3702
        /// </summary>
        /// <remarks>On initialization it sends a Discovery Hello message and listens on the Ws-Discovery
        /// endpoint for a request. When a request arrives it starts a UdpProcess thread that processes the message.
        /// The number of UdpProcessing threads are limited by the Device.MaxUdpRequestThreads property.
        /// </remarks>
        public void Listen()
        {
            // Create a duplicate message tester.
            WsMessageCheck messageCheck = new WsMessageCheck(40);

            // Create remote endpoint reference, start listening
            byte[] buffer = new byte[c_MaxUdpPacketSize];
            int size;
            bool threadPoolDepletedFlag = false;
            m_servicesRunning = !m_requestStop;
            while (!m_requestStop)
            {
                try
                {
                    // If threads ara availble receive next message. If we are waiting on threads let the socket
                    // buffer request until we get a thread. This will work until the reveice buffer is depleted
                    // at which time request will be dropped
                    if (m_threadManager.ThreadsAvailable == true)
                    {
                        threadPoolDepletedFlag = false;

                        EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

                        size = m_udpReceiveClient.ReceiveFrom(buffer, c_MaxUdpPacketSize, SocketFlags.None, ref remoteEP);

                        // If the stack is set to ignore request from this address do so
                        if (this.IgnoreRequestFromThisIP == true &&
                            ((IPEndPoint)remoteEP).Address.ToString() == WsNetworkServices.GetLocalIPV4Address())
                        {
                            continue;
                        }

                        if (size > 0)
                        {
                            byte[] soapMessage = new byte[size];
                            Array.Copy(buffer, soapMessage, size);

                            System.Ext.Console.Write("UDP Request From: " + remoteEP.ToString());
                            System.Ext.Console.Write(new String(System.Text.Encoding.UTF8.GetChars(soapMessage)));

                            // Try to get a processing thread and process the request
                            m_threadManager.StartNewThread(new WsUdpMessageProcessor(m_serviceEndpoints, soapMessage, (IPEndPoint)remoteEP, messageCheck));
                        }
                        else
                        {
                            System.Ext.Console.Write("UDP Receive returned 0 bytes");
                        }
                    }
                    else
                    {
                        if (threadPoolDepletedFlag == false)
                        {
                            System.Ext.Console.Write("Udp service host waiting for a thread...");
                            threadPoolDepletedFlag = true;
                        }
                    }
                }
                catch (SocketException se)
                {
                    // Since the MF Socket does not have IOControl that would be used to turn off ICMP notifications
                    // for UDP, catch 10054 and try to continue
                    if (se.ErrorCode == 10054)
                        continue;
                }
                catch (Exception e)
                {
                    System.Ext.Console.Write(e.Message + " " + e.InnerException);
                }

                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Use to determine if UDP service are running.
        /// </summary>
        public bool IsRunning { get { return m_servicesRunning; } }

        /// <summary>
        /// Use to get or set the maximum number of processing threads for Udp request. Default is 5.
        /// </summary>
        public int MaxThreadCount { get { return m_threadManager.MaxThreadCount; } set { m_threadManager.MaxThreadCount = value; } }

        /// <summary>
        /// Sends a Udp message to a remote endpoint.
        /// </summary>
        /// <param name="remoteEP">A IPEndPoint address containing the destination address for the request.</param>
        /// <param name="message">A byte array containing a soap response message.</param>
        /// <remarks>
        /// To save resources and the overhead of creating and destroying thread based UdpClients,
        /// this method uses a single UdpCLient. Since all udp senders will use this method it will block.
        /// </remarks>
        internal static void SendMessage(IPEndPoint remoteEP, byte[] message)
        {
            lock (m_udpSendLock)
            {
                Random rand = new Random();
                for (int i = 0; i < 3; ++i)
                {
                    int backoff = rand.Next(200) + 50; // 50-250
                    Thread.Sleep(backoff);
                    m_udpSendClient.SendTo(message, message.Length, SocketFlags.None, remoteEP);
                }
            }
        }

        /// <summary>
        /// Use to start the Udp service listening.
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
        /// Stops the WsUdpServiceHost listening process.
        /// </summary>
        public void Stop()
        {
            // Stop processing loop;
            m_requestStop = true;
            m_servicesRunning = false;

            // Close Socket
            m_udpReceiveClient.Close();
            m_udpSendClient.Close();

            // Release the thread reference
            while (m_thread.IsAlive == true)
                Thread.Sleep(100);

            m_thread = null;
        }
    }

    /// <summary>
    /// The WsUdpServiceHost spins a UdpProcess thread for each request. This class is responsible for sending a
    /// request to the processing system and returning a Udp response.
    /// </summary>
    sealed class WsUdpMessageProcessor : IWsTransportMessageProcessor
    {
        private WsServiceEndpoints m_serviceEndpoints = null;
        private byte[] m_soapMessage;
        private IPEndPoint m_remoteEP;
        private WsMessageCheck m_messageCheck;

        /// <summary>
        /// Creates an empty instance of the UdpProcess class.
        /// </summary>
        public WsUdpMessageProcessor(WsServiceEndpoints serviceEndpoints, byte[] soapMessage, IPEndPoint remoteEP, WsMessageCheck messageCheck)
        {
            m_serviceEndpoints = serviceEndpoints;
            m_soapMessage = soapMessage;
            m_remoteEP = remoteEP;
            m_messageCheck = messageCheck;
        }

        /// <summary>
        /// This method is called by the process manager to process a request.
        /// </summary>
        public void ProcessRequest()
        {

            // Performance debuging
            DebugTiming timeDebuger = new DebugTiming();
            timeDebuger.ResetStartTime("***Request Debug timer started");

            // Process the message
            byte[] soapResponse = ProcessRequestMessage();

            // If response is null the requested service is not implemented so just ignore this request
            if (soapResponse == null)
                return;

            // Performance debuging
            timeDebuger.PrintElapsedTime("***ProcessMessage Took");

            // Send the response
            WsUdpServiceHost.SendMessage(m_remoteEP, soapResponse);

            // Performance Debuging
            timeDebuger.PrintElapsedTime("***Send Message Took");

            System.Ext.Console.Write("Response To: " + m_remoteEP.ToString());
            System.Ext.Console.Write(new String(System.Text.Encoding.UTF8.GetChars(soapResponse)));

            return;
        }

        /// <summary>
        /// Parses a Udp transport message and builds a header object and envelope document then calls processRequest
        /// on a service endpoint contained.
        /// </summary>
        /// <param name="soapRequest">A byte array containing a raw soap request message.  If null no check is performed.</param>
        /// <param name="messageCheck">A WsMessageCheck objct used to test for duplicate request.</param>
        /// <param name="remoteEP">The remote endpoint address of the requestor.</param>
        /// <returns>A byte array containing a soap response message returned from a service endpoint.</returns>
        public byte[] ProcessRequestMessage()
        {
            Debug.Assert(m_messageCheck != null && m_remoteEP != null);

            // Parse and validate the soap message
            WsWsaHeader header;
            XmlReader reader;

            try
            {
                reader = WsSoapMessageParser.ParseSoapMessage(m_soapMessage, out header);
            }
            catch
            {
                return null;
            }

            try
            {
                if (m_messageCheck.IsDuplicate(header.MessageID, m_remoteEP.ToString()) == true)
                {
                    System.Ext.Console.Write("Duplicate \"" + header.Action + "\" request received");
                    System.Ext.Console.Write("Request Ignored.");
                    return null;
                }

                // Check Udp service endpoints collection for a target service.
                IWsServiceEndpoint serviceEndpoint = m_serviceEndpoints[header.To];
                if (serviceEndpoint != null && serviceEndpoint.ServiceOperations[header.Action] != null)
                {
                    // Don't block discovery processes.
                    serviceEndpoint.BlockingCall = false;
                    try
                    {
                        return serviceEndpoint.ProcessRequest(header, reader);
                    }
                    catch
                    {
                        return null;
                    }
                }

                // Return null if service endpoint was not found
                System.Ext.Console.Write("Udp service endpoint was not found.");
                System.Ext.Console.Write("  Endpoint Address: " + header.To);
                System.Ext.Console.Write("  Action: " + header.Action);
                return null;
            }
            finally
            {
                reader.Close();
            }
        }
    }
}


