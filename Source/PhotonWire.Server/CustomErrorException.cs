using System;

namespace PhotonWire.Server
{
    public class CustomErrorException : Exception
    {
        /// <summary>
        /// Receiving returnCode. Code must not be 0 or -1.
        /// </summary>
        public short ReturnCode { get; }

        /// <summary>
        /// Return message to client.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Return parameter, only photon supported type.
        /// </summary>
        public object Parameter { get; }

        /// <summary>
        /// Client receive CustomErrorException.
        /// </summary>
        /// <param name="returnCode">Receiving returnCode. Code must not be 0 or -1.</param>
        public CustomErrorException(short returnCode)
        {
            if (returnCode == 0 || returnCode == -1) throw new ArgumentOutOfRangeException("returnCode must not be 0 or -1");

            this.ReturnCode = returnCode;
        }

        /// <summary>
        /// Client receive CustomErrorException.
        /// </summary>
        /// <param name="returnCode">Receiving returnCode. Code must not be 0 or -1.</param>
        /// <param name="errorMessage">Return message</param>
        public CustomErrorException(short returnCode, string errorMessage)
        {
            if (returnCode == 0 || returnCode == -1) throw new ArgumentOutOfRangeException("returnCode must not be 0 or -1");

            this.ReturnCode = returnCode;
            this.ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Client receive CustomErrorException.
        /// </summary>
        /// <param name="returnCode">Receiving returnCode. Code must not be 0 or -1.</param>
        /// <param name="parameter">Return parameter, only photon supported type: http://doc.photonengine.com/en/onpremise/current/reference/serialization-in-photon </param>
        public CustomErrorException(short returnCode, object parameter)
        {
            if (returnCode == 0 || returnCode == -1) throw new ArgumentOutOfRangeException("returnCode must not be 0 or -1");

            this.ReturnCode = returnCode;
            this.Parameter = parameter;
        }

        /// <summary>
        /// Client receive CustomErrorException.
        /// </summary>
        /// <param name="returnCode">Receiving returnCode. Code must not be 0 or -1.</param>
        /// <param name="errorMessage">Return message</param>
        /// <param name="parameter">Return parameter, only photon supported type: http://doc.photonengine.com/en/onpremise/current/reference/serialization-in-photon </param>
        public CustomErrorException(short returnCode, string errorMessage, object parameter)
        {
            if (returnCode == 0 || returnCode == -1) throw new ArgumentOutOfRangeException("returnCode must not be 0 or -1");

            this.ReturnCode = returnCode;
            this.ErrorMessage = errorMessage;
            this.Parameter = parameter;
        }
    }
}