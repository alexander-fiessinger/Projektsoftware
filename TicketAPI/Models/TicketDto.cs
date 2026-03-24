using System.ComponentModel.DataAnnotations;

namespace TicketAPI.Models
{
    public class CreateTicketDto
    {
        [Required(ErrorMessage = "Name ist erforderlich")]
        [StringLength(255, ErrorMessage = "Name darf maximal 255 Zeichen lang sein")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "E-Mail ist erforderlich")]
        [EmailAddress(ErrorMessage = "Ungültige E-Mail-Adresse")]
        [StringLength(255, ErrorMessage = "E-Mail darf maximal 255 Zeichen lang sein")]
        public string CustomerEmail { get; set; }

        [StringLength(50, ErrorMessage = "Telefon darf maximal 50 Zeichen lang sein")]
        public string CustomerPhone { get; set; }

        [Required(ErrorMessage = "Betreff ist erforderlich")]
        [StringLength(255, ErrorMessage = "Betreff darf maximal 255 Zeichen lang sein")]
        [MinLength(5, ErrorMessage = "Betreff muss mindestens 5 Zeichen lang sein")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Beschreibung ist erforderlich")]
        [MinLength(20, ErrorMessage = "Beschreibung muss mindestens 20 Zeichen lang sein")]
        public string Description { get; set; }

        [Range(0, 4, ErrorMessage = "Ungültige Kategorie")]
        public int Category { get; set; } = 0;

        [Range(0, 3, ErrorMessage = "Ungültige Priorität")]
        public int Priority { get; set; } = 1;
    }

    public class TicketResponseDto
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string Subject { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public string Category { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; }
    }
}
