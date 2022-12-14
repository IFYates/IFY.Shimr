﻿namespace IFY.Shimr.Tests;

/// <summary>
/// Tests around extending/replacing shim functionality
/// https://github.com/IFYates/Shimterface/issues/3
/// </summary>
[TestClass]
public class ExtendedFunctionalityTests_Method
{
#if !SHIMRGEN
        [TestInitialize]
        public void ResetState()
        {
            ShimBuilder.ResetState();
        }
#endif

    public interface ITestShim
    {
        void MethodA();
    }

    [ExcludeFromCodeCoverage]
    public class TestClass_OnlyMethodA
    {
        public bool MethodACalled { get; private set; }
        public void MethodA()
        {
            MethodACalled = true;
        }
    }

    [ExcludeFromCodeCoverage]
    public class TestClass_HasMethodB : TestClass_OnlyMethodA
    {
        public string? MethodBCalledWith { get; private set; }
        public void MethodB(string arg)
        {
            MethodBCalledWith = arg;
        }
    }

    [ExcludeFromCodeCoverage]
    public class TestClass_HasMethodC : TestClass_OnlyMethodA
    {
        public string? MethodCCalledWith { get; private set; }
        public void MethodC(string arg)
        {
            MethodCCalledWith = arg;
        }
    }

    #region Override

#if SHIMRGEN
    [ShimOf<TestClass_HasMethodB>]
#endif
    public interface ITestShim_MethodOverride : ITestShim
    {
        [ShimProxy(typeof(TestImpl_MethodOverride), ProxyBehaviour.Override)]
        void MethodB(string arg);
    }
    [ExcludeFromCodeCoverage]
    public class TestImpl_MethodOverride
    {
        public static ITestShim? MethodBCalledObj { get; private set; }
        public static string? MethodBCalledWith { get; private set; }
        public static void MethodB(ITestShim_MethodOverride obj, string arg)
        {
            MethodBCalledObj = obj;
            MethodBCalledWith = arg;
        }
    }

    [TestMethod]
    public void Shim_can_define_proxy_to_override_member()
    {
        // Arrange
        var obj = new TestClass_HasMethodB();
        var shim = obj.Shim<ITestShim_MethodOverride>();

        // Act
        shim.MethodB("value");

        // Assert
        Assert.IsNull(obj.MethodBCalledWith);
        Assert.AreSame(shim, TestImpl_MethodOverride.MethodBCalledObj);
        Assert.AreEqual("value", TestImpl_MethodOverride.MethodBCalledWith);
    }

#if SHIMRGEN
    [ShimOf<TestClass_HasMethodB>]
#endif
    public interface ITestShim_DefaultOverride : ITestShim
    {
        [ShimProxy(typeof(TestImpl_DefaultOverride))]
        void MethodB(string arg);
    }
    [ExcludeFromCodeCoverage]
    public class TestImpl_DefaultOverride
    {
        public static ITestShim? MethodBCalledObj { get; private set; }
        public static string? MethodBCalledWith { get; private set; }
        public static void MethodB(ITestShim_DefaultOverride obj, string arg)
        {
            MethodBCalledObj = obj;
            MethodBCalledWith = arg;
        }
    }

    [TestMethod]
    public void Shim_can_define_proxy_to_override_member_by_default()
    {
        // Arrange
        var obj = new TestClass_HasMethodB();
        var shim = obj.Shim<ITestShim_DefaultOverride>();

        // Act
        shim.MethodB("value");

        // Assert
        Assert.IsNull(obj.MethodBCalledWith);
        Assert.AreSame(shim, TestImpl_DefaultOverride.MethodBCalledObj);
        Assert.AreEqual("value", TestImpl_DefaultOverride.MethodBCalledWith);
    }

#if SHIMRGEN
    [ShimOf<TestClass_HasMethodB>]
#endif
    public interface ITestShim_MethodOverrideAlias : ITestShim
    {
        [ShimProxy(typeof(TestImpl_MethodOverrideAlias), "MethodD", ProxyBehaviour.Override)]
        void MethodB(string arg);
    }
    [ExcludeFromCodeCoverage]
    public class TestImpl_MethodOverrideAlias
    {
        public static ITestShim? MethodBCalledObj { get; private set; }
        public static string? MethodBCalledWith { get; private set; }
        public static void MethodD(ITestShim_MethodOverrideAlias obj, string arg)
        {
            MethodBCalledObj = obj;
            MethodBCalledWith = arg;
        }
    }

