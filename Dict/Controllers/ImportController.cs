using Dict.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;

namespace Dict.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private ResponseDTO _response;

        public ImportController()
        {
            _response = new ResponseDTO();
        }

        [HttpGet]
        [Route("ImportKanji")]
        public async Task<IActionResult> GetAllUser()
        {
            try
            {
                //_response.Result = await _userService.GetAllUser();
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }
    }
}
