using Dict.Data;
using Dict.DTO.Deck;
using Dict.DTO.User;
using Dict.Models;
using Dict.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace Dict.Service
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✨ SỬA ĐỔI: Lấy tất cả user kèm theo deck của họ
        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .AsNoTracking() // Tối ưu cho truy vấn chỉ đọc
                .Include(u => u.Decks) // Tải kèm các Deck
                    .ThenInclude(d => d.Cards) // Tải kèm Cards để tính CardCount
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    Decks = u.Decks.Select(d => new DeckSummaryDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Description = d.Description ?? "",
                        IsPublic = d.IsPublic ?? false,
                        CardCount = d.Cards.Count(),
                        AuthorName = u.Username
                    }).ToList()
                })
                .ToListAsync();

            return users;
        }

        // ✨ SỬA ĐỔI: Lấy một user theo ID kèm theo deck
        public async Task<UserDto> GetUserByIdAsync(int id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Decks)
                    .ThenInclude(d => d.Cards)
                .Where(u => u.Id == id)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    Decks = u.Decks.Select(d => new DeckSummaryDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Description = d.Description ?? "",
                        IsPublic = d.IsPublic ?? false,
                        CardCount = d.Cards.Count(),
                        AuthorName = u.Username
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            return user;
        }

        // ✨ SỬA ĐỔI: Tìm kiếm user theo username, kết quả trả về cũng kèm theo deck
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
                .Where(u => u.Username.ToLower().Contains(query))
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    Decks = u.Decks.Select(d => new DeckSummaryDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Description = d.Description ?? "",
                        IsPublic = d.IsPublic ?? false,
                        CardCount = d.Cards.Count(),
                        AuthorName = u.Username
                    }).ToList()
                })
                .ToListAsync();

            return users;
        }


        // --- CÁC PHƯƠNG THỨC CẬP NHẬT VÀ XÓA KHÔNG THAY ĐỔI ---

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

        public async Task<bool> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(updateUserDto.Username))
            {
                user.Username = updateUserDto.Username;
            }
            if (!string.IsNullOrEmpty(updateUserDto.Email))
            {
                user.Email = updateUserDto.Email;
            }
            if (updateUserDto.IsActive.HasValue)
            {
                user.IsActive = updateUserDto.IsActive.Value;
            }
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateUserByUsernameAsync(string username, UpdateUserDto updateUserDto)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            if (user == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(updateUserDto.Username))
            {
                if (user.Username.ToLower() != updateUserDto.Username.ToLower() &&
                    await _context.Users.AnyAsync(u => u.Username.ToLower() == updateUserDto.Username.ToLower()))
                {
                    throw new InvalidOperationException("New username is already taken.");
                }
                user.Username = updateUserDto.Username;
            }
            if (!string.IsNullOrEmpty(updateUserDto.Email))
            {
                user.Email = updateUserDto.Email;
            }
            if (updateUserDto.IsActive.HasValue)
            {
                user.IsActive = updateUserDto.IsActive.Value;
            }
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
