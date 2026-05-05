//using Dict.Data;
//using Dict.Models;
//using Dict.Service; // Namespace của UserService
//using Dict.Service.IService;
//using Dict.DTO.User; // Namespace của các DTOs
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration; // Cần cho IConfiguration
//using Microsoft.AspNetCore.Http; // Cần cho IFormFile
//using Microsoft.AspNetCore.Identity; // <-- THÊM
//using Moq;
//using Xunit;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Collections.Generic;
//using System;
//using System.IO; // Cần cho MemoryStream

//namespace Dict.Tests.IntegrationTests.Database
//{
//    // Không cần IDisposable vì chúng ta không dùng DB transaction thật nữa
//    public class UserServiceTests
//    {
//        // 1. DỊCH VỤ CẦN TEST (SUT - System Under Test)
//        private readonly IUserService _service;

//        // 2. CÁC MOCKS (GIẢ LẬP)
//        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
//        private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
//        private readonly Mock<IBlobService> _mockBlobService;
//        private readonly Mock<IConfiguration> _mockConfiguration;

//        private readonly IConfiguration _realConfiguration;

//        private const string TestContainerName = "test-avatars";

//        public UserServiceTests()
//        {
//            // 3. THIẾT LẬP MOCK CHO UserManager (Giữ nguyên)
//            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
//            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
//                userStoreMock.Object, null, null, null, null, null, null, null, null);

//            // 4. THIẾT LẬP MOCK CHO RoleManager (Giữ nguyên)
//            var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
//            _mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
//                roleStoreMock.Object, null, null, null, null);

//            // 5. THIẾT LẬP MOCK CHO BLOB (Giữ nguyên)
//            _mockBlobService = new Mock<IBlobService>();

//            // 6. ✨ SỬA LỖI TẠI ĐÂY: TẠO ICONFIGURATION THẬT (IN-MEMORY) ✨
//            var inMemorySettings = new Dictionary<string, string> {
//                // Cung cấp chính xác key mà UserService (dòng 40) cần
//                {"AzureBlob:ContainerName", TestContainerName}
//            };

//            _realConfiguration = new ConfigurationBuilder()
//                .AddInMemoryCollection(inMemorySettings)
//                .Build();

//            // 7. KHỞI TẠO SERVICE (VỚI CÁC MOCK VÀ CONFIG THẬT)
//            // Dòng 57 (sẽ gọi dòng 40 của UserService) sẽ chạy thành công
//            _service = new UserService(
//                _mockUserManager.Object,
//                _mockRoleManager.Object,
//                _mockBlobService.Object,
//                _realConfiguration // <-- Truyền IConfiguration thật vào
//            );
//        }

//        // Không cần Dispose nữa
//        // public void Dispose() { ... }

//        #region Helper Functions

//        /// <summary>
//        /// Tạo một đối tượng User giả để trả về từ Mock
//        /// </summary>
//        private ApplicationUser CreateMockUser(int id, string username, string email, string avatarUrl = "")
//        {
//            return new ApplicationUser
//            {
//                Id = id,
//                UserName = username,
//                Email = email,
//                AvatarUrl = avatarUrl,
//                IsActive = true,
//                CreatedAt = DateTime.UtcNow,
//                UpdatedAt = DateTime.UtcNow,
//                Decks = new List<Deck>() // Khởi tạo để tránh NullReference
//            };
//        }

//        /// <summary>
//        /// Tạo một file upload (IFormFile) giả (Giữ nguyên)
//        /// </summary>
//        private Mock<IFormFile> CreateMockFormFile()
//        {
//            var fileMock = new Mock<IFormFile>();
//            var content = "Hello World from a Fake File";
//            var fileName = "test_avatar.png";
//            var ms = new MemoryStream();
//            var writer = new StreamWriter(ms);
//            writer.Write(content);
//            writer.Flush();
//            ms.Position = 0;

