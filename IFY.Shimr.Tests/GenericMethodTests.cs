using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace IFY.Shimr.Tests
{
    // https://github.com/IFYates/Shimterface/issues/5
    [TestClass]
    public class GenericMethodTests
    {
        public class TestClass
        {
            internal bool WasCalled;

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


            public IDictionary<T1, T2> ComplexTest<T1, T2>(T1 key)
                where T2 : IEnumerable<T1>
            {
                WasCalled = true;
                return new Dictionary<T1, T2>
                {
                    [key] = default
                };
            }
        }

        public interface IBasicTestShim
        {
            void BasicTest<T>();
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

        public interface IReturnTestShim
        {
            U ReturnTest<U>()
                where U : class, IComparable;
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

        public interface IFullTestShim
        {
            T FullTest<T>(T val)
                where T : class, IComparable;
        }
        [TestMethod]
        public void Facade_of_generic_method_can_send_and_receive_generic_types()
        {
            // Arrange
            var inst = new TestClass();

            var shim = ShimBuilder.Shim<IFullTestShim>(inst);

            var val = "Abcd1234";

            // Act
            Assert.IsFalse(inst.WasCalled);
            var res = shim.FullTest(val);

            // Assert
            Assert.IsTrue(inst.WasCalled);
            Assert.AreSame(val, res);
        }

        public interface IDeepTestShim
        {
            IEnumerable<T> DeepTest<T>(Func<IEnumerable<T>> val)
                where T : class;
        }
        [TestMethod]
        public void Facade_of_generic_method_can_send_and_receive_deep_generic_types()
        {
            // Arrange
            var inst = new TestClass();

            var shim = ShimBuilder.Shim<IDeepTestShim>(inst);

            var val = new[] { "Abcd1234" };

            // Act
            Assert.IsFalse(inst.WasCalled);
            var res = shim.DeepTest(() => val);

            // Assert
            Assert.IsTrue(inst.WasCalled);
            Assert.AreSame(val, res);
        }

        public interface IComplexTestShim
        {
            IDictionary<T1, T2> ComplexTest<T1, T2>(T1 key)
                where T2 : IEnumerable<T1>;
        }
        [TestMethod]
        public void Support_facade_of_complex_generics()
        {
            // Arrange
            var inst = new TestClass();

            var shim = ShimBuilder.Shim<IComplexTestShim>(inst);

            var val = "Abcd1234";

            // Act
            Assert.IsFalse(inst.WasCalled);
            var res = shim.ComplexTest<string, string[]>(val);

            // Assert
            Assert.IsTrue(inst.WasCalled);
            Assert.IsTrue(res.ContainsKey(val));
        }
    }
}
