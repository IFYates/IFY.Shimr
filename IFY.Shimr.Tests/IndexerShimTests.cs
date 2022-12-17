﻿namespace IFY.Shimr.Tests;

[TestClass]
public class IndexerShimTests
{
#if SHIMRGEN
    [ShimOf<GetIndexerTest>]
    [ShimOf<GetSetIndexerTest>]
#endif
    public interface IGetIndexerTest
    {
        string this[int i] { get; }
    }
#if SHIMRGEN
    [ShimOf<SetIndexerTest>]
    [ShimOf<GetSetIndexerTest>]
#endif
    public interface ISetIndexerTest
    {
        string this[int i] { set; }
    }
#if SHIMRGEN
    [ShimOf<GetSetIndexerTest>]
#endif
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

        var shim = obj.Shim<IGetIndexerTest>();

        Assert.AreEqual("test0", shim[0]);
    }

    [TestMethod]
    public void Can_use_a_set_indexer()
    {
        var obj = new SetIndexerTest();

        var shim = obj.Shim<ISetIndexerTest>();
        shim[0] = "test0";

        Assert.AreEqual("test0", obj._Values[0]);
    }

    [TestMethod]
    public void Can_use_a_getset_indexer()
    {
        var obj = new GetSetIndexerTest();

        var shim = obj.Shim<IGetSetIndexerTest>();
        shim[0] = "test0";

        Assert.AreEqual("test0", shim[0]);
    }

    [TestMethod]
    public void Can_use_a_get_indexer_with_real_set()
    {
        var obj = new GetSetIndexerTest();
        obj[0] = "test0";

        var shim = obj.Shim<IGetIndexerTest>();

        Assert.AreEqual("test0", shim[0]);
    }

    [TestMethod]
    public void Can_use_a_set_indexer_with_real_get()
    {
        var obj = new GetSetIndexerTest();

        var shim = obj.Shim<ISetIndexerTest>();
        shim[0] = "test0";

        Assert.AreEqual("test0", obj[0]);
    }
}
