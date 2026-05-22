using Microsoft.Extensions.Logging;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Enums;
using SportsLeague.Domain.Interfaces.Repositories;
using SportsLeague.Domain.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SportsLeague.Domain.Services
{
    public class MatchLineupService : IMatchLineupService
    {
        private readonly IMatchLineupRepository _lineupRepository;
        private readonly IMatchRepository _matchRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly ILogger<MatchLineupService> _logger;

        public MatchLineupService(
            IMatchLineupRepository lineupRepository,
            IMatchRepository matchRepository,
            IPlayerRepository playerRepository,
            ILogger<MatchLineupService> logger)
        {
            _lineupRepository = lineupRepository;
            _matchRepository = matchRepository;
            _playerRepository = playerRepository;
            _logger = logger;
        }

        public async Task<MatchLineup> AddToLineupAsync(int matchId, int playerId, bool isStarter, string position)
        {
            _logger.LogInformation("Adding player {PlayerId} to match {MatchId}", playerId, matchId);

            // V1: Validar que el partido existe
            var match = await _matchRepository.GetByIdAsync(matchId);
            if (match == null)
                throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");

            // V6: Validar que el partido esté en estado Scheduled
            if (match.Status != MatchStatus.Scheduled)
                throw new InvalidOperationException("Solo se pueden registrar alineaciones en partidos Scheduled");

            // V3: Validar que el jugador existe y pertenece a HomeTeam o AwayTeam
            var player = await _playerRepository.GetByIdAsync(playerId);
            if (player == null)
                throw new KeyNotFoundException($"No se encontró el jugador con ID {playerId}");

            if (player.TeamId != match.HomeTeamId && player.TeamId != match.AwayTeamId)
                throw new InvalidOperationException("El jugador no pertenece a ninguno de los equipos del partido");

            // V4: Validar que el jugador no esté duplicado
            var alreadyExists = await _lineupRepository.ExistsByMatchAndPlayerAsync(matchId, playerId);
            if (alreadyExists)
                throw new InvalidOperationException("El jugador ya está registrado en la alineación de este partido");

            // V5: Si es titular, contar máx 11
            if (isStarter)
            {
                var startersCount = await _lineupRepository.CountStartersByMatchAndTeamAsync(matchId, player.TeamId);
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

            var result = await _lineupRepository.CreateAsync(lineup);
            _logger.LogInformation("Player {PlayerId} added to lineup with id {LineupId}", playerId, result.Id);
            return result;
        }

        public async Task<IEnumerable<MatchLineup>> GetLineupByMatchAsync(int matchId)
        {
            _logger.LogInformation("Retrieving lineup for match {MatchId}", matchId);

            var matchExists = await _matchRepository.ExistsAsync(matchId);
            if (!matchExists)
                throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");

            return await _lineupRepository.GetByMatchAsync(matchId);
        }

        public async Task<IEnumerable<MatchLineup>> GetLineupByMatchAndTeamAsync(int matchId, int teamId)
        {
            _logger.LogInformation("Retrieving lineup for match {MatchId} and team {TeamId}", matchId, teamId);

            var match = await _matchRepository.GetByIdAsync(matchId);
            if (match == null)
                throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");

            if (match.HomeTeamId != teamId && match.AwayTeamId != teamId)
                throw new InvalidOperationException($"El equipo {teamId} no participa en el partido {matchId}");

            return await _lineupRepository.GetByMatchAndTeamAsync(matchId, teamId);
        }

        public async Task RemoveFromLineupAsync(int id)
        {
            _logger.LogInformation("Removing lineup with id {LineupId}", id);

            var exists = await _lineupRepository.ExistsAsync(id);
            if (!exists)
                throw new KeyNotFoundException($"No se encontró la alineación con ID {id}");

            await _lineupRepository.DeleteAsync(id);
            _logger.LogInformation("Lineup {LineupId} removed", id);
        }
    }
}