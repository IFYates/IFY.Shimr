using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IFY.Shimr.Tests
{
    [TestClass]
    public class IndexerShimTests
    {
        public interface IGetIndexerTest
        {
            string this[int i] { get; }
        }
        public interface ISetIndexerTest
        {
            string this[int i] { set; }
        }
        public interface IGetSetIndexerTest
        {
            string this[int i] { get; set; }
        }

        public class GetIndexerTest
        {
            public string[] _Values = new string[10];
            public string this[int i] { get { return _Values[i]; } }
        }
        public class SetIndexerTest
        {
            public string[] _Values = new string[10];
            public string this[int i] { set { _Values[i] = value; } }
        }
        public class GetSetIndexerTest
        {
            public string[] _Values = new string[10];
            public string this[int i] { get { return _Values[i]; } set { _Values[i] = value; } }
        }

        [TestMethod]
        public void Can_use_a_get_indexer()
        {
            var obj = new GetIndexerTest();
            obj._Values[0] = "test0";

            var shim = ShimBuilder.Shim<IGetIndexerTest>(obj);

            Assert.AreEqual("test0", shim[0]);
        }

        [TestMethod]
        public void Can_use_a_set_indexer()
        {
            var obj = new SetIndexerTest();

            var shim = ShimBuilder.Shim<ISetIndexerTest>(obj);
            shim[0] = "test0";

            Assert.AreEqual("test0", obj._Values[0]);
        }

        [TestMethod]
        public void Can_use_a_getset_indexer()
        {
            var obj = new GetSetIndexerTest();

            var shim = ShimBuilder.Shim<IGetSetIndexerTest>(obj);
            shim[0] = "test0";

            Assert.AreEqual("test0", shim[0]);
        }

        [TestMethod]
        public void Can_use_a_get_indexer_with_real_set()
        {
            var obj = new GetSetIndexerTest();
            obj[0] = "test0";

            var shim = ShimBuilder.Shim<IGetIndexerTest>(obj);

            Assert.AreEqual("test0", shim[0]);
        }

        [TestMethod]
        public void Can_use_a_set_indexer_with_real_get()
        {
            var obj = new GetSetIndexerTest();

            var shim = ShimBuilder.Shim<ISetIndexerTest>(obj);
            shim[0] = "test0";

            Assert.AreEqual("test0", obj[0]);
        }
    }
}
