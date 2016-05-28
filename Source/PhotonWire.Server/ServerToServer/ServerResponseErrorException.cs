using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotonWire.Server.ServerToServer
{
    public class ServerResponseErrorException : Exception
    {
        public short ReturnCode { get; }
        public string DebugMessage { get; }

        public ServerResponseErrorException(short returnCode, string debugMessage)
            : base($"Server returns error code, ReturnCode:{returnCode} DebugMessage:{debugMessage}")
        {
            this.ReturnCode = returnCode;
            this.DebugMessage = debugMessage;
        }
    }
}
