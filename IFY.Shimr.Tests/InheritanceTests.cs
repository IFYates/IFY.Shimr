using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable CA1822 // Mark members as static
namespace Shimterface.Tests
{
	[TestClass]
	public class InheritanceTests
	{
		public interface IRootShim : IChildShim
		{
			int TestA();
		}
		public interface IChildShim : IGrandchildShim
		{
			int TestB();
		}
		public interface IGrandchildShim
		{
			int TestC();
		}

		public class TestClass
		{
			public int TestA()
			{
				return 1;
			}
			public int TestB()
			{
				return 2;
			}
			public int TestC()
			{
				return 3;
			}
		}

		[TestMethod]
		public void Can_shim_full_hierarchy()
		{
			var obj = new TestClass();

			var shim = obj.Shim<IRootShim>();

			Assert.AreEqual(1, shim.TestA());
			Assert.AreEqual(2, shim.TestB());
			Assert.AreEqual(3, shim.TestC());
		}

		#region Issue 11 - Unable to shim hidden method
		// https://github.com/IFYates/Shimterface/issues/11

		public abstract class Issue11BaseClass
		{
			public string GetValue() { return "value"; }
		}

		public class Issue11Class : Issue11BaseClass
		{
			new public int GetValue() { return 1; }
		}

		public interface IShimIssue11
		{
			string GetValue();
		}

		[TestMethod]
		public void Issue11__Can_shim_hidden_method()
		{
			var obj = new Issue11Class();
			Assert.IsInstanceOfType(obj.GetValue(), typeof(int)); // New method takes precedence

			var shim = obj.Shim<IShimIssue11>();

			var result = shim.GetValue();
			Assert.AreEqual("value", result);
		}

		#endregion Issue 11

		public interface IMethod1
		{
			string Method();
		}
		public class Method2 : IMethod1
		{
			public int Method() => 1;
			string IMethod1.Method() => "value";
		}
		public class Method3 : Method2
		{
			new public byte Method() => 2;
		}

        [TestMethod]
        public void Can_expose_explicit_interface_method()
        {
			var obj = new Method3();
			Assert.AreEqual((byte)2, obj.Method());
			Assert.AreEqual(1, ((Method2)obj).Method());

			var shim = obj.Shim<IMethod1>();
			Assert.AreEqual("value", shim.Method());
        }
	}
}
