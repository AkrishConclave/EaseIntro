using System.Security.Claims;
using System.Text.Json;
using ease_intro_api.Controllers;
using ease_intro_api.Core.Services;
using ease_intro_api.Core.Services.Interfaces;
using ease_intro_api.Data;
using ease_intro_api.Data.Repository;
using ease_intro_api.DTOs.Member;
using ease_intro_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MySqlConnector;

namespace Tests;

public sealed class MySqlFactAttribute : FactAttribute
{
    public MySqlFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EASEINTRO_TEST_MYSQL")))
            Skip = "Set EASEINTRO_TEST_MYSQL to run against a disposable MySQL database.";
    }
}

// Every test creates and drops its own database; never modifies the configured database.
public class MySqlIntegrationTests : IAsyncLifetime
{
    private string? connection;
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseMySql(connection!, new MySqlServerVersion(new Version(8, 4, 0))).Options);

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("EASEINTRO_TEST_MYSQL");
        if (string.IsNullOrEmpty(configured)) return;
        var builder = new MySqlConnectionStringBuilder(configured) { Database = "easeintro_test_" + Guid.NewGuid().ToString("N")[..20] };
        connection = builder.ConnectionString;
        await using var db = Db();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (connection == null) return;
        await using var db = Db();
        await db.Database.EnsureDeletedAsync();
    }

    private MembersController Controller(ApplicationDbContext db, int userId = 1, string email = "owner@example.com")
    {
        var user = new Mock<IUserContextService>();
        user.SetupGet(x => x.UserId).Returns(userId);
        user.SetupGet(x => x.IsAuthenticated).Returns(true);
        user.SetupGet(x => x.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, email) }, "test")));
        var meets = new MeetRepository(db, user.Object);
        var members = new MemberRepository(db);
        return new MembersController(db, NullLogger<MembersController>.Instance, members,
            new MemberService(members, meets, user.Object), meets,
            new ConfigurationBuilder().Build(), user.Object);
    }

    private async Task<Guid> Seed(int limit = 1, DateTime? date = null, bool member = false)
    {
        await using var db = Db();
        var owner = new User { Id = 1, UserEmail = "owner@example.com", PasswordHash = "hash" };
        var meet = new Meet { Uid = Guid.NewGuid(), Title = "Test", Location = "Room", Owner = owner,
            StatusId = 1, LimitMembers = limit, Date = date ?? DateTime.UtcNow.AddDays(1) };
        db.Meets.Add(meet);
        if (member) db.Member.Add(new Member { Name = "Victim", Contact = "victim@example.com", Meet = meet, QrCode = "ticket" });
        await db.SaveChangesAsync();
        return meet.Uid;
    }

    [MySqlFact]
    public async Task ConcurrentRegistrations_OnlyOneGetsLastPlace()
    {
        var uid = await Seed();
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(async i =>
        {
            await using var db = Db();
            return (await Controller(db).CreateMember(new CreateMemberDto { MeetUid = uid, Name = "Guest", Contact = $"guest{i}@example.com" })).Result;
        }));
        Assert.Single(results.OfType<OkObjectResult>());
        Assert.Equal(7, results.OfType<BadRequestObjectResult>().Count());
        await using var check = Db();
        Assert.Equal(1, await check.Member.CountAsync());
    }

    [MySqlFact]
    public async Task RegistrationWaitsForMeetUpdate_AndSeesCommittedRules()
    {
        var uid = await Seed(limit: 10);
        await using var writer = Db();
        var user = new Mock<IUserContextService>();
        var repository = new MeetRepository(writer, user.Object);
        using var transaction = await repository.LockMeetAsync(uid);
        var meet = await writer.Meets.SingleAsync();
        meet.StatusId = 4;
        await writer.SaveChangesAsync();

        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var registration = Task.Run(async () =>
        {
            await using var reader = Db();
            started.SetResult();
            return await Controller(reader).CreateMember(new CreateMemberDto
                { MeetUid = uid, Name = "Guest", Contact = "waiting@example.com" });
        });
        await started.Task;
        await transaction!.CommitAsync();
        var result = await registration;
        Assert.IsType<BadRequestObjectResult>(result.Result);
        await using var check = Db();
        Assert.Equal(0, await check.Member.CountAsync());
    }

    [MySqlFact]
    public async Task ConcurrentDuplicateRegistration_ReturnsConflictAndOneRow()
    {
        var uid = await Seed(limit: 0);
        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(async i =>
        {
            await using var db = Db();
            return (await Controller(db).CreateMember(new CreateMemberDto { MeetUid = uid, Name = "Guest", Contact = "same@example.com" })).Result;
        }));
        Assert.Single(results.OfType<OkObjectResult>());
        Assert.Equal(3, results.OfType<ConflictObjectResult>().Count());
    }

    [MySqlFact]
    public async Task EmailClaim_DoesNotGrantAccessToAnotherOwnersTickets()
    {
        await Seed(member: true);
        await using var db = Db();
        var result = Assert.IsType<OkObjectResult>(await Controller(db, 999, "victim@example.com").GetMemberMeets("victim@example.com"));
        Assert.Equal("[]", JsonSerializer.Serialize(result.Value));
        var own = Assert.IsType<OkObjectResult>(await Controller(db).GetMemberMeets("victim@example.com"));
        Assert.Contains("ticket", JsonSerializer.Serialize(own.Value));
    }

    [MySqlFact]
    public async Task ConcurrentCheckIn_HasOneWinnerAndPreservesFirstTimestamp()
    {
        await Seed(date: DateTime.UtcNow, member: true);
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(async i =>
        {
            await using var db = Db();
            var repo = new MemberRepository(db);
            var member = (await repo.GetMemberByQrCodeOrNullAsync("ticket"))!;
            return await repo.CheckInMemberAsync(member);
        }));
        Assert.Single(results, x => x);
        await using var check = Db();
        var repo = new MemberRepository(check);
        var saved = (await repo.GetMemberByQrCodeOrNullAsync("ticket"))!;
        var original = saved.CheckedInAt;
        Assert.NotNull(original);
        Assert.False(await repo.CheckInMemberAsync(saved));
        Assert.Equal(original, saved.CheckedInAt);
    }

    [MySqlFact]
    public async Task CheckInOutsideWindow_IsInvalidAndDoesNotChangeTicket()
    {
        await Seed(date: DateTime.UtcNow.AddDays(2), member: true);
        await using var db = Db();
        var result = Assert.IsType<OkObjectResult>(await Controller(db).CheckInMemberQr("ticket"));
        var json = JsonSerializer.SerializeToElement(result.Value);
        Assert.False(json.GetProperty("qrValid").GetBoolean());
        Assert.False(json.GetProperty("isCheckedIn").GetBoolean());
        Assert.False((await db.Member.SingleAsync()).IsCheckedIn);
        Assert.IsType<ForbidResult>(await Controller(db, 999).CheckInMemberQr("ticket"));
    }

    [MySqlFact]
    public async Task DatabaseRejectsDuplicateEmail_AndUnrelatedErrorsAreNotDuplicates()
    {
        await Seed();
        await using var db = Db();
        db.Users.Add(new User { UserEmail = "owner@example.com", PasswordHash = "hash" });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.True(ease_intro_api.Helpers.DatabaseErrors.IsDuplicateKey(error));
        Assert.False(ease_intro_api.Helpers.DatabaseErrors.IsDuplicateKey(new DbUpdateException("Other failure")));
    }
}
