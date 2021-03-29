using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;

namespace Shimterface.Tests
{
	/// <summary>
	/// Tests around extending/replacing shim functionality
	/// https://github.com/IanYates83/Shimterface/issues/3
	/// </summary>
	[TestClass]
	public class ExtendedFunctionalityTests_Property
	{
		public interface ITestShim
		{
		}

		[ExcludeFromCodeCoverage]
		public class TestClass_NoPropB
		{
		}

		[ExcludeFromCodeCoverage]
		public class TestClass_HasPropB
		{
		}

		[ExcludeFromCodeCoverage]
		public class TestClass
		{
			public string PropertyA { get; set; }
		}
	}
}
