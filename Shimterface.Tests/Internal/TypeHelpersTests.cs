using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Shimterface.Internal.Tests
{
	[TestClass]
	public class TypeHelpersTests
	{
		#region GetMethod

		public class TestClass1
		{
			public void Generic() { } // Must not match
			public void Generic<T>() { }
			public void FixedParam<T>(string s) { }
			public void GenericParam<T>(T s) { }
			public void DeepGenericParam<T>(List<T> s) { }
		}
		public class TestClass2
		{
			public void Generic<U>() { }
			public void FixedParam<U>(string s) { }
			public void GenericParam<U>(U s) { }
			public void DeepGenericParam<U>(List<U> s) { }
		}

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

		#endregion GetMethod

		#region IsEquivalentTo

		[TestMethod]
		public void IsEquivalentTo__Compares_generics()
		{
			// Arrange
			var type = typeof(IList<>);

			var leftGens = new[] { typeof(string) };
			var rightGens = new[] { typeof(string) };

			// Act
			var res = type.IsEquivalentTo(type, leftGens, rightGens);

			// Assert
			Assert.IsTrue(res);
		}

		#endregion IsEquivalentTo
	}
}
