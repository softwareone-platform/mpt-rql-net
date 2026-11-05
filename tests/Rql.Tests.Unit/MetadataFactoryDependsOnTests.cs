using FluentAssertions;
using Mpt.Rql;
using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Exception;
using Mpt.Rql.Core.Metadata;
using Rql.Tests.Common.Factory;
using System.Reflection;
using Xunit;

namespace Rql.Tests.Unit;

public class MetadataFactoryDependsOnTests
{
    private static MetadataFactory CreateSut() => new(RqlSettingsFactory.Default());

    [Fact]
    public void MakeRqlPropertyInfo_IgnoredPropertyDeclaresDependencies_ThrowsInvalidDependencyException()
    {
        // Arrange
        var factory = CreateSut();
        var property = typeof(InvalidModel).GetProperty(nameof(InvalidModel.IgnoredComputed))!;

        // Act
        var act = () => factory.MakeRqlPropertyInfo("ignoredComputed", property);

        // Assert
        act.Should().Throw<RqlInvalidDependencyException>()
            .WithMessage("*IgnoredComputed is ignored and cannot declare dependencies*");
    }

    [Fact]
    public void MakeRqlPropertyInfo_DependsOnItself_ThrowsInvalidDependencyException()
    {
        // Arrange
        var factory = CreateSut();
        var property = typeof(InvalidModel).GetProperty(nameof(InvalidModel.SelfDependent))!;

        // Act
        var act = () => factory.MakeRqlPropertyInfo("selfDependent", property);

        // Assert
        act.Should().Throw<RqlInvalidDependencyException>()
            .WithMessage("*SelfDependent cannot depend on itself*");
    }

    [Theory]
    [InlineData(nameof(InvalidModel.SelfLastAmongOthers))]
    [InlineData(nameof(InvalidModel.SelfFirstAmongOthers))]
    public void MakeRqlPropertyInfo_DependsOnItselfAmongValidNames_ThrowsInvalidDependencyException(string propertyName)
    {
        // Arrange
        var factory = CreateSut();
        var property = typeof(InvalidModel).GetProperty(propertyName)!;

        // Act
        var act = () => factory.MakeRqlPropertyInfo(propertyName, property);

        // Assert
        act.Should().Throw<RqlInvalidDependencyException>()
            .WithMessage($"*{propertyName} cannot depend on itself*");
    }

    [Fact]
    public void MakeRqlPropertyInfo_DependsOnItselfInDifferentCase_ThrowsInvalidDependencyException()
    {
        // Arrange
        var factory = CreateSut();
        var property = typeof(InvalidModel).GetProperty(nameof(InvalidModel.SelfDependentLower))!;

        // Act
        var act = () => factory.MakeRqlPropertyInfo("selfDependentLower", property);

        // Assert — the attribute names the property itself with a lowercase first letter
        act.Should().Throw<RqlInvalidDependencyException>()
            .WithMessage("*depends on 'selfDependentLower', which is not a property of its type*");
    }

    [Fact]
    public void MakeRqlPropertyInfo_DependsOnUnknownName_ThrowsInvalidDependencyException()
    {
        // Arrange
        var factory = CreateSut();
        var property = typeof(InvalidModel).GetProperty(nameof(InvalidModel.DependsOnMissing))!;

        // Act
        var act = () => factory.MakeRqlPropertyInfo("dependsOnMissing", property);

        // Assert
        act.Should().Throw<RqlInvalidDependencyException>()
            .WithMessage("*depends on 'Missing', which is not a property of its type*");
    }

    [Theory]
    [InlineData(nameof(InvalidModel.DependsOnEmptyName), "")]
    // the message interpolates a null name as an empty string
    [InlineData(nameof(InvalidModel.DependsOnNullName), "")]
    [InlineData(nameof(InvalidModel.DependsOnOtherTypeProperty), nameof(ValidModel.First))]
    public void MakeRqlPropertyInfo_DependsOnNameNotOnType_ThrowsInvalidDependencyException(string declaringName, string dependencyName)
    {
        // Arrange
        var factory = CreateSut();
        var property = typeof(InvalidModel).GetProperty(declaringName)!;

        // Act
        var act = () => factory.MakeRqlPropertyInfo(declaringName, property);

        // Assert
        act.Should().Throw<RqlInvalidDependencyException>()
            .WithMessage($"*depends on '{dependencyName}', which is not a property of its type*");
    }

