using IFY.Shimr.Extensions;
using System;
using System.Diagnostics.CodeAnalysis;

namespace IFY.Shimr.Tests;

/// <summary>
/// Tests around extending/replacing shim functionality
/// https://github.com/IFYates/Shimterface/issues/3
/// </summary>
[TestClass]
public class ExtendedFunctionalityTests_Method
{
#if !SHIMR_CG
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
        public bool MethodBCalled { get; private set; }
        public void MethodB()
        {
            MethodBCalled = true;
        }
    }

    //    [ExcludeFromCodeCoverage]
    //    public class TestClass_HasMethodC : TestClass_OnlyMethodA
    //    {
    //        public string MethodCCalledWith { get; private set; } = null!;
    //        public void MethodC(string arg)
    //        {
    //            MethodCCalledWith = arg;
    //        }
    //    }

    //    #region Override

    public interface ITestShim_MethodOverride : ITestShim
    {
        [ShimProxy(typeof(ProxyImpl_MethodOverride), ProxyBehaviour.Override)]
        void MethodB();
    }
    [ExcludeFromCodeCoverage]
    public class ProxyImpl_MethodOverride
    {
        public static ITestShim MethodBCalledObj { get; set; } = null!;
        public static void MethodB(ITestShim_MethodOverride obj)
        {
            MethodBCalledObj = obj;
        }
    }

    [TestMethod]
    public void Shim_can_define_proxy_to_override_member()
    {
        // Arrange
        var obj = new TestClass_HasMethodB();
        var shim = obj.Shim<ITestShim_MethodOverride>();

        // Act
        shim.MethodB();

        // Assert
        Assert.IsFalse(obj.MethodBCalled);
        Assert.AreSame(shim, ProxyImpl_MethodOverride.MethodBCalledObj);
    }

    //    public interface ITestShim_DefaultOverride : ITestShim
    //    {
    //        [ShimProxy(typeof(ProxyImpl_DefaultOverride))]
    //        void MethodB();
    //    }
    //    [ExcludeFromCodeCoverage]
    //    public class ProxyImpl_DefaultOverride
    //    {
    //        public static ITestShim MethodBCalledObj { get; set; } = null!;
    //        public static void MethodB(ITestShim_DefaultOverride obj)
    //        {
    //            MethodBCalledObj = obj;
    //        }
    //    }

    //    [TestMethod]
    //    public void Shim_can_define_proxy_to_override_member_by_default()
    //    {
    //        // Arrange
    //        var obj = new TestClass_HasMethodB();
    //        var shim = obj.Shim<ITestShim_DefaultOverride>();

    //        // Act
    //        shim.MethodB();

    //        // Assert
    //        Assert.IsFalse(obj.MethodBCalled);
    //        Assert.AreSame(shim, ProxyImpl_DefaultOverride.MethodBCalledObj);
    //    }

    //    public interface ITestShim_MethodOverrideAlias : ITestShim
    //    {
    //        [Shim("MethodB")]
    //        [ShimProxy(typeof(ProxyImpl_MethodOverrideAlias), "MethodD", ProxyBehaviour.Override)]
    //        void MethodC();
    //    }
    //    [ExcludeFromCodeCoverage]
    //    public class ProxyImpl_MethodOverrideAlias
    //    {
    //        public static ITestShim MethodBCalledObj { get; set; } = null!;
    //        public static void MethodD(ITestShim_MethodOverrideAlias obj)
    //        {
    //            MethodBCalledObj = obj;
    //        }
    //    }

    //    [TestMethod]
    //    public void Shim_can_define_proxy_to_override_member_with_alias()
    //    {
    //        // Arrange
    //        var obj = new TestClass_HasMethodB();
    //        var shim = obj.Shim<ITestShim_MethodOverrideAlias>();

    //        // Act
    //        shim.MethodC(); // Actually ProxyImpl_MethodOverrideAlias.MethodB

    //        // Assert
    //        Assert.IsFalse(obj.MethodBCalled);
    //        Assert.AreSame(shim, ProxyImpl_MethodOverrideAlias.MethodBCalledObj);
    //    }

    //    public interface ITestShim_CallBase : ITestShim
    //    {
    //        [ShimProxy(typeof(ProxyImpl_CallBase))]
    //        void MethodB();
    //    }
    //    [ExcludeFromCodeCoverage]
    //    public class ProxyImpl_CallBase
    //    {
    //        public static ITestShim MethodBCalledObj { get; set; } = null!;
    //        public static void MethodB(ITestShim_CallBase obj)
    //        {
    //            if (MethodBCalledObj != null)
    //            {
    //                throw new InvalidOperationException("recursion");
    //            }

    //            MethodBCalledObj = obj;
    //            obj.MethodB();
    //        }
    //    }

    //    [TestMethod]
    //    [Timeout(1_000)] // Incase of recursion
    //    public void Override_member_can_call_shimmed_implementation()
    //    {
    //        // Arrange
    //        var obj = new TestClass_HasMethodB();
    //        var shim = obj.Shim<ITestShim_CallBase>();

    //        // Act
    //        shim.MethodB();

    //        // Assert
    //        Assert.AreSame(shim, ProxyImpl_CallBase.MethodBCalledObj);
    //        Assert.IsTrue(obj.MethodBCalled);
    //    }

    //    [ExcludeFromCodeCoverage]
    //    public class TestClass_MethodFails
    //    {
    //#pragma warning disable CA1822 // Mark members as static
    //        public void Fail()
    //        {
    //            throw new InvalidOperationException();
    //        }
    //#pragma warning restore CA1822 // Mark members as static
    //    }
    //    public interface ITestShim_MethodFails
    //    {
    //        [ShimProxy(typeof(ProxyImpl_MethodFails), ProxyBehaviour.Override)]
    //        void Fail();
    //    }
    //    [ExcludeFromCodeCoverage]
    //    public class ProxyImpl_MethodFails
    //    {
    //        public static int CallPre { get; private set; }
    //        public static int CallPost { get; private set; }
    //        public static int CallEnd { get; private set; }

    //        public static void Fail(ITestShim_MethodFails inst)
    //        {
    //            ++CallPre;
    //            try
    //            {
    //                inst.Fail();
    //                ++CallPost;
    //            }
    //            finally
    //            {
    //                ++CallEnd;
    //            }
    //        }
    //    }

    //    [TestMethod]
    //    [Timeout(1_000)] // Incase of recursion
    //    public void Failure_during_underlying_call_does_not_break_proxy()
    //    {
    //        // Arrange
    //        var obj = new TestClass_MethodFails();
    //        var shim = obj.Shim<ITestShim_MethodFails>();

    //        var fails = 0;

    //        // Act
    //        try { shim.Fail(); } catch (InvalidOperationException) { ++fails; }
    //        try { shim.Fail(); } catch (InvalidOperationException) { ++fails; }

    //        // Assert
    //        Assert.AreEqual(2, fails);
    //        Assert.AreEqual(2, ProxyImpl_MethodFails.CallPre);
    //        Assert.AreEqual(0, ProxyImpl_MethodFails.CallPost);
    //        Assert.AreEqual(2, ProxyImpl_MethodFails.CallEnd);
    //    }

    //    public interface ITestShim_ArgImpl : ITestShim
    //    {
    //        [ShimProxy(typeof(ProxyImpl_ArgImpl))]
    //        void MethodB();
    //    }
    //    [ExcludeFromCodeCoverage]
    //    public class ProxyImpl_ArgImpl
    //    {
    //        public static ITestShim MethodBCalledObj { get; set; } = null!;
    //        public static void MethodB(ITestShim obj)
    //        {
    //            MethodBCalledObj = obj;
    //        }
    //    }

    //    [TestMethod]
    //    public void Override_implementation_can_invoke_on_compatible_arg()
    //    {
    //        // Arrange
    //        var obj = new TestClass_HasMethodB();
    //        var shim = obj.Shim<ITestShim_ArgImpl>();

    //        // Act
    //        shim.MethodB();

    //        // Assert
    //        Assert.AreSame(shim, ProxyImpl_ArgImpl.MethodBCalledObj);
    //    }

    //    public interface ITestShim_WithArg : ITestShim
    //    {
    //        [ShimProxy(typeof(ProxyImpl_WithArg))]
    //        void MethodC(string arg);
    //    }
    //    [ExcludeFromCodeCoverage]
    //    public class ProxyImpl_WithArg
    //    {
    //        public static string MethodCCalledWith { get; set; } = null!;
    //        public static void MethodC(ITestShim obj, string arg)
    //        {
    //            _ = obj.ToString();
    //            MethodCCalledWith = arg;
    //        }
    //    }

    //    [TestMethod]
    //    public void Override_implementation_can_invoke_with_args()
    //    {
    //        // Arrange
    //        var obj = new TestClass_HasMethodC();
    //        var shim = obj.Shim<ITestShim_WithArg>();

    //        // Act
    //        obj.MethodC("test1");
    //        shim.MethodC("test2");

    //        // Assert
    //        Assert.AreEqual("test1", obj.MethodCCalledWith);
    //        Assert.AreEqual("test2", ProxyImpl_WithArg.MethodCCalledWith);
    //    }

    //    public interface ITestShim_MissingBase : ITestShim
    //    {
    //        [ShimProxy(typeof(ProxyImpl_MissingBase), ProxyBehaviour.Override)]
    //        void MethodB();
    //    }
    //    [ExcludeFromCodeCoverage]
    //    public class ProxyImpl_MissingBase
    //    {
    //        public static void MethodB(ITestShim_MissingBase obj)
    //        {
    //            obj.ToString();
    //        }
    //    }

    //#if !SHIMR_CG
    //    [TestMethod]
    //    public void Override_member_must_exist_in_shimmed_type()
    //    {
    //        // Arrange
    //        var obj = new TestClass_OnlyMethodA();

    //        // Act
    //        Assert.ThrowsException<InvalidCastException>(() =>
    //        {
    //            obj.Shim<ITestShim_MissingBase>();
    //        });
    //    }
    //#endif

    //    public interface ITestShim_BadImpl : ITestShim
    //    {
    //        [ShimProxy(typeof(ProxyImpl_BadImpl))]
    //        void MethodB();
    //    }
    //    [ExcludeFromCodeCoverage]
    //    public class ProxyImpl_BadImpl
    //    {
    //        public static void MethodB()
    //        {
    //            // Test
    //        }
    //    }
    //#if !SHIMR_CG

    //    [TestMethod]
    //    public void Override_implementation_must_take_compatible_first_param()
    //    {
    //        // Arrange
    //        var obj = new TestClass_HasMethodB();

    //        // Act
    //        Assert.ThrowsException<MissingMemberException>(() =>
    //        {
    //            obj.Shim<ITestShim_BadImpl>();
    //        });
    //    }
    //#endif

    //    public interface ITestShim_ChangeShim : ITestShim
    //    {
    //        [ShimProxy(typeof(ProxyImpl_ChangeShim))]
    //        new void MethodA();
    //    }
    //    [ExcludeFromCodeCoverage]
    //    public class ProxyImpl_ChangeShim
    //    {
    //        public static ITestShim_ChangeShim MethodACalledObj { get; set; } = null!;
    //        public static void MethodA(ITestShim_ChangeShim inst)
    //        {
    //            MethodACalledObj = inst;
    //        }
    //    }

    //    [TestMethod]
    //    public void Can_change_shim_in_hierarchy()
    //    {
    //        // Arrange
    //        var obj = new TestClass_OnlyMethodA();
    //        var shim = obj.Shim<ITestShim_ChangeShim>();

    //        // Act
    //        shim.MethodA();

    //        // Assert
    //        Assert.IsFalse(obj.MethodACalled);
    //        Assert.AreSame(shim, ProxyImpl_ChangeShim.MethodACalledObj);
    //    }

    //#if !SHIMR_CG
    //    public interface ITestShim_Constructor
    //    {
    //        [ConstructorShim(typeof(TestClass_OnlyMethodA))]
    //        [ShimProxy(typeof(ProxyImpl_DefaultOverride))]
    //        ITestShim MethodB();
    //    }
    //    [TestMethod]
    //    public void Cannot_override_constructor()
    //    {
    //        // Act
    //        var ex = Assert.ThrowsException<InvalidCastException>(() =>
    //        {
    //            ShimBuilder.Create<ITestShim_Constructor>();
    //        });

    //        // Assert
    //        Assert.AreEqual("Cannot proxy IFY.Shimr.Tests.ExtendedFunctionalityTests_Method+TestClass_OnlyMethodA constructor in IFY.Shimr.Tests.ExtendedFunctionalityTests_Method+ITestShim_Constructor", ex.Message);
    //    }
    //#endif

    //    #endregion Override

    //    #region Add

    //    public interface ITestShim_MethodAdd : ITestShim
    //    {
    //        [ShimProxy(typeof(ProxyImpl_MethodAdd), ProxyBehaviour.Add)]
    //        void MethodB();
    //    }
    //    [ExcludeFromCodeCoverage]
    //    public class ProxyImpl_MethodAdd
    //    {
    //        public static ITestShim MethodBCalledObj { get; set; } = null!;
    //        public static void MethodB(ITestShim_MethodAdd obj)
    //        {
    //            MethodBCalledObj = obj;
    //        }
    //    }

    //    [TestMethod]
    //    public void Shim_can_define_proxy_to_add_member()
    //    {
    //        // Arrange
    //        var obj = new TestClass_OnlyMethodA();
    //        var shim = obj.Shim<ITestShim_MethodAdd>();

    //        // Act
    //        shim.MethodB();

    //        // Assert
    //        Assert.AreSame(shim, ProxyImpl_MethodAdd.MethodBCalledObj);
    //    }

    //    public interface ITestShim_DefaultAdd : ITestShim
    //    {
    //        [ShimProxy(typeof(ProxyImpl_DefaultAdd))]
    //        void MethodB();
    //    }
    //    [ExcludeFromCodeCoverage]
    //    public class ProxyImpl_DefaultAdd
    //    {
    //        public static ITestShim MethodBCalledObj { get; set; } = null!;
    //        public static void MethodB(ITestShim_DefaultAdd obj)
    //        {
    //            MethodBCalledObj = obj;
    //        }
    //    }

    //    [TestMethod]
    //    public void Shim_can_define_proxy_to_add_member_by_default()
    //    {
    //        // Arrange
    //        var obj = new TestClass_OnlyMethodA();
    //        var shim = obj.Shim<ITestShim_DefaultAdd>();

    //        // Act
    //        shim.MethodB();

    //        // Assert
    //        Assert.AreSame(shim, ProxyImpl_DefaultAdd.MethodBCalledObj);
    //    }

    //    public interface ITestShim_MethodAddAlias : ITestShim
    //    {
    //        [ShimProxy(typeof(ProxyImpl_MethodAddAlias), "MethodD", ProxyBehaviour.Add)]
    //        void MethodC();
    //    }
    //    [ExcludeFromCodeCoverage]
    //    public class ProxyImpl_MethodAddAlias
    //    {
    //        public static ITestShim MethodBCalledObj { get; set; } = null!;
    //        public static void MethodD(ITestShim_MethodAddAlias obj)
    //        {
    //            MethodBCalledObj = obj;
    //        }
    //    }

    //    [TestMethod]
    //    public void Shim_can_define_proxy_to_add_member_to_alias_impl()
    //    {
    //        // Arrange
    //        var obj = new TestClass_OnlyMethodA();
    //        var shim = obj.Shim<ITestShim_MethodAddAlias>();

    //        // Act
    //        shim.MethodC(); // Actually ProxyImpl_MethodOverrideAlias.MethodB

    //        // Assert
    //        Assert.AreSame(shim, ProxyImpl_MethodAddAlias.MethodBCalledObj);
    //    }

    //#if !SHIMR_CG
    //    [TestMethod]
    //    public void Added_member_must_not_exist_in_shimmed_type()
    //    {
    //        // Arrange
    //        var obj = new TestClass_HasMethodB();

    //        // Act
    //        Assert.ThrowsException<InvalidCastException>(() =>
    //        {
    //            obj.Shim<ITestShim_MethodAdd>();
    //        });
    //    }
    //#endif

    //#if !SHIMR_CG
    //    [TestMethod]
    //    public void Added_implementation_must_take_compatible_first_param()
    //    {
    //        // Arrange
    //        var obj = new TestClass_OnlyMethodA();

    //        // Act
    //        var ex = Assert.ThrowsException<MissingMemberException>(() =>
    //        {
    //            obj.Shim<ITestShim_BadImpl>();
    //        });

    //        Assert.IsTrue(ex.Message.Contains(" missing method:"), ex.Message);
    //    }
    //#endif

    //    #endregion Add

    //#if !SHIMR_CG
    //    public interface ITestShim_MissingImpl : ITestShim
    //    {
    //        [ShimProxy(typeof(ProxyImpl_DefaultOverride))]
    //        void MethodC(); // Not in ProxyImpl_Default
    //    }

    //    [TestMethod]
    //    public void Proxy_member_must_exist_in_impl_type()
    //    {
    //        // Arrange
    //        var obj = new TestClass_HasMethodB();

    //        // Act
    //        Assert.ThrowsException<MissingMemberException>(() =>
    //        {
    //            obj.Shim<ITestShim_MissingImpl>();
    //        });
    //    }

    //    [TestMethod]
    //    public void Factory_cannot_define_proxy()
    //    {
    //        // Arrange
    //        var obj = new TestClass_HasMethodB();

    //        // Act
    //        Assert.ThrowsException<MissingMemberException>(() =>
    //        {
    //            obj.Shim<ITestShim_MissingImpl>();
    //        });
    //    }
    //#endif
}
