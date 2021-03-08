using System;

namespace Shimterface
{
	/// <summary>
	/// Mark a member type as explicitly shimming an item with a different name or return type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
	public class ShimAttribute : Attribute
	{
		public string ImplementationName { get; internal set; }
		public Type ReturnType { get; internal set; }

		public ShimAttribute(string name)
		{
			ImplementationName = name;
		}
		public ShimAttribute(Type returnType)
		{
			ReturnType = returnType;
		}
		public ShimAttribute(string name, Type returnType)
		{
			ImplementationName = name;
			ReturnType = returnType;
		}
	}
}
