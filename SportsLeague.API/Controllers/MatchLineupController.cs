using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SportsLeague.API.DTOs.Request;
using SportsLeague.API.DTOs.Response;
using SportsLeague.Domain.Interfaces.Services;

namespace SportsLeague.API.Controllers
{
    [ApiController]
    [Route("api/match/{matchId}/lineup")]
    public class MatchLineupController : ControllerBase
    {
        private readonly IMatchLineupService _lineupService;
        private readonly IMapper _mapper;

        public MatchLineupController(
            IMatchLineupService lineupService,
            IMapper mapper)
        {
            _lineupService = lineupService;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<MatchLineupResponseDTO>> AddToLineup(int matchId, CreateMatchLineupDTO dto)
        {
            try
            {
                var lineup = await _lineupService.AddToLineupAsync(matchId, dto.PlayerId, dto.IsStarter, dto.Position);
                var response = _mapper.Map<MatchLineupResponseDTO>(lineup);
                return CreatedAtAction(nameof(GetLineupByMatch), new { matchId }, response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MatchLineupResponseDTO>>> GetLineupByMatch(int matchId)
        {
            try
            {
                var lineups = await _lineupService.GetLineupByMatchAsync(matchId);
                var response = _mapper.Map<IEnumerable<MatchLineupResponseDTO>>(lineups);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("team/{teamId}")]
        public async Task<ActionResult<IEnumerable<MatchLineupResponseDTO>>> GetLineupByMatchAndTeam(int matchId, int teamId)
        {
            try
            {
                var lineups = await _lineupService.GetLineupByMatchAndTeamAsync(matchId, teamId);
                var response = _mapper.Map<IEnumerable<MatchLineupResponseDTO>>(lineups);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveFromLineup(int matchId, int id)
        {
            try
            {
                await _lineupService.RemoveFromLineupAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}