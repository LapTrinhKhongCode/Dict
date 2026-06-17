using Dict.Data;
using Dict.Models;
using Dict.Service;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dict.Tests.UnitTests
{
    public class PlanLimitServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly PlanLimitService _sut;

        public PlanLimitServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);
            _sut = new PlanLimitService(_db);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private ApplicationUser MakeUser(int id, string tier = "FREE", DateTime? expiresAt = null)
        {
            var u = new ApplicationUser { Id = id, PersonalTier = tier, PremiumExpiresAt = expiresAt, AvatarUrl = "" };
            u.UserName = $"user{id}";
            u.NormalizedUserName = $"USER{id}";
            u.Email = $"user{id}@test.com";
            u.NormalizedEmail = $"USER{id}@TEST.COM";
            u.SecurityStamp = Guid.NewGuid().ToString();
            return u;
        }

        private Workspace MakePersonalWorkspace(int id) =>
            new Workspace { Id = id, Name = "WS", Description = "", OwnerType = "PERSONAL" };

        private Workspace MakeOrgWorkspace(int id, Organization org) =>
            new Workspace { Id = id, Name = "WS", Description = "", OwnerType = "ORGANIZATION", OrganizationId = org.Id, Organization = org };

        private Organization MakeOrg(int id, string plan = "FREE") =>
            new Organization { Id = id, Name = "Org", Slug = "org", OwnerId = 999, OrgPlan = plan };

        // ─── CheckFileSizeAsync ───────────────────────────────────────────────

        [Fact]
        public async Task CheckFileSizeAsync_WhenUserNotFound_ReturnsFalse()
        {
            var (allowed, error) = await _sut.CheckFileSizeAsync(999, 1024);
            allowed.Should().BeFalse();
            error.Should().Contain("not found");
        }

        [Fact]
        public async Task CheckFileSizeAsync_FreeUser_UnderLimit_ReturnsAllowed()
        {
            _db.Users.Add(MakeUser(1, "FREE"));
            await _db.SaveChangesAsync();

            var (allowed, error) = await _sut.CheckFileSizeAsync(1, 5 * 1024 * 1024); // 5MB
            allowed.Should().BeTrue();
            error.Should().BeNull();
        }

        [Fact]
        public async Task CheckFileSizeAsync_FreeUser_OverLimit_ReturnsDenied()
        {
            _db.Users.Add(MakeUser(1, "FREE"));
            await _db.SaveChangesAsync();

            var (allowed, error) = await _sut.CheckFileSizeAsync(1, 15 * 1024 * 1024); // 15MB > 10MB
            allowed.Should().BeFalse();
            error.Should().Contain("10MB");
        }

        [Fact]
        public async Task CheckFileSizeAsync_PremiumUser_CanUploadLargeFile()
        {
            _db.Users.Add(MakeUser(1, "PREMIUM", null)); // null = lifetime
            await _db.SaveChangesAsync();

            var (allowed, error) = await _sut.CheckFileSizeAsync(1, 200 * 1024 * 1024); // 200MB
            allowed.Should().BeTrue();
            error.Should().BeNull();
        }

        [Fact]
        public async Task CheckFileSizeAsync_PremiumExpired_TreatedAsFree()
        {
            var expired = MakeUser(1, "PREMIUM", DateTime.UtcNow.AddDays(-1)); // expired yesterday
            _db.Users.Add(expired);
            await _db.SaveChangesAsync();

            var (allowed, error) = await _sut.CheckFileSizeAsync(1, 15 * 1024 * 1024); // 15MB
            allowed.Should().BeFalse();
            error.Should().Contain("10MB");
        }

        [Fact]
        public async Task CheckFileSizeAsync_OrgTeamWorkspace_AllowsLargeFile()
        {
            var user = MakeUser(1, "FREE");
            var org  = MakeOrg(1, OrgPlan.TEAM);
            var ws   = MakeOrgWorkspace(10, org);
            _db.Users.Add(user);
            _db.Organizations.Add(org);
            _db.Workspaces.Add(ws);
            await _db.SaveChangesAsync();

            var (allowed, error) = await _sut.CheckFileSizeAsync(1, 200 * 1024 * 1024, workspaceId: 10);
            allowed.Should().BeTrue();
            error.Should().BeNull();
        }

        [Fact]
        public async Task CheckFileSizeAsync_OrgFreeWorkspace_EnforcesLimit()
        {
            var user = MakeUser(1, "FREE");
            var org  = MakeOrg(1, OrgPlan.FREE);
            var ws   = MakeOrgWorkspace(10, org);
            _db.Users.Add(user);
            _db.Organizations.Add(org);
            _db.Workspaces.Add(ws);
            await _db.SaveChangesAsync();

            var (allowed, error) = await _sut.CheckFileSizeAsync(1, 15 * 1024 * 1024, workspaceId: 10);
            allowed.Should().BeFalse();
            error.Should().Contain("10MB");
        }

        // ─── CheckOcrQuotaAsync ───────────────────────────────────────────────

        [Fact]
        public async Task CheckOcrQuotaAsync_FreeUser_UnderQuota_ReturnsAllowed()
        {
            _db.Users.Add(MakeUser(1, "FREE"));
            await _db.SaveChangesAsync();

            var (allowed, _) = await _sut.CheckOcrQuotaAsync(1);
            allowed.Should().BeTrue();
        }

        [Fact]
        public async Task CheckOcrQuotaAsync_FreeUser_AtLimit_ReturnsDenied()
        {
            _db.Users.Add(MakeUser(1, "FREE"));
            var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            for (int i = 0; i < PlanLimitService.FREE_OCR_PER_MONTH; i++)
                _db.OcrJobs.Add(new OcrJob { UserId = 1, Status = "completed", DetectedText = "", CreatedAt = start.AddHours(i) });
            await _db.SaveChangesAsync();

            var (allowed, error) = await _sut.CheckOcrQuotaAsync(1);
            allowed.Should().BeFalse();
            error.Should().Contain($"{PlanLimitService.FREE_OCR_PER_MONTH}");
        }

        [Fact]
        public async Task CheckOcrQuotaAsync_PremiumUser_NeverBlocked()
        {
            _db.Users.Add(MakeUser(1, "PREMIUM", null));
            var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            for (int i = 0; i < 100; i++)
                _db.OcrJobs.Add(new OcrJob { UserId = 1, Status = "completed", DetectedText = "", CreatedAt = start.AddHours(i) });
            await _db.SaveChangesAsync();

            var (allowed, _) = await _sut.CheckOcrQuotaAsync(1);
            allowed.Should().BeTrue();
        }

        [Fact]
        public async Task CheckOcrQuotaAsync_OcrJobsFromLastMonth_NotCounted()
        {
            _db.Users.Add(MakeUser(1, "FREE"));
            var lastMonth = DateTime.UtcNow.AddMonths(-1);
            for (int i = 0; i < PlanLimitService.FREE_OCR_PER_MONTH; i++)
                _db.OcrJobs.Add(new OcrJob { UserId = 1, Status = "completed", DetectedText = "", CreatedAt = lastMonth.AddHours(i) });
            await _db.SaveChangesAsync();

            var (allowed, _) = await _sut.CheckOcrQuotaAsync(1);
            allowed.Should().BeTrue(); // last month jobs don't count
        }

        [Fact]
        public async Task CheckOcrQuotaAsync_OrgTeam_AlwaysAllowed()
        {
            var user = MakeUser(1, "FREE");
            var org  = MakeOrg(1, OrgPlan.TEAM);
            var ws   = MakeOrgWorkspace(10, org);
            _db.Users.Add(user);
            _db.Organizations.Add(org);
            _db.Workspaces.Add(ws);
            var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            for (int i = 0; i < 100; i++)
                _db.OcrJobs.Add(new OcrJob { UserId = 1, Status = "completed", DetectedText = "", CreatedAt = start.AddHours(i) });
            await _db.SaveChangesAsync();

            var (allowed, _) = await _sut.CheckOcrQuotaAsync(1, workspaceId: 10);
            allowed.Should().BeTrue();
        }

        // ─── GetUsageInfoAsync ────────────────────────────────────────────────

        [Fact]
        public async Task GetUsageInfoAsync_FreeUser_ReturnsCorrectLimits()
        {
            _db.Users.Add(MakeUser(1, "FREE"));
            await _db.SaveChangesAsync();

            var info = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(
                System.Text.Json.JsonSerializer.Serialize(await _sut.GetUsageInfoAsync(1)));
            info.GetProperty("isPremium").GetBoolean().Should().BeFalse();
            info.GetProperty("ocr").GetProperty("unlimited").GetBoolean().Should().BeFalse();
            info.GetProperty("ocr").GetProperty("limit").GetInt32().Should().Be(PlanLimitService.FREE_OCR_PER_MONTH);
            info.GetProperty("fileSize").GetProperty("maxMb").GetInt64().Should().Be(10);
        }

        [Fact]
        public async Task GetUsageInfoAsync_PremiumUser_ReturnsUnlimitedOcr()
        {
            _db.Users.Add(MakeUser(1, "PREMIUM", null));
            await _db.SaveChangesAsync();

            var info = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(
                System.Text.Json.JsonSerializer.Serialize(await _sut.GetUsageInfoAsync(1)));
            info.GetProperty("isPremium").GetBoolean().Should().BeTrue();
            info.GetProperty("ocr").GetProperty("unlimited").GetBoolean().Should().BeTrue();
            info.GetProperty("fileSize").GetProperty("maxMb").GetInt64().Should().Be(500);
        }
    }
}
