using Projektsoftware.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Service für PDF-Export von Tickets
    /// </summary>
    public class PdfExportService
    {
        public PdfExportService()
        {
            // Lizenz für QuestPDF setzen (Community-Lizenz für nicht-kommerzielle Nutzung)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Exportiert eine Liste von Tickets als PDF
        /// </summary>
        public void ExportTicketsToPdf(List<Ticket> tickets, string filename, string title = "Support-Tickets")
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header
                    page.Header()
                        .Background(Colors.Blue.Medium)
                        .Padding(15)
                        .Column(column =>
                        {
                            column.Item().Text(title)
                                .FontSize(24)
                                .Bold()
                                .FontColor(Colors.White);

                            column.Item().Text($"Erstellt am: {DateTime.Now:dd.MM.yyyy HH:mm}")
                                .FontSize(10)
                                .FontColor(Colors.White);

                            column.Item().Text($"Anzahl Tickets: {tickets.Count}")
                                .FontSize(10)
                                .FontColor(Colors.White);
                        });

                    // Content
                    page.Content()
                        .PaddingVertical(10)
                        .Column(column =>
                        {
                            // Zusammenfassung
                            column.Item().PaddingBottom(10).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Background(Colors.Grey.Lighten3).Padding(10).Text(text =>
                                    {
                                        text.Span("Status-Übersicht\n").FontSize(12).Bold();
                                        text.Span($"Neu: {tickets.Count(t => t.Status == TicketStatus.New)} | ");
                                        text.Span($"In Bearbeitung: {tickets.Count(t => t.Status == TicketStatus.InProgress)} | ");
                                        text.Span($"Gelöst: {tickets.Count(t => t.Status == TicketStatus.Resolved)} | ");
                                        text.Span($"Geschlossen: {tickets.Count(t => t.Status == TicketStatus.Closed)}");
                                    });
                                });
                            });

                            // Tickets Tabelle
                            column.Item().Table(table =>
                            {
                                // Spalten definieren
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(60);  // Ticket#
                                    columns.ConstantColumn(80);  // Datum
                                    columns.RelativeColumn(1.2f); // Kunde
                                    columns.RelativeColumn(1.5f); // Betreff
                                    columns.ConstantColumn(70);  // Priorität
                                    columns.ConstantColumn(80);  // Status
                                });

                                // Header
                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Ticket#").Bold();
                                    header.Cell().Element(CellStyle).Text("Erstellt").Bold();
                                    header.Cell().Element(CellStyle).Text("Kunde").Bold();
                                    header.Cell().Element(CellStyle).Text("Betreff").Bold();
                                    header.Cell().Element(CellStyle).Text("Priorität").Bold();
                                    header.Cell().Element(CellStyle).Text("Status").Bold();

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container
                                            .Background(Colors.Blue.Medium)
                                            .Padding(5)
                                            .AlignMiddle();
                                    }
                                });

                                // Daten
                                foreach (var ticket in tickets)
                                {
                                    var bgColor = GetRowColor(ticket);
                                    
                                    table.Cell().Element(c => CellStyle(c, bgColor)).Text(ticket.TicketNumber).FontSize(9);
                                    table.Cell().Element(c => CellStyle(c, bgColor)).Text(ticket.CreatedAt.ToString("dd.MM.yy\nHH:mm")).FontSize(8);
                                    table.Cell().Element(c => CellStyle(c, bgColor)).Text(ticket.CustomerName).FontSize(9);
                                    table.Cell().Element(c => CellStyle(c, bgColor)).Text(TruncateText(ticket.Subject, 50)).FontSize(9);
                                    table.Cell().Element(c => CellStyle(c, bgColor)).Text(ticket.PriorityText).FontSize(9);
                                    table.Cell().Element(c => CellStyle(c, bgColor)).Text(ticket.StatusText).FontSize(9);
                                }

                                static IContainer CellStyle(IContainer container, string color)
                                {
                                    return container
                                        .Background(color)
                                        .BorderBottom(1, Unit.Point)
                                        .BorderColor(Colors.Grey.Lighten1)
                                        .Padding(5)
                                        .AlignMiddle();
                                }
                            });
                        });

                    // Footer
                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Seite ");
                            x.CurrentPageNumber();
                            x.Span(" von ");
                            x.TotalPages();
                        });
                });
            })
            .GeneratePdf(filename);
        }

        /// <summary>
        /// Exportiert ein einzelnes Ticket mit allen Details als PDF
        /// </summary>
        public void ExportTicketDetailToPdf(Ticket ticket, List<TicketComment> comments, 
            List<TicketTimeLog> timeLogs, string filename)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // Header
                    page.Header()
                        .Background(Colors.Blue.Medium)
                        .Padding(15)
                        .Column(column =>
                        {
                            column.Item().Text($"Ticket {ticket.TicketNumber}")
                                .FontSize(28)
                                .Bold()
                                .FontColor(Colors.White);

                            column.Item().Text(ticket.Subject)
                                .FontSize(14)
                                .FontColor(Colors.White);

                            column.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text($"Status: {ticket.StatusText}")
                                    .FontSize(10)
                                    .FontColor(Colors.White);
                                row.RelativeItem().AlignRight().Text($"Erstellt: {ticket.CreatedAt:dd.MM.yyyy HH:mm}")
                                    .FontSize(10)
                                    .FontColor(Colors.White);
                            });
                        });

                    // Content
                    page.Content()
                        .PaddingVertical(10)
                        .Column(column =>
                        {
                            // Kundendaten
                            column.Item().PaddingBottom(15).Column(col =>
                            {
                                col.Item().Text("Kundendaten").FontSize(16).Bold();
                                col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                col.Item().PaddingTop(10).Text(text =>
                                {
                                    text.Line($"Name: {ticket.CustomerName}");
                                    text.Line($"E-Mail: {ticket.CustomerEmail}");
                                    if (!string.IsNullOrEmpty(ticket.CustomerPhone))
                                        text.Line($"Telefon: {ticket.CustomerPhone}");
                                });
                            });

                            // Ticket-Details
                            column.Item().PaddingBottom(15).Column(col =>
                            {
                                col.Item().Text("Ticket-Details").FontSize(16).Bold();
                                col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                col.Item().PaddingTop(10).Row(row =>
                                {
                                    row.RelativeItem().Text($"Priorität: {ticket.PriorityText}");
                                    row.RelativeItem().Text($"Kategorie: {ticket.CategoryText}");
                                    row.RelativeItem().Text($"Status: {ticket.StatusText}");
                                });
                                if (!string.IsNullOrEmpty(ticket.AssignedToEmployeeName))
                                {
                                    col.Item().PaddingTop(5).Text($"Zugewiesen an: {ticket.AssignedToEmployeeName}");
                                }
                            });

                            // Beschreibung
                            column.Item().PaddingBottom(15).Column(col =>
                            {
                                col.Item().Text("Beschreibung").FontSize(16).Bold();
                                col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                col.Item().PaddingTop(10).Text(ticket.Description);
                            });

                            // Resolution (falls vorhanden)
                            if (!string.IsNullOrEmpty(ticket.Resolution))
                            {
                                column.Item().PaddingBottom(15).Column(col =>
                                {
                                    col.Item().Text("Loesung").FontSize(16).Bold();
                                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                    col.Item().PaddingTop(10).Text(ticket.Resolution);
                                    if (ticket.ResolvedAt.HasValue)
                                    {
                                        col.Item().PaddingTop(5).Text($"Gelöst am: {ticket.ResolvedAt:dd.MM.yyyy HH:mm}")
                                            .FontSize(10)
                                            .Italic();
                                    }
                                });
                            }

                            // Kommentare
                            if (comments != null && comments.Count > 0)
                            {
                                column.Item().PaddingBottom(15).Column(col =>
                                {
                                    col.Item().Text($"Kommentare ({comments.Count})").FontSize(16).Bold();
                                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                    
                                    foreach (var comment in comments)
                                    {
                                        col.Item().PaddingTop(10).Background(
                                            comment.IsInternal ? Colors.Yellow.Lighten3 : Colors.Blue.Lighten4
                                        ).Padding(10).Column(c =>
                                        {
                                            c.Item().Row(r =>
                                            {
                                                r.RelativeItem().Text(comment.EmployeeName).Bold();
                                                r.RelativeItem().AlignRight().Text(comment.CreatedAt.ToString("dd.MM.yyyy HH:mm"))
                                                    .FontSize(9);
                                            });
                                            c.Item().PaddingTop(3).Text(comment.CommentTypeText)
                                                .FontSize(9)
                                                .Italic()
                                                .FontColor(Colors.Grey.Darken2);
                                            c.Item().PaddingTop(5).Text(comment.Comment);
                                        });
                                    }
                                });
                            }

                            // Zeiterfassung
                            if (timeLogs != null && timeLogs.Count > 0)
                            {
                                var totalMinutes = timeLogs.Sum(t => t.MinutesSpent);
                                var totalHours = totalMinutes / 60;
                                var totalMins = totalMinutes % 60;

                                column.Item().PaddingBottom(15).Column(col =>
                                {
                                    col.Item().Text($"Zeiterfassung ({timeLogs.Count} Eintraege)").FontSize(16).Bold();
                                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                    col.Item().PaddingTop(10).Background(Colors.Green.Lighten4).Padding(10)
                                        .Text($"Gesamtzeit: {totalHours}h {totalMins}min").Bold().FontSize(14);
                                    
                                    col.Item().PaddingTop(10).Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(80);
                                            columns.RelativeColumn(1.5f);
                                            columns.RelativeColumn(2);
                                            columns.ConstantColumn(60);
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Text("Datum").Bold().FontSize(10);
                                            header.Cell().Text("Mitarbeiter").Bold().FontSize(10);
                                            header.Cell().Text("Beschreibung").Bold().FontSize(10);
                                            header.Cell().Text("Zeit").Bold().FontSize(10);
                                        });

                                        foreach (var log in timeLogs)
                                        {
                                            table.Cell().Text(log.LoggedAt.ToString("dd.MM.yyyy"));
                                            table.Cell().Text(log.EmployeeName);
                                            table.Cell().Text(log.Description ?? "-");
                                            table.Cell().Text(log.DurationText);
                                        }
                                    });
                                });
                            }

                            // Technische Infos
                            column.Item().PaddingTop(20).Column(col =>
                            {
                                col.Item().Text("Technische Informationen").FontSize(12).Bold();
                                col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                                col.Item().PaddingTop(5).Text($"IP-Adresse: {ticket.IpAddress ?? "Unbekannt"}")
                                    .FontSize(9)
                                    .FontColor(Colors.Grey.Darken1);
                                col.Item().Text($"User-Agent: {TruncateText(ticket.UserAgent ?? "Unbekannt", 80)}")
                                    .FontSize(8)
                                    .FontColor(Colors.Grey.Darken1);
                                col.Item().Text($"Ticket-ID: {ticket.Id}")
                                    .FontSize(9)
                                    .FontColor(Colors.Grey.Darken1);
                            });
                        });

                    // Footer
                    page.Footer()
                        .AlignCenter()
                        .DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Medium))
                        .Text(text =>
                        {
                            text.Span("Seite ");
                            text.CurrentPageNumber();
                            text.Span(" von ");
                            text.TotalPages();
                            text.Span(" • Projektierungssoftware Professional");
                        });
                });
            })
            .GeneratePdf(filename);
        }

        /// <summary>
        /// Exportiert Ticket-Statistiken als PDF
        /// </summary>
        public void ExportStatisticsToPdf(TicketStatistics stats, List<Ticket> recentTickets, string filename)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // Header
                    page.Header()
                        .Background(Colors.Blue.Medium)
                        .Padding(15)
                        .Column(column =>
                        {
                            column.Item().Text("Ticket-Dashboard Report")
                                .FontSize(24)
                                .Bold()
                                .FontColor(Colors.White);

                            column.Item().Text($"Berichtszeitraum: {DateTime.Now:dd.MM.yyyy HH:mm}")
                                .FontSize(11)
                                .FontColor(Colors.White);
                        });

                    // Content
                    page.Content()
                        .PaddingVertical(10)
                        .Column(column =>
                        {
                            // Übersicht Kacheln
                            column.Item().PaddingBottom(20).Row(row =>
                            {
                                row.RelativeItem().Padding(5).Column(col =>
                                {
                                    col.Item().Background(Colors.Blue.Medium).Padding(15).Column(c =>
                                    {
                                        c.Item().Text("Gesamt").FontColor(Colors.White).Bold();
                                        c.Item().Text(stats.TotalTickets.ToString()).FontSize(32).Bold().FontColor(Colors.White);
                                        c.Item().Text("Tickets").FontSize(10).FontColor(Colors.White);
                                    });
                                });

                                row.RelativeItem().Padding(5).Column(col =>
                                {
                                    col.Item().Background(Colors.Orange.Medium).Padding(15).Column(c =>
                                    {
                                        c.Item().Text("Neu").FontColor(Colors.White).Bold();
                                        c.Item().Text(stats.NewTickets.ToString()).FontSize(32).Bold().FontColor(Colors.White);
                                        c.Item().Text("Tickets").FontSize(10).FontColor(Colors.White);
                                    });
                                });

                                row.RelativeItem().Padding(5).Column(col =>
                                {
                                    col.Item().Background(Colors.Red.Medium).Padding(15).Column(c =>
                                    {
                                        c.Item().Text("Dringend").FontColor(Colors.White).Bold();
                                        c.Item().Text(stats.UrgentTickets.ToString()).FontSize(32).Bold().FontColor(Colors.White);
                                        c.Item().Text("Tickets").FontSize(10).FontColor(Colors.White);
                                    });
                                });
                            });

                            // Status-Verteilung
                            column.Item().PaddingBottom(15).Column(col =>
                            {
                                col.Item().Text("Status-Verteilung").FontSize(16).Bold();
                                col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                col.Item().PaddingTop(10).Row(row =>
                                {
                                    row.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text($"Neu: {stats.NewTickets}");
                                        c.Item().Text($"In Bearbeitung: {stats.InProgressTickets}");
                                        c.Item().Text($" Warten: {stats.WaitingTickets}");
                                    });
                                    row.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text($" Gelöst: {stats.ResolvedTickets}");
                                        c.Item().Text($" Geschlossen: {stats.ClosedTickets}");
                                        c.Item().Text($" Nicht zugewiesen: {stats.UnassignedTickets}");
                                    });
                                });
                            });

                            // Zeitliche Übersicht
                            column.Item().PaddingBottom(15).Column(col =>
                            {
                                col.Item().Text(" Zeitliche Übersicht").FontSize(16).Bold();
                                col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                col.Item().PaddingTop(10).Row(row =>
                                {
                                    row.RelativeItem().Background(Colors.Blue.Lighten4).Padding(10).Column(c =>
                                    {
                                        c.Item().Text("Heute").Bold();
                                        c.Item().Text(stats.TodayTickets.ToString()).FontSize(24).Bold();
                                    });
                                    row.RelativeItem().Background(Colors.Green.Lighten4).Padding(10).Column(c =>
                                    {
                                        c.Item().Text("Diese Woche").Bold();
                                        c.Item().Text(stats.WeekTickets.ToString()).FontSize(24).Bold();
                                    });
                                    row.RelativeItem().Background(Colors.Orange.Lighten4).Padding(10).Column(c =>
                                    {
                                        c.Item().Text("Dieser Monat").Bold();
                                        c.Item().Text(stats.MonthTickets.ToString()).FontSize(24).Bold();
                                    });
                                });
                            });

                            // Performance
                            if (stats.AverageResolutionTimeHours > 0)
                            {
                                column.Item().PaddingBottom(15).Background(Colors.Green.Lighten4).Padding(15).Column(col =>
                                {
                                    col.Item().Text(" Performance").FontSize(16).Bold();
                                    col.Item().PaddingTop(5).Text($"Durchschnittliche Bearbeitungszeit: {stats.AverageResolutionTimeText}")
                                        .FontSize(14);
                                });
                            }

                            // Neueste Tickets
                            if (recentTickets != null && recentTickets.Count > 0)
                            {
                                column.Item().PageBreak();
                                column.Item().PaddingTop(15).Column(col =>
                                {
                                    col.Item().Text(" Neueste Tickets").FontSize(16).Bold();
                                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                    
                                    col.Item().PaddingTop(10).Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(60);
                                            columns.ConstantColumn(80);
                                            columns.RelativeColumn();
                                            columns.ConstantColumn(70);
                                            columns.ConstantColumn(80);
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Ticket#").Bold();
                                            header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Datum").Bold();
                                            header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Betreff").Bold();
                                            header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Priorität").Bold();
                                            header.Cell().Background(Colors.Grey.Medium).Padding(5).Text("Status").Bold();
                                        });

                                        foreach (var t in recentTickets.Take(20))
                                        {
                                            table.Cell().Padding(5).Text(t.TicketNumber).FontSize(9);
                                            table.Cell().Padding(5).Text(t.CreatedAt.ToString("dd.MM.yy")).FontSize(9);
                                            table.Cell().Padding(5).Text(TruncateText(t.Subject, 40)).FontSize(9);
                                            table.Cell().Padding(5).Text(t.PriorityText).FontSize(9);
                                            table.Cell().Padding(5).Text(t.StatusText).FontSize(9);
                                        }
                                    });
                                });
                            }
                        });

                    // Footer
                    page.Footer()
                        .AlignCenter()
                        .DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Medium))
                        .Text(text =>
                        {
                            text.Span("Seite ");
                            text.CurrentPageNumber();
                            text.Span(" von ");
                            text.TotalPages();
                            text.Span(" • Erstellt am " + DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                        });
                });
            })
            .GeneratePdf(filename);
        }

        private string GetRowColor(Ticket ticket)
        {
            // Priorität hat Vorrang
            if (ticket.Priority == TicketPriority.Urgent)
                return Colors.Red.Lighten4;
            if (ticket.Priority == TicketPriority.High)
                return Colors.Orange.Lighten4;
            
            // Dann Status
            if (ticket.Status == TicketStatus.Resolved)
                return Colors.Green.Lighten4;
            if (ticket.Status == TicketStatus.Closed)
                return Colors.Grey.Lighten3;

            return Colors.White;
        }

        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            
            if (text.Length <= maxLength)
                return text;
            
            return text.Substring(0, maxLength - 3) + "...";
        }
    }
}
