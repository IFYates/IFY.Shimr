using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;

namespace Shimterface.Tests
{
    [TestClass]
    public class GenericConstructorTests
    {
        public interface IInstanceInterface<T>
        {
            T Value { get; }
        }
        public interface IFactoryInterface
        {
            [ConstructorShim(typeof(TestClass<>))]
            IInstanceInterface<T> Create<T>(T value);
        }

        [ExcludeFromCodeCoverage]
        public class TestClass<T>
        {
            public T Value { get; private set; }

            public TestClass(T value)
            {
                Value = value;
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
    }
}
