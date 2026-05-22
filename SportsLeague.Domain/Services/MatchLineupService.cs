using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Enums;
using SportsLeague.Domain.Helpers;
using SportsLeague.Domain.Interfaces.Repositories;
using SportsLeague.Domain.Interfaces.Services;

namespace SportsLeague.Domain.Services;

public class MatchLineupService : IMatchLineupService
{
    private readonly IMatchLineupRepository _lineupRepository;
    private readonly MatchValidationHelper _validationHelper;

    public MatchLineupService(IMatchLineupRepository lineupRepository, MatchValidationHelper validationHelper)
    {
        _lineupRepository = lineupRepository;
        _validationHelper = validationHelper;
    }

    // 1. Agregar un jugador a la alineación
    public async Task<MatchLineup> AddToLineupAsync(int matchId, int playerId, bool isStarter, string position)
    {
        // V1+V6: Validar que el partido existe y está en estado Scheduled
        var match = await _validationHelper.ValidateMatchForEventAsync(matchId);
        if (match.Status != MatchStatus.Scheduled)
            throw new InvalidOperationException("Solo se pueden registrar alineaciones en partidos Scheduled");

        // V3: Validar que el jugador pertenece al HomeTeam o AwayTeam del partido
        var player = await _validationHelper.ValidatePlayerInMatchAsync(playerId, match);

        // V4: Validar que el jugador no esté duplicado en la misma alineación
        bool alreadyExists = await _lineupRepository.ExistsByMatchAndPlayerAsync(matchId, playerId);
        if (alreadyExists)
            throw new InvalidOperationException("El jugador ya está registrado en la alineación de este partido");

        // V5: Si es titular, contar cuántos titulares tiene el equipo (máx 11)
        if (isStarter)
        {
            int startersCount = await _lineupRepository.CountStartersByMatchAndTeamAsync(matchId, player.TeamId);
            if (startersCount >= 11)
                throw new InvalidOperationException("El equipo ya tiene 11 titulares registrados en este partido");
        }

        var lineup = new MatchLineup
        {
            MatchId = matchId,
            PlayerId = playerId,
            IsStarter = isStarter,
            Position = position
        };

        return await _lineupRepository.CreateAsync(lineup);
    }

    // 2. Obtener toda la alineación de un partido
    public async Task<IEnumerable<MatchLineup>> GetLineupByMatchAsync(int matchId)
    {
        // Validar que el partido exista (reutilizamos el helper, pero sin exigir Scheduled)
        var match = await _validationHelper.ValidateMatchExistsAsync(matchId);
        if (match == null)
            throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");

        return await _lineupRepository.GetByMatchAsync(matchId);
    }

    // 3. Obtener alineación de un partido filtrada por equipo
    public async Task<IEnumerable<MatchLineup>> GetLineupByMatchAndTeamAsync(int matchId, int teamId)
    {
        var match = await _validationHelper.ValidateMatchExistsAsync(matchId);
        if (match == null)
            throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");

        // Validar que el equipo pertenezca al partido
        if (match.HomeTeamId != teamId && match.AwayTeamId != teamId)
            throw new InvalidOperationException($"El equipo {teamId} no participa en el partido {matchId}");

        return await _lineupRepository.GetByMatchAndTeamAsync(matchId, teamId);
    }

    // 4. Eliminar un registro de alineación por su ID
    public async Task RemoveFromLineupAsync(int lineupId)
    {
        var lineup = await _lineupRepository.GetByIdAsync(lineupId);
        if (lineup == null)
            throw new KeyNotFoundException($"No se encontró la alineación con ID {lineupId}");

        await _lineupRepository.DeleteAsync(lineupId);
    }
}