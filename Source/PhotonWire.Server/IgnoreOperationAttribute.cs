using System;

namespace PhotonWire.Server
{
    /// <summary>
    /// Indicated cless or method don't register and don't generate in PhotonWire engine.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class IgnoreOperationAttribute : Attribute
    {

    }
}