    [Theory]
    [InlineData(nameof(MemberKindModel.DependsOnField), nameof(MemberKindModel.PublicField))]
    [InlineData(nameof(MemberKindModel.DependsOnPrivateProperty), "PrivateValue")]
    public void MakeRqlPropertyInfo_DependsOnMemberNotListedAsPublicProperty_ThrowsInvalidDependencyException(string declaringName, string dependencyName)
    {
        // Arrange
        var factory = CreateSut();
        var property = typeof(MemberKindModel).GetProperty(declaringName)!;

        // Act
        var act = () => factory.MakeRqlPropertyInfo(declaringName, property);

        // Assert
        act.Should().Throw<RqlInvalidDependencyException>()
            .WithMessage($"*depends on '{dependencyName}', which is not a property of its type*");
    }

    [Fact]
    public void MakeRqlPropertyInfo_DependsOnPublicStaticProperty_DoesNotThrowAndStoresDependency()
    {
        // Arrange
        var factory = CreateSut();
        var property = typeof(MemberKindModel).GetProperty(nameof(MemberKindModel.DependsOnStaticProperty))!;

        // Act
        var act = () => factory.MakeRqlPropertyInfo("dependsOnStaticProperty", property);

        // Assert
        act.Should().NotThrow().Which.DependsOn.Should().Equal(nameof(MemberKindModel.StaticValue));
    }

    [Fact]
    public void MakeRqlPropertyInfo_DependsOnIgnoredProperty_ThrowsInvalidDependencyException()
    {
        // Arrange
        var factory = CreateSut();
        var property = typeof(InvalidModel).GetProperty(nameof(InvalidModel.DependsOnIgnored))!;

        // Act
        var act = () => factory.MakeRqlPropertyInfo("dependsOnIgnored", property);

        // Assert
        act.Should().Throw<RqlInvalidDependencyException>()
            .WithMessage($"*depends on '{nameof(InvalidModel.Hidden)}', which is ignored*");
    }

    [Theory]
    [InlineData(nameof(InvalidModel.DependsOnReference), nameof(InvalidModel.Reference))]
    [InlineData(nameof(InvalidModel.DependsOnCollection), nameof(InvalidModel.Items))]
    public void MakeRqlPropertyInfo_DependsOnNonPrimitiveProperty_ThrowsInvalidDependencyException(string declaringName, string dependencyName)
    {
        // Arrange
        var factory = CreateSut();
        var property = typeof(InvalidModel).GetProperty(declaringName)!;

        // Act
        var act = () => factory.MakeRqlPropertyInfo(declaringName, property);

        // Assert
        act.Should().Throw<RqlInvalidDependencyException>()
            .WithMessage($"*depends on '{dependencyName}', which is not a primitive property*");
    }

    [Fact]
    public void MakeRqlPropertyInfo_ValidDependenciesWithDuplicate_StoresDistinctNames()
    {
        // Arrange
        var factory = CreateSut();
        var property = typeof(ValidModel).GetProperty(nameof(ValidModel.FullName))!;

        // Act
        var result = factory.MakeRqlPropertyInfo("fullName", property);

        // Assert — the attribute lists First twice, then Last
        result.DependsOn.Should().Equal(nameof(ValidModel.First), nameof(ValidModel.Last));
    }

    [Theory]
    [InlineData(nameof(ValidModel.First))]
    [InlineData(nameof(ValidModel.AttributeWithoutDependsOn))]
    [InlineData(nameof(ValidModel.EmptyDependsOn))]
    public void MakeRqlPropertyInfo_NoDependenciesDeclared_DependsOnIsNull(string propertyName)
    {
        // Arrange
        var factory = CreateSut();
        var property = typeof(ValidModel).GetProperty(propertyName)!;

        // Act
        var result = factory.MakeRqlPropertyInfo(propertyName, property);

        // Assert
        result.DependsOn.Should().BeNull();
    }

    [Fact]
    public void MakeRqlPropertyInfo_DependsOnInheritedProperty_StoresDependency()
    {
        // Arrange
        var factory = CreateSut();
        var property = typeof(DerivedModel).GetProperty(nameof(DerivedModel.Description))!;

        // Act
        var result = factory.MakeRqlPropertyInfo("description", property);

        // Assert
        result.DependsOn.Should().Equal(nameof(BaseModel.BaseValue));
    }

