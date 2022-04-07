using System;

namespace Shimterface
{
	/// <summary>
	/// Mark signature type as being automatically shimmed from real implementation type
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class TypeShimAttribute : Attribute
	{
		public Type RealType { get; }

		public TypeShimAttribute(Type realType)
		{
			RealType = realType;
		}
	}
}