    [TestMethod]
    public void Shim_can_define_proxy_to_override_aliased_member_()
    {
        // Arrange
        var obj = new TestClass_HasMethodB();
        var shim = obj.Shim<ITestShim_MethodOverrideAlias>();

        // Act
        shim.MethodB("value"); // Actually TestImpl_MethodOverrideAlias.MethodB

        // Assert
        Assert.IsNull(obj.MethodBCalledWith);
        Assert.AreSame(shim, TestImpl_MethodOverrideAlias.MethodBCalledObj);
        Assert.AreEqual("value", TestImpl_MethodOverrideAlias.MethodBCalledWith);
    }

#if SHIMRGEN
    [ShimOf<TestClass_HasMethodB>]
#endif
    public interface ITestShim_CallBase : ITestShim
    {
        [ShimProxy(typeof(TestImpl_CallBase), ProxyBehaviour.Override)]
        void MethodB(string arg);
    }
    [ExcludeFromCodeCoverage]
    public class TestImpl_CallBase
    {
        public static ITestShim? MethodBCalledObj { get; set; }
        public static string? MethodBCalledWith { get; private set; }
        public static void MethodB(ITestShim_CallBase obj, string arg)
        {
            if (MethodBCalledObj != null)
            {
                throw new InvalidOperationException("recursion");
            }

            MethodBCalledObj = obj;
            MethodBCalledWith = arg;
            obj.MethodB(arg);
        }
    }

    [TestMethod]
    [Timeout(1_000)] // Incase of recursion
    public void Override_member_can_call_shimmed_implementation()
    {
        // Arrange
        var obj = new TestClass_HasMethodB();
        var shim = obj.Shim<ITestShim_CallBase>();

        var value = Guid.NewGuid().ToString();

        // Act
        shim.MethodB(value);

        // Assert
        Assert.AreSame(shim, TestImpl_CallBase.MethodBCalledObj);
        Assert.AreEqual(value, TestImpl_CallBase.MethodBCalledWith);
        Assert.AreEqual(value, obj.MethodBCalledWith);
    }

    [ExcludeFromCodeCoverage]
    public class TestClass_MethodFails
    {
#pragma warning disable CA1822 // Mark members as static
        public void Fail()
        {
            throw new InvalidOperationException();
        }
#pragma warning restore CA1822 // Mark members as static
    }
#if SHIMRGEN
    [ShimOf<TestClass_MethodFails>]
#endif
    public interface ITestShim_MethodFails
    {
        [ShimProxy(typeof(TestImpl_MethodFails), ProxyBehaviour.Override)]
        void Fail();
    }
    [ExcludeFromCodeCoverage]
    public class TestImpl_MethodFails
    {
        public static int CallPre { get; private set; }
        public static int CallPost { get; private set; }
        public static int CallEnd { get; private set; }

        public static void Fail(ITestShim_MethodFails inst)
        {
            ++CallPre;
            try
            {
                inst.Fail();
                ++CallPost;
            }
            finally
            {
                ++CallEnd;
            }
        }
    }

