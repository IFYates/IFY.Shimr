using Microsoft.CodeAnalysis;
using Moq;
using System.Collections.Immutable;

namespace IFY.Shimr.Gen.Model.Tests;

[TestClass]
public class ShimPropertyModelTests
{
    private static IParameterSymbol getParameter<T>(string name = "param")
    {
        var parMock = new Mock<IParameterSymbol>(MockBehavior.Strict);
        parMock.SetupGet(m => m.Name).Returns(name);
        parMock.SetupGet(m => m.Type).Returns(getType<T>());
        return parMock.Object;
    }

    private static INamedTypeSymbol getType<T>()
    {
        var nsMock = new Mock<INamespaceSymbol>(MockBehavior.Strict);
        nsMock.SetupGet(m => m.Name).Returns(typeof(T).Namespace!);
        nsMock.SetupGet(m => m.ContainingNamespace).Returns(() => null!);

        var symMock = new Mock<INamedTypeSymbol>(MockBehavior.Strict);
        symMock.SetupGet(m => m.Arity).Returns(0);
        symMock.SetupGet(m => m.AllInterfaces).Returns(ImmutableArray<INamedTypeSymbol>.Empty);
        symMock.SetupGet(m => m.ContainingNamespace).Returns(nsMock.Object);
        symMock.SetupGet(m => m.ContainingType).Returns(() => null!);
        symMock.SetupGet(m => m.IsValueType).Returns(false);
        symMock.SetupGet(m => m.Name).Returns(typeof(T).Name);
        symMock.SetupGet(m => m.NullableAnnotation).Returns(NullableAnnotation.None);
        symMock.SetupGet(m => m.TypeArguments).Returns(ImmutableArray<ITypeSymbol>.Empty);
        symMock.SetupGet(m => m.TypeKind).Returns(TypeKind.Class);
        return symMock.Object;
    }

    [TestMethod]
    public void ToString__Readonly_property()
    {
        // Arrange
        var propSymMock = new Mock<IPropertySymbol>(MockBehavior.Strict);
        propSymMock.SetupGet(m => m.Name).Returns("Property");
        propSymMock.SetupGet(m => m.SetMethod).Returns(() => null);

        var getMethodMock = new Mock<IMethodSymbol>(MockBehavior.Strict);
        getMethodMock.SetupGet(m => m.ReturnType).Returns(getType<string>());
        propSymMock.SetupGet(m => m.GetMethod).Returns(getMethodMock.Object);

        var spm = new ShimPropertyMember(propSymMock.Object, null!);

        // Act
        var str = spm.ToString();

        // Assert
        Assert.AreEqual("public System.String Property\n\t=> _obj.Property;", str);
    }

    [TestMethod]
    public void ToString__Writeonly_property()
    {
        // Arrange
        var propSymMock = new Mock<IPropertySymbol>(MockBehavior.Strict);
        propSymMock.SetupGet(m => m.Name).Returns("Property");
        propSymMock.SetupGet(m => m.GetMethod).Returns(() => null);

        var setMethodMock = new Mock<IMethodSymbol>(MockBehavior.Strict);
        setMethodMock.SetupGet(m => m.Parameters).Returns(ImmutableArray.Create(getParameter<string>()));
        propSymMock.SetupGet(m => m.SetMethod).Returns(setMethodMock.Object);

        var spm = new ShimPropertyMember(propSymMock.Object, null!);

        // Act
        var str = spm.ToString();

        // Assert
        Assert.AreEqual("public System.String Property\n{\n\tset => _obj.Property = value;\n}", str);
    }

    [TestMethod]
    public void ToString__ReadWrite_property()
    {
        // Arrange
        var propSymMock = new Mock<IPropertySymbol>(MockBehavior.Strict);
        propSymMock.SetupGet(m => m.Name).Returns("Property");

        var getMethodMock = new Mock<IMethodSymbol>(MockBehavior.Strict);
        getMethodMock.SetupGet(m => m.ReturnType).Returns(getType<string>());
        propSymMock.SetupGet(m => m.GetMethod).Returns(getMethodMock.Object);

        var setMethodMock = new Mock<IMethodSymbol>(MockBehavior.Strict);
        setMethodMock.SetupGet(m => m.Parameters).Returns(ImmutableArray.Create(getParameter<string>()));
        propSymMock.SetupGet(m => m.SetMethod).Returns(setMethodMock.Object);

        var spm = new ShimPropertyMember(propSymMock.Object, null!);

        // Act
        var str = spm.ToString();

        // Assert
        Assert.AreEqual("public System.String Property\n{\n\tget => _obj.Property;\n\tset => _obj.Property = value;\n}", str);
    }

    [TestMethod]
    public void ToString__ReadWrite_property_methods()
    {
        // Arrange
        var propSymMock = new Mock<IPropertySymbol>(MockBehavior.Strict);
        propSymMock.SetupGet(m => m.Name).Returns("Property");

        var getMethodMock = new Mock<IMethodSymbol>(MockBehavior.Strict);
        getMethodMock.SetupGet(m => m.ReturnType).Returns(getType<string>());
        propSymMock.SetupGet(m => m.GetMethod).Returns(getMethodMock.Object);

        var setMethodMock = new Mock<IMethodSymbol>(MockBehavior.Strict);
        setMethodMock.SetupGet(m => m.Parameters).Returns(ImmutableArray.Create(getParameter<string>()));
        propSymMock.SetupGet(m => m.SetMethod).Returns(setMethodMock.Object);

        var spm = new ShimPropertyMember(propSymMock.Object, null!)
        {
            UseMethods = true // TEMP
        };

        // Act
        var str = spm.ToString();

        // Assert
        Assert.AreEqual("public System.String Property\n{\n\tget => _obj.get_Property();\n\tset => _obj.set_Property(value);\n}", str);
    }
}
