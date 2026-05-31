using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dict.DTO.Auth;
using Dict.Models;
using Dict.Service;
using Dict.Service.IService;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Dict.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
        private readonly Mock<IJwtService> _mockJwtService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly IMemoryCache _memoryCache; // Dùng đồ thật cho MemoryCache vì Mock cái này rất khổ
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            // 1. MOCK USER MANAGER
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            // 2. MOCK SIGN-IN MANAGER
            var contextAccessorMock = new Mock<IHttpContextAccessor>();
            var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                _mockUserManager.Object, contextAccessorMock.Object, claimsFactoryMock.Object, null, null, null, null);

            // 3. MOCK ROLE MANAGER
            var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
            _mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
                roleStoreMock.Object, null, null, null, null);

            // 4. Các Mock cơ bản khác
            _mockJwtService = new Mock<IJwtService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockConfig = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<AuthService>>();

            // Dùng MemoryCache thật để test OTP
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            _mockConfig.Setup(c => c["FrontendUrl"]).Returns("http://localhost:3000");

            // 5. KHỞI TẠO SERVICE
            _sut = new AuthService(
                _mockUserManager.Object,
                _mockSignInManager.Object,
                _mockRoleManager.Object,
                _mockJwtService.Object,
                _mockEmailService.Object,
                _mockConfig.Object,
                _mockLogger.Object,
                _memoryCache
            );
        }

        [Fact]
        public async Task RegisterAsync_WhenValid_ShouldCreateUser_AndSendEmail()
        {
            // Arrange
            var req = new RegistrationRequestDto
            {
                Username = "testuser",
                Email = "test@abc.com",
                Password = "Password123!"
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(req.Email)).ReturnsAsync((ApplicationUser?)null);
            _mockUserManager.Setup(x => x.FindByNameAsync(req.Username)).ReturnsAsync((ApplicationUser?)null);

            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), req.Password))
                            .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
                            .ReturnsAsync("fake-token");

            // Act
            var result = await _sut.RegisterAsync(req);

            // Assert
            result.Should().Contain("Đăng ký thành công");
            _mockUserManager.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => u.Email == req.Email), req.Password), Times.Once);
            _mockEmailService.Verify(x => x.SendEmailAsync(req.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WhenEmailFails_ShouldDeleteUser_AndThrowException()
        {
            // Arrange: Setup y hệt case thành công nhưng ép EmailService ném lỗi
            var req = new RegistrationRequestDto { Username = "testuser", Email = "test@abc.com", Password = "Password123!" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(req.Email)).ReturnsAsync((ApplicationUser?)null);
            _mockUserManager.Setup(x => x.FindByNameAsync(req.Username)).ReturnsAsync((ApplicationUser?)null);
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), req.Password)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>())).ReturnsAsync("fake-token");

            // Giả lập lỗi gửi mail (ví dụ SMTP sập)
            _mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .ThrowsAsync(new Exception("SMTP Error"));

            // Act & Assert
            Func<Task> act = async () => await _sut.RegisterAsync(req);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Không thể gửi email xác nhận. Vui lòng kiểm tra lại địa chỉ Email hoặc thử lại sau.");

            // QUAN TRỌNG: Đảm bảo user đã bị xóa (rollback logic)
            _mockUserManager.Verify(x => x.DeleteAsync(It.Is<ApplicationUser>(u => u.Email == req.Email)), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WhenEmailNotConfirmed_ShouldThrowException()
        {
            // Arrange
            var req = new LoginRequestDto { Username = "testuser", Password = "123" };
            var user = new ApplicationUser { UserName = "testuser", IsActive = true };

            _mockUserManager.Setup(x => x.FindByNameAsync(req.Username)).ReturnsAsync(user);
            // Báo rằng email chưa xác nhận
            _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(false);

            // Act & Assert
            Func<Task> act = async () => await _sut.LoginAsync(req);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Vui lòng kiểm tra email và xác nhận tài khoản trước khi đăng nhập.");
        }

        [Fact]
        public async Task ForgotPasswordAsync_ShouldGenerateOtp_AndStoreInCache()
        {
            // Arrange
            string email = "forgot@abc.com";
            var user = new ApplicationUser { Email = email };

            _mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token-xyz");

            // Act
            var result = await _sut.ForgotPasswordAsync(email);

            // Assert
            result.Should().Be("Nếu email hợp lệ, mã OTP khôi phục đã được gửi.");

            // Kiểm tra xem Cache đã lưu OTP chưa
            bool hasCache = _memoryCache.TryGetValue($"OTP_{email}", out OtpCacheEntry? cacheEntry);
            hasCache.Should().BeTrue();
            cacheEntry.Should().NotBeNull();
            cacheEntry!.ResetToken.Should().Be("reset-token-xyz");
            cacheEntry.OtpCode.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ResetPasswordAsync_WhenOtpIsCorrect_ShouldResetPassword_AndClearCache()
        {
            // Arrange
            string email = "reset@abc.com";
            string validOtp = "123456";
            string resetToken = "valid-token";
            var user = new ApplicationUser { Email = email };

            // Nạp sẵn OTP vào RAM ảo
            _memoryCache.Set($"OTP_{email}", new OtpCacheEntry { OtpCode = validOtp, ResetToken = resetToken });

            _mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.ResetPasswordAsync(user, resetToken, "NewPass123!"))
                            .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _sut.ResetPasswordAsync(email, validOtp, "NewPass123!", "NewPass123!");

            // Assert
            result.Should().Contain("Đổi mật khẩu thành công");

            // Kiểm tra Cache xem đã bị xóa sạch sẽ OTP chưa
            bool stillHasCache = _memoryCache.TryGetValue($"OTP_{email}", out _);
            stillHasCache.Should().BeFalse();
        }

        [Fact]
        public async Task ResetPasswordAsync_WhenOtpIsIncorrect_ShouldThrowException()
        {
            // Arrange
            string email = "reset@abc.com";
            _memoryCache.Set($"OTP_{email}", new OtpCacheEntry { OtpCode = "111111" }); // Mã đúng là 111111

            // Act & Assert
            Func<Task> act = async () => await _sut.ResetPasswordAsync(email, "999999", "NewPass123!", "NewPass123!");

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Mã OTP không chính xác.");
        }
    }
}