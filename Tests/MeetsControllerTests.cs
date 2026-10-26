using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using ease_intro_api.Controllers;
using ease_intro_api.Data;
using ease_intro_api.Data.Repository;
using ease_intro_api.DTOs.Meet;
using ease_intro_api.DTOs.Member;
using ease_intro_api.Models;
using ease_intro_api.Core.Services;
using ease_intro_api.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests;

public class MeetsControllerTests
{
    private ApplicationDbContext GetDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new ApplicationDbContext(options);
    }

    private Mock<IUserContextService> GetUserContextMock(int userId = 1)
    {
        var mock = new Mock<IUserContextService>();
        mock.Setup(x => x.UserId).Returns(userId);
        return mock;
    }

    private User CreateOwnerUser()
    {
        return new User { Id = 1, UserEmail = "owner@test.com", PasswordHash = "hash", PublicName = "Owner" };
    }

    [Fact]
    public async Task CreateMeet_StatusId5_NullMembers_Success()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = GetDbContext(dbName);

        context.Users.Add(CreateOwnerUser());
        context.MeetStatus.Add(new MeetStatus { Id = 5, Title = "Открыто для регистрации" });
        await context.SaveChangesAsync();

        var userContextMock = GetUserContextMock();
        var meetRepo = new MeetRepository(context, userContextMock.Object);
        var memberRepo = new MemberRepository(context);
        var meetService = new MeetService(meetRepo, userContextMock.Object);
        var loggerMock = new Mock<ILogger<MeetsController>>();

        var controller = new MeetsController(context, loggerMock.Object, meetService, meetRepo, memberRepo);

        var dto = new CreateMeetDto
        {
            Title = "Open Meet",
            Date = DateTime.UtcNow.AddDays(1),
            Location = "Hall 1",
            StatusId = 5,
            LimitMembers = 50,
            AllowedPlusOne = true,
            Members = null
        };

        var result = await controller.CreateMeet(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var createdMeet = Assert.IsType<ResponseMeetDto>(createdResult.Value);
        Assert.Equal("Open Meet", createdMeet.Title);
        Assert.Equal("Открыто для регистрации", createdMeet.Status!.Title);
    }

    [Fact]
    public async Task CreateMeet_DuplicateMemberContacts_Returns400BadRequest()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = GetDbContext(dbName);

        context.Users.Add(CreateOwnerUser());
        context.MeetStatus.Add(new MeetStatus { Id = 1, Title = "Запланировано" });
        await context.SaveChangesAsync();

        var userContextMock = GetUserContextMock();
        var meetRepo = new MeetRepository(context, userContextMock.Object);
        var memberRepo = new MemberRepository(context);
        var meetService = new MeetService(meetRepo, userContextMock.Object);
        var loggerMock = new Mock<ILogger<MeetsController>>();

        var controller = new MeetsController(context, loggerMock.Object, meetService, meetRepo, memberRepo);

        var dto = new CreateMeetDto
        {
            Title = "Meet with duplicates",
            Date = DateTime.UtcNow.AddDays(1),
            Location = "Hall 1",
            StatusId = 1,
            LimitMembers = 10,
            AllowedPlusOne = true,
            Members = new List<CreateMemberWithMeetDto>
            {
                new CreateMemberWithMeetDto { Name = "User 1", Contact = "duplicate@example.com" },
                new CreateMemberWithMeetDto { Name = "User 2", Contact = "DUPLICATE@example.com" }
            }
        };

        var result = await controller.CreateMeet(dto);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("В списке участников присутствуют дублирующиеся контакты.", badRequestResult.Value);
    }

    [Fact]
    public async Task CreateMeet_AllowedPlusOneFalse_WithCompanion_Returns400BadRequest()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = GetDbContext(dbName);

        context.Users.Add(CreateOwnerUser());
        context.MeetStatus.Add(new MeetStatus { Id = 1, Title = "Запланировано" });
        await context.SaveChangesAsync();

        var userContextMock = GetUserContextMock();
        var meetRepo = new MeetRepository(context, userContextMock.Object);
        var memberRepo = new MemberRepository(context);
        var meetService = new MeetService(meetRepo, userContextMock.Object);
        var loggerMock = new Mock<ILogger<MeetsController>>();

        var controller = new MeetsController(context, loggerMock.Object, meetService, meetRepo, memberRepo);

        var dto = new CreateMeetDto
        {
            Title = "Meet no +1",
            Date = DateTime.UtcNow.AddDays(1),
            Location = "Hall 1",
            StatusId = 1,
            LimitMembers = 10,
            AllowedPlusOne = false,
            Members = new List<CreateMemberWithMeetDto>
            {
                new CreateMemberWithMeetDto { Name = "User 1", Contact = "user1@example.com", Companion = "Guest Companion" }
            }
        };

        var result = await controller.CreateMeet(dto);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("На данную встречу нельзя регистрироваться со спутником (+1).", badRequestResult.Value);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(6, false)]
    public void CreateMeetDto_StatusIdValidation(int statusId, bool isValid)
    {
        var dto = new CreateMeetDto
        {
            Title = "Test",
            Date = DateTime.UtcNow.AddDays(1),
            Location = "Test Location",
            StatusId = statusId
        };

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(dto);
        var result = Validator.TryValidateObject(dto, validationContext, validationResults, true);

        Assert.Equal(isValid, result);
    }
}
