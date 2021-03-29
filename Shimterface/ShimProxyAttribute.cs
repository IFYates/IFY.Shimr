using System;

namespace Shimterface
{
    /// <summary>
    /// Mark a shim member as being a proxy to an implementation elsewhere.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    public class ShimProxyAttribute : Attribute
    {
        /// <summary>
        /// The type that implements this member.
        /// </summary>
        public Type ImplementationType { get; }
        /// <summary>
        /// The name of the implemenation member.
        /// </summary>
        public string? ImplementationName { get; }
        /// <summary>
        /// The behaviour of this proxy member.
        /// </summary>
        public ProxyBehaviour Behaviour { get; }
        
        public ShimProxyAttribute(Type implType, ProxyBehaviour behaviour)
        {
            ImplementationType = implType;
            Behaviour = behaviour;
        }
        public ShimProxyAttribute(Type implType, string? implName = null, ProxyBehaviour behaviour = ProxyBehaviour.Default)
        {
            ImplementationType = implType;
            ImplementationName = implName;
            Behaviour = behaviour;
        }
    }
}