//            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
//            fileMock.Setup(_ => _.FileName).Returns(fileName);
//            fileMock.Setup(_ => _.Length).Returns(ms.Length);
//            fileMock.Setup(_ => _.ContentType).Returns("image/png");

//            return fileMock;
//        }

//        #endregion

//        #region CRUD Tests (Đã viết lại)

//        //[Fact]
//        //public async Task UpdateUserAsync_WhenUsernameIsTaken_ThrowsInvalidOperationException()
//        //{
//        //    // ----- ARRANGE -----
//        //    var user1 = CreateMockUser(1, "user1", "user1@example.com");
//        //    var user2 = CreateMockUser(2, "user2", "user2@example.com");
//        //    var dto = new UpdateUserDto {  = user2.UserName };

//        //    // "Dạy" mock trả về user1 khi được tìm
//        //    _mockUserManager.Setup(m => m.FindByIdAsync(user1.Id.ToString()))
//        //                    .ReturnsAsync(user1);

//        //    // "Dạy" mock báo lỗi khi SetUserName
//        //    _mockUserManager.Setup(m => m.SetUserNameAsync(user1, user2.UserName))
//        //                    .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Username already taken." }));

//        //    // ----- ACT & ASSERT -----
//        //    var exception = await Assert.ThrowsAsync<InvalidOperationException>(
//        //        () => _service.UpdateUserAsync(user1.Id, dto)
//        //    );

//        //    // Kiểm tra lỗi (nên dùng Contains thay vì Equal)
//        //    Assert.Contains("Username already taken.", exception.Message);
//        //}

//        //[Fact]
//        //public async Task UpdateUserAsync_WhenEmailIsTaken_ThrowsInvalidOperationException()
//        //{
//        //    // ----- ARRANGE -----
//        //    var user1 = CreateMockUser(1, "user_email_1", "user1@example.com");
//        //    var user2 = CreateMockUser(2, "user_email_2", "user2@example.com");
//        //    var dto = new UpdateUserDto { Email = user2.Email };

//        //    _mockUserManager.Setup(m => m.FindByIdAsync(user1.Id.ToString()))
//        //                    .ReturnsAsync(user1);

//        //    _mockUserManager.Setup(m => m.SetEmailAsync(user1, user2.Email))
//        //                    .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email already taken." }));

//        //    // ----- ACT & ASSERT -----
//        //    var exception = await Assert.ThrowsAsync<InvalidOperationException>(
//        //        () => _service.UpdateUserAsync(user1.Id, dto)
//        //    );

//        //    Assert.Contains("Email already taken.", exception.Message);
//        //}

//        [Fact]
//        public async Task DeleteUserAsync_WhenUserExists_DeletesUser()
//        {
//            // ----- ARRANGE -----
//            var user = CreateMockUser(1, "to_be_deleted", "delete@example.com");

//            _mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString()))
//                            .ReturnsAsync(user);

//            _mockUserManager.Setup(m => m.DeleteAsync(user))
//                            .ReturnsAsync(IdentityResult.Success);

//            // ----- ACT -----
//            var result = await _service.DeleteUserAsync(user.Id);

//            // ----- ASSERT -----
//            Assert.True(result);

//            // Kiểm tra xem hàm DeleteAsync có được gọi đúng 1 lần không
//            _mockUserManager.Verify(m => m.DeleteAsync(user), Times.Once);
//        }

//        #endregion

//        #region Logic Tests (Password & Blob) (Đã viết lại)

//        [Fact]
//        public async Task ChangePasswordAsync_WithCorrectOldPassword_ChangesHash()
//        {
//            // ----- ARRANGE -----
//            var oldPass = "password123";
//            var newPass = "newPassword456";
//            var user = CreateMockUser(1, "user_pass", "pass@example.com");

//            _mockUserManager.Setup(m => m.FindByNameAsync(user.UserName))
//                            .ReturnsAsync(user);

//            // "Dạy" mock rằng ChangePasswordAsync sẽ thành công
//            _mockUserManager.Setup(m => m.ChangePasswordAsync(user, oldPass, newPass))
//                            .ReturnsAsync(IdentityResult.Success);

