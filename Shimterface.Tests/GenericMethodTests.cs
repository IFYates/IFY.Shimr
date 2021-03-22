using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shimterface.Tests
{
    // https://github.com/IanYates83/Shimterface/issues/5
    [TestClass]
    public class GenericMethodTests
    {
        public class TestClass
        {
            public void Test<T>()
            {
            }
        }

        public interface ITestShim
        {
            void Test<T>();
        }

        [TestMethod]
        public void Facade_can_include_generic_methods()
        {
            var shim = ShimBuilder.Shim<ITestShim>(new TestClass());
            shim.Test<string>();
        }
    }
}
