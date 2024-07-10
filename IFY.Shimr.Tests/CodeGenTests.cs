using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IFY.Shimr.Tests;

//[AttributeUsage(AttributeTargets.Interface)]
//public class ShimrAttribute : Attribute
//{
//    public Type ImplementationType { get; }

//    public ShimrAttribute(Type implementationType)
//    {
//        ImplementationType = implementationType;
//    }
//}

[TestClass]
public class CodeGenTests
{
//    [IFY.Shimr.Auto.Shimgen(typeof(string))]
//    public interface IToString
//    {
//        string ToString();
//    }

//    [TestMethod]
//    public void Can_always_shim_null()
//    {
//        var t = Type.GetType("IFY.Shimr.Auto.StringShimr");
//        var s = (IToString)Activator.CreateInstance(t, "test");
//        var x = s.ToString();
//        StringShimr a;
//        var t2 = ShimgenAttribute._types[(typeof(IToString), typeof(string))];
//    }
}