using Dict.Data;
using Dict.DTO.Deck;
using Dict.DTO.User;
using Dict.Models;
using Dict.Service.IService;
using Microsoft.EntityFrameworkCore;
using System; // Thêm
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; // Thêm

namespace Dict.Service
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private const string DefaultAvatarUrl = "/images/default_ava.jpg"; // Đường dẫn avatar mặc định
        private readonly IBlobService _blobService;
        private readonly string _containerName;

        public UserService(ApplicationDbContext context, IBlobService blobService, IConfiguration configuration)
        {
            _context = context;
            _blobService = blobService;
            _containerName = configuration.GetValue<string>("AzureBlob:ContainerName") ?? "avatars";
        }
        // --- HÀM HELPER ĐỂ ÁNH XẠ ---
        private UserDto MapUserToDto(User user)
        {
            // Xác định URL avatar, dùng mặc định nếu null hoặc rỗng
            string avatarUrl = string.IsNullOrEmpty(user.AvatarUrl)
                ? DefaultAvatarUrl
                : user.AvatarUrl;

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsActive = user.IsActive,
                Role = user.Role, // Lấy Role từ model
                AvatarUrl = avatarUrl, // Lấy AvatarUrl đã xử lý
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                // Ánh xạ Decks (nếu có và đã được Include)
                Decks = user.Decks?.Select(d => new DeckSummaryDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description ?? "",
                    IsPublic = d.IsPublic ?? false,
                    // Cần Include Cards để tính Count() chính xác
                    CardCount = d.Cards?.Count() ?? 0,
                    AuthorName = user.Username
                }).ToList() ?? new List<DeckSummaryDto>() // Trả về list rỗng nếu Decks là null
            };
        }

        // --- CÁC HÀM GET ---
        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Include(u => u.Decks)
                    .ThenInclude(d => d.Cards) // Include Cards cho CardCount
                .ToListAsync(); // Lấy tất cả user

            // Ánh xạ từng user sang DTO
            return users.Select(user => MapUserToDto(user));
        }

        public async Task<UserDto?> GetUserByIdAsync(int id) // Trả về UserDto? (nullable)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Decks)
                    .ThenInclude(d => d.Cards)
                .FirstOrDefaultAsync(u => u.Id == id); // Tìm user theo Id

            // Trả về null nếu không tìm thấy, ngược lại ánh xạ sang DTO
            return user == null ? null : MapUserToDto(user);
        }

        public async Task<IEnumerable<UserDto>> SearchUsersByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return Enumerable.Empty<UserDto>();
            }

            var query = username.ToLower();
            var users = await _context.Users
                .AsNoTracking()
                .Include(u => u.Decks)
                    .ThenInclude(d => d.Cards)
                .Where(u => u.Username.ToLower().Contains(query)) // Tìm theo username
                .ToListAsync();

            return users.Select(user => MapUserToDto(user));
        }

        // --- CÁC HÀM UPDATE VÀ DELETE ---
        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return false;
            }
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<UserResponseDto?> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;

            if (!string.IsNullOrWhiteSpace(dto.Username))
            {
                var newUsername = dto.Username!.Trim();
                if (!user.Username.Equals(newUsername, StringComparison.OrdinalIgnoreCase) &&
                    await _context.Users.AnyAsync(u => u.Username.ToLower() == newUsername.ToLower() && u.Id != id))
                    throw new InvalidOperationException("Username already taken.");
                user.Username = newUsername;
            }

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var newEmail = dto.Email!.Trim();
                if (!user.Email.Equals(newEmail, StringComparison.OrdinalIgnoreCase) &&
                    await _context.Users.AnyAsync(u => u.Email.ToLower() == newEmail.ToLower() && u.Id != id))
                    throw new InvalidOperationException("Email already taken.");
                user.Email = newEmail;
            }

            if (dto.IsActive.HasValue)
                user.IsActive = dto.IsActive.Value;

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
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsActive = user.IsActive,
                AvatarUrl = user.AvatarUrl,
                UpdatedAt = user.UpdatedAt
            };
        }

        public async Task<UserResponseDto?> UpdateUserByUsernameAsync(string username, UpdateUserDto dto)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            if (user == null) return null;

            if (!string.IsNullOrWhiteSpace(dto.Username))
            {
                var newUsername = dto.Username!.Trim();
                if (!user.Username.Equals(newUsername, StringComparison.OrdinalIgnoreCase) &&
                    await _context.Users.AnyAsync(u => u.Username.ToLower() == newUsername.ToLower() && u.Id != user.Id))
                    throw new InvalidOperationException("New username is already taken.");
                user.Username = newUsername;
            }

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var newEmail = dto.Email!.Trim();
                if (!user.Email.Equals(newEmail, StringComparison.OrdinalIgnoreCase) &&
                    await _context.Users.AnyAsync(u => u.Email.ToLower() == newEmail.ToLower() && u.Id != user.Id))
                    throw new InvalidOperationException("New email is already taken.");
                user.Email = newEmail;
            }

            if (dto.IsActive.HasValue)
                user.IsActive = dto.IsActive.Value;



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
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsActive = user.IsActive,
                AvatarUrl = user.AvatarUrl,
                UpdatedAt = user.UpdatedAt
            };
        }

        private static string? GetBlobNameFromUrl(string blobUrl, string containerName)
        {
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

        // ❌ REMOVE: The dedicated UpdateAvatarUrlAsync method is no longer needed
        // public async Task<bool> UpdateAvatarUrlAsync(int userId, string newAvatarUrl) { ... }
    }
}
