using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SportsLeague.API.DTOs.Request;
using SportsLeague.API.DTOs.Response;
using SportsLeague.Domain.Interfaces.Services;

namespace SportsLeague.API.Controllers;

[ApiController]
[Route("api/match/{matchId}/lineup")]
public class MatchLineupController : ControllerBase
{
    private readonly IMatchLineupService _lineupService;
    private readonly IMapper _mapper;

    public MatchLineupController(IMatchLineupService lineupService, IMapper mapper)
    {
        _lineupService = lineupService;
        _mapper = mapper;
    }

    // POST /api/match/{matchId}/lineup
    [HttpPost]
    public async Task<ActionResult<MatchLineupResponseDTO>> AddToLineup(int matchId, CreateMatchLineupDTO dto)
    {
        var lineup = await _lineupService.AddToLineupAsync(matchId, dto.PlayerId, dto.IsStarter, dto.Position);
        return CreatedAtAction(nameof(GetLineupByMatch), new { matchId }, _mapper.Map<MatchLineupResponseDTO>(lineup));
    }

    // GET /api/match/{matchId}/lineup
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MatchLineupResponseDTO>>> GetLineupByMatch(int matchId)
    {
        var lineups = await _lineupService.GetLineupByMatchAsync(matchId);
        return Ok(_mapper.Map<IEnumerable<MatchLineupResponseDTO>>(lineups));
    }

    // GET /api/match/{matchId}/lineup/team/{teamId}
    [HttpGet("team/{teamId}")]
    public async Task<ActionResult<IEnumerable<MatchLineupResponseDTO>>> GetLineupByMatchAndTeam(int matchId, int teamId)
    {
        var lineups = await _lineupService.GetLineupByMatchAndTeamAsync(matchId, teamId);
        return Ok(_mapper.Map<IEnumerable<MatchLineupResponseDTO>>(lineups));
    }

    // DELETE /api/match/{matchId}/lineup/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveFromLineup(int matchId, int id)
    {
        await _lineupService.RemoveFromLineupAsync(id);
        return NoContent();
    }
}