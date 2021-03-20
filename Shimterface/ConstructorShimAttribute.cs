using System;

namespace Shimterface
{
	/// <summary>
	/// Mark property/field or method as being a constructor shim for the specified.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class ConstructorShimAttribute : StaticShimAttribute
	{
		public ConstructorShimAttribute(Type targetType)
			: base(targetType)
		{
			IsConstructor = true;
		}
	}
}
