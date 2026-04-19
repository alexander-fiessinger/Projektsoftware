using Projektsoftware.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Globalization;
using System.IO;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Erzeugt Geschäftsbriefe als PDF strikt nach DIN 5008 Form B.
    /// Alle vertikalen Positionen sind absolute mm-Werte vom Blattrand.
    /// </summary>
    public class LetterGeneratorService
    {
        private static readonly CultureInfo De = new("de-DE");

        private const string Primary = "#1B365D";
        private const string Accent = "#C8A251";      // Gold-Akzent
        private const string TextDark = "#222222";
        private const string TextMuted = "#6B7280";
        private const string LineColor = "#BFBFBF";
        private const string FoldMarkColor = "#D1D5DB";
        private const string InfoBg = "#F7F8FA";       // Dezenter Hintergrund Infoblock

        // ── DIN 5008 Form B – alle Maße in mm ──
        private const float MarginLeft = 25f;
        private const float MarginRight = 10f;   // DIN erlaubt min. 8mm
        private const float FoldMarkLen = 4f;

        // Vertikale Positionen (absolute mm von Blattoberseite)
        private const float AddressFieldTop = 45f;        // Oberkante Anschriftfeld
        private const float ReturnLineHeight = 5f;         // Rücksendezeile 5mm
        private const float AddressZoneHeight = 40f;       // Empfängeradresse 40mm
        private const float AddressFieldWidth = 85f;       // max. Breite Anschriftfeld
        private const float InfoBlockLeft = 100f;          // Infoblock ab 100mm von links (= 75mm vom MarginLeft)

        public LetterGeneratorService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public void GenerateLetter(LetterData data, string outputPath)
        {
            Document.Create(container =>
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
            .GeneratePdf(outputPath);
        }

        private static void ComposeFoldMarks(IContainer container)
        {
            container.Layers(layers =>
            {
                layers.PrimaryLayer();

                // Falzmarken + Lochmarke
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

        private void ComposeContent(IContainer container, LetterData data)
        {
            container.Column(page =>
            {
                // ════════════════════════════════════════════════════
                // AKZENTSTREIFEN OBEN (Gold, volle Breite innerhalb Margins)
                // ════════════════════════════════════════════════════
                page.Item()
                    .Height(2.5f, Unit.Millimetre)
                    .Background(Accent);

                // ════════════════════════════════════════════════════
                // LOGO (im oberen Bereich, ersetzt Firmenüberschrift)
                // ════════════════════════════════════════════════════
                if (!string.IsNullOrWhiteSpace(data.LogoPath) && File.Exists(data.LogoPath))
                {
                    page.Item()
                        .PaddingTop(12, Unit.Millimetre)
                        .Height(18, Unit.Millimetre)
                        .Image(data.LogoPath)
                        .FitHeight();
                }

                // ════════════════════════════════════════════════════
                // ANSCHRIFTFELD (45 mm von oben)
                // ════════════════════════════════════════════════════
                page.Item()
                    .PaddingTop(20, Unit.Millimetre)
                    .Row(topRow =>
                    {
                        // ── LINKS: Anschriftfeld (85mm breit) ──
                        topRow.ConstantItem(AddressFieldWidth, Unit.Millimetre).Column(left =>
                        {
                            // Rücksendezeile + Akzentlinie
                            left.Item()
                                .Text($"{data.SenderCompany}, {data.SenderStreet} – {data.SenderZipCity}")
                                .FontSize(6).FontColor(TextMuted);

                            left.Item()
                                .PaddingTop(1, Unit.Millimetre)
                                .PaddingBottom(3, Unit.Millimetre)
                                .Width(75, Unit.Millimetre)
                                .LineHorizontal(0.75f)
                                .LineColor(Accent);

                            // Empfängeradresse
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

                        // ── RECHTS: Infoblock mit dezenter Box ──
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

                // ════════════════════════════════════════════════════
                // BETREFF (DIN 5008: ca. 97,4mm = 2 Leerzeilen nach Anschriftfeld)
                // ════════════════════════════════════════════════════
                if (!string.IsNullOrWhiteSpace(data.Subject))
                {
                    page.Item()
                        .PaddingTop(8.4f, Unit.Millimetre)   // ≈ 2 Leerzeilen
                        .Text(data.Subject)
                        .FontSize(10).Bold().FontColor(Primary);
                }

                // ════════════════════════════════════════════════════
                // ANREDE (1 Leerzeile nach Betreff)
                // ════════════════════════════════════════════════════
                page.Item()
                    .PaddingTop(4.2f, Unit.Millimetre)   // ≈ 1 Leerzeile
                    .Text(data.Salutation).FontSize(10);

                // ════════════════════════════════════════════════════
                // BRIEFTEXT (1 Leerzeile nach Anrede)
                // ════════════════════════════════════════════════════
                page.Item()
                    .PaddingTop(4.2f, Unit.Millimetre)
                    .Text(data.Body).FontSize(10).LineHeight(1.5f);

                // ════════════════════════════════════════════════════
                // GRUSSFORMEL (1 Leerzeile nach Text)
                // ════════════════════════════════════════════════════
                page.Item()
                    .PaddingTop(4.2f, Unit.Millimetre)
                    .Text(data.Closing).FontSize(10);

                // ════════════════════════════════════════════════════
                // UNTERSCHRIFT (3 Leerzeilen Platz)
                // ════════════════════════════════════════════════════
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

        // ════════════════════════════════════════════════════
        // FUSSZEILE (page.Footer – immer am Seitenende)
        // ════════════════════════════════════════════════════
        private void ComposeFooter(IContainer container, LetterData data)
        {
            container.PaddingBottom(8, Unit.Millimetre)
                .PaddingLeft(0).PaddingRight(0)
                .Column(ft =>
            {
                // Doppellinie: Gold dünn + Primary dick
                ft.Item().LineHorizontal(1.5f).LineColor(Accent);
                ft.Item().PaddingTop(1).LineHorizontal(0.4f).LineColor(Primary);

                ft.Item().PaddingTop(4).Row(row =>
                {
                    // Spalte 1 – Firma + Adresse
                    row.RelativeItem().Column(c =>
                    {
                        Ft(c, data.SenderCompany, true);
                        Ft(c, data.SenderName);
                        Ft(c, data.SenderStreet);
                        Ft(c, data.SenderZipCity);
                    });

                    // Spalte 2 – Kontakt
                    row.RelativeItem().Column(c =>
                    {
                        FtKv(c, "Tel.", data.SenderPhone);
                        FtKv(c, "E-Mail", data.SenderEmail);
                        FtKv(c, "Web", data.SenderWeb);
                    });

                    // Spalte 3 – Finanzen
                    row.RelativeItem().Column(c =>
                    {
                        FtKv(c, "USt-IdNr.", data.SenderVatId);
                        FtKv(c, "Steuernr.", data.SenderTaxNumber);
                        FtKv(c, "IBAN", data.SenderIban);
                        FtKv(c, "BIC", data.SenderBic);
                        Ft(c, data.SenderBankName);
                    });
                });
            });
        }

        // ── Infoblock: Label über Value ──
        private static void InfoRow(ColumnDescriptor col, string label, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            col.Item().PaddingBottom(1, Unit.Millimetre).Column(pair =>
            {
                pair.Item().Text(label).FontSize(7).FontColor(TextMuted);
                pair.Item().Text(value).FontSize(9).FontColor(TextDark);
            });
        }

        // ── Footer: einfacher Text ──
        private static void Ft(ColumnDescriptor c, string? text, bool bold = false)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var t = c.Item().Text(text).FontSize(6).FontColor(TextMuted);
            if (bold) t.Bold();
        }

        // ── Footer: Label + Wert ──
        private static void FtKv(ColumnDescriptor c, string label, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            c.Item().Text(t =>
            {
                t.Span($"{label}: ").FontSize(6).FontColor(TextMuted);
                t.Span(value).FontSize(6).FontColor(TextDark);
            });
        }
    }
}
