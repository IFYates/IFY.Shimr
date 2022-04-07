using System;
using System.Linq;
using System.Reflection;

namespace IFY.Shimr.Internal
{
    internal class ShimBinding
    {
        public MethodInfo InterfaceMethod { get; }
        public MemberInfo? ImplementedMember { get; private set; }
        public MemberInfo? ProxyImplementationMember { get; internal set; }
        public bool IsProperty { get; private set; }

        public ShimBinding(MethodInfo interfaceMethod)
        {
            InterfaceMethod = interfaceMethod;
        }

        public bool Resolve(Type implType, bool isConstructor)
        {
            return resolve(implType, isConstructor, true);
        }

        private bool resolve(Type implType, bool isConstructor, bool resolveProxy)
        {
            // If really a property, will need to get attributes from PropertyInfo
            var isPropertySetShim = InterfaceMethod.IsSpecialName && InterfaceMethod.Name.StartsWith("set_");
            IsProperty = isPropertySetShim || (InterfaceMethod.IsSpecialName && InterfaceMethod.Name.StartsWith("get_"));
            MemberInfo reflectMember = InterfaceMethod;
            var propertyType = InterfaceMethod.ReturnType;
            if (IsProperty)
            {
                reflectMember = InterfaceMethod.DeclaringType.GetProperty(InterfaceMethod.Name[4..]);
            }
            if (isPropertySetShim)
            {
                propertyType = InterfaceMethod.GetParameters().First().ParameterType;
            }

            // Decide if proxy
            ShimBinding? proxiedBinding = null;
            ShimProxyAttribute? proxyAttr = null;
            if (resolveProxy)
            {
                proxyAttr = reflectMember.GetCustomAttribute<ShimProxyAttribute>(false);
                if (proxyAttr != null)
                {
                    // Cannot proxy constructor
                    if (isConstructor)
                    {
                        throw new InvalidCastException($"Cannot proxy {implType.FullName} constructor in {InterfaceMethod.DeclaringType.FullName}");
                    }

                    proxiedBinding = new ShimBinding(InterfaceMethod);
                    proxiedBinding.resolve(implType, false, false);

                    // Confirm behaviour is valid
                    if (proxyAttr.Behaviour != ProxyBehaviour.Default)
                    {
                        if (proxyAttr.Behaviour == ProxyBehaviour.Add)
                        {
                            if (proxiedBinding.ImplementedMember != null)
                            {
                                throw new InvalidCastException($"Cannot proxy {implType.FullName} as {InterfaceMethod.DeclaringType.FullName}; adding existing method: {InterfaceMethod}");
                            }
                        }
                        else if (proxiedBinding.ImplementedMember == null)
                        {
                            throw new InvalidCastException($"Cannot proxy {implType.FullName} as {InterfaceMethod.DeclaringType.FullName}; override of missing method: {InterfaceMethod}");
                        }
                    }
                }
            }

            // Workout real parameter types
            var paramTypes = InterfaceMethod.GetParameters()
                .Select(p =>
                {
                    var paramAttr = p.GetCustomAttribute<TypeShimAttribute>();
                    if (paramAttr != null && !p.ParameterType.IsInterfaceType())
                    {
                        throw new NotSupportedException($"Shimmed parameter type must be an interface: {InterfaceMethod.DeclaringType.FullName}");
                    }
                    return paramAttr?.RealType ?? p.ParameterType;
                }).ToArray();
            void addInstanceParam(Type type)
            {
                var paramList = paramTypes.ToList();
                paramList.Insert(0, type);
                paramTypes = paramList.ToArray();
            }

            // Constructors don't provide other functionality
            if (isConstructor)
            {
                ImplementedMember = implType.GetConstructor(paramTypes, InterfaceMethod.GetGenericArguments());
                return ImplementedMember != null;
            }

            // Look for name/type override
            var implMemberName = InterfaceMethod.Name;
            var attr = reflectMember.GetAttribute<ShimAttribute>();
            if (attr?.ImplementationName != null)
            {
                implMemberName = (IsProperty ? implMemberName[0..4] : string.Empty)
                    + attr.ImplementationName;
            }
            if (attr?.DefinitionType != null)
            {
                implType = attr.DefinitionType;
            }

            // Handle proxy logic
            if (proxyAttr != null)
            {
                // Apply proxy redirect
                implType = proxyAttr.ImplementationType;
                implMemberName = proxyAttr.ImplementationName;

                if (!IsProperty)
                {
                    addInstanceParam(InterfaceMethod.DeclaringType);
                }
                else if (implMemberName != null)
                {
                    implMemberName = InterfaceMethod.Name[0..4] + implMemberName;
                }

                implMemberName ??= InterfaceMethod.Name;
            }

            // If member is a direct implementation, use it
            if (InterfaceMethod.ReflectedType.IsAssignableFrom(implType))
            {
                ImplementedMember = InterfaceMethod;
                return true;
            }

            // Find implementation return type
            Type? implReturnType = null;
            if (IsProperty)
            {
                var propName = implMemberName[4..];
                var propInfo = implType.FindProperty(propName, propertyType);

                implReturnType = propInfo?.PropertyType;
                if (implReturnType == null)
                {
                    // Check if this is a property wrapping a field
                    var fieldInfo = implType.GetField(propName);
                    implReturnType = fieldInfo?.FieldType;
                    ImplementedMember = fieldInfo;
                }

                // Property set arg will need to be unshimmed
                if (isPropertySetShim && implReturnType != null)
                {
                    paramTypes[^1] = implReturnType;
                    implReturnType = null;
                }
            }

            // Find method
            if (ImplementedMember == null)
            {
                var genArgs = InterfaceMethod.GetGenericArguments();
                var methodInfo = implType.GetMethod(implMemberName, InterfaceMethod.ReturnType, paramTypes, genArgs);
                if (methodInfo == null)
                {
                    // Try for mismatch returntype
                    methodInfo = implType.GetMethod(implMemberName, paramTypes, genArgs);
                }
                if (methodInfo == null && IsProperty && proxiedBinding != null)
                {
                    // Try again for proxy property to method override
                    addInstanceParam(InterfaceMethod.DeclaringType);
                    methodInfo = implType.GetMethod(implMemberName, paramTypes, InterfaceMethod.GetGenericArguments());
                }
                else if (InterfaceMethod.IsSpecialName != methodInfo?.IsSpecialName)
                {
                    methodInfo = null;
                }

                implReturnType = methodInfo?.ReturnType;
                ImplementedMember = methodInfo;
            }

            // Can only override with an interface
            if (implReturnType != null && !InterfaceMethod.ReturnType.IsEquivalentGenericMethodType(implReturnType) && !InterfaceMethod.ReturnType.IsInterfaceType())
            {
                throw new NotSupportedException($"Shimmed return type ({InterfaceMethod.ReturnType.FullName}) must be an interface, on member: {InterfaceMethod.DeclaringType.FullName}.{reflectMember.Name}");
            }

            if (proxiedBinding != null)
            {
                ProxyImplementationMember = ImplementedMember;
                ImplementedMember = proxiedBinding.ImplementedMember;
                return ProxyImplementationMember != null;
            }

            return ImplementedMember != null;
        }
    }
}
