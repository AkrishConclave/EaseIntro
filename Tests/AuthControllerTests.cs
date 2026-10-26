using System;
using System.Threading;
using System.Threading.Tasks;
using ease_intro_api.Controllers;
using ease_intro_api.Data;
using ease_intro_api.DTOs.User;
using ease_intro_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Tests;

public class AuthControllerTests
{
    private class ThrowingDbContext : ApplicationDbContext
    {
        public ThrowingDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new DbUpdateException("Duplicate entry for IX_Users_UserEmail", new Exception("Inner exception"));
        }
    }

    private ApplicationDbContext GetDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new ApplicationDbContext(options);
    }

    private ThrowingDbContext GetThrowingDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new ThrowingDbContext(options);
    }

    private Mock<IConfiguration> GetConfigMock()
    {
        var mock = new Mock<IConfiguration>();
        mock.Setup(c => c["Jwt:Key"]).Returns("SuperSecretJwtKeyWith32BytesLength!");
        mock.Setup(c => c["Jwt:Issuer"]).Returns("https://localhost:5001");
        mock.Setup(c => c["Jwt:Audience"]).Returns("https://localhost:5001");
        return mock;
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400BadRequest()
    {
        using var context = GetDbContext(Guid.NewGuid().ToString());
        var configMock = GetConfigMock();
        var controller = new AuthController(configMock.Object, context);

        var dto = new RegisterUserDto
        {
            UserEmail = "test@example.com",
            Password = "123",
            PublicName = "Test",
            PublicContact = "test@example.com"
        };

        var result = await controller.Register(dto);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Пароль должен содержать минимум 6 символов.", badRequestResult.Value);
    }

    [Fact]
    public async Task Register_ExistingUserEmailInDatabase_Returns400BadRequest()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = GetDbContext(dbName);
        context.Users.Add(new User
        {
            UserEmail = "existing@example.com",
            PasswordHash = "hash",
            PublicName = "Existing",
            PublicContact = "existing@example.com"
        });
        await context.SaveChangesAsync();

        var configMock = GetConfigMock();
        var controller = new AuthController(configMock.Object, context);

        var dto = new RegisterUserDto
        {
            UserEmail = "existing@example.com",
            Password = "password123",
            PublicName = "New User",
            PublicContact = "new@example.com"
        };

        var result = await controller.Register(dto);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Такой email уже зарегистрирован.", badRequestResult.Value);
    }

    [Fact]
    public async Task Register_DbUpdateException_Returns409Conflict()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = GetThrowingDbContext(dbName);
        var configMock = GetConfigMock();
        var controller = new AuthController(configMock.Object, context);

        var dto = new RegisterUserDto
        {
            UserEmail = "concurrent@example.com",
            Password = "password123",
            PublicName = "Concurrent User",
            PublicContact = "concurrent@example.com"
        };

        var result = await controller.Register(dto);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Такой email уже зарегистрирован.", conflictResult.Value);
    }

    [Fact]
    public async Task Register_Success_Returns200Ok()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = GetDbContext(dbName);
        var configMock = GetConfigMock();
        var controller = new AuthController(configMock.Object, context);

        var dto = new RegisterUserDto
        {
            UserEmail = "newuser@example.com",
            Password = "password123",
            PublicName = "New User",
            PublicContact = "newuser@example.com"
        };

        var result = await controller.Register(dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Пользователь успешно зарегистрирован.", okResult.Value);

        var userInDb = await context.Users.FirstOrDefaultAsync(u => u.UserEmail == "newuser@example.com");
        Assert.NotNull(userInDb);
    }
}
