using Dict.DTO.User;

namespace Dict.DTO.Admin
{
    public class PaginatedUsersDto
    {
        public List<UserDto> Users { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

}
