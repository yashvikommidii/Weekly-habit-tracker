using Microsoft.AspNetCore.Mvc;

namespace WeeklyHabitTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HelloWorldController : ControllerBase
{
    /// <summary>
    /// Returns a whimsical greeting with the current date and time.
    /// </summary>
    [HttpGet]
    public ActionResult<HelloWorldResponse> Get()
    {
        return Ok(new HelloWorldResponse
        {
            DateTime = DateTime.Now,
            Message = "Another moment to choose habits that matter—what will you track today? 🌱"
        });
    }
}

public class HelloWorldResponse
{
    public DateTime DateTime { get; set; }
    public string Message { get; set; } = string.Empty;
}
