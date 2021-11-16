using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Shimterface.Internal.Tests
{
    // NOTE: Not aiming at full coverage here, due to the complexity of what TypeHelpers does.
    // Full coverage is provided when combined with the rest of the test suite.
    [TestClass]
    public class TypeHelpersTests
    {
        #region GetAttribute

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

        #endregion GetAttribute

        #region GetConstructor

        [TestMethod]
        public void GetConstructor__Mismatch_generic__Null()
        {
            // Act
            var res = typeof(string).GetConstructor(Array.Empty<Type>(), new[] { typeof(int) });

            // Assert
            Assert.IsNull(res);
        }

        [TestMethod]
        public void GetConstructor__No_matches__Null()
        {
            // Act
            var res = typeof(string).GetConstructor(Array.Empty<Type>(), Array.Empty<Type>());

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
                b.ToString();
            }
        }

        [TestMethod]
        public void GetConstructor__Multiple_constructors__Exception()
        {
            Assert.ThrowsException<AmbiguousMatchException>(() =>
            {
                typeof(TestClass3<int>).GetConstructor(new[] { typeof(int) }, Array.Empty<Type>());
            });
        }

        #endregion GetConstructor

        #region GetMethod

#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable CA1822 // Mark members as static
        [ExcludeFromCodeCoverage]
        public class TestClass1
        {
            public void Generic() { } // Must not match
            public void Generic<T>() { }
            public void FixedParam<T>(string s) { }
            public void GenericParam<T>(T s) { }
            public void DeepGenericParam<T>(List<T> s) { }
        }
        [ExcludeFromCodeCoverage]
        public class TestClass2
        {
            public void Generic<U>() { }
            public void FixedParam<U>(string s) { }
            public void GenericParam<U>(U s) { }
            public void DeepGenericParam<U>(List<U> s) { }
        }
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore IDE0060 // Remove unused parameter

        [TestMethod]
        public void GetMethod__Generic_method_No_parameter__Match()
        {
            // Arrange
            var method2 = typeof(TestClass2).GetMethod(nameof(TestClass2.Generic));
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
            var method2 = typeof(TestClass2).GetMethod(nameof(TestClass2.FixedParam), new[] { typeof(string) });
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
            var method2 = typeof(TestClass2).GetMethod(nameof(TestClass2.GenericParam));
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
            var method2 = typeof(TestClass2).GetMethod(nameof(TestClass2.DeepGenericParam));
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
            var res = TypeHelpers.GetMethod(typeof(TestClass2), "FixedParamX", new[] { typeof(string) }, new Type[1]);

            // Assert
            Assert.IsNull(res);
        }

        [TestMethod]
        public void GetMethod__No_potential_methods_by_params__Null()
        {
            // Act
            var res = TypeHelpers.GetMethod(typeof(TestClass2), "FixedParam", new[] { typeof(int) }, new Type[1]);

            // Assert
            Assert.IsNull(res);
        }

        [TestMethod]
        public void GetMethod__No_potential_methods_by_generic_args_count__Null()
        {
            // Act
            var res = TypeHelpers.GetMethod(typeof(TestClass2), "FixedParam", new[] { typeof(string) }, new Type[2]);

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

            Assert.AreEqual("Found 2 methods matching given criteria", ex.Message);
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
}
