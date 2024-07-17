using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

#pragma warning disable CA1822 // Mark members as static
namespace IFY.Shimr.Internal.Tests;

// NOTE: Not aiming at full coverage here, due to the complexity of what TypeHelpers does.
// Full coverage is provided when combined with the rest of the test suite.
[TestClass]
public class TypeHelpersTests
{
    [TestMethod]
    public void BindStaticMethod__Supports_nongeneric()
    {
        // Act
        var res = typeof(DateTime).BindStaticMethod(nameof(DateTime.Parse), [], [typeof(string)]);

        // Assert
        Assert.AreEqual(nameof(DateTime.Parse), res.Name);
        Assert.AreEqual(typeof(string), res.GetParameters().Single().ParameterType);
    }

    [ExcludeFromCodeCoverage]
    public class FindPropertyTestBaseClass
    {
        public long Value { get; set; }
    }
    public interface IFindPropertyTestInterface
    {
        long Value { get; set; }
    }
    [ExcludeFromCodeCoverage]
    public class FindPropertyTestClass : FindPropertyTestBaseClass, IFindPropertyTestInterface
    {
        long IFindPropertyTestInterface.Value { get; set; }
        public new string Value { get; set; } = null!;
    }

    [TestMethod]
    public void FindProperty__Different_property_type_Multiple_matches__Fail()
    {
        // Act
        var ex = Assert.ThrowsException<AmbiguousMatchException>(() =>
        {
            _ = typeof(FindPropertyTestClass).FindProperty(nameof(FindPropertyTestClass.Value), typeof(int));
        });

        // Assert
        Assert.AreEqual("Found more than 1 property called 'Value' in the hierarchy for type 'IFY.Shimr.Internal.Tests.TypeHelpersTests+FindPropertyTestClass'. Consider using ShimAttribute to specify the definition type of the property to shim.", ex.Message);
    }

    #region GetAttribute

#if !NET7_0_OR_GREATER // TODO: Moq isn't letting this work in .NET 7
    [TestMethod]
    [DataRow("get_"), DataRow("set_")]
    public void GetAttribute__No_property_for_special_method__Null(string prefix)
    {
        // Arrange
        var methodInfoMock = new Mock<MethodInfo>();
        methodInfoMock.SetupGet(m => m.Attributes)
            .Returns(MethodAttributes.SpecialName);
        methodInfoMock.SetupGet(m => m.Name)
            .Returns($"{prefix}Test");
        methodInfoMock.SetupGet(m => m.ReflectedType)
            .Returns(typeof(object)); // Any type without a "Test" property

        // Act
        var res = TypeHelpers.GetAttribute<Attribute>(methodInfoMock.Object);

        // Assert
        Assert.IsNull(res);
    }
#endif

#endregion GetAttribute

    #region GetConstructor

    [TestMethod]
    public void GetConstructor__Mismatch_generic__Null()
    {
        // Act
        var res = typeof(string).GetConstructor([], [typeof(int)]);

        // Assert
        Assert.IsNull(res);
    }

    [TestMethod]
    public void GetConstructor__No_matches__Null()
    {
        // Act
        var res = typeof(string).GetConstructor([], []);

        // Assert
        Assert.IsNull(res);
    }

    [ExcludeFromCodeCoverage]
    public class TestClass3<T>
    {
        public TestClass3(int a)
        {
            a.ToString();
        }
        public TestClass3(T b)
        {
            b!.ToString();
        }
    }

    [TestMethod]
    public void GetConstructor__Multiple_constructors__Exception()
    {
        // Act
        var ex = Assert.ThrowsException<AmbiguousMatchException>(() =>
        {
            typeof(TestClass3<int>).GetConstructor([typeof(int)], []);
        });

        // Assert
        Assert.IsTrue(ex.Message.StartsWith("Found 2 constructors matching given criteria for type 'IFY.Shimr.Internal.Tests.TypeHelpersTests+TestClass3`1[["));
    }

    #endregion GetConstructor

    #region GetMethod

    [ExcludeFromCodeCoverage]
    public class TestClass1
    {
        public void Generic() { } // Must not match
        public void Generic<T>() { }
        public void FixedParam<T>(string s) { _ = s; }
        public void GenericParam<T>(T s) { _ = s; }
        public void DeepGenericParam<T>(List<T> s) { _ = s; }
    }
    [ExcludeFromCodeCoverage]
    public class TestClass2
    {
        public void Generic<U>() { }
        public void FixedParam<U>(string s) { _ = s; }
        public void GenericParam<U>(U s) { _ = s; }
        public void DeepGenericParam<U>(List<U> s) { _ = s; }
    }

    [TestMethod]
    public void GetMethod__Generic_method_No_parameter__Match()
    {
        // Arrange
        var method2 = typeof(TestClass2).GetMethod(nameof(TestClass2.Generic))!;
        var genArgs2 = method2.GetGenericArguments();
        var params2 = method2.GetParameters().Select(p => p.ParameterType).ToArray();

        // Act
        var res = typeof(TestClass1).GetMethod(method2.Name, params2, genArgs2);

        // Assert
        Assert.IsNotNull(res);
    }

