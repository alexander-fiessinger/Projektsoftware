using Projektsoftware.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Globalization;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Service zur Generierung von Vertr\u00e4gen als PDF.
    /// Unterst\u00fctzt Dienstleistungsvertr\u00e4ge (Robotik) gem\u00e4\u00df \u00a7\u00a7 611 ff. BGB
    /// und Werkvertr\u00e4ge gem\u00e4\u00df \u00a7\u00a7 631 ff. BGB.
    /// </summary>
    public class ContractGeneratorService
    {
        private static readonly CultureInfo De = new("de-DE");

        // ── Farbpalette ──
        private const string Primary      = "#1B365D";   // dunkles Marineblau
        private const string PrimaryLight  = "#2B5797";
        private const string Accent        = "#C8A251";   // Gold-Akzent
        private const string TextDark      = "#1A1A1A";
        private const string TextMuted     = "#5A6270";
        private const string BorderLight   = "#D0D5DD";
        private const string BgSubtle      = "#F6F7F9";

        public ContractGeneratorService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public void GenerateContract(ContractData data, string outputPath)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginTop(2.0f, Unit.Centimetre);
                    page.MarginBottom(2.2f, Unit.Centimetre);
                    page.MarginHorizontal(2.4f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(TextDark).LineHeight(1.45f));

                    page.Header().Element(c => ComposeHeader(c, data));
                    page.Content().Element(c => ComposeContent(c, data));
                    page.Footer().Element(c => ComposeFooter(c, data));
                });
            })
            .GeneratePdf(outputPath);
        }

        // ═══════════════════════════════════════════════════════
        //  HEADER
        // ═══════════════════════════════════════════════════════
        private void ComposeHeader(IContainer container, ContractData data)
        {
            container.Column(col =>
            {
                // Goldlinie oben
                col.Item().Height(3).Background(Accent);

                col.Item().PaddingTop(14).PaddingBottom(6).Row(row =>
                {
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text(data.ContractTypeDisplay.ToUpper())
                            .FontSize(22).Bold().FontColor(Primary).LetterSpacing(0.04f);

                        var lawRef = data.ContractType == ContractType.Dienstleistungsvertrag
                            ? "\u00a7\u00a7 611 ff. BGB" : "\u00a7\u00a7 631 ff. BGB";
                        inner.Item().PaddingTop(2).Text($"gem\u00e4\u00df {lawRef}")
                            .FontSize(9).Italic().FontColor(TextMuted);
                    });

                    row.ConstantItem(140).AlignRight().AlignBottom().Column(inner =>
                    {
                        inner.Item().Text(data.ContractorCompany)
                            .FontSize(8).FontColor(TextMuted).AlignRight();
                        if (!string.IsNullOrWhiteSpace(data.ContractorZipCity))
                            inner.Item().Text(data.ContractorZipCity)
                                .FontSize(7.5f).FontColor(TextMuted).AlignRight();
                    });
                });

                // Trennlinie
                col.Item().Height(1.5f).Background(Primary);
            });
        }

        // ═══════════════════════════════════════════════════════
        //  CONTENT
        // ═══════════════════════════════════════════════════════
        private void ComposeContent(IContainer container, ContractData data)
        {
            container.PaddingTop(10).Column(column =>
            {
                column.Spacing(2);

                // Pr\u00e4ambel
                column.Item().PaddingTop(6).PaddingBottom(8).Text(t =>
                {
                    t.Span("Zwischen den nachstehend bezeichneten Parteien wird folgender ")
                        .FontSize(9.5f).FontColor(TextDark);
                    t.Span(data.ContractTypeDisplay).FontSize(9.5f).Bold().FontColor(Primary);
                    t.Span(" geschlossen:").FontSize(9.5f).FontColor(TextDark);
                });

                ComposeParties(column, data);

                if (data.ContractType == ContractType.Dienstleistungsvertrag)
                    ComposeDienstleistungsvertrag(column, data);
                else
                    ComposeWerkvertrag(column, data);

                ComposeCommonClauses(column, data);
                ComposeSignatureBlock(column, data);
            });
        }

        // ─────────────────────────────────────────────────────
        //  \u00a7 1  VERTRAGSPARTEIEN
        // ─────────────────────────────────────────────────────
        private void ComposeParties(ColumnDescriptor column, ContractData data)
        {
            SectionHeading(column, "\u00a7 1 Vertragsparteien");

            column.Item().PaddingLeft(12).PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Border(0.5f).BorderColor(BorderLight)
                    .Background(BgSubtle).Padding(10).Column(col =>
                {
                    col.Item().Text("AUFTRAGNEHMER").FontSize(7.5f).Bold()
                        .FontColor(Accent).LetterSpacing(0.08f);
                    col.Item().PaddingTop(4).Text(data.ContractorCompany)
                        .FontSize(10).Bold().FontColor(Primary);
                    OptionalLine(col, data.ContractorName, "vertreten durch ");
                    OptionalLine(col, data.ContractorStreet);
                    OptionalLine(col, data.ContractorZipCity);
                    if (!string.IsNullOrWhiteSpace(data.ContractorEmail))
                        col.Item().PaddingTop(3).Text($"E-Mail: {data.ContractorEmail}")
                            .FontSize(8.5f).FontColor(TextMuted);
                    if (!string.IsNullOrWhiteSpace(data.ContractorPhone))
                        col.Item().Text($"Tel.: {data.ContractorPhone}")
                            .FontSize(8.5f).FontColor(TextMuted);
                    if (!string.IsNullOrWhiteSpace(data.ContractorVatId))
                        col.Item().PaddingTop(3).Text($"USt-IdNr.: {data.ContractorVatId}")
                            .FontSize(8).FontColor(TextMuted);
                    if (!string.IsNullOrWhiteSpace(data.ContractorTaxNumber))
                        col.Item().Text($"Steuernr.: {data.ContractorTaxNumber}")
                            .FontSize(8).FontColor(TextMuted);
                });

                row.ConstantItem(16);

                row.RelativeItem().Border(0.5f).BorderColor(BorderLight)
                    .Background(BgSubtle).Padding(10).Column(col =>
                {
                    col.Item().Text("AUFTRAGGEBER").FontSize(7.5f).Bold()
                        .FontColor(Accent).LetterSpacing(0.08f);
                    col.Item().PaddingTop(4).Text(
                        !string.IsNullOrWhiteSpace(data.ClientCompany) ? data.ClientCompany : data.ClientName)
                        .FontSize(10).Bold().FontColor(Primary);
                    if (!string.IsNullOrWhiteSpace(data.ClientCompany) &&
                        !string.IsNullOrWhiteSpace(data.ClientName))
                        col.Item().Text($"vertreten durch {data.ClientName}")
                            .FontSize(9).FontColor(TextDark);
                    OptionalLine(col, data.ClientStreet);
                    OptionalLine(col, data.ClientZipCity);
                    if (!string.IsNullOrWhiteSpace(data.ClientEmail))
                        col.Item().PaddingTop(3).Text($"E-Mail: {data.ClientEmail}")
                            .FontSize(8.5f).FontColor(TextMuted);
                    if (!string.IsNullOrWhiteSpace(data.ClientPhone))
                        col.Item().Text($"Tel.: {data.ClientPhone}")
                            .FontSize(8.5f).FontColor(TextMuted);
                });
            });

            column.Item().PaddingTop(6).AlignCenter().Text(
                "\u2013 nachfolgend einzeln auch \u201EPartei\u201C und gemeinsam \u201EParteien\u201C genannt \u2013")
                .FontSize(8.5f).Italic().FontColor(TextMuted);
        }

        // ═══════════════════════════════════════════════════════
        //  DIENSTLEISTUNGSVERTRAG  (\u00a7\u00a7 611 ff. BGB)
        // ═══════════════════════════════════════════════════════
        private void ComposeDienstleistungsvertrag(ColumnDescriptor column, ContractData data)
        {
            // \u00a7 2 Vertragsgegenstand
            SectionHeading(column, "\u00a7 2 Vertragsgegenstand");
            Para(column,
                "Der Auftragnehmer verpflichtet sich, f\u00fcr den Auftraggeber die nachstehend " +
                "n\u00e4her beschriebenen Dienstleistungen im Bereich Robotik, Automatisierungstechnik " +
                "und/oder Software Engineering zu erbringen. Der Auftragnehmer erbringt diese " +
                "Leistungen als selbstst\u00e4ndiger Unternehmer; ein Arbeitsverh\u00e4ltnis wird durch " +
                "diesen Vertrag nicht begr\u00fcndet.");
            if (!string.IsNullOrWhiteSpace(data.ContractSubject))
                Para(column, data.ContractSubject);

            // \u00a7 3 Art und Umfang
            SectionHeading(column, "\u00a7 3 Art und Umfang der Leistungen");
            Para(column, data.ServiceDescription);
            if (!string.IsNullOrWhiteSpace(data.ServiceLocation))
                Para(column, $"Leistungsort: {data.ServiceLocation}.");
            if (!string.IsNullOrWhiteSpace(data.WorkingHours))
                Para(column, $"Arbeitszeiten: {data.WorkingHours}.");
            Para(column,
                "(1) Der Auftragnehmer schuldet die sorgf\u00e4ltige Erbringung der vereinbarten " +
                "T\u00e4tigkeiten, nicht jedoch einen bestimmten Erfolg (\u00a7 611 Abs. 1 BGB). Die " +
                "Art und Weise der Leistungserbringung bestimmt der Auftragnehmer im Rahmen " +
                "der vertraglichen Vereinbarungen nach eigenem pflichtgem\u00e4\u00dfem Ermessen.");
            Para(column,
                "(2) Der Auftragnehmer ist berechtigt, zur Erf\u00fcllung seiner vertraglichen " +
                "Pflichten qualifizierte Erf\u00fcllungsgehilfen einzusetzen. Er haftet f\u00fcr deren " +
                "Verschulden wie f\u00fcr eigenes (\u00a7 278 BGB). Der Einsatz von Subunternehmern " +
                "bedarf der vorherigen schriftlichen Zustimmung des Auftraggebers.");
            Para(column,
                "(3) \u00c4nderungen des Leistungsumfangs bed\u00fcrfen einer schriftlichen Vereinbarung " +
                "zwischen den Parteien (Change-Request-Verfahren). Mehrleistungen, die \u00fcber den " +
                "vereinbarten Umfang hinausgehen, werden gesondert verg\u00fctet, sofern der Auftraggeber " +
                "diese beauftragt oder ihnen zugestimmt hat.");

            // \u00a7 4 Verg\u00fctung
            SectionHeading(column, "\u00a7 4 Verg\u00fctung und Zahlungsbedingungen");
            if (data.HourlyRate.HasValue && data.HourlyRate.Value > 0)
            {
                Para(column,
                    $"(1) Die Verg\u00fctung erfolgt auf Basis der tats\u00e4chlich geleisteten Stunden " +
                    $"zu einem Stundensatz von {Eur(data.HourlyRate.Value)} (netto) zzgl. der " +
                    $"gesetzlichen Umsatzsteuer in H\u00f6he von derzeit {data.VatRate.ToString("N0", De)} %. " +
                    "Die Erfassung der Arbeitszeiten erfolgt in Einheiten von je 15 Minuten. " +
                    "Der Auftragnehmer legt dem Auftraggeber monatlich einen Leistungsnachweis vor.");
            }
            if (data.NetAmount > 0)
            {
                var prefix = data.HourlyRate.HasValue && data.HourlyRate.Value > 0 ? "(2)" : "(1)";
                Para(column,
                    $"{prefix} Die vereinbarte Pauschalverg\u00fctung betr\u00e4gt {Eur(data.NetAmount)} (netto) " +
                    $"zzgl. {data.VatRate.ToString("N0", De)} % Umsatzsteuer " +
                    $"= {Eur(data.GrossAmount)} (brutto).");
            }
            Para(column,
                $"Die Rechnungsstellung erfolgt monatlich nachtr\u00e4glich. Rechnungen sind " +
                $"innerhalb von {data.PaymentTerms} zur Zahlung f\u00e4llig. Bei Zahlungsverzug " +
                "gelten die gesetzlichen Verzugsregeln (\u00a7\u00a7 286, 288 BGB); der Verzugszinssatz " +
                "betr\u00e4gt f\u00fcr Rechtsgesch\u00e4fte, an denen ein Verbraucher nicht beteiligt ist, " +
                "9 Prozentpunkte \u00fcber dem jeweiligen Basiszinssatz (\u00a7 288 Abs. 2 BGB).");
            Para(column,
                "Reise- und \u00dcbernachtungskosten werden nur erstattet, wenn sie im Vorfeld " +
                "schriftlich genehmigt wurden. Die Erstattung erfolgt gegen Vorlage von Belegen " +
                "nach tats\u00e4chlichem Aufwand. Fahrtkosten werden mit 0,30 \u20ac/km (PKW) bzw. " +
                "nach tats\u00e4chlichen Kosten (\u00f6ffentliche Verkehrsmittel) abgerechnet.");

            // \u00a7 5 Laufzeit und K\u00fcndigung
            SectionHeading(column, "\u00a7 5 Vertragslaufzeit und K\u00fcndigung");
            Para(column,
                $"(1) Dieser Vertrag tritt am {data.ContractStart:dd.MM.yyyy} in Kraft" +
                (data.ContractEnd.HasValue
                    ? $" und endet mit Ablauf des {data.ContractEnd.Value:dd.MM.yyyy}, " +
                      "ohne dass es einer K\u00fcndigung bedarf (befristeter Vertrag)."
                    : " und l\u00e4uft auf unbestimmte Zeit."));
            if (!data.ContractEnd.HasValue)
            {
                Para(column,
                    $"(2) Der Vertrag kann von jeder Partei ordentlich mit einer Frist von " +
                    $"{data.NoticePeriod} gek\u00fcndigt werden. Die K\u00fcndigung bedarf der " +
                    "Schriftform (\u00a7 126 BGB).");
            }
            Para(column,
                $"({(data.ContractEnd.HasValue ? "2" : "3")}) Das Recht beider Parteien zur " +
                "fristlosen K\u00fcndigung aus wichtigem Grund gem\u00e4\u00df \u00a7 626 BGB bleibt unber\u00fchrt. " +
                "Ein wichtiger Grund liegt insbesondere vor, wenn " +
                "a) die andere Partei trotz schriftlicher Abmahnung und angemessener Fristsetzung " +
                "wesentliche Vertragspflichten wiederholt verletzt, " +
                "b) \u00fcber das Verm\u00f6gen der anderen Partei ein Insolvenzverfahren er\u00f6ffnet oder " +
                "die Er\u00f6ffnung mangels Masse abgelehnt wird, oder " +
                "c) die andere Partei ihre Gesch\u00e4ftst\u00e4tigkeit einstellt.");
            Para(column,
                $"({(data.ContractEnd.HasValue ? "3" : "4")}) Im Falle der K\u00fcndigung hat der " +
                "Auftragnehmer alle bis dahin erbrachten Leistungen ordnungsgem\u00e4\u00df zu " +
                "dokumentieren und s\u00e4mtliche Unterlagen, Materialien und Zugangsdaten " +
                "des Auftraggebers unverz\u00fcglich zur\u00fcckzugeben. Bereits erbrachte Leistungen " +
                "sind entsprechend dem Leistungsstand zu verg\u00fcten.");

            // \u00a7 6 Mitwirkungspflichten
            SectionHeading(column, "\u00a7 6 Mitwirkungspflichten des Auftraggebers");
            Para(column,
                "(1) Der Auftraggeber stellt dem Auftragnehmer alle f\u00fcr die Erbringung der " +
                "Leistungen erforderlichen Informationen, Unterlagen, Zug\u00e4nge zu IT-Systemen " +
                "und sonstige Ressourcen rechtzeitig, vollst\u00e4ndig und unentgeltlich zur " +
                "Verf\u00fcgung.");
            Para(column,
                "(2) Der Auftraggeber benennt einen fachlich qualifizierten Ansprechpartner, " +
                "der f\u00fcr die Dauer der Vertragslaufzeit zur Abstimmung und Entscheidungsfindung " +
                "berechtigt ist. Entscheidungen, die f\u00fcr den Fortgang der Leistungserbringung " +
                "erforderlich sind, trifft der Ansprechpartner innerhalb angemessener Frist.");
            Para(column,
                "(3) Verz\u00f6gerungen, die auf eine Verletzung der Mitwirkungspflichten des " +
                "Auftraggebers zur\u00fcckzuf\u00fchren sind, gehen nicht zulasten des Auftragnehmers. " +
                "Hieraus entstehende Mehrkosten tr\u00e4gt der Auftraggeber. Vereinbarte Termine " +
                "verschieben sich entsprechend.");
        }

        // ═══════════════════════════════════════════════════════
        //  WERKVERTRAG  (\u00a7\u00a7 631 ff. BGB)
        // ═══════════════════════════════════════════════════════
        private void ComposeWerkvertrag(ColumnDescriptor column, ContractData data)
        {
            // \u00a7 2 Vertragsgegenstand
            SectionHeading(column, "\u00a7 2 Vertragsgegenstand");
            Para(column,
                "Der Auftragnehmer verpflichtet sich, das nachfolgend n\u00e4her beschriebene Werk " +
                "herzustellen und dem Auftraggeber das vereinbarte Ergebnis frei von Sach- und " +
                "Rechtsm\u00e4ngeln zu \u00fcbergeben (\u00a7 631 Abs. 1 BGB). Der Auftraggeber verpflichtet " +
                "sich, das vertragsm\u00e4\u00df hergestellte Werk abzunehmen und die vereinbarte " +
                "Verg\u00fctung zu entrichten.");
            if (!string.IsNullOrWhiteSpace(data.ContractSubject))
                Para(column, data.ContractSubject);

            // \u00a7 3 Werkleistung
            SectionHeading(column, "\u00a7 3 Werkleistung und technische Spezifikation");
            Para(column, data.ServiceDescription);
            Para(column,
                "(1) Das Werk muss den anerkannten Regeln der Technik sowie s\u00e4mtlichen " +
                "einschl\u00e4gigen Normen und Sicherheitsvorschriften entsprechen, insbesondere:");
            BulletPoint(column, "Maschinenverordnung (EU) 2023/1230 (ab 20.01.2027; bis dahin Maschinenrichtlinie 2006/42/EG)");
            BulletPoint(column, "DIN EN ISO 10218-1/-2 (Industrieroboter \u2013 Sicherheitsanforderungen)");
            BulletPoint(column, "DIN EN ISO 13849-1/-2 (Sicherheit von Steuerungen)");
            BulletPoint(column, "DIN EN 62061 (Funktionale Sicherheit)");
            BulletPoint(column, "DIN EN ISO 12100 (Risikobeurteilung und Risikominderung)");
            BulletPoint(column, "Ggf. weitere projektspezifisch vereinbarte Normen und Richtlinien");
            Para(column,
                "(2) Der Auftragnehmer erstellt die f\u00fcr die CE-Konformit\u00e4tserkl\u00e4rung und " +
                "Risikobeurteilung erforderliche technische Dokumentation, sofern dies vertraglich " +
                "vereinbart ist. Die Dokumentation umfasst mindestens: Beschreibung der verwendeten " +
                "Normen, Ergebnisse der Risikoanalyse, Schaltpl\u00e4ne und Softwarebeschreibung.");
            Para(column,
                "(3) \u00c4nderungen der Werkleistung nach Vertragsschluss bed\u00fcrfen einer " +
                "schriftlichen \u00c4nderungsvereinbarung (Change Order). Der Auftragnehmer " +
                "informiert den Auftraggeber unverz\u00fcglich \u00fcber die Auswirkungen auf Kosten " +
                "und Zeitplan. Ohne schriftliche Beauftragung besteht kein Anspruch auf " +
                "Verg\u00fctung von Mehrleistungen.");

            // \u00a7 4 Verg\u00fctung
            SectionHeading(column, "\u00a7 4 Verg\u00fctung und Zahlungsbedingungen");
            Para(column,
                $"(1) Die Gesamtverg\u00fctung f\u00fcr das Werk betr\u00e4gt {Eur(data.NetAmount)} (netto) " +
                $"zzgl. {data.VatRate.ToString("N0", De)} % Umsatzsteuer " +
                $"= {Eur(data.GrossAmount)} (brutto).");
            Para(column,
                $"(2) Sofern nicht anders vereinbart, wird die Verg\u00fctung wie folgt f\u00e4llig: " +
                $"{data.WorkPaymentSchedule} (\u00a7 641 Abs. 1 BGB).");
            Para(column,
                $"(3) Zahlungen sind innerhalb von {data.PaymentTerms} nach Rechnungszugang " +
                "zu leisten. Bei Zahlungsverzug gelten die gesetzlichen Verzugsregeln " +
                "(\u00a7\u00a7 286, 288 BGB); der Verzugszinssatz betr\u00e4gt f\u00fcr unternehmerische " +
                "Rechtsgesch\u00e4fte 9 Prozentpunkte \u00fcber dem jeweiligen Basiszinssatz " +
                "(\u00a7 288 Abs. 2 BGB).");
            Para(column,
                "(4) Der Auftraggeber ist zur Aufrechnung nur mit unbestrittenen oder " +
                "rechtskr\u00e4ftig festgestellten Forderungen berechtigt. Ein Zur\u00fcckbehaltungsrecht " +
                "kann nur geltend gemacht werden, soweit es auf demselben Vertragsverh\u00e4ltnis beruht.");

            // \u00a7 5 Fertigstellung
            SectionHeading(column, "\u00a7 5 Fertigstellung und Lieferung");
            if (data.DeliveryDate.HasValue)
            {
                Para(column,
                    $"(1) Die Fertigstellung und \u00dcbergabe des Werkes erfolgt sp\u00e4testens am " +
                    $"{data.DeliveryDate.Value:dd.MM.yyyy}. Dieser Termin ist ein verbindlicher " +
                    "Fixtermin im Sinne des \u00a7 323 Abs. 2 Nr. 2 BGB, sofern die Parteien nichts " +
                    "anderes schriftlich vereinbaren.");
            }
            else
            {
                Para(column,
                    "(1) Der Fertigstellungstermin wird zwischen den Parteien in einem " +
                    "gesonderten Projektplan schriftlich vereinbart.");
            }
            Para(column,
                "(2) Ger\u00e4t der Auftragnehmer mit der Fertigstellung in Verzug, kann der " +
                "Auftraggeber nach fruchtlosem Ablauf einer angemessenen Nachfrist von mindestens " +
                "14 Werktagen die in \u00a7\u00a7 323, 281 BGB vorgesehenen Rechte geltend machen " +
                "(R\u00fccktritt und/oder Schadensersatz statt der Leistung).");
            Para(column,
                "(3) Der Auftragnehmer informiert den Auftraggeber unverz\u00fcglich, sobald " +
                "erkennbar ist, dass der vereinbarte Fertigstellungstermin voraussichtlich " +
                "nicht eingehalten werden kann. Die Parteien stimmen in diesem Fall gemeinsam " +
                "eine angepasste Terminplanung ab.");

            // \u00a7 6 Abnahme
            SectionHeading(column, "\u00a7 6 Abnahme");
            Para(column,
                "(1) Der Auftragnehmer zeigt dem Auftraggeber die Fertigstellung des Werkes " +
                "schriftlich an (Fertigstellungsanzeige). Der Auftraggeber ist verpflichtet, " +
                "das Werk innerhalb von 14 Werktagen nach Zugang der Fertigstellungsanzeige " +
                "zu pr\u00fcfen und abzunehmen (\u00a7 640 BGB).");
            Para(column,
                "(2) Die Abnahme erfolgt durch Unterzeichnung eines gemeinsamen " +
                "Abnahmeprotokolls. In diesem sind etwaige M\u00e4ngel, offene Punkte und " +
                "Vorbehalte zu vermerken.");
            if (!string.IsNullOrWhiteSpace(data.AcceptanceCriteria))
                Para(column, $"(3) Abnahmekriterien: {data.AcceptanceCriteria}");
            var nextAbnahme = string.IsNullOrWhiteSpace(data.AcceptanceCriteria) ? 3 : 4;
            Para(column,
                $"({nextAbnahme}) Die Abnahme darf nicht wegen unwesentlicher M\u00e4ngel verweigert werden " +
                "(\u00a7 640 Abs. 1 Satz 2 BGB). Ein Mangel ist unwesentlich, wenn er die " +
                "Gebrauchstauglichkeit des Werkes nicht oder nur geringf\u00fcgig beeintr\u00e4chtigt " +
                "und mit vertretbarem Aufwand beseitigt werden kann.");
            Para(column,
                $"({nextAbnahme + 1}) L\u00e4sst der Auftraggeber die Abnahmefrist verstreichen, ohne M\u00e4ngel " +
                "zu r\u00fcgen, gilt das Werk als abgenommen (fiktive Abnahme). Der Auftragnehmer " +
                "weist den Auftraggeber in der Fertigstellungsanzeige auf diese Rechtsfolge hin.");

            // \u00a7 7 Gew\u00e4hrleistung
            SectionHeading(column, "\u00a7 7 M\u00e4ngelansp\u00fcche und Gew\u00e4hrleistung");
            Para(column,
                $"(1) Die Gew\u00e4hrleistungsfrist betr\u00e4gt {data.WarrantyPeriod}. Die Frist " +
                "beginnt mit der Abnahme des Werkes (\u00a7 634a Abs. 2 BGB). F\u00fcr Nachbesserungen " +
                "beginnt die Frist hinsichtlich des nachgebesserten Teils erneut zu laufen.");
            Para(column,
                "(2) Im Falle eines Mangels hat der Auftraggeber zun\u00e4chst Anspruch auf " +
                "Nacherf\u00fcllung (\u00a7 635 BGB). Der Auftragnehmer hat das Wahlrecht zwischen " +
                "Mangelbeseitigung und Neuherstellung, soweit die gew\u00e4hlte Art der " +
                "Nacherf\u00fcllung nicht f\u00fcr den Auftraggeber unzumutbar ist.");
            Para(column,
                "(3) Scheitert die Nacherf\u00fcllung auch nach dem zweiten Versuch oder " +
                "verweigert der Auftragnehmer die Nacherf\u00fcllung, kann der Auftraggeber " +
                "nach seiner Wahl Minderung (\u00a7 638 BGB), R\u00fccktritt (\u00a7 636 BGB) " +
                "oder Schadensersatz statt der Leistung (\u00a7 634 Nr. 4 i.V.m. \u00a7\u00a7 280, 281 BGB) " +
                "verlangen.");
            Para(column,
                "(4) Offensichtliche M\u00e4ngel sind dem Auftragnehmer unverz\u00fcglich, sp\u00e4testens " +
                "jedoch innerhalb von 10 Werktagen nach Abnahme, schriftlich und unter " +
                "genauer Beschreibung des Mangels anzuzeigen. Verdeckte M\u00e4ngel sind unverz\u00fcglich " +
                "nach Entdeckung schriftlich anzuzeigen. Unterl\u00e4sst der Auftraggeber die " +
                "rechtzeitige M\u00e4ngelanzeige, verliert er seine Gew\u00e4hrleistungsanspr\u00fcche " +
                "hinsichtlich des betreffenden Mangels, es sei denn, der Auftragnehmer hat " +
                "den Mangel arglistig verschwiegen.");

            // \u00a7 8 Mitwirkungspflichten
            SectionHeading(column, "\u00a7 8 Mitwirkungspflichten des Auftraggebers");
            Para(column,
                "(1) Der Auftraggeber stellt dem Auftragnehmer alle f\u00fcr die Herstellung des " +
                "Werkes erforderlichen Informationen, Zug\u00e4nge, technischen Voraussetzungen " +
                "und r\u00e4umlichen Gegebenheiten rechtzeitig, vollst\u00e4ndig und unentgeltlich " +
                "zur Verf\u00fcgung.");
            Para(column,
                "(2) Der Auftraggeber benennt einen fachlich und organisatorisch verantwortlichen " +
                "Ansprechpartner, der zur Abgabe und Entgegennahme von Erkl\u00e4rungen im Rahmen " +
                "dieses Vertrages bevollm\u00e4chtigt ist.");
            Para(column,
                "(3) Verz\u00f6gert sich die Fertigstellung aufgrund unterlassener oder versp\u00e4teter " +
                "Mitwirkung des Auftraggebers, verl\u00e4ngert sich der vereinbarte " +
                "Fertigstellungstermin entsprechend. Dem Auftragnehmer stehen in diesem Fall " +
                "Anspr\u00fcche auf Ersatz der durch die Verz\u00f6gerung entstandenen Mehrkosten zu.");
        }

        // ═══════════════════════════════════════════════════════
        //  GEMEINSAME KLAUSELN
        // ═══════════════════════════════════════════════════════
        private void ComposeCommonClauses(ColumnDescriptor column, ContractData data)
        {
            var n = data.ContractType == ContractType.Werkvertrag ? 9 : 7;

            // Haftung
            SectionHeading(column, $"\u00a7 {n} Haftungsbeschr\u00e4nkung");
            Para(column,
                "(1) Der Auftragnehmer haftet unbeschr\u00e4nkt f\u00fcr Sch\u00e4den aus der Verletzung " +
                "des Lebens, des K\u00f6rpers oder der Gesundheit, die auf einer vors\u00e4tzlichen " +
                "oder fahrl\u00e4ssigen Pflichtverletzung des Auftragnehmers, seiner gesetzlichen " +
                "Vertreter oder Erf\u00fcllungsgehilfen beruhen.");
            Para(column,
                "(2) F\u00fcr sonstige Sch\u00e4den haftet der Auftragnehmer nur bei Vorsatz und grober " +
                "Fahrl\u00e4ssigkeit sowie bei schuldhafter Verletzung wesentlicher Vertragspflichten " +
                "(Kardinalpflichten). Wesentliche Vertragspflichten sind solche, deren Erf\u00fcllung " +
                "die ordnungsgem\u00e4\u00dfe Durchf\u00fchrung des Vertrages \u00fcberhaupt erst erm\u00f6glicht und " +
                "auf deren Einhaltung der Auftraggeber regelm\u00e4\u00dfig vertrauen darf.");
            Para(column,
                "(3) Bei der Verletzung wesentlicher Vertragspflichten durch leichte " +
                "Fahrl\u00e4ssigkeit ist die Haftung auf den vertragstypischen, bei Vertragsschluss " +
                "vorhersehbaren Schaden begrenzt.");
            Para(column,
                "(4) Die Haftung nach dem Produkthaftungsgesetz (ProdHaftG), f\u00fcr die \u00dcbernahme " +
                "einer Garantie oder aus arglistigem Verschweigen eines Mangels bleibt von den " +
                "vorstehenden Beschr\u00e4nkungen unber\u00fchrt.");
            Para(column,
                "(5) Soweit die Haftung des Auftragnehmers ausgeschlossen oder beschr\u00e4nkt ist, " +
                "gilt dies auch zugunsten seiner Organe, Mitarbeiter, Vertreter und " +
                "Erf\u00fcllungsgehilfen.");
            n++;

            // Geheimhaltung
            SectionHeading(column, $"\u00a7 {n} Vertraulichkeit und Geheimhaltung");
            Para(column,
                "(1) Die Parteien verpflichten sich, s\u00e4mtliche im Zusammenhang mit diesem " +
                "Vertrag erlangten vertraulichen Informationen, Gesch\u00e4fts- und " +
                "Betriebsgeheimnisse der jeweils anderen Partei streng vertraulich zu " +
                "behandeln, nicht an Dritte weiterzugeben und ausschlie\u00dflich f\u00fcr die " +
                "Zwecke dieses Vertrages zu verwenden. Als vertraulich gelten alle " +
                "Informationen, die als solche gekennzeichnet sind oder deren vertraulicher " +
                "Charakter sich aus den Umst\u00e4nden ergibt.");
            Para(column,
                "(2) Die Geheimhaltungspflicht gilt nicht f\u00fcr Informationen, die " +
                "a) zum Zeitpunkt der Mitteilung bereits \u00f6ffentlich bekannt waren, " +
                "b) nach der Mitteilung ohne Verschulden der empfangenden Partei \u00f6ffentlich " +
                "werden, c) der empfangenden Partei bereits vor der Mitteilung " +
                "rechtm\u00e4\u00dfig bekannt waren, d) der empfangenden Partei von einem Dritten " +
                "ohne Vertraulichkeitsbeschr\u00e4nkung rechtm\u00e4\u00dfig mitgeteilt werden, oder " +
                "e) aufgrund gesetzlicher Verpflichtung oder beh\u00f6rdlicher/gerichtlicher " +
                "Anordnung offengelegt werden m\u00fcssen.");
            Para(column,
                "(3) Die Geheimhaltungspflicht besteht w\u00e4hrend der Vertragslaufzeit und " +
                "f\u00fcr einen Zeitraum von 5 (f\u00fcnf) Jahren nach Vertragsbeendigung fort. " +
                "Auf Verlangen der offenlegenden Partei sind alle vertraulichen Unterlagen " +
                "und Kopien unverz\u00fcglich zur\u00fcckzugeben oder nachweislich zu vernichten.");
            n++;

            // Datenschutz
            SectionHeading(column, $"\u00a7 {n} Datenschutz");
            Para(column,
                "(1) Die Parteien verpflichten sich, die Bestimmungen der Verordnung (EU) " +
                "2016/679 (Datenschutz-Grundverordnung \u2013 DSGVO) sowie des " +
                "Bundesdatenschutzgesetzes (BDSG) in der jeweils geltenden Fassung einzuhalten.");
            Para(column,
                "(2) Soweit der Auftragnehmer im Rahmen der Vertragserfüllung personenbezogene " +
                "Daten im Auftrag des Auftraggebers verarbeitet, schlie\u00dfen die Parteien vor " +
                "Beginn der Verarbeitung eine gesonderte Auftragsverarbeitungsvereinbarung " +
                "gem\u00e4\u00df Art. 28 DSGVO ab.");
            Para(column,
                "(3) Jede Partei benennt, soweit gesetzlich erforderlich, einen " +
                "Datenschutzbeauftragten und teilt der anderen Partei dessen Kontaktdaten mit.");
            n++;

            // Geistiges Eigentum
            SectionHeading(column, $"\u00a7 {n} Geistiges Eigentum und Nutzungsrechte");
            if (data.ContractType == ContractType.Werkvertrag)
            {
                Para(column,
                    "(1) Mit vollst\u00e4ndiger Bezahlung der vereinbarten Verg\u00fctung gehen " +
                    "s\u00e4mtliche ausschlie\u00dflichen, \u00f6rtlich, zeitlich und inhaltlich " +
                    "unbeschr\u00e4nkten Nutzungs- und Verwertungsrechte an den im Rahmen dieses " +
                    "Vertrages erstellten Arbeitsergebnissen auf den Auftraggeber \u00fcber. " +
                    "Dies umfasst insbesondere das Recht zur Vervielf\u00e4ltigung, Bearbeitung, " +
                    "Verbreitung und \u00f6ffentlichen Zug\u00e4nglichmachung.");
            }
            else
            {
                Para(column,
                    "(1) Arbeitsergebnisse, die der Auftragnehmer im Rahmen der " +
                    "Vertragserfüllung erstellt, stehen dem Auftraggeber nach vollst\u00e4ndiger " +
                    "Bezahlung zur vertragsgem\u00e4\u00dfen Nutzung zu. Die \u00dcbertragung " +
                    "weitergehender Nutzungsrechte bedarf einer gesonderten schriftlichen " +
                    "Vereinbarung.");
            }
            Para(column,
                "(2) Vorbestehende Schutzrechte (Background-IP) des Auftragnehmers bleiben " +
                "unber\u00fchrt. Soweit vorbestehende Schutzrechte in die Arbeitsergebnisse " +
                "einflie\u00dfen, r\u00e4umt der Auftragnehmer dem Auftraggeber ein nicht-ausschlie\u00dfliches, " +
                "zeitlich unbeschr\u00e4nktes Nutzungsrecht im vertraglich vereinbarten Umfang ein.");
            Para(column,
                "(3) Der Auftragnehmer gew\u00e4hrleistet, dass die erbrachten Leistungen und " +
                "Arbeitsergebnisse keine Schutzrechte Dritter (insbesondere Urheber-, Patent- " +
                "oder Markenrechte) verletzen. Er stellt den Auftraggeber von allen Anspr\u00fcchen " +
                "Dritter frei, die aus einer Verletzung solcher Rechte entstehen.");
            n++;

            // H\u00f6here Gewalt
            SectionHeading(column, $"\u00a7 {n} H\u00f6here Gewalt (Force Majeure)");
            Para(column,
                "(1) Keine Partei haftet f\u00fcr die Nicherf\u00fcllung oder versp\u00e4tete Erf\u00fcllung " +
                "ihrer vertraglichen Pflichten, soweit dies auf h\u00f6here Gewalt zur\u00fcckzuf\u00fchren " +
                "ist. H\u00f6here Gewalt umfasst insbesondere Naturkatastrophen, Epidemien/Pandemien, " +
                "beh\u00f6rdliche Anordnungen, Krieg, Terror, Aufruhr, Streik, Aussperrung, " +
                "Lieferkettenunterbrechungen sowie großfl\u00e4chige Ausf\u00e4lle der " +
                "Telekommunikationsinfrastruktur oder Energieversorgung.");
            Para(column,
                "(2) Die betroffene Partei hat die andere Partei unverz\u00fcglich, sp\u00e4testens " +
                "jedoch innerhalb von 5 Werktagen, schriftlich \u00fcber den Eintritt und die " +
                "voraussichtliche Dauer des Hindernisses zu informieren. Vertragliche Fristen " +
                "verl\u00e4ngern sich um die Dauer der Behinderung.");
            Para(column,
                "(3) Dauert die h\u00f6here Gewalt l\u00e4nger als 90 Kalendertage an, ist jede Partei " +
                "berechtigt, den Vertrag mit sofortiger Wirkung schriftlich zu k\u00fcndigen. " +
                "Bereits erbrachte Leistungen sind in diesem Fall anteilig zu verg\u00fcten.");
            n++;

            // Compliance / Anti-Korruption
            SectionHeading(column, $"\u00a7 {n} Compliance und Anti-Korruption");
            Para(column,
                "Die Parteien verpflichten sich, alle anwendbaren gesetzlichen Bestimmungen " +
                "einzuhalten, insbesondere die Vorschriften zur Bek\u00e4mpfung von Korruption " +
                "und Bestechung (\u00a7\u00a7 299, 331\u2013335 StGB), das Geldw\u00e4schegesetz (GwG) sowie " +
                "das Lieferkettensorgfaltspflichtengesetz (LkSG), soweit anwendbar. " +
                "Verst\u00f6\u00dfe berechtigen zur fristlosen K\u00fcndigung.");
            n++;

            // Besondere Vereinbarungen
            if (!string.IsNullOrWhiteSpace(data.AdditionalClauses))
            {
                SectionHeading(column, $"\u00a7 {n} Besondere Vereinbarungen");
                Para(column, data.AdditionalClauses);
                n++;
            }

            // Schlussbestimmungen
            SectionHeading(column, $"\u00a7 {n} Schlussbestimmungen");
            Para(column,
                "(1) \u00c4nderungen, Erg\u00e4nzungen und die Aufhebung dieses Vertrages bed\u00fcrfen " +
                "der Schriftform (\u00a7 126 BGB). Dies gilt auch f\u00fcr die Aufhebung dieses " +
                "Schriftformerfordernisses. M\u00fcndliche Nebenabreden bestehen nicht.");
            Para(column,
                "(2) Sollten einzelne Bestimmungen dieses Vertrages ganz oder teilweise " +
                "unwirksam oder undurchf\u00fchrbar sein oder werden, wird hierdurch die " +
                "Wirksamkeit der \u00fcbrigen Bestimmungen nicht ber\u00fchrt (salvatorische Klausel). " +
                "An die Stelle der unwirksamen oder undurchf\u00fchrbaren Bestimmung tritt eine " +
                "wirksame und durchf\u00fchrbare Regelung, die dem wirtschaftlichen Zweck der " +
                "unwirksamen Bestimmung am n\u00e4chsten kommt. Gleiches gilt f\u00fcr etwaige " +
                "Vertragsluecken.");
            Para(column,
                "(3) Die Abtretung von Rechten und Pflichten aus diesem Vertrag an Dritte " +
                "bedarf der vorherigen schriftlichen Zustimmung der jeweils anderen Partei. " +
                "\u00a7 354a HGB bleibt unber\u00fchrt.");
            if (!string.IsNullOrWhiteSpace(data.Jurisdiction))
            {
                Para(column,
                    $"(4) Ausschlie\u00dflicher Gerichtsstand f\u00fcr alle Streitigkeiten aus oder " +
                    $"im Zusammenhang mit diesem Vertrag ist {data.Jurisdiction}, sofern " +
                    "beide Parteien Kaufleute im Sinne des HGB, juristische Personen des " +
                    "\u00f6ffentlichen Rechts oder \u00f6ffentlich-rechtliche Sonderverm\u00f6gen sind " +
                    "(\u00a7 38 Abs. 1 ZPO).");
            }
            var lastSub = string.IsNullOrWhiteSpace(data.Jurisdiction) ? 4 : 5;
            Para(column,
                $"({lastSub}) Auf diesen Vertrag findet ausschlie\u00dflich das Recht der " +
                "Bundesrepublik Deutschland Anwendung unter Ausschluss des \u00dcbereinkommens " +
                "der Vereinten Nationen \u00fcber Vertr\u00e4ge \u00fcber den internationalen Warenkauf " +
                "(UN-Kaufrecht / CISG) sowie der Verweisungsnormen des internationalen " +
                "Privatrechts.");
        }

        // ═══════════════════════════════════════════════════════
        //  UNTERSCHRIFTENBLOCK
        // ═══════════════════════════════════════════════════════
        private void ComposeSignatureBlock(ColumnDescriptor column, ContractData data)
        {
            column.Item().PaddingTop(25).Column(outer =>
            {
                outer.Item().Height(0.5f).Background(BorderLight);
                outer.Item().PaddingTop(20).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("______________________________")
                            .FontSize(9).FontColor(TextMuted);
                        col.Item().PaddingTop(3).Text("Ort, Datum")
                            .FontSize(8).FontColor(TextMuted);
                        col.Item().PaddingTop(28).Text("______________________________")
                            .FontSize(9).FontColor(TextMuted);
                        col.Item().PaddingTop(3).Text("Auftragnehmer")
                            .FontSize(8).Bold().FontColor(Primary);
                        col.Item().Text(data.ContractorCompany)
                            .FontSize(7.5f).FontColor(TextMuted);
                        if (!string.IsNullOrWhiteSpace(data.ContractorName))
                            col.Item().Text(data.ContractorName)
                                .FontSize(7.5f).FontColor(TextMuted);
                    });

                    row.ConstantItem(35);

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("______________________________")
                            .FontSize(9).FontColor(TextMuted);
                        col.Item().PaddingTop(3).Text("Ort, Datum")
                            .FontSize(8).FontColor(TextMuted);
                        col.Item().PaddingTop(28).Text("______________________________")
                            .FontSize(9).FontColor(TextMuted);
                        col.Item().PaddingTop(3).Text("Auftraggeber")
                            .FontSize(8).Bold().FontColor(Primary);
                        col.Item().Text(!string.IsNullOrWhiteSpace(data.ClientCompany)
                            ? data.ClientCompany : data.ClientName)
                            .FontSize(7.5f).FontColor(TextMuted);
                        if (!string.IsNullOrWhiteSpace(data.ClientCompany) &&
                            !string.IsNullOrWhiteSpace(data.ClientName))
                            col.Item().Text(data.ClientName)
                                .FontSize(7.5f).FontColor(TextMuted);
                    });
                });
            });
        }

        // ═══════════════════════════════════════════════════════
        //  FOOTER
        // ═══════════════════════════════════════════════════════
        private void ComposeFooter(IContainer container, ContractData data)
        {
            container.Column(col =>
            {
                col.Item().Height(1).Background(Primary);
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text(data.ContractorCompany)
                            .FontSize(7).Bold().FontColor(Primary);
                        inner.Item().Text(t =>
                        {
                            if (!string.IsNullOrWhiteSpace(data.ContractorStreet))
                                t.Span($"{data.ContractorStreet} | ").FontSize(6.5f).FontColor(TextMuted);
                            if (!string.IsNullOrWhiteSpace(data.ContractorZipCity))
                                t.Span($"{data.ContractorZipCity} | ").FontSize(6.5f).FontColor(TextMuted);
                            if (!string.IsNullOrWhiteSpace(data.ContractorEmail))
                                t.Span(data.ContractorEmail).FontSize(6.5f).FontColor(TextMuted);
                        });
                        if (!string.IsNullOrWhiteSpace(data.ContractorVatId))
                            inner.Item().Text($"USt-IdNr.: {data.ContractorVatId}")
                                .FontSize(6.5f).FontColor(TextMuted);
                    });

                    row.ConstantItem(20);

                    row.ConstantItem(90).AlignRight().AlignBottom().Text(t =>
                    {
                        t.Span("Seite ").FontSize(7).FontColor(TextMuted);
                        t.CurrentPageNumber().FontSize(7).FontColor(Primary).Bold();
                        t.Span(" von ").FontSize(7).FontColor(TextMuted);
                        t.TotalPages().FontSize(7).FontColor(Primary).Bold();
                    });
                });
            });
        }

        // ═══════════════════════════════════════════════════════
        //  HILFSMETHODEN
        // ═══════════════════════════════════════════════════════
        private static void SectionHeading(ColumnDescriptor column, string text)
        {
            column.Item().PaddingTop(14).Column(inner =>
            {
                inner.Item().Row(row =>
                {
                    row.ConstantItem(4).PaddingTop(1).Height(14).Background(Accent);
                    row.ConstantItem(8);
                    row.RelativeItem().Text(text)
                        .FontSize(11.5f).Bold().FontColor(Primary);
                });
                inner.Item().PaddingTop(2).PaddingLeft(12).Height(0.5f).Background(BorderLight);
            });
        }

        private static void Para(ColumnDescriptor column, string text)
        {
            column.Item().PaddingLeft(12).PaddingTop(4).Text(text)
                .FontSize(9.5f).FontColor(TextDark).LineHeight(1.5f);
        }

        private static void BulletPoint(ColumnDescriptor column, string text)
        {
            column.Item().PaddingLeft(20).PaddingTop(1).Row(row =>
            {
                row.ConstantItem(12).PaddingTop(4).Text("\u2022")
                    .FontSize(8).FontColor(Accent);
                row.RelativeItem().Text(text)
                    .FontSize(9).FontColor(TextDark).LineHeight(1.4f);
            });
        }

        private static void OptionalLine(ColumnDescriptor col, string? value, string prefix = "")
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            col.Item().Text($"{prefix}{value}").FontSize(9).FontColor(TextDark);
        }

        private static string Eur(decimal amount) =>
            amount.ToString("N2", De) + " \u20ac";
    }
}
