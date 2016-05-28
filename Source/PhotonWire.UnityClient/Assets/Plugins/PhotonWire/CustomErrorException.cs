using System;

namespace PhotonWire.Client
{
    public sealed class CustomErrorException : Exception
    {
        public short ReturnCode { get; private set; }
        public string ErrorMessage { get; private set; }
        public object Parameter { get; private set; }

        public CustomErrorException(short returnCode, string errorMessage, object parameter)
        {
            this.ReturnCode = returnCode;
            this.ErrorMessage = errorMessage;
            this.Parameter = parameter;
        }

        public override string ToString()
        {
            return ReturnCode + " " + ErrorMessage + " " + (Parameter ?? "");
        }
    }
}