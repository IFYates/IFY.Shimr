using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Shimterface.Tests
{
    [TestClass]
    public class GenericConstructorTests
    {
        public interface IInstanceInterface<T>
        {
            T Value { get; }
            string Text { get; }
            int Count { get; }
        }
        public interface IFactoryInterface
        {
            [ConstructorShim(typeof(TestClass<>))]
            IInstanceInterface<T> Create<T>(T value);
            [ConstructorShim(typeof(TestClass<>))]
            IInstanceInterface<T> Create<T>(IEnumerable<T> value);
            [ConstructorShim(typeof(TestClass<>))]
            IInstanceInterface<T> Create<T>(int a, int b, int c, int d, int e);
        }

        [ExcludeFromCodeCoverage]
        public class TestClass<T>
        {
            public T Value { get; private set; }
            public string Text { get; set; }
            public int Count { get; set; }

            public TestClass(T value)
            {
                Value = value;
                Count = 1;
            }
            public TestClass(IEnumerable<T> value)
            {
                Value = value.First();
                Count = value.Count();
            }
            public TestClass(int a, int b, int c, int d, int e)
            {
                Value = default;
                Count = a + b + c + d + e;
            }
        }

        public interface ITest
        {
            object Exec();
        }

        [TestMethod]
        public void Can_shim_to_constructor()
        {
            var shim = ShimBuilder.Create<IFactoryInterface>();

            var instA = shim.Create("one");
            var instB = shim.Create(2);

            Assert.AreEqual("one", instA.Value);
            Assert.AreEqual(2, instB.Value);
            Assert.IsInstanceOfType(((IShim)instA).Unshim(), typeof(TestClass<string>));
        }

        [TestMethod]
        public void Can_shim_to_constructor_with_deep_generic()
        {
            var shim = ShimBuilder.Create<IFactoryInterface>();

            var instA = shim.Create<string>(new[] { "one", "two" });
            var instB = shim.Create<int>(new[] { 2, 3, 4, 5 });

            Assert.AreEqual("one", instA.Value);
            Assert.AreEqual(2, instA.Count);
            Assert.AreEqual(2, instB.Value);
            Assert.AreEqual(4, instB.Count);
            Assert.IsInstanceOfType(((IShim)instA).Unshim(), typeof(TestClass<string>));
        }

        [TestMethod]
        public void Can_shim_to_constructor_with_multiple_args()
        {
            var shim = ShimBuilder.Create<IFactoryInterface>();

            var inst = shim.Create<string>(1, 2, 3, 4, 5);

            Assert.IsNull(inst.Value);
            Assert.AreEqual(1 + 2 + 3 + 4 + 5, inst.Count);
            Assert.IsInstanceOfType(((IShim)inst).Unshim(), typeof(TestClass<string>));
        }
    }
}