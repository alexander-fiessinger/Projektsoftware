using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Projektsoftware.Api.Services
{
    /// <summary>
    /// Eingabemodell für den Briefgenerator (DIN 5008, Fensterumschlag-optimiert).
    /// </summary>
    public class LetterRequest
    {
        // Absender
        public string SenderCompany { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderStreet { get; set; } = string.Empty;
        public string SenderZipCity { get; set; } = string.Empty;
        public string SenderPhone { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;

        // Empfänger
        public string RecipientCompany { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientStreet { get; set; } = string.Empty;
        public string RecipientZipCity { get; set; } = string.Empty;

        // Brief-Metadaten
        public string Subject { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Today;
        public string Reference { get; set; } = string.Empty;

        // Inhalt
        public string Salutation { get; set; } = "Sehr geehrte Damen und Herren,";
        public string Body { get; set; } = string.Empty;
        public string Closing { get; set; } = "Mit freundlichen Grüßen";
        public string SignatureName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Erzeugt Geschäftsbriefe als PDF nach DIN 5008 (serverseitig für Blazor).
    /// </summary>
    public class PdfLetterService
    {
        private static readonly CultureInfo De = new("de-DE");

        private const string Primary = "#1B365D";
        private const string Accent = "#C8A251";
        private const string TextDark = "#222222";
        private const string TextMuted = "#6B7280";
        private const string LineColor = "#BFBFBF";
        private const string FoldMarkColor = "#D1D5DB";
        private const string InfoBg = "#F7F8FA";

        private const float MarginLeft = 25f;
        private const float MarginRight = 10f;
        private const float FoldMarkLen = 4f;
        private const float AddressFieldWidth = 85f;

        public PdfLetterService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] Generate(LetterRequest data)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginLeft(MarginLeft, Unit.Millimetre);
                    page.MarginRight(MarginRight, Unit.Millimetre);
                    page.MarginTop(0);
                    page.MarginBottom(0);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x
                        .FontSize(10f)
                        .FontFamily("Arial")
                        .FontColor(TextDark)
                        .LineHeight(1.35f));

                    page.Content().Element(c => ComposeContent(c, data));
                    page.Footer().Element(c => ComposeFooter(c, data));
                    page.Foreground().Element(ComposeFoldMarks);
                });
            })
            .GeneratePdf();
        }

        private static void ComposeFoldMarks(IContainer container)
        {
            container.Layers(layers =>
            {
                layers.PrimaryLayer();

                foreach (float y in new[] { 105f, 148.5f, 210f })
                {
                    layers.Layer()
                        .TranslateX(-MarginLeft, Unit.Millimetre)
                        .TranslateY(y, Unit.Millimetre)
                        .Width(FoldMarkLen, Unit.Millimetre)
                        .Height(0.25f, Unit.Millimetre)
                        .Background(FoldMarkColor);
                }
            });
        }

        private void ComposeContent(IContainer container, LetterRequest data)
        {
            container.Column(page =>
            {
                page.Item()
                    .Height(2.5f, Unit.Millimetre)
                    .Background(Accent);

                if (!string.IsNullOrWhiteSpace(data.SenderCompany))
                {
                    page.Item()
                        .PaddingTop(12, Unit.Millimetre)
                        .Text(data.SenderCompany)
                        .FontSize(16).Bold().FontColor(Primary);
                }

                page.Item()
                    .PaddingTop(20, Unit.Millimetre)
                    .Row(topRow =>
                    {
                        topRow.ConstantItem(AddressFieldWidth, Unit.Millimetre).Column(left =>
                        {
                            left.Item()
                                .Text($"{data.SenderCompany}, {data.SenderStreet} – {data.SenderZipCity}")
                                .FontSize(6).FontColor(TextMuted);

                            left.Item()
                                .PaddingTop(1, Unit.Millimetre)
                                .PaddingBottom(3, Unit.Millimetre)
                                .Width(75, Unit.Millimetre)
                                .LineHorizontal(0.75f)
                                .LineColor(Accent);

                            left.Item().Column(addr =>
                            {
                                if (!string.IsNullOrWhiteSpace(data.RecipientCompany))
                                    addr.Item().Text(data.RecipientCompany).FontSize(10).Bold();
                                if (!string.IsNullOrWhiteSpace(data.RecipientName))
                                    addr.Item().Text(data.RecipientName).FontSize(10);
                                if (!string.IsNullOrWhiteSpace(data.RecipientStreet))
                                    addr.Item().Text(data.RecipientStreet).FontSize(10);
                                if (!string.IsNullOrWhiteSpace(data.RecipientZipCity))
                                    addr.Item().Text(data.RecipientZipCity).FontSize(10);
                            });
                        });

                        topRow.RelativeItem()
                            .PaddingLeft(8, Unit.Millimetre)
                            .Background(InfoBg)
                            .Border(0.5f)
                            .BorderColor(LineColor)
                            .Padding(8)
                            .Column(info =>
                            {
                                InfoRow(info, "Ansprechpartner:", data.SenderName);
                                if (!string.IsNullOrWhiteSpace(data.SenderPhone))
                                    InfoRow(info, "Telefon:", data.SenderPhone);
                                if (!string.IsNullOrWhiteSpace(data.SenderEmail))
                                    InfoRow(info, "E-Mail:", data.SenderEmail);
                                InfoRow(info, "Datum:", data.Date.ToString("dd.MM.yyyy", De));
                                if (!string.IsNullOrWhiteSpace(data.Reference))
                                    InfoRow(info, "Zeichen:", data.Reference);
                            });
                    });

                if (!string.IsNullOrWhiteSpace(data.Subject))
                {
                    page.Item()
                        .PaddingTop(8.4f, Unit.Millimetre)
                        .Text(data.Subject)
                        .FontSize(10).Bold().FontColor(Primary);
                }

                page.Item()
                    .PaddingTop(4.2f, Unit.Millimetre)
                    .Text(data.Salutation).FontSize(10);

                page.Item()
                    .PaddingTop(4.2f, Unit.Millimetre)
                    .Text(data.Body).FontSize(10).LineHeight(1.5f);

                page.Item()
                    .PaddingTop(4.2f, Unit.Millimetre)
                    .Text(data.Closing).FontSize(10);

                page.Item()
                    .PaddingTop(12.6f, Unit.Millimetre)
                    .Column(sig =>
                    {
                        var name = !string.IsNullOrWhiteSpace(data.SignatureName)
                            ? data.SignatureName : data.SenderName;
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            sig.Item().Text(name).FontSize(10).Bold().FontColor(Primary);
                            sig.Item()
                                .PaddingTop(1)
                                .Width(50, Unit.Millimetre)
                                .LineHorizontal(0.5f)
                                .LineColor(Accent);
                        }
                    });
            });
        }

        private static void InfoRow(ColumnDescriptor col, string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            col.Item().Row(row =>
            {
                row.ConstantItem(28, Unit.Millimetre)
                    .Text(label).FontSize(8).FontColor(TextMuted);
                row.RelativeItem()
                    .Text(value).FontSize(8).FontColor(TextDark);
            });
        }

        private void ComposeFooter(IContainer container, LetterRequest data)
        {
            container
                .PaddingBottom(10, Unit.Millimetre)
                .Column(footer =>
                {
                    footer.Item()
                        .PaddingBottom(2, Unit.Millimetre)
                        .Width(170, Unit.Millimetre)
                        .LineHorizontal(0.5f)
                        .LineColor(LineColor);

                    footer.Item().Row(row =>
                    {
                        row.RelativeItem().Text(data.SenderCompany)
                            .FontSize(7).FontColor(TextMuted);
                        row.RelativeItem().AlignRight().Text($"{data.SenderStreet} · {data.SenderZipCity}")
                            .FontSize(7).FontColor(TextMuted);
                    });
                });
        }

        // ════════════════════════════════════════════════════════════
        // REPORTS (Tickets & Statistik)
        // ════════════════════════════════════════════════════════════

        public byte[] GenerateTicketReport(IReadOnlyList<Models.TicketDto> tickets, string title = "Support-Tickets")
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigureReportPage(page);
                    page.Header().Element(c => ComposeReportHeader(c, title, $"{tickets.Count} Tickets"));
                    page.Content().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(28);   // ID
                            cols.RelativeColumn(3);     // Betreff
                            cols.RelativeColumn(2);     // Kunde
                            cols.ConstantColumn(60);   // Priorität
                            cols.ConstantColumn(75);   // Status
                            cols.ConstantColumn(60);   // Erstellt
                        });

                        table.Header(header =>
                        {
                            HeaderCell(header, "Nr.");
                            HeaderCell(header, "Betreff");
                            HeaderCell(header, "Kunde");
                            HeaderCell(header, "Priorität");
                            HeaderCell(header, "Status");
                            HeaderCell(header, "Erstellt");
                        });

                        var zebra = false;
                        foreach (var t in tickets)
                        {
                            var bg = zebra ? InfoBg : "#FFFFFF";
                            zebra = !zebra;
                            BodyCell(table, bg, t.Id.ToString());
                            BodyCell(table, bg, t.Subject);
                            BodyCell(table, bg, t.CustomerName);
                            BodyCell(table, bg, t.PriorityText);
                            BodyCell(table, bg, t.StatusText);
                            BodyCell(table, bg, t.CreatedAt.ToString("dd.MM.yyyy", De));
                        }
                    });
                    page.Footer().Element(ComposeReportFooter);
                });
            })
            .GeneratePdf();
        }

        public byte[] GenerateStatisticsReport(Models.AnalyticsDto stats, string title = "Auswertung")
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigureReportPage(page);
                    page.Header().Element(c => ComposeReportHeader(c, title, DateTime.Now.ToString("dd.MM.yyyy HH:mm", De)));
                    page.Content().PaddingTop(8).Column(col =>
                    {
                        SectionTitle(col, "Tickets");
                        StatLine(col, "Gesamt", stats.TicketsTotal.ToString());
                        StatLine(col, "Offen", stats.TicketsOpen.ToString());
                        StatLine(col, "Gelöst", stats.TicketsResolved.ToString());
                        StatLine(col, "Lösungsquote", $"{stats.TicketResolveRate.ToString("0.#", De)} %");

                        SectionTitle(col, "Vertrieb / Leads");
                        StatLine(col, "Gesamt", stats.LeadsTotal.ToString());
                        StatLine(col, "Neu", stats.LeadsNew.ToString());
                        StatLine(col, "In Bearbeitung", stats.LeadsInProgress.ToString());
                        StatLine(col, "Gewonnen", stats.LeadsWon.ToString());
                        StatLine(col, "Verloren", stats.LeadsLost.ToString());
                        StatLine(col, "Conversion-Rate", $"{stats.LeadConversionRate.ToString("0.#", De)} %");

                        SectionTitle(col, "Aufgaben");
                        StatLine(col, "Gesamt", stats.TasksTotal.ToString());
                        StatLine(col, "Offen", stats.TasksOpen.ToString());
                        StatLine(col, "Erledigt", stats.TasksDone.ToString());
                        StatLine(col, "Überfällig", stats.TasksOverdue.ToString());

                        if (stats.ProjectsByStatus.Count > 0)
                        {
                            SectionTitle(col, "Projekte nach Status");
                            foreach (var b in stats.ProjectsByStatus)
                                StatLine(col, b.Label, b.Count.ToString());
                        }
                    });
                    page.Footer().Element(ComposeReportFooter);
                });
            })
            .GeneratePdf();
        }

        private static void ConfigureReportPage(PageDescriptor page)
        {
            page.Size(PageSizes.A4);
            page.Margin(20, Unit.Millimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x
                .FontSize(9f)
                .FontFamily("Arial")
                .FontColor(TextDark)
                .LineHeight(1.25f));
        }

        private static void ComposeReportHeader(IContainer container, string title, string subtitle)
        {
            container.Column(col =>
            {
                col.Item().Height(2.5f, Unit.Millimetre).Background(Accent);
                col.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(title).FontSize(16).Bold().FontColor(Primary);
                        c.Item().Text(subtitle).FontSize(8).FontColor(TextMuted);
                    });
                });
                col.Item().PaddingTop(4)
                    .LineHorizontal(0.75f).LineColor(Accent);
            });
        }

        private static void ComposeReportFooter(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Erstellt am ").FontSize(7).FontColor(TextMuted);
                    t.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm", De)).FontSize(7).FontColor(TextMuted);
                });
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span("Seite ").FontSize(7).FontColor(TextMuted);
                    t.CurrentPageNumber().FontSize(7).FontColor(TextMuted);
                    t.Span(" / ").FontSize(7).FontColor(TextMuted);
                    t.TotalPages().FontSize(7).FontColor(TextMuted);
                });
            });
        }

        private static void HeaderCell(TableCellDescriptor header, string text)
        {
            header.Cell()
                .Background(Primary)
                .Padding(4)
                .Text(text).FontSize(8).Bold().FontColor(Colors.White);
        }

        private static void BodyCell(TableDescriptor table, string background, string text)
        {
            table.Cell()
                .Background(background)
                .BorderBottom(0.5f).BorderColor(LineColor)
                .Padding(4)
                .Text(text).FontSize(8);
        }

        private static void SectionTitle(ColumnDescriptor col, string text)
        {
            col.Item().PaddingTop(8).PaddingBottom(2)
                .Text(text).FontSize(11).Bold().FontColor(Primary);
        }

        private static void StatLine(ColumnDescriptor col, string label, string value)
        {
            col.Item()
                .BorderBottom(0.5f).BorderColor(LineColor)
                .PaddingVertical(2)
                .Row(row =>
                {
                    row.RelativeItem().Text(label).FontSize(9).FontColor(TextDark);
                    row.ConstantItem(80).AlignRight().Text(value).FontSize(9).Bold().FontColor(Primary);
                });
        }

        // ════════════════════════════════════════════════════════════
        // BESTELLBELEG (Einkauf)
        // ════════════════════════════════════════════════════════════

        public byte[] GeneratePurchaseOrderReport(Models.PurchaseOrderDto order)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigureReportPage(page);

                    page.Header().Column(col =>
                    {
                        col.Item().Height(2.5f, Unit.Millimetre).Background(Accent);
                        col.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Bestellbeleg").FontSize(18).Bold().FontColor(Primary);
                                var nr = string.IsNullOrWhiteSpace(order.OrderNumber)
                                    ? $"#{order.Id}" : order.OrderNumber;
                                c.Item().Text($"Bestellnummer: {nr}").FontSize(9).FontColor(TextMuted);
                            });
                        });
                        col.Item().PaddingTop(4).LineHorizontal(0.75f).LineColor(Accent);
                    });

                    page.Content().PaddingTop(10).Column(column =>
                    {
                        column.Item().PaddingBottom(12).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Lieferant").FontSize(9).Bold().FontColor(TextMuted);
                                col.Item().Text(order.SupplierName).FontSize(11);
                            });
                            row.ConstantItem(200).Column(col =>
                            {
                                col.Item().Text("Bestelldatum").FontSize(9).Bold().FontColor(TextMuted);
                                col.Item().Text(order.OrderDate.ToString("dd.MM.yyyy", De)).FontSize(11);
                                if (order.DeliveryDateExpected.HasValue)
                                {
                                    col.Item().PaddingTop(4).Text("Liefertermin (erwartet)").FontSize(9).Bold().FontColor(TextMuted);
                                    col.Item().Text(order.DeliveryDateExpected.Value.ToString("dd.MM.yyyy", De)).FontSize(11);
                                }
                                col.Item().PaddingTop(4).Text("Status").FontSize(9).Bold().FontColor(TextMuted);
                                col.Item().Text(order.Status).FontSize(11);
                            });
                        });

                        column.Item().PaddingVertical(6).LineHorizontal(0.5f).LineColor(LineColor);

                        column.Item().PaddingBottom(12).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(4);
                                cols.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                HeaderCell(header, "Beschreibung");
                                header.Cell().Background(Primary).Padding(4).AlignRight()
                                    .Text("Betrag").FontSize(8).Bold().FontColor(Colors.White);
                            });

                            var desc = string.IsNullOrWhiteSpace(order.Notes)
                                ? "Leistungen gemäß Bestellung" : order.Notes;
                            table.Cell().BorderBottom(0.5f).BorderColor(LineColor).Padding(4)
                                .Text(desc).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(LineColor).Padding(4).AlignRight()
                                .Text(order.TotalNetDisplay).FontSize(9);

                            if (order.TotalGross != order.TotalNet && order.TotalNet > 0)
                            {
                                var vat = order.TotalGross - order.TotalNet;
                                var vatRate = (int)Math.Round(vat / order.TotalNet * 100);
                                table.Cell().BorderBottom(0.5f).BorderColor(LineColor).Padding(4)
                                    .Text($"Umsatzsteuer ({vatRate}%)").FontSize(9);
                                table.Cell().BorderBottom(0.5f).BorderColor(LineColor).Padding(4).AlignRight()
                                    .Text(vat.ToString("N2", De) + " €").FontSize(9);
                            }
                        });

                        column.Item().Background(Primary).Padding(8).Row(row =>
                        {
                            row.RelativeItem().Text("Gesamtbetrag (Brutto)").FontColor(Colors.White).Bold();
                            row.ConstantItem(120).AlignRight()
                                .Text(order.TotalGrossDisplay).FontColor(Colors.White).Bold().FontSize(12);
                        });
                    });

                    page.Footer().Element(ComposeReportFooter);
                });
            })
            .GeneratePdf();
        }
    }
}
