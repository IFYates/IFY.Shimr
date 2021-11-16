using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable IDE0051 // Remove unused private members
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

			var ex = Assert.ThrowsException<TypeLoadException>(() =>
			{
				ShimBuilder.Shim<IPrivateInterface>(obj);
			});

			Assert.IsTrue(ex.Message.Contains(" attempting to implement an inaccessible interface."), ex.Message);
		}

		[TestMethod]
		public void Cannot_use_shim_of_private_class()
		{
			var obj = new PrivateTestClass();

			var shim = ShimBuilder.Shim<IPublicInterface>(obj);

			var ex = Assert.ThrowsException<MethodAccessException>(() =>
			{
				shim.Test();
			});

			Assert.IsTrue(ex.Message.Contains(" access method 'Shimterface.Tests.AccessibilityTests+PrivateTestClass.Test()' failed."), ex.Message);
		}

		[TestMethod]
		public void Cannot_shim_class_with_private_interface_member()
		{
			var obj = new PublicTestClass();

			var ex = Assert.ThrowsException<MissingMemberException>(() =>
			{
				ShimBuilder.Shim<IPublicInterface>(obj);
			});

			Assert.IsTrue(ex.Message.Contains(" missing method: Void Test()"), ex.Message);
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
#pragma warning restore IDE0051 // Remove unused private members
