namespace IFY.Shimr.Gen.Tests;

[TestClass]
public class StaticMemberTests
{
    public class TestClass
    {
        public static string Value { get; set; }
        public static void Test()
        {
        }

        // Instance members
    }
    public class AnotherTestClass
    {
        public static void AnotherTest()
        {
        }
    }
    public interface IStaticTest
    {
        [StaticShim(typeof(TestClass))]
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

    public void DoTest()
    {
        // TODO
        //IStaticTest factory = ShimBuilder.Create<IStaticTest>();
        //factory.Test();
    }
}