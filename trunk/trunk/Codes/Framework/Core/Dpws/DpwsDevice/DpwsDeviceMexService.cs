using System;
using System.Collections;
using System.Text;
using System.IO;
using System.Xml;
using Dpws.Device;
using Dpws.Device.Services;
using Ws.Services;
using Ws.Services.WsaAddressing;
using Ws.Services.Xml;

namespace Dpws.Device.Mex
{

    // This is a Hosted Service class used by the Device to provide DPWS compliant metadata services.
    internal class DpwsDeviceMexService : DpwsHostedService
    {
        // Creates an instance of the metadata services class.
        public DpwsDeviceMexService()
        {
            Init();
        }

        // Add MEX namespaces, set the MEX address = to the device address, and adds the
        // Mex service to the internal device hosted service collection.
        private void Init()
        {

            // Set the service type name to internal to hide from discovery services
            ServiceTypeName = "Internal";

            // Set service namespace
            ServiceNamespace = new WsXmlNamespace("wsx", WsWellKnownUri.WsxNamespaceUri);

            // Set the endpoint address
            EndpointAddress = Device.EndpointAddress;

            // Add Discovery service operations
            ServiceOperations.Add(new WsServiceOperation(WsWellKnownUri.WstNamespaceUri, "Get"));
        }

        // MetadataExchange GetResponse service stub
        public byte[] Get(WsWsaHeader header, XmlReader reader)
        {
            DpwsWsxMetdataResponse mexResponse = new DpwsWsxMetdataResponse();
            return mexResponse.GetResponse(header, reader);
        }

    }

}