    [TestMethod]
    [Timeout(1_000)] // Incase of recursion
    public void Failure_during_underlying_call_does_not_break_proxy()
    {
        // Arrange
        var obj = new TestClass_MethodFails();
        var shim = obj.Shim<ITestShim_MethodFails>();

        var fails = 0;

        // Act
        try { shim.Fail(); } catch (InvalidOperationException) { ++fails; }
        try { shim.Fail(); } catch (InvalidOperationException) { ++fails; }

        // Assert
        Assert.AreEqual(2, fails);
        Assert.AreEqual(2, TestImpl_MethodFails.CallPre);
        Assert.AreEqual(0, TestImpl_MethodFails.CallPost);
        Assert.AreEqual(2, TestImpl_MethodFails.CallEnd);
    }

//#if SHIMRGEN
//    [ShimOf<TestClass_HasMethodB>]
//#endif
//    public interface ITestShim_ArgImpl : ITestShim
//    {
//        [ShimProxy(typeof(TestImpl_ArgImpl))]
//        void MethodB(string arg);
//    }
//    [ExcludeFromCodeCoverage]
//    public class TestImpl_ArgImpl
//    {
//        public static ITestShim? MethodBCalledObj { get; set; }
//        public static void MethodB(ITestShim obj, string arg)
//        {
//            MethodBCalledObj = obj;
//            _ = arg;
//        }
//    }

    [TestMethod]
    public void Override_implementation_can_invoke_on_compatible_arg()
    {
        // Arrange
        var obj = new TestClass_HasMethodB();
        var shim = obj.Shim<ITestShim_ArgImpl>();

        // Act
        shim.MethodB("value");

        // Assert
        Assert.AreSame(shim, TestImpl_ArgImpl.MethodBCalledObj);
    }

#if SHIMRGEN
    [ShimOf<TestClass_HasMethodC>]
#endif
    public interface ITestShim_WithArg : ITestShim
    {
        [ShimProxy(typeof(TestImpl_WithArg))]
        void MethodC(string arg);
    }
    [ExcludeFromCodeCoverage]
    public class TestImpl_WithArg
    {
        public static string? MethodCCalledWith { get; set; }
        public static void MethodC(ITestShim obj, string arg)
        {
            _ = obj.ToString();
            MethodCCalledWith = arg;
        }
    }

    [TestMethod]
    public void Override_implementation_can_invoke_with_args()
    {
        // Arrange
        var obj = new TestClass_HasMethodC();
        var shim = obj.Shim<ITestShim_WithArg>();

        // Act
        obj.MethodC("test1");
        shim.MethodC("test2");

        // Assert
        Assert.AreEqual("test1", obj.MethodCCalledWith);
        Assert.AreEqual("test2", TestImpl_WithArg.MethodCCalledWith);
    }

#if !SHIMRGEN // Not possible in ShimrGen
//#if SHIMRGEN
//    [ShimOf<TestClass_OnlyMethodA>]
//#endif
    public interface ITestShim_MissingBase : ITestShim
    {
        [ShimProxy(typeof(TestImpl_MissingBase), ProxyBehaviour.Override)]
        void MethodB(string arg);
    }
    [ExcludeFromCodeCoverage]
    public class TestImpl_MissingBase
    {
        public static void MethodB(ITestShim_MissingBase obj)
        {
            obj.ToString();
        }
    }

    [TestMethod]
    public void Override_member_must_exist_in_shimmed_type()
    {
        // Arrange
        var obj = new TestClass_OnlyMethodA();

        // Act
        Assert.ThrowsException<InvalidCastException>(() =>
        {
            obj.Shim<ITestShim_MissingBase>();
        });
    }
#endif

#if SHIMRGEN
    [ShimOf<TestImpl_BadImpl>]
    [ShimOf<TestClass_OnlyMethodA>]
    [ShimOf<TestClass_HasMethodB>]
#endif
    public interface ITestShim_BadImpl : ITestShim
    {
        [ShimProxy(typeof(TestImpl_BadImpl))]
        void MethodB(string arg);
    }
    [ExcludeFromCodeCoverage]
    public class TestImpl_BadImpl
    {
        public void MethodA()
        {
            _ = GetType();
        }

