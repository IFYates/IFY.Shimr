using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shimterface.Tests
{
    [TestClass]
    public class AccessibilityTests
    {
        interface IPrivateInterface
        {
            void Test();
        }
        public interface IPublicInterface
        {
            void Test();
        }

        class PrivateTestClass
        {
            public void Test()
            {
            }
        }
        public class PublicTestClass
        {
            private void Test()
            {
            }
        }

        [TestMethod]
        [ExpectedException(typeof(TypeLoadException))]
        public void Cannot_shim_to_private_interface()
        {
            var obj = new PrivateTestClass();

            ShimBuilder.Shim<IPrivateInterface>(obj);
        }

        [TestMethod]
        [ExpectedException(typeof(MethodAccessException))]
        public void Cannot_use_shim_of_private_class()
        {
            var obj = new PrivateTestClass();

            var shim = ShimBuilder.Shim<IPublicInterface>(obj);
            shim.Test();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidCastException))]
        public void Cannot_shim_class_with_private_interface_member()
        {
            var obj = new PublicTestClass();

            ShimBuilder.Shim<IPublicInterface>(obj);
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

            Assert.AreSame(obj, ((IShim)shim).Unshim());
        }
    }
}