    [TestMethod]
    public void GetMethod__Generic_method_Fixed_parameter__Match()
    {
        // Arrange
        var method2 = typeof(TestClass2).GetMethod(nameof(TestClass2.FixedParam), [typeof(string)])!;
        var genArgs2 = method2.GetGenericArguments();
        var params2 = method2.GetParameters().Select(p => p.ParameterType).ToArray();

        // Act
        var res = typeof(TestClass1).GetMethod(method2.Name, params2, genArgs2);

        // Assert
        Assert.IsNotNull(res);
    }

    [TestMethod]
    public void GetMethod__Generic_method_Generic_parameter__Match()
    {
        // Arrange
        var method2 = typeof(TestClass2).GetMethod(nameof(TestClass2.GenericParam))!;
        var genArgs2 = method2.GetGenericArguments();
        var params2 = method2.GetParameters().Select(p => p.ParameterType).ToArray();

        // Act
        var res = typeof(TestClass1).GetMethod(method2.Name, params2, genArgs2);

        // Assert
        Assert.IsNotNull(res);
    }

    [TestMethod]
    public void GetMethod__Generic_method_Deep_generic_parameter__Match()
    {
        // Arrange
        var method2 = typeof(TestClass2).GetMethod(nameof(TestClass2.DeepGenericParam))!;
        var genArgs2 = method2.GetGenericArguments();
        var params2 = method2.GetParameters().Select(p => p.ParameterType).ToArray();

        // Act
        var res = typeof(TestClass1).GetMethod(method2.Name, params2, genArgs2);

        // Assert
        Assert.IsNotNull(res);
    }

    [TestMethod]
    public void GetMethod__No_potential_methods_by_name__Null()
    {
        // Act
        var res = TypeHelpers.GetMethod(typeof(TestClass2), "FixedParamX", [typeof(string)], new Type[1]);

        // Assert
        Assert.IsNull(res);
    }

    [TestMethod]
    public void GetMethod__No_potential_methods_by_params__Null()
    {
        // Act
        var res = TypeHelpers.GetMethod(typeof(TestClass2), "FixedParam", [typeof(int)], new Type[1]);

        // Assert
        Assert.IsNull(res);
    }

    [TestMethod]
    public void GetMethod__No_potential_methods_by_generic_args_count__Null()
    {
        // Act
        var res = TypeHelpers.GetMethod(typeof(TestClass2), "FixedParam", [typeof(string)], new Type[2]);

        // Assert
        Assert.IsNull(res);
    }

    public interface IMethods1
    {
        T Method<T, U>(U a);
        U Method<T, U>(T b);
    }
    [TestMethod]
    public void GetMethod__Method_collission__Fails()
    {
        // Arrange
        var method2 = typeof(IMethods1).GetMethods()[0]; // Typical GetMethod here would fail with AmbiguousMatchException
        var genArgs2 = method2.GetGenericArguments();
        var params2 = method2.GetParameters().Select(p => p.ParameterType).ToArray();

        // Act
        var ex = Assert.ThrowsException<AmbiguousMatchException>(() =>
        {
            typeof(IMethods1).GetMethod(method2.Name, params2, genArgs2);
        });

        Assert.AreEqual("Found 2 methods matching criteria for 'Method' in the hierarchy for type 'IFY.Shimr.Internal.Tests.TypeHelpersTests+IMethods1'. Consider using ShimAttribute to specify the definition type of the property to shim.", ex.Message);
    }

    #endregion GetMethod

    #region IsEquivalentGenericMethodType

    [TestMethod]
    public void IsEquivalentGenericMethodType__Same_open_type__True()
    {
        // Arrange
        var type = typeof(IList<>);

        // Act
        var res = type.IsEquivalentGenericMethodType(type);

        // Assert
        Assert.IsTrue(res);
    }

    [TestMethod]
    public void IsEquivalentGenericMethodType__Same_closed_type__True()
    {
        // Arrange
        var type = typeof(IList<string>);

        // Act
        var res = type.IsEquivalentGenericMethodType(type);

        // Assert
        Assert.IsTrue(res);
    }

    [TestMethod]
    public void IsEquivalentGenericMethodType__Different_open_type__False()
    {
        // Arrange
        var type = typeof(IList<>);
        var type2 = typeof(IEnumerable<>);

        // Act
        var res = type.IsEquivalentGenericMethodType(type2);

        // Assert
        Assert.IsFalse(res);
    }

    [TestMethod]
    public void IsEquivalentGenericMethodType__Different_closed_type__False()
    {
        // Arrange
        var type = typeof(IList<string>);
        var type2 = typeof(IList<int>);

        // Act
        var res = type.IsEquivalentGenericMethodType(type2);

        // Assert
        Assert.IsFalse(res);
    }

    #endregion IsEquivalentGenericMethodType

    [TestMethod]
    public void IsEquivalentGenericType__Same_type__True()
    {
        // Arrange
        var type1 = typeof(string);
        var type2 = typeof(string);

        // Act
        var res = type1.IsEquivalentGenericType(type2);

        // Assert
        Assert.IsTrue(res);
    }
}
