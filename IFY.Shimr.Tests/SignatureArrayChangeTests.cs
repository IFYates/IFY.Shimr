using IFY.Shimr.Extensions;
using System.Linq;

namespace IFY.Shimr.Tests;

[TestClass]
public class SignatureArrayChangeTests
{
    public class ReturnTypeTest
    {
        private string[] _array = ["a", "b", "c"];
        public string[] GetArray()
        {
            return _array;
        }
        public void SetArray(string[] arr)
        {
            _array = arr;
        }
    }
    public interface IToString
    {
        string ToString();
    }
    public interface ICoveredArrayMethodTest
    {
        IToString[] GetArray();
    }
    public interface ICoveredArraySetMethodTest
    {
        void SetArray([TypeShim(typeof(string[]))] IToString[] arr);
    }

    [TestMethod]
    public void Can_shim_method_with_covered_array_return_type()
    {
        var obj = new ReturnTypeTest();

        var shim = (IShim)obj.Shim<ICoveredArrayMethodTest>();

        Assert.AreSame(obj, shim.Unshim());
    }

#if !SHIMR_CG // TODO: Unshim(IEnumerable<T>)
    [TestMethod]
    public void Can_get_array_result_as_shims()
    {
        var obj = new ReturnTypeTest();

        var shim = obj.Shim<ICoveredArrayMethodTest>();
        var res = shim.GetArray();

        Assert.AreEqual(3, res.Length);
        var arr = res.Unshim<string>().ToArray();
        CollectionAssert.AreEqual(obj.GetArray(), arr);
    }
#endif

#if !SHIMR_CG // TODO: Shim(IEnumerable<T>)
    // TODO: can shim, cannot set
    [TestMethod]
    public void Can_set_array_parameter_as_appropriate_shims()
    {
        var obj = new ReturnTypeTest();
        var data = new[] { "1", "2", "3" };

        var shim = obj.Shim<ICoveredArraySetMethodTest>();
        var arr = ObjectExtensions.Shim<IToString>(data).ToArray();
        shim.SetArray(arr);

        var res = obj.GetArray();
        CollectionAssert.AreEqual(data, res);
    }
#endif
}
