using System;

namespace IFY.Shimr
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

        public ShimProxyAttribute(Type implementationType)
            : this(implementationType, null!, ProxyBehaviour.Default)
        {
        }
        public ShimProxyAttribute(Type implementationType, ProxyBehaviour behaviour)
            : this(implementationType, null!, behaviour)
        {
        }
        public ShimProxyAttribute(Type implementationType, string implementationName)
            : this(implementationType, implementationName, ProxyBehaviour.Default)
        {
        }
        public ShimProxyAttribute(Type implementationType, string implementationName, ProxyBehaviour behaviour)
        {
            ImplementationType = implementationType;
            ImplementationName = implementationName;
            Behaviour = behaviour;
        }
    }
}
