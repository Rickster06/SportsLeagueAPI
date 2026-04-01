using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Interfaces.Repositories;
using SportsLeague.Domain.Interfaces.Services;
using System.Text.RegularExpressions;

namespace SportsLeague.API.Services
{
    public class SponsorService : ISponsorService
    {
        private readonly ISponsorRepository _sponsorRepository;
        private readonly ITournamentRepository _tournamentRepository;
        private readonly ITournamentSponsorRepository _tournamentSponsorRepository;

        public SponsorService(
            ISponsorRepository sponsorRepository,
            ITournamentRepository tournamentRepository,
            ITournamentSponsorRepository tournamentSponsorRepository)
        {
            _sponsorRepository = sponsorRepository;
            _tournamentRepository = tournamentRepository;
            _tournamentSponsorRepository = tournamentSponsorRepository;
        }

        public async Task<IEnumerable<Sponsor>> GetAllAsync()
            => await _sponsorRepository.GetAllAsync();

        public async Task<Sponsor?> GetByIdAsync(int id)
            => await _sponsorRepository.GetByIdAsync(id);

        public async Task<Sponsor> CreateAsync(Sponsor sponsor)
        {
            // Validar email
            if (!IsValidEmail(sponsor.ContactEmail))
                throw new InvalidOperationException("El formato del email de contacto no es válido.");

            // Validar nombre duplicado
            if (await _sponsorRepository.ExistsByNameAsync(sponsor.Name))
                throw new InvalidOperationException($"Ya existe un patrocinador con el nombre '{sponsor.Name}'.");

            return await _sponsorRepository.CreateAsync(sponsor);
        }

        public async Task UpdateAsync(int id, Sponsor sponsor)
        {
            var existing = await _sponsorRepository.GetByIdAsync(id);
            if (existing == null)
                throw new KeyNotFoundException($"Sponsor con ID {id} no encontrado.");

            // Validar email
            if (!IsValidEmail(sponsor.ContactEmail))
                throw new InvalidOperationException("El formato del email de contacto no es válido.");

            // Validar nombre duplicado (excluyendo el propio)
            if (await _sponsorRepository.ExistsByNameAsync(sponsor.Name, id))
                throw new InvalidOperationException($"Ya existe otro patrocinador con el nombre '{sponsor.Name}'.");

            // Actualizar propiedades
            existing.Name = sponsor.Name;
            existing.ContactEmail = sponsor.ContactEmail;
            existing.Phone = sponsor.Phone;
            existing.WebsiteUrl = sponsor.WebsiteUrl;
            existing.Category = sponsor.Category;

            await _sponsorRepository.UpdateAsync(existing);
        }

        public async Task DeleteAsync(int id)
        {
            if (!await _sponsorRepository.ExistsAsync(id))
                throw new KeyNotFoundException($"Sponsor con ID {id} no encontrado.");

            await _sponsorRepository.DeleteAsync(id);
        }

        public async Task<TournamentSponsor> LinkSponsorToTournamentAsync(int sponsorId, int tournamentId, decimal contractAmount)
        {
            // Validar que sponsor y torneo existen
            var sponsor = await _sponsorRepository.GetByIdAsync(sponsorId);
            if (sponsor == null)
                throw new KeyNotFoundException($"Sponsor con ID {sponsorId} no encontrado.");

            var tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
            if (tournament == null)
                throw new KeyNotFoundException($"Torneo con ID {tournamentId} no encontrado.");

            // Validar monto
            if (contractAmount <= 0)
                throw new InvalidOperationException("El monto del contrato debe ser mayor a cero.");

            // Validar que no esté ya vinculado
            var existingLink = await _tournamentSponsorRepository.GetByTournamentAndSponsorAsync(tournamentId, sponsorId);
            if (existingLink != null)
                throw new InvalidOperationException("Este patrocinador ya está vinculado a este torneo.");

            var tournamentSponsor = new TournamentSponsor
            {
                TournamentId = tournamentId,
                SponsorId = sponsorId,
                ContractAmount = contractAmount,
                JoinedAt = DateTime.UtcNow
            };

            return await _tournamentSponsorRepository.CreateAsync(tournamentSponsor);
        }

        public async Task UnlinkSponsorFromTournamentAsync(int sponsorId, int tournamentId)
        {
            var link = await _tournamentSponsorRepository.GetByTournamentAndSponsorAsync(tournamentId, sponsorId);
            if (link == null)
                throw new KeyNotFoundException("La vinculación no existe.");

            await _tournamentSponsorRepository.DeleteAsync(link.Id);
        }

        public async Task<IEnumerable<TournamentSponsor>> GetTournamentsBySponsorAsync(int sponsorId)
        {
            if (!await _sponsorRepository.ExistsAsync(sponsorId))
                throw new KeyNotFoundException($"Sponsor con ID {sponsorId} no encontrado.");

            return await _tournamentSponsorRepository.GetBySponsorIdAsync(sponsorId);
        }

        private bool IsValidEmail(string email)
        {
            // Validación simple con regex
            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return regex.IsMatch(email);
        }
    }
}