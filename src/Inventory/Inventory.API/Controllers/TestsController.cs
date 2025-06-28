using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestsController : ControllerBase
    {

        [HttpGet("DateTime")]
        public IActionResult Get()
        {
            var result = new
            {
                Date = DateTime.UtcNow.ToLongDateString(),
                Time = DateTime.UtcNow.ToLongTimeString()
            };

            return Ok(result);
        }

    }
}
