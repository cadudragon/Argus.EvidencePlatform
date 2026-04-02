using Argus.EvidencePlatform.Domain.Cases;
using FluentAssertions;
using NetArchTest.Rules;

namespace Argus.EvidencePlatform.ArchTests;

public sealed class DomainDependencyRulesTests
{
    [Fact]
    public void Domain_should_not_depend_on_infrastructure()
    {
        var result = Types.InAssembly(typeof(Case).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Argus.EvidencePlatform.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
