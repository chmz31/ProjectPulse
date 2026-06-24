using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPulse.Api.Persistence;
using ProjectPulse.Api.Domain;
using ProjectPulse.Api.DTOs;
using ProjectPulse.Api.Security;
using Microsoft.AspNetCore.Authorization;

namespace ProjectPulse.Api.Controllers;

[Authorize]
[ApiController]
[Route("projects")]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProjectsController(AppDbContext db) => _db = db;

    // GET /projects?query=&page=&pageSize=&sort=
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> Get([FromQuery] ProjectQueryDto q)
    {
        if (!User.TryGetUserId(out var userId)) return Unauthorized();

        var query = _db.Projects.AsNoTracking().Where(p => p.OwnerId == userId);

        if (!string.IsNullOrWhiteSpace(q.Query))
            query = query.Where(p => p.Name.Contains(q.Query) || (p.Description ?? "").Contains(q.Query));

        // sort básico
        query = q.Sort switch
        {
            "name" => query.OrderBy(p => p.Name),
            "-name" => query.OrderByDescending(p => p.Name),
            "createdAt" => query.OrderBy(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt) // "-createdAt" por defecto
        };

        var page = Math.Max(q.Page, 1);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProjectDto(p.Id, p.Name, p.Description, p.CreatedAt))
            .ToListAsync();

        // headers de paginación útiles para el frontend
        Response.Headers["X-Total-Count"] = total.ToString();
        Response.Headers["X-Page"] = page.ToString();
        Response.Headers["X-Page-Size"] = pageSize.ToString();

        return Ok(items);
    }

    // GET /projects/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id)
    {
        if (!User.TryGetUserId(out var userId)) return Unauthorized();

        var p = await _db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == userId);
        if (p is null) return NotFound();

        return new ProjectDto(p.Id, p.Name, p.Description, p.CreatedAt);
    }

    // POST /projects  (usa ProjectCreateDto)
    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] ProjectCreateDto dto)
    {
        if (!User.TryGetUserId(out var userId)) return Unauthorized();

        var entity = new Project
        {
            Name = dto.Name,
            Description = dto.Description,
            OwnerId = userId
        };

        _db.Projects.Add(entity);
        await _db.SaveChangesAsync();

        var result = new ProjectDto(entity.Id, entity.Name, entity.Description, entity.CreatedAt);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
    }

    // PUT /projects/{id}  (usa ProjectUpdateDto)
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProjectUpdateDto dto)
    {
        if (!User.TryGetUserId(out var userId)) return Unauthorized();

        var p = await _db.Projects.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == userId);
        if (p is null) return NotFound();

        p.Name = dto.Name;
        p.Description = dto.Description;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE /projects/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!User.TryGetUserId(out var userId)) return Unauthorized();

        var p = await _db.Projects.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == userId);
        if (p is null) return NotFound();

        _db.Projects.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
