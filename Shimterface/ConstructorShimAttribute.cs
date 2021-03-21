using System;

namespace Shimterface
{
	/// <summary>
	/// Mark a method as being a shim of a constructor.
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
