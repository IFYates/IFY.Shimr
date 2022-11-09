using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace IFY.Shimr.Tests
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class ShimrAttribute : Attribute
    {
        public Type ImplementationType { get; }

        public ShimrAttribute(Type implementationType)
        {
            ImplementationType = implementationType;
        }
    }

    [TestClass]
    public class CodeGenTests
    {
        private readonly static Dictionary<(Type, Type), Type> _types = new Dictionary<(Type, Type), Type>();

        public static string Register(Type c, Type a, Type b)
        {
            _types[(a, b)] = c;
            return _types.Count + " > " + a.FullName + ", " + b.FullName + " = " + c.FullName;
        }

        [Shimr(typeof(string))]
        public interface IToString
        {
            string ToString();
        }

        [TestMethod]
        public void Can_always_shim_null()
        {
            var t = Type.GetType("Shimr.Auto.StringShimr");
            var s = (IToString)Activator.CreateInstance(t, "test");
            var x = s.ToString();
            var t2 = _types[(typeof(IToString), typeof(string))];
        }
    }
}