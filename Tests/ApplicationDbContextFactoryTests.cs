using ease_intro_api.Data;
using Xunit;

namespace Tests;

public class ApplicationDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_ReturnsValidDbContextInstance()
    {
        var factory = new ApplicationDbContextFactory();
        using var dbContext = factory.CreateDbContext(Array.Empty<string>());

        Assert.NotNull(dbContext);
    }
}