        public static string? MethodBCalledWith { get; private set; }
        public static void MethodB(string arg)
        {
            MethodBCalledWith = arg;
        }
    }

#if !SHIMRGEN // Logic to be changed in IFY.Shimr
    [TestMethod]
    public void Override_implementation_must_take_compatible_first_param()
    {
        // Arrange
        var obj = new TestClass_HasMethodB();

        // Act
        Assert.ThrowsException<MissingMemberException>(() =>
        {
            obj.Shim<ITestShim_BadImpl>();
        });
    }
#endif

#if SHIMRGEN
    [ShimOf<TestClass_OnlyMethodA>]
#endif
    public interface ITestShim_ChangeShim : ITestShim
    {
        [ShimProxy(typeof(TestImpl_ChangeShim))]
        new void MethodA();
    }
    [ExcludeFromCodeCoverage]
    public class TestImpl_ChangeShim
    {
        public static ITestShim_ChangeShim? MethodACalledObj { get; set; }
        public static void MethodA(ITestShim_ChangeShim inst)
        {
            MethodACalledObj = inst;
        }
    }

    [TestMethod]
    public void Can_change_shim_in_hierarchy()
    {
        // Arrange
        var obj = new TestClass_OnlyMethodA();
        var shim = obj.Shim<ITestShim_ChangeShim>();

        // Act
        shim.MethodA();

        // Assert
        Assert.IsFalse(obj.MethodACalled);
        Assert.AreSame(shim, TestImpl_ChangeShim.MethodACalledObj);
    }

#if !SHIMRGEN // Not possible in ShimrGen
//#if SHIMRGEN
//    [ShimOf<TestClass_OnlyMethodA>]
//#endif
    public interface ITestShim_Constructor
    {
        [ConstructorShim(typeof(TestClass_OnlyMethodA))]
        [ShimProxy(typeof(TestImpl_DefaultOverride))]
        ITestShim MethodB(string arg);
    }
    [TestMethod]
    public void Cannot_override_constructor()
    {
        // Act
        var ex = Assert.ThrowsException<InvalidCastException>(() =>
        {
            ShimBuilder.Create<ITestShim_Constructor>();
        });

        // Assert
        Assert.AreEqual("Cannot proxy IFY.Shimr.Tests.ExtendedFunctionalityTests_Method+TestClass_OnlyMethodA constructor in IFY.Shimr.Tests.ExtendedFunctionalityTests_Method+ITestShim_Constructor", ex.Message);
    }
#endif

    #endregion Override

    #region Add

#if SHIMRGEN
    [ShimOf<TestClass_OnlyMethodA>]
#endif
    public interface ITestShim_MethodAdd : ITestShim
    {
        [ShimProxy(typeof(TestImpl_MethodAdd), ProxyBehaviour.Add)]
        void MethodB(string arg);
    }
    [ExcludeFromCodeCoverage]
    public class TestImpl_MethodAdd
    {
        public static ITestShim? MethodBCalledObj { get; private set; }
        public static string? MethodBCalledWith { get; private set; }
        public static void MethodB(ITestShim_MethodAdd obj, string arg)
        {
            MethodBCalledObj = obj;
            MethodBCalledWith = arg;
        }
    }

    [TestMethod]
    public void Shim_can_define_proxy_to_add_member()
    {
        // Arrange
        var obj = new TestClass_OnlyMethodA();
        var shim = obj.Shim<ITestShim_MethodAdd>();

        // Act
        shim.MethodB("value");

        // Assert
        Assert.AreSame(shim, TestImpl_MethodAdd.MethodBCalledObj);
    }

#if SHIMRGEN
    [ShimOf<TestClass_OnlyMethodA>]
#endif
    public interface ITestShim_DefaultAdd : ITestShim
    {
        [ShimProxy(typeof(TestImpl_DefaultAdd))]
        void MethodB(string arg);
    }
    [ExcludeFromCodeCoverage]
    public class TestImpl_DefaultAdd
    {
        public static ITestShim? MethodBCalledObj { get; set; }
        public static string? MethodBCalledWith { get; private set; }
        public static void MethodB(ITestShim_DefaultAdd obj, string arg)
        {
            MethodBCalledObj = obj;
            MethodBCalledWith = arg;
        }
    }

