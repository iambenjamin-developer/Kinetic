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
            var argentinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");
            var argentinaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, argentinaTimeZone);

            var result = new
            {
                Utc = new
                {
                    Zone = "UTC",
                    Date = DateTime.UtcNow.ToLongDateString(),
                    Time = DateTime.UtcNow.ToLongTimeString()
                },
                Argentina = new
                {
                    Zone = argentinaTimeZone.DisplayName,
                    Date = argentinaTime.ToLongDateString(),
                    Time = argentinaTime.ToLongTimeString()
                }
            };

            return Ok(result);
        }

    }
}
