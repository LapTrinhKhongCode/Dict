using Dict.Data;
using Dict.DTO.Deck;
using Dict.DTO.User;
using Dict.Models;
using Dict.Service.IService;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // Thêm
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO; // Thêm cho Path

namespace Dict.Service
{
    public class UserService : IUserService
    {
        // 1. THÊM CÁC MANAGER CỦA IDENTITY
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        // private readonly ApplicationDbContext _context; // <-- XÓA, UserManager đã chứa nó

        private const string DefaultAvatarUrl = "/images/default_ava.jpg";
        private readonly IBlobService _blobService;
        private readonly string _containerName;

        // 2. CẬP NHẬT CONSTRUCTOR
        public UserService(
            // ApplicationDbContext context, // <-- XÓA
            UserManager<ApplicationUser> userManager, // <-- THÊM
            RoleManager<ApplicationRole> roleManager, // <-- THÊM
            IBlobService blobService,
            IConfiguration configuration)
        {
            // _context = context; // <-- XÓA
            _userManager = userManager;
            _roleManager = roleManager;
            _blobService = blobService;
            _containerName = configuration.GetValue<string>("AzureBlob:ContainerName") ?? "avatars";
        }

        // 3. CẬP NHẬT HÀM MAP (PHẢI LÀ ASYNC VÀ LẤY ROLE)
        private async Task<UserDto> MapUserToDto(ApplicationUser user)
        {
            string avatarUrl = string.IsNullOrEmpty(user.AvatarUrl)
                ? DefaultAvatarUrl
                : user.AvatarUrl;

            // Lấy vai trò (roles) của user
            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                IsActive = user.IsActive,
                Role = roles.FirstOrDefault(), // Lấy vai trò đầu tiên (hoặc null)
                AvatarUrl = avatarUrl,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Decks = user.Decks?.Select(d => new DeckSummaryDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description ?? "",
                    IsPublic = d.IsPublic ?? false,
                    CardCount = d.Cards?.Count() ?? 0,
                    AuthorName = user.UserName
                }).ToList() ?? new List<DeckSummaryDto>()
            };
        }

        // --- CÁC HÀM GET (PHẢI DÙNG _userManager.Users) ---
        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            // 4. DÙNG _userManager.Users thay vì _context.Users
            var users = await _userManager.Users
                .AsNoTracking()
                .Include(u => u.Decks)
                    .ThenInclude(d => d.Cards)
                .ToListAsync();

            // 5. Phải lặp (loop) vì MapUserToDto giờ là async
            var dtos = new List<UserDto>();
            foreach (var user in users)
            {
                dtos.Add(await MapUserToDto(user));
            }
            return dtos;
        }

        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            var user = await _userManager.Users
                .AsNoTracking()
                .Include(u => u.Decks)
                    .ThenInclude(d => d.Cards)
                .FirstOrDefaultAsync(u => u.Id == id);

            return user == null ? null : await MapUserToDto(user);
        }

        public async Task<IEnumerable<UserDto>> SearchUsersByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return Enumerable.Empty<UserDto>();
            }

            var query = username.ToLower();
            var users = await _userManager.Users
                .AsNoTracking()
                .Include(u => u.Decks)
                    .ThenInclude(d => d.Cards)
                .Where(u => u.UserName.ToLower().Contains(query))
                .ToListAsync();

            var dtos = new List<UserDto>();
            foreach (var user in users)
            {
                dtos.Add(await MapUserToDto(user));
            }
            return dtos;
        }

        // --- CÁC HÀM UPDATE VÀ DELETE (PHẢI DÙNG _userManager) ---
        public async Task<bool> DeleteUserAsync(int id)
        {
            // 6. DÙNG _userManager.FindByIdAsync VÀ DeleteAsync
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return false;
            }
            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }


        public async Task<UserResponseDto?> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return null;

            // << --- TOÀN BỘ KHỐI CODE CŨ (DÙNG _context) ĐÃ BỊ XÓA Ở ĐÂY --- >>

            // Chỉ giữ lại dòng gọi hàm helper
            return await UpdateUserInternalAsync(user, dto);
        }

        public async Task<UserResponseDto?> UpdateUserByUsernameAsync(string username, UpdateUserDto dto)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return null;

            return await UpdateUserInternalAsync(user, dto);
        }

        // 7. TÁCH HÀM UPDATE RA ĐỂ TÁI SỬ DỤNG
        private async Task<UserResponseDto?> UpdateUserInternalAsync(ApplicationUser user, UpdateUserDto dto)
        {
            bool needsUpdate = false;
            List<string> errors = new List<string>();

            // Cập nhật Email
            if (!string.IsNullOrWhiteSpace(dto.Email) && user.Email != dto.Email.Trim())
            {
                var result = await _userManager.SetEmailAsync(user, dto.Email.Trim());
                if (!result.Succeeded) errors.AddRange(result.Errors.Select(e => e.Description));
                needsUpdate = true;
            }

            // Cập nhật các trường tùy chỉnh
            if (dto.IsActive.HasValue && user.IsActive != dto.IsActive.Value)
            {
                user.IsActive = dto.IsActive.Value;
                needsUpdate = true;
            }

            // Cập nhật Avatar (logic này giữ nguyên vì nó là tùy chỉnh)
            if (dto.AvatarUrl != null && dto.AvatarUrl.Length > 0)
            {
                var file = dto.AvatarUrl;
                var blobFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                using var stream = file.OpenReadStream();
                var newBlobUrl = await _blobService.UploadFileBlobAsync(_containerName, stream, file.ContentType ?? "application/octet-stream", blobFileName);

                if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
                {
                    var existingBlobName = GetBlobNameFromUrl(user.AvatarUrl, _containerName);
                    if (!string.IsNullOrWhiteSpace(existingBlobName))
                        await _blobService.DeleteFileBlobAsync(_containerName, existingBlobName);
                }
                user.AvatarUrl = newBlobUrl;
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                user.UpdatedAt = DateTime.UtcNow;
                var updateResult = await _userManager.UpdateAsync(user); // Lưu tất cả thay đổi
                if (!updateResult.Succeeded) errors.AddRange(updateResult.Errors.Select(e => e.Description));
            }

            if (errors.Any())
            {
                throw new InvalidOperationException(string.Join("\n", errors));
            }

            // Trả về DTO
            return new UserResponseDto
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                IsActive = user.IsActive,
                AvatarUrl = user.AvatarUrl,
                UpdatedAt = user.UpdatedAt
            };
        }


        public async Task<bool> ChangePasswordAsync(string username, string oldPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException("Username, old password, and new password are required.");

            // 8. DÙNG _userManager
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            // 9. DÙNG _userManager.ChangePasswordAsync (thay vì BCrypt)
            var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);

            if (!result.Succeeded)
            {
                var error = result.Errors.FirstOrDefault()?.Description ?? "Old password is incorrect.";
                throw new InvalidOperationException(error);
            }

            return true;
        }

        private static string? GetBlobNameFromUrl(string blobUrl, string containerName)
        {
            // ... (hàm này giữ nguyên, không thay đổi) ...
            if (string.IsNullOrWhiteSpace(blobUrl)) return null;
            try
            {
                var uri = new Uri(blobUrl);
                var path = uri.AbsolutePath;
                var prefix = $"/{containerName}/";
                if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
                return path.Substring(prefix.Length);
            }
            catch
            {
                return null;
            }
        }
    }
}