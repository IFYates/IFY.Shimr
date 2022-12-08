namespace IFY.Shimr.Tests;

[TestClass]
public class SignatureEnumerableChangeTests
{
//#if SHIMRGEN
//    class ShimBuilder
//    {
//        public static T Shim<T>(ReturnTypeTest obj) => obj.Shim<T>();
//    }
//#endif

    public class ReturnTypeTest
    {
        private IEnumerable<string> _list = new[] { "a", "b", "c" };
        public IEnumerable<string> GetList()
        {
            return _list;
        }
        public void SetList(IEnumerable<string> arr)
        {
            _list = arr;
        }
    }
//#if SHIMRGEN
//    [ShimOf(typeof(ReturnTypeTest))]
//#endif
//    public interface IToString
//    {
//        string ToString();
//    }
//#if SHIMRGEN
//    [ShimOf(typeof(ReturnTypeTest))]
//#endif
//    public interface ICoveredEnumMethodTest
//    {
//        IEnumerable<IToString> GetList();
//    }
    //    public interface ICoveredEnumSetMethodTest
    //    {
    //        void SetList([TypeShim(typeof(IEnumerable<string>))] IEnumerable<IToString> arr);
    //    }

    //    [TestMethod]
    //    public void Can_shim_method_with_covered_enum_return_type()
    //    {
    //        var obj = new ReturnTypeTest();

    //        var shim = (IShim)ShimBuilder.Shim<ICoveredEnumMethodTest>(obj);

    //        Assert.AreSame(obj, shim.Unshim());
    //    }

    //    [TestMethod]
    //    public void Can_get_enum_result_as_shims()
    //    {
    //        var obj = new ReturnTypeTest();

    //        var shim = ShimBuilder.Shim<ICoveredEnumMethodTest>(obj);
    //        var res = shim.GetList();

    //        Assert.AreEqual(3, res.Count());
    //        var arr = ((IShim)res).Unshim<string>().ToArray();
    //        CollectionAssert.AreEqual(obj.GetList().ToArray(), arr);
    //    }

    //    // TODO: can shim, cannot set
    //    [TestMethod]
    //    public void Can_set_enum_parameter_as_appropriate_shims()
    //    {
    //        var obj = new ReturnTypeTest();
    //        var data = (IEnumerable<string>)new[] { "1", "2", "3" };

    //        var shim = ShimBuilder.Shim<ICoveredEnumSetMethodTest>(obj);
    //        var arr = ShimBuilder.Shim<IToString>(data);
    //        shim.SetList(arr);

    //        var res = obj.GetList();
    //        CollectionAssert.AreEqual(data.ToArray(), res.ToArray());
    //    }
}
