using IFY.Shimr.Extensions;
using System;

namespace IFY.Shimr.Tests;

#pragma warning disable IDE1006 // Naming Styles
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
        public string this[int i] => _Values[i];
    }
    public class SetIndexerTest
    {
        public string[] _Values = new string[10];
        public string this[int i] { set => _Values[i] = value; }
    }
    public class GetSetIndexerTest
    {
        public string[] _Values = new string[10];
        public string this[int i] { get => _Values[i]; set => _Values[i] = value; }
    }

    [TestMethod]
    public void Can_use_a_get_indexer()
    {
        var obj = new GetIndexerTest();
        obj._Values[0] = "test0";

        var shim = obj.Shim<IGetIndexerTest>()!;

        Assert.AreEqual("test0", shim[0]);
    }

    [TestMethod]
    public void Can_use_a_set_indexer()
    {
        var obj = new SetIndexerTest();

        var shim = obj.Shim<ISetIndexerTest>()!;
        shim[0] = "test0";

        Assert.AreEqual("test0", obj._Values[0]);
    }

    [TestMethod]
    public void Can_use_a_getset_indexer()
    {
        var obj = new GetSetIndexerTest();

        var shim = obj.Shim<IGetSetIndexerTest>()!;
        shim[0] = "test0";

        Assert.AreEqual("test0", shim[0]);
    }

    [TestMethod]
    public void Can_use_a_get_indexer_with_real_set()
    {
        var obj = new GetSetIndexerTest();
        obj[0] = "test0";

        var shim = obj.Shim<IGetIndexerTest>()!;

        Assert.AreEqual("test0", shim[0]);
    }

    [TestMethod]
    public void Can_use_a_set_indexer_with_real_get()
    {
        var obj = new GetSetIndexerTest();

        var shim = obj.Shim<ISetIndexerTest>()!;
        shim[0] = "test0";

        Assert.AreEqual("test0", obj[0]);
    }

#if !XSHIMR_CG
    // TODO: IFY.Shimr doesn't support this
    public interface IMultipleIndexerTest
    {
        string this[int i] { get; set; }
        int this[string s] { get; }
    }
    public class MultipleIndexerTest
    {
        public string[] _Values = new string[10];
        public string this[int i] { get => _Values[i]; set => _Values[i] = value; }
        public int this[string s] => Array.IndexOf(_Values, s);
    }

    [TestMethod]
    public void Can_shim_multiple_indexers()
    {
        var obj = new MultipleIndexerTest();
        obj._Values[0] = "test0";

        var shim = obj.Shim<IMultipleIndexerTest>()!;
        shim[1] = "test1";

        Assert.AreEqual("test0", shim[0]);
        Assert.AreEqual("test1", obj[1]);
        Assert.AreEqual(1, shim["test1"]);
    }
#endif
}
