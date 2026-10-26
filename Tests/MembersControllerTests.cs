using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ease_intro_api.Controllers;
using ease_intro_api.Data;
using ease_intro_api.Data.Repository;
using ease_intro_api.DTOs.Member;
using ease_intro_api.Models;
using ease_intro_api.Core.Services;
using ease_intro_api.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests;

public class MembersControllerTests
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
        mock.Setup(x => x.IsAuthenticated).Returns(true);
        return mock;
    }

    private User CreateOwnerUser()
    {
        return new User { Id = 1, UserEmail = "owner@test.com", PasswordHash = "hash", PublicName = "Owner" };
    }

    [Fact]
    public async Task CreateMember_Success_WithoutCompanion()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = GetDbContext(dbName);

        context.Users.Add(CreateOwnerUser());

        var status = new MeetStatus { Id = 1, Title = "Встреча запланирована" };
        context.MeetStatus.Add(status);

        var meetUid = Guid.NewGuid();
        var meet = new Meet
        {
            Uid = meetUid,
            Title = "Test Meet",
            Date = DateTime.UtcNow.AddDays(1),
            Location = "Test Room",
            StatusId = 1,
            Status = status,
            LimitMembers = 10,
            AllowedPlusOne = false,
            OwnerId = 1
        };
        context.Meets.Add(meet);
        await context.SaveChangesAsync();

        var userContextMock = GetUserContextMock();
        var meetRepo = new MeetRepository(context, userContextMock.Object);
        var memberRepo = new MemberRepository(context);
        var memberService = new MemberService(memberRepo, meetRepo, userContextMock.Object);
        var loggerMock = new Mock<ILogger<MembersController>>();
        var configMock = new Mock<IConfiguration>();

        var controller = new MembersController(context, loggerMock.Object, memberRepo, memberService, meetRepo, configMock.Object, userContextMock.Object);

        var dto = new CreateMemberDto
        {
            Name = "John Doe",
            Companion = null,
            Contact = "john@example.com",
            MeetUid = meetUid
        };

        var result = await controller.CreateMember(dto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
        Assert.Contains("Это ваш уникальный идентификатор:", okResult.Value.ToString());

        var savedMember = await context.Member.FirstOrDefaultAsync(m => m.Contact == "john@example.com");
        Assert.NotNull(savedMember);
        Assert.Equal(string.Empty, savedMember.Companion);
    }

    [Fact]
    public async Task CreateMember_DuplicateContact_Returns409Conflict()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = GetDbContext(dbName);

        context.Users.Add(CreateOwnerUser());

        var meetUid = Guid.NewGuid();
        var existingMember = new Member
        {
            Name = "Existing User",
            Companion = "",
            Contact = "duplicate@example.com",
            MeetGuid = meetUid,
            QrCode = "QR123"
        };
        context.Member.Add(existingMember);
        await context.SaveChangesAsync();

        var userContextMock = GetUserContextMock();
        var meetRepo = new MeetRepository(context, userContextMock.Object);
        var memberRepo = new MemberRepository(context);
        var memberService = new MemberService(memberRepo, meetRepo, userContextMock.Object);
        var loggerMock = new Mock<ILogger<MembersController>>();
        var configMock = new Mock<IConfiguration>();

        var controller = new MembersController(context, loggerMock.Object, memberRepo, memberService, meetRepo, configMock.Object, userContextMock.Object);

        var dto = new CreateMemberDto
        {
            Name = "New User",
            Contact = "duplicate@example.com",
            MeetUid = meetUid
        };

        var result = await controller.CreateMember(dto);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("Контакт с такой почтой уже зарегистрирован.", conflictResult.Value);
    }

    [Fact]
    public async Task CreateMember_InvalidStatus_Returns400BadRequest()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = GetDbContext(dbName);

        context.Users.Add(CreateOwnerUser());

        var status = new MeetStatus { Id = 2, Title = "Встреча в процессе" };
        context.MeetStatus.Add(status);

        var meetUid = Guid.NewGuid();
        var meet = new Meet
        {
            Uid = meetUid,
            Title = "Test Meet",
            Date = DateTime.UtcNow.AddDays(1),
            Location = "Test Room",
            StatusId = 2,
            Status = status,
            LimitMembers = 10,
            AllowedPlusOne = true,
            OwnerId = 1
        };
        context.Meets.Add(meet);
        await context.SaveChangesAsync();

        var userContextMock = GetUserContextMock();
        var meetRepo = new MeetRepository(context, userContextMock.Object);
        var memberRepo = new MemberRepository(context);
        var memberService = new MemberService(memberRepo, meetRepo, userContextMock.Object);
        var loggerMock = new Mock<ILogger<MembersController>>();
        var configMock = new Mock<IConfiguration>();

        var controller = new MembersController(context, loggerMock.Object, memberRepo, memberService, meetRepo, configMock.Object, userContextMock.Object);

        var dto = new CreateMemberDto
        {
            Name = "John Doe",
            Contact = "john@example.com",
            MeetUid = meetUid
        };

        var result = await controller.CreateMember(dto);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Регистрация на данную встречу недоступна.", badRequestResult.Value);
    }

    [Fact]
    public async Task CreateMember_ExpiredDate_Returns400BadRequest()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = GetDbContext(dbName);

        context.Users.Add(CreateOwnerUser());

        var status = new MeetStatus { Id = 1, Title = "Встреча запланирована" };
        context.MeetStatus.Add(status);

        var meetUid = Guid.NewGuid();
        var meet = new Meet
        {
            Uid = meetUid,
            Title = "Test Meet",
            Date = DateTime.UtcNow.AddDays(-1),
            Location = "Test Room",
            StatusId = 1,
            Status = status,
            LimitMembers = 10,
            AllowedPlusOne = true,
            OwnerId = 1
        };
        context.Meets.Add(meet);
        await context.SaveChangesAsync();

        var userContextMock = GetUserContextMock();
        var meetRepo = new MeetRepository(context, userContextMock.Object);
        var memberRepo = new MemberRepository(context);
        var memberService = new MemberService(memberRepo, meetRepo, userContextMock.Object);
        var loggerMock = new Mock<ILogger<MembersController>>();
        var configMock = new Mock<IConfiguration>();

        var controller = new MembersController(context, loggerMock.Object, memberRepo, memberService, meetRepo, configMock.Object, userContextMock.Object);

        var dto = new CreateMemberDto
        {
            Name = "John Doe",
            Contact = "john@example.com",
            MeetUid = meetUid
        };

        var result = await controller.CreateMember(dto);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Срок регистрации на встречу истек.", badRequestResult.Value);
    }

    [Fact]
    public async Task CreateMember_CompanionNotAllowed_Returns400BadRequest()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = GetDbContext(dbName);

        context.Users.Add(CreateOwnerUser());

        var status = new MeetStatus { Id = 1, Title = "Встреча запланирована" };
        context.MeetStatus.Add(status);

        var meetUid = Guid.NewGuid();
        var meet = new Meet
        {
            Uid = meetUid,
            Title = "Test Meet",
            Date = DateTime.UtcNow.AddDays(1),
            Location = "Test Room",
            StatusId = 1,
            Status = status,
            LimitMembers = 10,
            AllowedPlusOne = false,
            OwnerId = 1
        };
        context.Meets.Add(meet);
        await context.SaveChangesAsync();

        var userContextMock = GetUserContextMock();
        var meetRepo = new MeetRepository(context, userContextMock.Object);
        var memberRepo = new MemberRepository(context);
        var memberService = new MemberService(memberRepo, meetRepo, userContextMock.Object);
        var loggerMock = new Mock<ILogger<MembersController>>();
        var configMock = new Mock<IConfiguration>();

        var controller = new MembersController(context, loggerMock.Object, memberRepo, memberService, meetRepo, configMock.Object, userContextMock.Object);

        var dto = new CreateMemberDto
        {
            Name = "John Doe",
            Companion = "Jane Doe",
            Contact = "john@example.com",
            MeetUid = meetUid
        };

        var result = await controller.CreateMember(dto);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("На данную встречу нельзя регистрироваться со спутником (+1).", badRequestResult.Value);
    }

    [Fact]
    public async Task CreateMember_LimitReached_Returns400BadRequest()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = GetDbContext(dbName);

        context.Users.Add(CreateOwnerUser());

        var status = new MeetStatus { Id = 1, Title = "Встреча запланирована" };
        context.MeetStatus.Add(status);

        var meetUid = Guid.NewGuid();
        var meet = new Meet
        {
            Uid = meetUid,
            Title = "Test Meet",
            Date = DateTime.UtcNow.AddDays(1),
            Location = "Test Room",
            StatusId = 1,
            Status = status,
            LimitMembers = 1,
            AllowedPlusOne = true,
            OwnerId = 1
        };
        context.Meets.Add(meet);

        var existingMember = new Member
        {
            Name = "First Member",
            Companion = "",
            Contact = "first@example.com",
            MeetGuid = meetUid,
            QrCode = "QR111"
        };
        context.Member.Add(existingMember);
        await context.SaveChangesAsync();

        var userContextMock = GetUserContextMock();
        var meetRepo = new MeetRepository(context, userContextMock.Object);
        var memberRepo = new MemberRepository(context);
        var memberService = new MemberService(memberRepo, meetRepo, userContextMock.Object);
        var loggerMock = new Mock<ILogger<MembersController>>();
        var configMock = new Mock<IConfiguration>();

        var controller = new MembersController(context, loggerMock.Object, memberRepo, memberService, meetRepo, configMock.Object, userContextMock.Object);

        var dto = new CreateMemberDto
        {
            Name = "Second Member",
            Contact = "second@example.com",
            MeetUid = meetUid
        };

        var result = await controller.CreateMember(dto);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Достигнут лимит участников встречи.", badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateMember_PartialUpdate_NameOnly_Returns204NoContentAndDoesNotChangeRoleOrContact()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = GetDbContext(dbName);

        context.Users.Add(CreateOwnerUser());

        var status = new MeetStatus { Id = 1, Title = "Встреча запланирована" };
        context.MeetStatus.Add(status);

        var meetUid = Guid.NewGuid();
        var meet = new Meet
        {
            Uid = meetUid,
            Title = "Test Meet",
            Date = DateTime.UtcNow.AddDays(1),
            Location = "Test Room",
            StatusId = 1,
            Status = status,
            LimitMembers = 10,
            AllowedPlusOne = true,
            OwnerId = 1
        };
        context.Meets.Add(meet);

        var member = new Member
        {
            Id = 1,
            Name = "Старое Имя",
            Companion = "Companion",
            Contact = "old@example.com",
            Role = Member.MemberRole.Admin,
            MeetGuid = meetUid,
            QrCode = "QR123"
        };
        context.Member.Add(member);
        await context.SaveChangesAsync();

        var userContextMock = GetUserContextMock(1);
        var meetRepo = new MeetRepository(context, userContextMock.Object);
        var memberRepo = new MemberRepository(context);
        var memberService = new MemberService(memberRepo, meetRepo, userContextMock.Object);
        var loggerMock = new Mock<ILogger<MembersController>>();
        var configMock = new Mock<IConfiguration>();

        var controller = new MembersController(context, loggerMock.Object, memberRepo, memberService, meetRepo, configMock.Object, userContextMock.Object);

        var dto = new UpdateMemberDto
        {
            Name = "Новое Имя"
        };

        var result = await controller.UpdateMember(1, dto);

        Assert.IsType<NoContentResult>(result);

        var updatedMember = await context.Member.FindAsync(1);
        Assert.NotNull(updatedMember);
        Assert.Equal("Новое Имя", updatedMember.Name);
        Assert.Equal("old@example.com", updatedMember.Contact);
        Assert.Equal(Member.MemberRole.Admin, updatedMember.Role);
        Assert.Equal("Companion", updatedMember.Companion);
    }

    [Fact]
    public async Task UpdateMember_DuplicateContact_Returns409Conflict()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = GetDbContext(dbName);

        context.Users.Add(CreateOwnerUser());

        var status = new MeetStatus { Id = 1, Title = "Встреча запланирована" };
        context.MeetStatus.Add(status);

        var meetUid = Guid.NewGuid();
        var meet = new Meet
        {
            Uid = meetUid,
            Title = "Test Meet",
            Date = DateTime.UtcNow.AddDays(1),
            Location = "Test Room",
            StatusId = 1,
            Status = status,
            LimitMembers = 10,
            AllowedPlusOne = true,
            OwnerId = 1
        };
        context.Meets.Add(meet);

        var member1 = new Member
        {
            Id = 1,
            Name = "User 1",
            Contact = "user1@example.com",
            MeetGuid = meetUid,
            QrCode = "QR111"
        };
        var member2 = new Member
        {
            Id = 2,
            Name = "User 2",
            Contact = "user2@example.com",
            MeetGuid = meetUid,
            QrCode = "QR222"
        };
        context.Member.AddRange(member1, member2);
        await context.SaveChangesAsync();

        var userContextMock = GetUserContextMock(1);
        var meetRepo = new MeetRepository(context, userContextMock.Object);
        var memberRepo = new MemberRepository(context);
        var memberService = new MemberService(memberRepo, meetRepo, userContextMock.Object);
        var loggerMock = new Mock<ILogger<MembersController>>();
        var configMock = new Mock<IConfiguration>();

        var controller = new MembersController(context, loggerMock.Object, memberRepo, memberService, meetRepo, configMock.Object, userContextMock.Object);

        var dto = new UpdateMemberDto
        {
            Contact = "user2@example.com"
        };

        var result = await controller.UpdateMember(1, dto);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Контакт с такой почтой уже зарегистрирован.", conflictResult.Value);
    }

    [Fact]
    public async Task UpdateMember_CompanionNotAllowed_Returns400BadRequest()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = GetDbContext(dbName);

        context.Users.Add(CreateOwnerUser());

        var status = new MeetStatus { Id = 1, Title = "Встреча запланирована" };
        context.MeetStatus.Add(status);

        var meetUid = Guid.NewGuid();
        var meet = new Meet
        {
            Uid = meetUid,
            Title = "Test Meet",
            Date = DateTime.UtcNow.AddDays(1),
            Location = "Test Room",
            StatusId = 1,
            Status = status,
            LimitMembers = 10,
            AllowedPlusOne = false,
            OwnerId = 1
        };
        context.Meets.Add(meet);

        var member = new Member
        {
            Id = 1,
            Name = "John Doe",
            Contact = "john@example.com",
            MeetGuid = meetUid,
            QrCode = "QR111"
        };
        context.Member.Add(member);
        await context.SaveChangesAsync();

        var userContextMock = GetUserContextMock(1);
        var meetRepo = new MeetRepository(context, userContextMock.Object);
        var memberRepo = new MemberRepository(context);
        var memberService = new MemberService(memberRepo, meetRepo, userContextMock.Object);
        var loggerMock = new Mock<ILogger<MembersController>>();
        var configMock = new Mock<IConfiguration>();

        var controller = new MembersController(context, loggerMock.Object, memberRepo, memberService, meetRepo, configMock.Object, userContextMock.Object);

        var dto = new UpdateMemberDto
        {
            Companion = "Jane Doe"
        };

        var result = await controller.UpdateMember(1, dto);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("На данную встречу нельзя добавлять спутника (+1).", badRequestResult.Value);
    }
}
