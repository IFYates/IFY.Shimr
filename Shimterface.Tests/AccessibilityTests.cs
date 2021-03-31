using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE1006 // Naming Styles
namespace Shimterface.Tests
{
	[TestClass]
	public class AccessibilityTests
	{
		interface IPrivateInterface
		{
			void Test();
		}
		public interface IPublicInterface : IShim
		{
			void Test();
		}

		[ExcludeFromCodeCoverage]
		class PrivateTestClass
		{
			public void Test()
			{
			}
		}
		[ExcludeFromCodeCoverage]
		public class PublicTestClass
		{
			private void Test()
			{
			}
		}

		[TestMethod]
		public void Can_always_shim_null()
		{
			ShimBuilder.Shim<IPrivateInterface>((PrivateTestClass)null);
		}

		[TestMethod]
		public void Cannot_shim_to_private_interface()
		{
			var obj = new PrivateTestClass();

			Assert.ThrowsException<TypeLoadException>(() =>
			{
				ShimBuilder.Shim<IPrivateInterface>(obj);
			});
		}

		[TestMethod]
		public void Cannot_use_shim_of_private_class()
		{
			var obj = new PrivateTestClass();

			var shim = ShimBuilder.Shim<IPublicInterface>(obj);

			Assert.ThrowsException<MethodAccessException>(() =>
			{
				shim.Test();
			});
		}

		[TestMethod]
		public void Cannot_shim_class_with_private_interface_member()
		{
			var obj = new PublicTestClass();

			Assert.ThrowsException<MissingMemberException>(() =>
			{
				ShimBuilder.Shim<IPublicInterface>(obj);
			});
		}

		[TestMethod]
		public void Result_is_IShim()
		{
			var obj = new PrivateTestClass();

			var shim = ShimBuilder.Shim<IPublicInterface>(obj);

			Assert.IsTrue(shim is IShim);
		}

		[TestMethod]
		public void Can_unshim_original_object()
		{
			var obj = new PrivateTestClass();

			var shim = ShimBuilder.Shim<IPublicInterface>(obj);

			Assert.AreSame(obj, shim.Unshim());
		}
	}
}
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore IDE0051 // Remove unused private members
