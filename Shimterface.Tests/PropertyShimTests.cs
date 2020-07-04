using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shimterface.Tests
{
    [TestClass]
    public class PropertyShimTests
    {
        public interface IGetPropertyTest
        {
            string GetProperty { get; }
        }
        public interface IGetPropertyWithSetTest
        {
            string GetPropertyWithSet { get; }
        }
        public interface ISetPropertyTest
        {
            string SetProperty { set; }
        }
        public interface ISetPropertyWithGetTest
        {
            string SetPropertyWithGet { set; }
        }
        public interface IGetSetPropertyTest
        {
            string GetSetProperty { get; set; }
        }

        public class TestClass
        {
            public string GetProperty { get { return "value"; } }
            public string GetPropertyWithSet { get; set; }

            public string SetPropertyWithGet { get; set; }
            public string SetProperty { set { _SetPropertyValue = value; } }
            public string _SetPropertyValue = null;

            public string GetSetProperty { get; set; }
        }

        [TestMethod]
        public void Can_use_a_get_property()
        {
            var obj = new TestClass();

            var shim = ShimBuilder.Shim<IGetPropertyTest>(obj);

            var res = shim.GetProperty;
            Assert.AreEqual("value", res);
        }

        [TestMethod]
        public void Can_use_a_set_property()
        {
            var obj = new TestClass();
            Assert.IsNull(obj._SetPropertyValue);

            var shim = ShimBuilder.Shim<ISetPropertyTest>(obj);
            shim.SetProperty = "test";

            Assert.AreEqual("test", obj._SetPropertyValue);
        }

        [TestMethod]
        public void Can_use_a_set_property_with_real_get()
        {
            var obj = new TestClass();
            Assert.IsNull(obj.SetPropertyWithGet);

            var shim = ShimBuilder.Shim<ISetPropertyWithGetTest>(obj);
            shim.SetPropertyWithGet = "test";

            Assert.AreEqual("test", obj.SetPropertyWithGet);
        }

        [TestMethod]
        public void Can_use_a_get_property_with_real_set()
        {
            var obj = new TestClass();

            var shim = ShimBuilder.Shim<IGetPropertyWithSetTest>(obj);
            Assert.IsNull(shim.GetPropertyWithSet);
            obj.GetPropertyWithSet = "test";
            Assert.AreEqual("test", shim.GetPropertyWithSet);
        }

        [TestMethod]
        public void Can_use_a_getset_property()
        {
            var obj = new TestClass();

            var shim = ShimBuilder.Shim<IGetSetPropertyTest>(obj);

            Assert.IsNull(obj.GetSetProperty);
            shim.GetSetProperty = "test_getset";
            Assert.AreEqual("test_getset", shim.GetSetProperty);
        }
    }
}
