using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Shimterface.Tests
{
	[TestClass]
	public class MissingMemberTests
	{
		public interface IUnknownMethodTest
		{
			void UnknownMethod();
		}
		public interface IPropertyWithoutSetTest
		{
			string PropertyWithoutSet { get; set; }
		}

		[ExcludeFromCodeCoverage]
		public class TestClass
		{
			public string PropertyWithoutSet => null;
		}

		[TestInitialize]
		public void ResetState()
		{
			ShimBuilder.ResetState();
		}

		[TestMethod]
		public void All_interface_members_must_exist_in_object()
		{
			var obj = new TestClass();

			Assert.ThrowsException<MissingMemberException>(() =>
			{
				ShimBuilder.Shim<IUnknownMethodTest>(obj);
			});
		}

		[TestMethod]
		public void Can_choose_to_ignore_missing_members_on_creation()
		{
			var obj = new TestClass();

			ShimBuilder.IgnoreMissingMembers<IUnknownMethodTest>();
			ShimBuilder.Shim<IUnknownMethodTest>(obj);
		}

		[TestMethod]
		public void Ignored_missing_members_cannot_be_invoked()
		{
			var obj = new TestClass();

			ShimBuilder.IgnoreMissingMembers<IUnknownMethodTest>();
			var shim = ShimBuilder.Shim<IUnknownMethodTest>(obj);

			Assert.ThrowsException<NotImplementedException>(() =>
			{
				shim.UnknownMethod();
			});
		}

		[TestMethod]
		public void Properties_must_contain_all_required_parts()
		{
			var obj = new TestClass();

			Assert.ThrowsException<MissingMemberException>(() =>
			{
				ShimBuilder.Shim<IPropertyWithoutSetTest>(obj);
			});
		}
	}
}
