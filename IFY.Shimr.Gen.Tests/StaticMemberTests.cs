namespace IFY.Shimr.Gen.Tests;

[TestClass]
public class StaticMemberTests
{
    public static class TestClass
    {
        public static string Value { get; set; }
        public static void Test()
        {
        }

        // Instance members
    }
    public static class AnotherTestClass
    {
        public static void AnotherTest()
        {
        }
    }
    [StaticShim(typeof(TestClass))]
    public interface IStaticTest
    {
        string Value { get; set; }
        [StaticShim(typeof(TestClass))]
        void Test();
        [StaticShim(typeof(AnotherTestClass))]
        void AnotherTest();
    }
    [StaticShim(typeof(TestClass))]
    public interface IStaticTest2
    {
        string Value { get; set; }
        void Test();
        [StaticShim(typeof(AnotherTestClass))]
        void AnotherTest();
    }

    [TestMethod]
    public void DoTest()
    {
        IStaticTest factory = ShimBuilder.Create<IStaticTest>();
        factory.Test();
    }
}