//            // ----- ACT -----
//            var result = await _service.ChangePasswordAsync(user.UserName, oldPass, newPass);

//            // ----- ASSERT -----
//            Assert.True(result);

//            // Chúng ta không thể kiểm tra hash (vì nó nằm trong UserManager),
//            // nhưng chúng ta có thể kiểm tra xem hàm ChangePasswordAsync có được gọi đúng không.
//            _mockUserManager.Verify(m => m.ChangePasswordAsync(user, oldPass, newPass), Times.Once);
//        }

//        [Fact]
//        public async Task ChangePasswordAsync_WithIncorrectOldPassword_ThrowsException()
//        {
//            // ----- ARRANGE -----
//            var oldPass = "password123";
//            var newPass = "newPassword456";
//            var user = CreateMockUser(1, "user_pass_fail", "passfail@example.com");

//            _mockUserManager.Setup(m => m.FindByNameAsync(user.UserName))
//                           .ReturnsAsync(user);

//            // "Dạy" mock rằng ChangePasswordAsync sẽ THẤT BẠI
//            _mockUserManager.Setup(m => m.ChangePasswordAsync(user, "WRONG_OLD_PASSWORD", newPass))
//                            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Old password is incorrect." }));

//            // ----- ACT & ASSERT -----
//            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
//                () => _service.ChangePasswordAsync(user.UserName, "WRONG_OLD_PASSWORD", newPass)
//            );

//            Assert.Equal("Old password is incorrect.", exception.Message);
//        }

//        [Fact]
//        public async Task UpdateUserAsync_WithNewAvatar_UploadsNewAndDeletesOld()
//        {
//            // ----- ARRANGE -----
//            var oldAvatarUrl = $"http://fake.blob.core.windows.net/{TestContainerName}/old_avatar.jpg";
//            var user = CreateMockUser(1, "user_avatar", "avatar@example.com", oldAvatarUrl);

//            var mockFile = CreateMockFormFile();
//            var dto = new UpdateUserDto { AvatarUrl = mockFile.Object };

//            var newBlobUrl = $"http://fake.blob.core.windows.net/{TestContainerName}/new_avatar.png";

//            // Dạy Mock UserManager
//            _mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString()))
//                            .ReturnsAsync(user);

//            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())) // Dạy nó chấp nhận lưu
//                            .ReturnsAsync(IdentityResult.Success);

//            // Dạy Mock Blob Service (Giữ nguyên)
//            _mockBlobService
//                .Setup(b => b.UploadFileBlobAsync(
//                    TestContainerName,
//                    It.IsAny<Stream>(),
//                    It.IsAny<string>(),
//                    It.IsAny<string>()))
//                .ReturnsAsync(newBlobUrl);

//            // ----- ACT -----
//            var result = await _service.UpdateUserAsync(user.Id, dto);

//            // ----- ASSERT -----
//            Assert.NotNull(result);

//            // 1. Kiểm tra URL mới đã được trả về
//            Assert.Equal(newBlobUrl, result.AvatarUrl);

//            // 2. Kiểm tra Mock: Service có gọi Upload không?
//            _mockBlobService.Verify(b => b.UploadFileBlobAsync(
//                TestContainerName,
//                It.IsAny<Stream>(),
//                "image/png",
//                It.IsAny<string>()), Times.Once);

//            // 3. Kiểm tra Mock: Service có gọi Delete blob CŨ không?
//            _mockBlobService.Verify(b => b.DeleteFileBlobAsync(
//                TestContainerName,
//                "old_avatar.jpg"), Times.Once);

//            // 4. Kiểm tra Mock: Service có gọi UpdateAsync của UserManager không?
//            _mockUserManager.Verify(m => m.UpdateAsync(It.Is<ApplicationUser>(u => u.AvatarUrl == newBlobUrl)), Times.Once);
//        }

//        #endregion
//    }
//}