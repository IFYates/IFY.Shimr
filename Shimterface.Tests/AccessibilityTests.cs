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
        public interface IPublicInterface : IShim
        {
            void Test();
        }

        class PrivateTestClass
        {
            public void Test()
            {
            }
        }
        public class PrivateMemberTestClass
        {
            private void Test()
            {
            }
        }
        public class PublicTestClass
        {
            public void Test()
            {
            }
        }

        [TestInitialize]
        public void init()
        {
            ShimBuilder.ResetState();
        }

        [TestMethod]
        [ExpectedException(typeof(TypeLoadException))]
        public void Cannot_shim_to_private_interface()
        {
            var obj = new PublicTestClass();

            ShimBuilder.Shim<IPrivateInterface>(obj);
        }

        [TestMethod]
        [ExpectedException(typeof(TypeLoadException))]
        public void Cannot_shim_private_class()
        {
            var obj = new PrivateTestClass();

            var shim = ShimBuilder.Shim<IPublicInterface>(obj);
            shim.Test();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidCastException))]
        public void Cannot_shim_class_with_private_interface_member()
        {
            var obj = new PrivateMemberTestClass();

            ShimBuilder.Shim<IPublicInterface>(obj);
        }
    }
}
