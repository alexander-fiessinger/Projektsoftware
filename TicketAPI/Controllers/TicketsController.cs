using Microsoft.AspNetCore.Mvc;
using TicketAPI.Models;
using TicketAPI.Services;

namespace TicketAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly DatabaseService _databaseService;
        private readonly ILogger<TicketsController> _logger;

        public TicketsController(DatabaseService databaseService, ILogger<TicketsController> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        /// <summary>
        /// Erstellt ein neues Support-Ticket
        /// </summary>
        /// <param name="dto">Ticket-Daten vom Formular</param>
        /// <returns>Ticket-Bestätigung mit Ticketnummer</returns>
        [HttpPost]
        [ProducesResponseType(typeof(TicketResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TicketResponseDto>> CreateTicket([FromBody] CreateTicketDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new 
                    { 
                        success = false,
                        message = "Validierungsfehler", 
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) 
                    });
                }

                // IP-Adresse und User-Agent erfassen
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                var ticket = new Ticket
                {
                    CustomerName = dto.CustomerName.Trim(),
                    CustomerEmail = dto.CustomerEmail.Trim(),
                    CustomerPhone = dto.CustomerPhone?.Trim() ?? "",
                    Subject = dto.Subject.Trim(),
                    Description = dto.Description.Trim(),
                    Category = (TicketCategory)dto.Category,
                    Priority = (TicketPriority)dto.Priority,
                    Status = TicketStatus.New,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.Now
                };

                ticket.Id = await _databaseService.AddTicketAsync(ticket);

                _logger.LogInformation(
                    "Neues Ticket erstellt: #{TicketId} von {CustomerEmail}", 
                    ticket.Id, 
                    ticket.CustomerEmail);

                var response = new TicketResponseDto
                {
                    Id = ticket.Id,
                    TicketNumber = $"#{ticket.Id.ToString().PadLeft(6, '0')}",
                    CustomerName = ticket.CustomerName,
                    CustomerEmail = ticket.CustomerEmail,
                    Subject = ticket.Subject,
                    Status = "Neu",
                    Priority = GetPriorityText(ticket.Priority),
                    Category = GetCategoryText(ticket.Category),
                    CreatedAt = ticket.CreatedAt,
                    Message = "Ihr Support-Ticket wurde erfolgreich erstellt. Wir werden uns schnellstmöglich bei Ihnen melden."
                };

                return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, new { success = true, data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Erstellen eines Tickets");
                return StatusCode(500, new 
                { 
                    success = false,
                    message = "Ein Fehler ist aufgetreten. Bitte versuchen Sie es später erneut oder kontaktieren Sie uns telefonisch." 
                });
            }
        }

        /// <summary>
        /// Ruft ein Ticket anhand der ID ab
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Ticket), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Ticket>> GetTicket(int id)
        {
            try
            {
                var ticket = await _databaseService.GetTicketByIdAsync(id);
                if (ticket == null)
                {
                    return NotFound(new { success = false, message = "Ticket nicht gefunden" });
                }

                return Ok(new { success = true, data = ticket });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Abrufen des Tickets {TicketId}", id);
                return StatusCode(500, new { success = false, message = "Ein Fehler ist aufgetreten" });
            }
        }

        /// <summary>
        /// Testet die Datenbankverbindung
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var isConnected = await _databaseService.TestConnectionAsync();
                if (isConnected)
                {
                    return Ok(new { success = true, message = "API ist bereit", database = "Verbunden" });
                }
                else
                {
                    return StatusCode(503, new { success = false, message = "Datenbankverbindung fehlgeschlagen" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health Check fehlgeschlagen");
                return StatusCode(503, new { success = false, message = "Service nicht verfügbar" });
            }
        }

        private string GetPriorityText(TicketPriority priority) => priority switch
        {
            TicketPriority.Low => "Niedrig",
            TicketPriority.Medium => "Mittel",
            TicketPriority.High => "Hoch",
            TicketPriority.Urgent => "Dringend",
            _ => "Unbekannt"
        };

        private string GetCategoryText(TicketCategory category) => category switch
        {
            TicketCategory.General => "Allgemein",
            TicketCategory.Technical => "Technisch",
            TicketCategory.Billing => "Abrechnung",
            TicketCategory.FeatureRequest => "Feature-Anfrage",
            TicketCategory.Bug => "Fehler",
            _ => "Unbekannt"
        };
    }
}