    [Fact]
    public void MakeRqlPropertyInfo_DependsOnPropertyHiddenWithNewType_DoesNotThrowAndStoresDependency()
    {
        // Arrange — guard that the model really is the ambiguous case: both Code properties are listed
        typeof(HidingModel).Invoking(t => t.GetProperty(nameof(HidingModel.Code)))
            .Should().Throw<AmbiguousMatchException>();

        var factory = CreateSut();
        var property = typeof(HidingModel).GetProperty(nameof(HidingModel.Label))!;

        // Act
        var act = () => factory.MakeRqlPropertyInfo("label", property);

        // Assert
        act.Should().NotThrow().Which.DependsOn.Should().Equal(nameof(HidingModel.Code));
    }

    private class ReferenceModel
    {
        public int Id { get; set; }
    }

    private class InvalidModel
    {
        public string Name { get; set; } = string.Empty;

        [RqlProperty(Mode = RqlPropertyMode.Ignored)]
        public string Hidden { get; set; } = string.Empty;

        public ReferenceModel Reference { get; set; } = new();

        public List<int> Items { get; set; } = new();

        [RqlProperty(Mode = RqlPropertyMode.Ignored, DependsOn = new[] { nameof(Name) })]
        public string IgnoredComputed => Name;

        [RqlProperty(DependsOn = new[] { nameof(SelfDependent) })]
        public string SelfDependent => Name;

        [RqlProperty(DependsOn = new[] { nameof(Name), nameof(SelfLastAmongOthers) })]
        public string SelfLastAmongOthers => Name;

        [RqlProperty(DependsOn = new[] { nameof(SelfFirstAmongOthers), nameof(Name) })]
        public string SelfFirstAmongOthers => Name;

        [RqlProperty(DependsOn = new[] { "selfDependentLower" })]
        public string SelfDependentLower => Name;

        [RqlProperty(DependsOn = new[] { "Missing" })]
        public string DependsOnMissing => Name;

        [RqlProperty(DependsOn = new[] { "" })]
        public string DependsOnEmptyName => Name;

        [RqlProperty(DependsOn = new string[] { null! })]
        public string DependsOnNullName => Name;

        [RqlProperty(DependsOn = new[] { nameof(ValidModel.First) })]
        public string DependsOnOtherTypeProperty => Name;

        [RqlProperty(DependsOn = new[] { nameof(Hidden) })]
        public string DependsOnIgnored => Hidden;

        [RqlProperty(DependsOn = new[] { nameof(Reference) })]
        public int DependsOnReference => Reference.Id;

        [RqlProperty(DependsOn = new[] { nameof(Items) })]
        public int DependsOnCollection => Items.Count;
    }

    private class ValidModel
    {
        public string First { get; set; } = string.Empty;

        public string Last { get; set; } = string.Empty;

        [RqlProperty(DependsOn = new[] { nameof(First), nameof(First), nameof(Last) })]
        public string FullName => First + " " + Last;

        [RqlProperty(IsCore = true)]
        public string AttributeWithoutDependsOn => First;

        [RqlProperty(DependsOn = new string[0])]
        public string EmptyDependsOn => First;
    }

    private class MemberKindModel
    {
        public string PublicField = string.Empty;

        private string PrivateValue { get; set; } = string.Empty;

        public static string StaticValue { get; set; } = string.Empty;

        [RqlProperty(DependsOn = new[] { nameof(PublicField) })]
        public string DependsOnField => PublicField;

        [RqlProperty(DependsOn = new[] { nameof(PrivateValue) })]
        public string DependsOnPrivateProperty => PrivateValue;

        [RqlProperty(DependsOn = new[] { nameof(StaticValue) })]
        public string DependsOnStaticProperty => StaticValue;
    }

    private class BaseModel
    {
        public string BaseValue { get; set; } = string.Empty;
    }

    private class DerivedModel : BaseModel
    {
        [RqlProperty(DependsOn = new[] { nameof(BaseValue) })]
        public string Description => BaseValue;
    }

    private class HidingBase
    {
        public string Code { get; set; } = string.Empty;
    }

    private class HidingModel : HidingBase
    {
        public new int Code { get; set; }

        [RqlProperty(DependsOn = new[] { nameof(Code) })]
        public string Label => Code.ToString();
    }
}
