using System;

namespace Shimterface
{
	/// <summary>
	/// Mark property/field or method as being static within another type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
	public class StaticShimAttribute : Attribute
	{
		/// <summary>
		/// The type that implements this member.
		/// </summary>
		public Type TargetType { get; }
		/// <summary>
		/// True if this member calls a constructor on the target type.
		/// </summary>
		public bool IsConstructor { get; set; }

		public StaticShimAttribute(Type targetType)
		{
			TargetType = targetType;
		}
	}
}