    [TestMethod]
    public void Shim_can_define_proxy_to_add_member_by_default()
    {
        // Arrange
        var obj = new TestClass_OnlyMethodA();
        var shim = obj.Shim<ITestShim_DefaultAdd>();

        // Act
        shim.MethodB("value");

        // Assert
        Assert.AreSame(shim, TestImpl_DefaultAdd.MethodBCalledObj);
        Assert.AreEqual("value", TestImpl_DefaultAdd.MethodBCalledWith);
    }

#if SHIMRGEN
    [ShimOf<TestClass_OnlyMethodA>]
#endif
    public interface ITestShim_MethodAddAlias : ITestShim
    {
        [ShimProxy(typeof(TestImpl_MethodAddAlias), "MethodD", ProxyBehaviour.Add)]
        void MethodC();
    }
    [ExcludeFromCodeCoverage]
    public class TestImpl_MethodAddAlias
    {
        public static ITestShim? MethodBCalledObj { get; set; }
        public static void MethodD(ITestShim_MethodAddAlias obj)
        {
            MethodBCalledObj = obj;
        }
    }

    [TestMethod]
    public void Shim_can_define_proxy_to_add_member_to_alias_impl()
    {
        // Arrange
        var obj = new TestClass_OnlyMethodA();
        var shim = obj.Shim<ITestShim_MethodAddAlias>();

        // Act
        shim.MethodC(); // Actually TestImpl_MethodOverrideAlias.MethodB

        // Assert
        Assert.AreSame(shim, TestImpl_MethodAddAlias.MethodBCalledObj);
    }

#if !SHIMRGEN // Not possible in Shimr.Gen
    [TestMethod]
    public void Added_member_must_not_exist_in_shimmed_type()
    {
        // Arrange
        var obj = new TestClass_HasMethodB();

        // Act
        Assert.ThrowsException<InvalidCastException>(() =>
        {
            obj.Shim<ITestShim_MethodAdd>();
        });
    }
#endif

#if !SHIMRGEN // TODO: Shimr to support no first parameter
    [TestMethod]
    public void Added_implementation_must_take_compatible_first_param()
    {
        // Arrange
        var obj = new TestClass_OnlyMethodA();

        // Act
        var ex = Assert.ThrowsException<MissingMemberException>(() =>
        {
            obj.Shim<ITestShim_BadImpl>();
        });

        Assert.IsTrue(ex.Message.Contains(" missing method:"), ex.Message);
    }
#endif
#if SHIMRGEN
    [TestMethod]
    public void Added_implementation_works_without_first_parameter()
    {
        // Arrange
        var obj = new TestClass_OnlyMethodA();

        var shim = obj.Shim<ITestShim_BadImpl>();

        // Act
        shim.MethodB("value");

        Assert.AreEqual("value", TestImpl_BadImpl.MethodBCalledWith);
    }
#endif

    #endregion Add

#if !SHIMRGEN // Not possible in Shimr.Gen
//#if SHIMRGEN
//    [ShimOf<TestClass_HasMethodB>]
//#endif
    public interface ITestShim_MissingImpl : ITestShim
    {
        [ShimProxy(typeof(TestImpl_DefaultOverride))]
        void MethodC(); // Not in TestImpl_Default
    }

    [TestMethod]
    public void Proxy_member_must_exist_in_impl_type()
    {
        // Arrange
        var obj = new TestClass_HasMethodB();

        // Act
        Assert.ThrowsException<MissingMemberException>(() =>
        {
            obj.Shim<ITestShim_MissingImpl>();
        });
    }

    [TestMethod]
    public void Factory_cannot_define_proxy()
    {
        // Arrange
        var obj = new TestClass_HasMethodB();

        // Act
        Assert.ThrowsException<MissingMemberException>(() =>
        {
            obj.Shim<ITestShim_MissingImpl>();
        });
    }
#endif
}
