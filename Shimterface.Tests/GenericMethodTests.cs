using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Shimterface.Tests
{
	// https://github.com/IanYates83/Shimterface/issues/5
	[TestClass]
	public class GenericMethodTests
	{
		public class TestClass
		{
			public bool WasCalled = false;

			public void BasicTest<T>()
			{
				WasCalled = true;
			}
			
			public T ReturnTest<T>()
				where T : class, IComparable
			{
				WasCalled = true;
				return (T)(object)"result";
			}

			public T FullTest<T>(T val)
				where T : class, IComparable
			{
				WasCalled = true;
				return val;
			}

			public IEnumerable<T> DeepTest<T>(Func<IEnumerable<T>> val)
				where T : class
			{
				WasCalled = true;
				return val();
			}
		}

		public interface IBasicTestShim
		{
			void BasicTest<T>();
		}

		public interface IReturnTestShim
		{
			T ReturnTest<T>()
				where T : class, IComparable;
		}

		public interface ITestShim2
		{
			T FullTest<T>(T val)
				where T : class;

			public IEnumerable<T> DeepTest<T>(Func<IEnumerable<T>> val)
				where T : class;
		}

		[TestMethod]
		public void Facade_can_include_generic_methods()
		{
			// Arrange
			var inst = new TestClass();

			var shim = ShimBuilder.Shim<IBasicTestShim>(inst);

			// Act
			Assert.IsFalse(inst.WasCalled);
			shim.BasicTest<string>();

			// Assert
			Assert.IsTrue(inst.WasCalled);
		}
		
		[TestMethod]
		public void Facade_of_generic_method_can_return_generic()
		{
			// Arrange
			var inst = new TestClass();

			var shim = ShimBuilder.Shim<IReturnTestShim>(inst);

			// Act
			Assert.IsFalse(inst.WasCalled);
			var res = shim.ReturnTest<string>();

			// Assert
			Assert.IsTrue(inst.WasCalled);
			Assert.AreSame("result", res);
		}

		[TestMethod]
		public void Facade_of_generic_method_can_send_and_receive_generic_types()
		{
			// Arrange
			var inst = new TestClass();

			var shim = ShimBuilder.Shim<ITestShim2>(inst);

			var val = "Abcd1234";

			// Act
			Assert.IsFalse(inst.WasCalled);
			var res = shim.FullTest<string>(val);

			// Assert
			Assert.IsTrue(inst.WasCalled);
			Assert.AreSame(val, res);
		}

		[TestMethod]
		public void Facade_of_generic_method_can_send_and_receive_deep_generic_types()
		{
			// Arrange
			var inst = new TestClass();

			var shim = ShimBuilder.Shim<ITestShim2>(inst);

			var val = new[] { "Abcd1234" };

			// Act
			Assert.IsFalse(inst.WasCalled);
			var res = shim.DeepTest<string>(() => val);

			// Assert
			Assert.IsTrue(inst.WasCalled);
			Assert.AreSame(val, res);
		}
	}
}
