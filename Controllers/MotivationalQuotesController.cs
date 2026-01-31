using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeeklyHabitTracker.Data;
using WeeklyHabitTracker.Models;

namespace WeeklyHabitTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MotivationalQuotesController : ControllerBase
{
    private readonly AppDbContext _db;

    public MotivationalQuotesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Get all motivational quotes</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MotivationalQuote>>> GetAll()
    {
        return Ok(await _db.MotivationalQuotes.OrderBy(q => q.Id).ToListAsync());
    }

    /// <summary>Get a quote by ID</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MotivationalQuote>> GetById(int id)
    {
        var quote = await _db.MotivationalQuotes.FindAsync(id);
        if (quote == null) return NotFound();
        return Ok(quote);
    }

    /// <summary>Create a new quote</summary>
    [HttpPost]
    public async Task<ActionResult<MotivationalQuote>> Create([FromBody] MotivationalQuoteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Quote) || string.IsNullOrWhiteSpace(dto.Author))
            return BadRequest("Quote and Author are required.");
        var quote = new MotivationalQuote { Quote = dto.Quote.Trim(), Author = dto.Author.Trim() };
        _db.MotivationalQuotes.Add(quote);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = quote.Id }, quote);
    }

    /// <summary>Update an existing quote</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<MotivationalQuote>> Update(int id, [FromBody] MotivationalQuoteDto dto)
    {
        var quote = await _db.MotivationalQuotes.FindAsync(id);
        if (quote == null) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Quote) || string.IsNullOrWhiteSpace(dto.Author))
            return BadRequest("Quote and Author are required.");
        quote.Quote = dto.Quote.Trim();
        quote.Author = dto.Author.Trim();
        await _db.SaveChangesAsync();
        return Ok(quote);
    }

    /// <summary>Delete a quote</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var quote = await _db.MotivationalQuotes.FindAsync(id);
        if (quote == null) return NotFound();
        _db.MotivationalQuotes.Remove(quote);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public class MotivationalQuoteDto
{
    public string Quote { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
}
