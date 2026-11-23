using BocciaServer.Data;
using BocciaServer.Services;
using BocciaShared.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BocciaServer.Controllers;

[ApiController]
[Route("api/divisions")]
public class DivisionsController(BocciaDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<DivisionDto>> List(CancellationToken ct) =>
        (await db.Divisions.AsNoTracking().OrderBy(d => d.DivisionId).ToListAsync(ct)).Select(d => d.ToDto());
}

[ApiController]
[Route("api/athletes")]
public class AthletesController(BocciaDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<AthleteDto>> List(CancellationToken ct) =>
        (await db.Athletes.AsNoTracking().Where(a => a.IsActive)
            .OrderBy(a => a.LastName).ThenBy(a => a.FirstName).ToListAsync(ct))
        .Select(a => a.ToDto());

    [HttpPost]
    public async Task<ActionResult<AthleteDto>> Create(CreateAthleteRequest request, CancellationToken ct)
    {
        var athlete = new Athlete
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Classification = request.Classification,
            Club = request.Club?.Trim(),
        };
        db.Athletes.Add(athlete);
        await db.SaveChangesAsync(ct);
        return Created($"/api/athletes/{athlete.AthleteId}", athlete.ToDto());
    }
}
