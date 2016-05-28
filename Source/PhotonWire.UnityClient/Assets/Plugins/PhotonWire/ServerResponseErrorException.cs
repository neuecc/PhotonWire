using System;

namespace PhotonWire.Client
{
    public sealed class ServerResponseErrorException : Exception
    {
        public short ReturnCode { get; private set; }
        public string DebugMessage { get; private set; }

        public ServerResponseErrorException(short returnCode, string debugMessage)
            : base(string.Format("Server returns error code, ReturnCode:{0} DebugMessage:{1}", returnCode, debugMessage))
        {
            this.ReturnCode = returnCode;
            this.DebugMessage = debugMessage;
        }

        public override string ToString()
        {
            return ReturnCode + " " + DebugMessage;
        }
    }
}
