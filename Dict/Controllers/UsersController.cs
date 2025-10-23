using Dict.DTO;
using Dict.DTO.User;
using Dict.Service.IService;
using Microsoft.AspNetCore.Mvc;

namespace Dict.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ResponseDTO _response;

        public UsersController(IUserService userService)
        {
            _userService = userService;
            _response = new ResponseDTO();
        }

        // GET: api/users
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                _response.Result = users;
                _response.Message = "Successfully retrieved all users.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                // Trả về lỗi 500 Internal Server Error
                return StatusCode(500, _response);
            }
        }

        // GET: api/users/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "User not found.";
                    return NotFound(_response); // 404 Not Found
                }
                _response.Result = user;
                _response.Message = "Successfully retrieved the user.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
        }

        // POST: api/users
       
       

        // DELETE: api/users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(id);
                if (!result)
                {
                    _response.IsSuccess = false;
                    _response.Message = "User not found.";
                    return NotFound(_response);
                }
                _response.Message = "User deleted successfully.";
                return Ok(_response); // Thay vì NoContent(), trả về Ok() với thông điệp
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
        }
        [HttpGet("search/{username}")]
        public async Task<IActionResult> SearchUsers(string username)
        {
            try
            {
                var users = await _userService.SearchUsersByUsernameAsync(username);
                _response.Result = users;
                _response.Message = $"Successfully found users matching '{username}'.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
        }

        [HttpPut("by-username/{username}")]
        public async Task<IActionResult> UpdateUserByUsername([FromRoute] string username, [FromForm] UpdateUserDto updateUserDto)
        {
            if (!ModelState.IsValid)
            {
                _response.IsSuccess = false;
                _response.Message = "Invalid input data.";
                return BadRequest(_response);
            }

            try
            {
                var updated = await _userService.UpdateUserByUsernameAsync(username, updateUserDto);
                if (updated == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "User not found or update failed.";
                    return NotFound(_response);
                }

                _response.IsSuccess = true;
                _response.Message = "User updated successfully.";
                _response.Result = updated;
                return Ok(_response);
            }
            catch (InvalidOperationException ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return Conflict(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromForm] UpdateUserDto updateUserDto)
        {
            if (!ModelState.IsValid)
            {
                _response.IsSuccess = false;
                _response.Message = "Invalid input data.";
                return BadRequest(_response);
            }

            try
            {
                var updated = await _userService.UpdateUserAsync(id, updateUserDto);
                if (updated == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "User not found or update failed.";
                    return NotFound(_response);
                }

                _response.IsSuccess = true;
                _response.Message = "User updated successfully.";
                _response.Result = updated;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return StatusCode(500, _response);
            }
        }
    }
}
