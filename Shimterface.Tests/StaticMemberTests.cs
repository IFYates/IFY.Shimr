using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shimterface.Tests
{
	[TestClass]
	public class StaticMemberTests
	{
		public class StaticMemberClass
		{
			public static string Value { get; set; }

			public static bool _HasCalled = false;
			public static void Test()
			{
				_HasCalled = true;
			}
		}

		public interface IStaticMethod
		{
			[StaticShim(typeof(StaticMemberClass))]
			void Test();
		}

		public interface IStaticProperty
		{
			[StaticShim(typeof(StaticMemberClass))]
			string Value { get; set; }
		}

		public interface IBadStaticMethod
		{
			void Test();
		}

		[TestMethod]
		public void Can_define_static_method()
		{
			var factory = Shimterface.Create<IStaticMethod>();

			Assert.IsFalse(StaticMemberClass._HasCalled);
			factory.Test();
			Assert.IsTrue(StaticMemberClass._HasCalled);
		}

		[TestMethod]
		public void Can_define_static_property()
		{
			var factory = Shimterface.Create<IStaticProperty>();

			Assert.IsNull(StaticMemberClass.Value);
			Assert.IsNull(factory.Value);
			StaticMemberClass.Value = "one";
			Assert.AreEqual("one", factory.Value);
			factory.Value = "two";
			Assert.AreEqual("two", StaticMemberClass.Value);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidCastException))]
		public void Must_only_define_static_methods()
		{
			Shimterface.Create<IBadStaticMethod>();
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidCastException))]
		public void Normal_shims_cannot_use_StaticShimAttribute()
		{
			Shimterface.Shim<IStaticMethod>(new object());
		}
	}
}
