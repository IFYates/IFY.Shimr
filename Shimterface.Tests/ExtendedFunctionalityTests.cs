using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE1006 // Naming Styles
namespace Shimterface.Tests
{
	/// <summary>
	/// Tests around extending/replacing shim functionality
	/// https://github.com/IanYates83/Shimterface/issues/3
	/// </summary>
	[TestClass]
	public class ExtendedFunctionalityTests
	{
		public interface ITestClassFacade
		{
			void MethodA();
			[ShimProxy(typeof(TestProxy), ProxyBehaviour.MustOverride)]
			void MethodB(); // Will be overridden to call new implementation which will call the base implementation
			[ShimProxy(typeof(TestProxy), ProxyBehaviour.MustAdd)]
			void MethodC(); // Provides new member implementation
		}

		[ExcludeFromCodeCoverage]
		public class TestProxy
		{
			private void MethodA()
			{
			}
		}

		[TestMethod]
		public void Shim_can_define_proxy_to_override_member()
		{
			Assert.Fail();
		}

		[TestMethod]
		public void Shim_can_define_proxy_to_override_member_by_default()
		{
			Assert.Fail();
		}

		[TestMethod]
		public void Override_member_can_call_shimmed_implementation()
		{
			Assert.Fail();
		}

		[TestMethod]
		public void Shim_can_define_proxy_to_add_member()
		{
			Assert.Fail();
		}

		[TestMethod]
		public void Shim_can_define_proxy_to_add_member_by_default()
		{
			Assert.Fail();
		}

		[TestMethod]
		public void Override_member_must_exist_in_shimmed_type()
		{
			Assert.Fail();
		}

		[TestMethod]
		public void Added_member_must_not_exist_in_shimmed_type()
		{
			Assert.Fail();
		}
	}
}
