using Projektsoftware.Api.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Projektsoftware.Api.Services
{
    /// <summary>
    /// Serverseitiger PDF-Export für Ticket-Listen (QuestPDF), analog zum WPF-PdfExportService.
    /// </summary>
    public class TicketPdfService
    {
        public TicketPdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] Generate(List<TicketDto> tickets, string title = "Support-Tickets")
        {
            tickets ??= new List<TicketDto>();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .Background(Colors.Blue.Medium)
                        .Padding(15)
                        .Column(column =>
                        {
                            column.Item().Text(title).FontSize(24).Bold().FontColor(Colors.White);
                            column.Item().Text($"Erstellt am: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(10).FontColor(Colors.White);
                            column.Item().Text($"Anzahl Tickets: {tickets.Count}").FontSize(10).FontColor(Colors.White);
                        });

                    page.Content()
                        .PaddingVertical(10)
                        .Column(column =>
                        {
                            column.Item().PaddingBottom(10).Background(Colors.Grey.Lighten3).Padding(10).Text(text =>
                            {
                                text.Span("Status-Übersicht  ").FontSize(12).Bold();
                                text.Span($"Neu: {tickets.Count(t => t.Status == 0)} | ");
                                text.Span($"Offen: {tickets.Count(t => t.Status == 1)} | ");
                                text.Span($"In Bearbeitung: {tickets.Count(t => t.Status == 2)} | ");
                                text.Span($"Gelöst: {tickets.Count(t => t.Status == 4)} | ");
                                text.Span($"Geschlossen: {tickets.Count(t => t.Status == 5)}");
                            });

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(45);   // #
                                    columns.ConstantColumn(80);   // Datum
                                    columns.RelativeColumn(1.2f);  // Kunde
                                    columns.RelativeColumn(1.6f);  // Betreff
                                    columns.ConstantColumn(60);   // Priorität
                                    columns.ConstantColumn(85);   // Status
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCell).Text("#").Bold().FontColor(Colors.White);
                                    header.Cell().Element(HeaderCell).Text("Erstellt").Bold().FontColor(Colors.White);
                                    header.Cell().Element(HeaderCell).Text("Kunde").Bold().FontColor(Colors.White);
                                    header.Cell().Element(HeaderCell).Text("Betreff").Bold().FontColor(Colors.White);
                                    header.Cell().Element(HeaderCell).Text("Priorität").Bold().FontColor(Colors.White);
                                    header.Cell().Element(HeaderCell).Text("Status").Bold().FontColor(Colors.White);

                                    static IContainer HeaderCell(IContainer c) =>
                                        c.Background(Colors.Blue.Medium).Padding(5).AlignMiddle();
                                });

                                foreach (var ticket in tickets)
                                {
                                    var bg = RowColor(ticket);
                                    table.Cell().Element(c => Cell(c, bg)).Text($"#{ticket.Id}").FontSize(9);
                                    table.Cell().Element(c => Cell(c, bg)).Text(ticket.CreatedAt.ToString("dd.MM.yy HH:mm")).FontSize(8);
                                    table.Cell().Element(c => Cell(c, bg)).Text(ticket.CustomerName).FontSize(9);
                                    table.Cell().Element(c => Cell(c, bg)).Text(Truncate(ticket.Subject, 50)).FontSize(9);
                                    table.Cell().Element(c => Cell(c, bg)).Text(ticket.PriorityText).FontSize(9);
                                    table.Cell().Element(c => Cell(c, bg)).Text(ticket.StatusText).FontSize(9);
                                }

                                static IContainer Cell(IContainer c, string bg) =>
                                    c.Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignMiddle();
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Seite ");
                            text.CurrentPageNumber();
                            text.Span(" von ");
                            text.TotalPages();
                        });
                });
            })
            .GeneratePdf();
        }

        private static string RowColor(TicketDto t) => t.Priority switch
        {
            3 => Colors.Red.Lighten4,
            2 => Colors.Orange.Lighten4,
            _ => Colors.White
        };

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length <= max ? text : text[..max] + "...";
        }
    }
}
