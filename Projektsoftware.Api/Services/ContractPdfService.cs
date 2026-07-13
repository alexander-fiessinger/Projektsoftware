using System.Globalization;
using Projektsoftware.Api.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Projektsoftware.Api.Services
{
    /// <summary>
    /// Service zur Generierung von Verträgen als PDF (serverseitig für Blazor).
    /// Unterstützt Dienstleistungsverträge (Robotik) gemäß §§ 611 ff. BGB
    /// und Werkverträge gemäß §§ 631 ff. BGB.
    /// </summary>
    public class ContractPdfService
    {
        private static readonly CultureInfo De = new("de-DE");

        private const string Primary      = "#1B365D";
        private const string PrimaryLight = "#2B5797";
        private const string Accent       = "#C8A251";
        private const string TextDark     = "#1A1A1A";
        private const string TextMuted    = "#5A6270";
        private const string BorderLight  = "#D0D5DD";
        private const string BgSubtle     = "#F6F7F9";

        public ContractPdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] Generate(ContractData data)
        {
            return Document.Create(container =>
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
            .GeneratePdf();
        }

        private void ComposeHeader(IContainer container, ContractData data)
        {
            container.Column(col =>
            {
                col.Item().Height(3).Background(Accent);

                col.Item().PaddingTop(14).PaddingBottom(6).Row(row =>
                {
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text(data.ContractTypeDisplay.ToUpper())
                            .FontSize(22).Bold().FontColor(Primary).LetterSpacing(0.04f);

                        var lawRef = data.ContractType == ContractType.Dienstleistungsvertrag
                            ? "§§ 611 ff. BGB" : "§§ 631 ff. BGB";
                        inner.Item().PaddingTop(2).Text($"gemäß {lawRef}")
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

                col.Item().Height(1.5f).Background(Primary);
            });
        }

        private void ComposeContent(IContainer container, ContractData data)
        {
            container.PaddingTop(10).Column(column =>
            {
                column.Spacing(2);

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

        private void ComposeParties(ColumnDescriptor column, ContractData data)
        {
            SectionHeading(column, "§ 1 Vertragsparteien");

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
                "– nachfolgend einzeln auch „Partei“ und gemeinsam „Parteien“ genannt –")
                .FontSize(8.5f).Italic().FontColor(TextMuted);
        }

        private void ComposeDienstleistungsvertrag(ColumnDescriptor column, ContractData data)
        {
            SectionHeading(column, "§ 2 Vertragsgegenstand");
            Para(column,
                "Der Auftragnehmer verpflichtet sich, für den Auftraggeber die nachstehend " +
                "näher beschriebenen Dienstleistungen im Bereich Robotik, Automatisierungstechnik " +
                "und/oder Software Engineering zu erbringen. Der Auftragnehmer erbringt diese " +
                "Leistungen als selbstständiger Unternehmer; ein Arbeitsverhältnis wird durch " +
                "diesen Vertrag nicht begründet.");
            if (!string.IsNullOrWhiteSpace(data.ContractSubject))
                Para(column, data.ContractSubject);

            SectionHeading(column, "§ 3 Art und Umfang der Leistungen");
            Para(column, data.ServiceDescription);
            if (!string.IsNullOrWhiteSpace(data.ServiceLocation))
                Para(column, $"Leistungsort: {data.ServiceLocation}.");
            if (!string.IsNullOrWhiteSpace(data.WorkingHours))
                Para(column, $"Arbeitszeiten: {data.WorkingHours}.");
            Para(column,
                "(1) Der Auftragnehmer schuldet die sorgfältige Erbringung der vereinbarten " +
                "Tätigkeiten, nicht jedoch einen bestimmten Erfolg (§ 611 Abs. 1 BGB). Die " +
                "Art und Weise der Leistungserbringung bestimmt der Auftragnehmer im Rahmen " +
                "der vertraglichen Vereinbarungen nach eigenem pflichtgemäßem Ermessen.");
            Para(column,
                "(2) Der Auftragnehmer ist berechtigt, zur Erfüllung seiner vertraglichen " +
                "Pflichten qualifizierte Erfüllungsgehilfen einzusetzen. Er haftet für deren " +
                "Verschulden wie für eigenes (§ 278 BGB). Der Einsatz von Subunternehmern " +
                "bedarf der vorherigen schriftlichen Zustimmung des Auftraggebers.");
            Para(column,
                "(3) Änderungen des Leistungsumfangs bedürfen einer schriftlichen Vereinbarung " +
                "zwischen den Parteien (Change-Request-Verfahren). Mehrleistungen, die über den " +
                "vereinbarten Umfang hinausgehen, werden gesondert vergütet, sofern der Auftraggeber " +
                "diese beauftragt oder ihnen zugestimmt hat.");

            SectionHeading(column, "§ 4 Vergütung und Zahlungsbedingungen");
            if (data.HourlyRate.HasValue && data.HourlyRate.Value > 0)
            {
                Para(column,
                    $"(1) Die Vergütung erfolgt auf Basis der tatsächlich geleisteten Stunden " +
                    $"zu einem Stundensatz von {Eur(data.HourlyRate.Value)} (netto) zzgl. der " +
                    $"gesetzlichen Umsatzsteuer in Höhe von derzeit {data.VatRate.ToString("N0", De)} %. " +
                    "Die Erfassung der Arbeitszeiten erfolgt in Einheiten von je 15 Minuten. " +
                    "Der Auftragnehmer legt dem Auftraggeber monatlich einen Leistungsnachweis vor.");
            }
            if (data.NetAmount > 0)
            {
                var prefix = data.HourlyRate.HasValue && data.HourlyRate.Value > 0 ? "(2)" : "(1)";
                Para(column,
                    $"{prefix} Die vereinbarte Pauschalvergütung beträgt {Eur(data.NetAmount)} (netto) " +
                    $"zzgl. {data.VatRate.ToString("N0", De)} % Umsatzsteuer " +
                    $"= {Eur(data.GrossAmount)} (brutto).");
            }
            Para(column,
                $"Die Rechnungsstellung erfolgt monatlich nachträglich. Rechnungen sind " +
                $"innerhalb von {data.PaymentTerms} zur Zahlung fällig. Bei Zahlungsverzug " +
                "gelten die gesetzlichen Verzugsregeln (§§ 286, 288 BGB); der Verzugszinssatz " +
                "beträgt für Rechtsgeschäfte, an denen ein Verbraucher nicht beteiligt ist, " +
                "9 Prozentpunkte über dem jeweiligen Basiszinssatz (§ 288 Abs. 2 BGB).");
            Para(column,
                "Reise- und Übernachtungskosten werden nur erstattet, wenn sie im Vorfeld " +
                "schriftlich genehmigt wurden. Die Erstattung erfolgt gegen Vorlage von Belegen " +
                "nach tatsächlichem Aufwand. Fahrtkosten werden mit 0,30 €/km (PKW) bzw. " +
                "nach tatsächlichen Kosten (öffentliche Verkehrsmittel) abgerechnet.");

            SectionHeading(column, "§ 5 Vertragslaufzeit und Kündigung");
            Para(column,
                $"(1) Dieser Vertrag tritt am {data.ContractStart:dd.MM.yyyy} in Kraft" +
                (data.ContractEnd.HasValue
                    ? $" und endet mit Ablauf des {data.ContractEnd.Value:dd.MM.yyyy}, " +
                      "ohne dass es einer Kündigung bedarf (befristeter Vertrag)."
                    : " und läuft auf unbestimmte Zeit."));
            if (!data.ContractEnd.HasValue)
            {
                Para(column,
                    $"(2) Der Vertrag kann von jeder Partei ordentlich mit einer Frist von " +
                    $"{data.NoticePeriod} gekündigt werden. Die Kündigung bedarf der " +
                    "Schriftform (§ 126 BGB).");
            }
            Para(column,
                $"({(data.ContractEnd.HasValue ? "2" : "3")}) Das Recht beider Parteien zur " +
                "fristlosen Kündigung aus wichtigem Grund gemäß § 626 BGB bleibt unberührt. " +
                "Ein wichtiger Grund liegt insbesondere vor, wenn " +
                "a) die andere Partei trotz schriftlicher Abmahnung und angemessener Fristsetzung " +
                "wesentliche Vertragspflichten wiederholt verletzt, " +
                "b) über das Vermögen der anderen Partei ein Insolvenzverfahren eröffnet oder " +
                "die Eröffnung mangels Masse abgelehnt wird, oder " +
                "c) die andere Partei ihre Geschäftstätigkeit einstellt.");
            Para(column,
                $"({(data.ContractEnd.HasValue ? "3" : "4")}) Im Falle der Kündigung hat der " +
                "Auftragnehmer alle bis dahin erbrachten Leistungen ordnungsgemäß zu " +
                "dokumentieren und sämtliche Unterlagen, Materialien und Zugangsdaten " +
                "des Auftraggebers unverzüglich zurückzugeben. Bereits erbrachte Leistungen " +
                "sind entsprechend dem Leistungsstand zu vergüten.");

            SectionHeading(column, "§ 6 Mitwirkungspflichten des Auftraggebers");
            Para(column,
                "(1) Der Auftraggeber stellt dem Auftragnehmer alle für die Erbringung der " +
                "Leistungen erforderlichen Informationen, Unterlagen, Zugänge zu IT-Systemen " +
                "und sonstige Ressourcen rechtzeitig, vollständig und unentgeltlich zur " +
                "Verfügung.");
            Para(column,
                "(2) Der Auftraggeber benennt einen fachlich qualifizierten Ansprechpartner, " +
                "der für die Dauer der Vertragslaufzeit zur Abstimmung und Entscheidungsfindung " +
                "berechtigt ist. Entscheidungen, die für den Fortgang der Leistungserbringung " +
                "erforderlich sind, trifft der Ansprechpartner innerhalb angemessener Frist.");
            Para(column,
                "(3) Verzögerungen, die auf eine Verletzung der Mitwirkungspflichten des " +
                "Auftraggebers zurückzuführen sind, gehen nicht zulasten des Auftragnehmers. " +
                "Hieraus entstehende Mehrkosten trägt der Auftraggeber. Vereinbarte Termine " +
                "verschieben sich entsprechend.");
        }

        private void ComposeWerkvertrag(ColumnDescriptor column, ContractData data)
        {
            SectionHeading(column, "§ 2 Vertragsgegenstand");
            Para(column,
                "Der Auftragnehmer verpflichtet sich, das nachfolgend näher beschriebene Werk " +
                "herzustellen und dem Auftraggeber das vereinbarte Ergebnis frei von Sach- und " +
                "Rechtsmängeln zu übergeben (§ 631 Abs. 1 BGB). Der Auftraggeber verpflichtet " +
                "sich, das vertragsmäß hergestellte Werk abzunehmen und die vereinbarte " +
                "Vergütung zu entrichten.");
            if (!string.IsNullOrWhiteSpace(data.ContractSubject))
                Para(column, data.ContractSubject);

            SectionHeading(column, "§ 3 Werkleistung und technische Spezifikation");
            Para(column, data.ServiceDescription);
            Para(column,
                "(1) Das Werk muss den anerkannten Regeln der Technik sowie sämtlichen " +
                "einschlägigen Normen und Sicherheitsvorschriften entsprechen, insbesondere:");
            BulletPoint(column, "Maschinenverordnung (EU) 2023/1230 (ab 20.01.2027; bis dahin Maschinenrichtlinie 2006/42/EG)");
            BulletPoint(column, "DIN EN ISO 10218-1/-2 (Industrieroboter – Sicherheitsanforderungen)");
            BulletPoint(column, "DIN EN ISO 13849-1/-2 (Sicherheit von Steuerungen)");
            BulletPoint(column, "DIN EN 62061 (Funktionale Sicherheit)");
            BulletPoint(column, "DIN EN ISO 12100 (Risikobeurteilung und Risikominderung)");
            BulletPoint(column, "Ggf. weitere projektspezifisch vereinbarte Normen und Richtlinien");
            Para(column,
                "(2) Der Auftragnehmer erstellt die für die CE-Konformitätserklärung und " +
                "Risikobeurteilung erforderliche technische Dokumentation, sofern dies vertraglich " +
                "vereinbart ist. Die Dokumentation umfasst mindestens: Beschreibung der verwendeten " +
                "Normen, Ergebnisse der Risikoanalyse, Schaltpläne und Softwarebeschreibung.");
            Para(column,
                "(3) Änderungen der Werkleistung nach Vertragsschluss bedürfen einer " +
                "schriftlichen Änderungsvereinbarung (Change Order). Der Auftragnehmer " +
                "informiert den Auftraggeber unverzüglich über die Auswirkungen auf Kosten " +
                "und Zeitplan. Ohne schriftliche Beauftragung besteht kein Anspruch auf " +
                "Vergütung von Mehrleistungen.");

            SectionHeading(column, "§ 4 Vergütung und Zahlungsbedingungen");
            Para(column,
                $"(1) Die Gesamtvergütung für das Werk beträgt {Eur(data.NetAmount)} (netto) " +
                $"zzgl. {data.VatRate.ToString("N0", De)} % Umsatzsteuer " +
                $"= {Eur(data.GrossAmount)} (brutto).");
            Para(column,
                $"(2) Sofern nicht anders vereinbart, wird die Vergütung wie folgt fällig: " +
                $"{data.WorkPaymentSchedule} (§ 641 Abs. 1 BGB).");
            Para(column,
                $"(3) Zahlungen sind innerhalb von {data.PaymentTerms} nach Rechnungszugang " +
                "zu leisten. Bei Zahlungsverzug gelten die gesetzlichen Verzugsregeln " +
                "(§§ 286, 288 BGB); der Verzugszinssatz beträgt für unternehmerische " +
                "Rechtsgeschäfte 9 Prozentpunkte über dem jeweiligen Basiszinssatz " +
                "(§ 288 Abs. 2 BGB).");
            Para(column,
                "(4) Der Auftraggeber ist zur Aufrechnung nur mit unbestrittenen oder " +
                "rechtskräftig festgestellten Forderungen berechtigt. Ein Zurückbehaltungsrecht " +
                "kann nur geltend gemacht werden, soweit es auf demselben Vertragsverhältnis beruht.");

            SectionHeading(column, "§ 5 Fertigstellung und Lieferung");
            if (data.DeliveryDate.HasValue)
            {
                Para(column,
                    $"(1) Die Fertigstellung und Übergabe des Werkes erfolgt spätestens am " +
                    $"{data.DeliveryDate.Value:dd.MM.yyyy}. Dieser Termin ist ein verbindlicher " +
                    "Fixtermin im Sinne des § 323 Abs. 2 Nr. 2 BGB, sofern die Parteien nichts " +
                    "anderes schriftlich vereinbaren.");
            }
            else
            {
                Para(column,
                    "(1) Der Fertigstellungstermin wird zwischen den Parteien in einem " +
                    "gesonderten Projektplan schriftlich vereinbart.");
            }
            Para(column,
                "(2) Gerät der Auftragnehmer mit der Fertigstellung in Verzug, kann der " +
                "Auftraggeber nach fruchtlosem Ablauf einer angemessenen Nachfrist von mindestens " +
                "14 Werktagen die in §§ 323, 281 BGB vorgesehenen Rechte geltend machen " +
                "(Rücktritt und/oder Schadensersatz statt der Leistung).");
            Para(column,
                "(3) Der Auftragnehmer informiert den Auftraggeber unverzüglich, sobald " +
                "erkennbar ist, dass der vereinbarte Fertigstellungstermin voraussichtlich " +
                "nicht eingehalten werden kann. Die Parteien stimmen in diesem Fall gemeinsam " +
                "eine angepasste Terminplanung ab.");

            SectionHeading(column, "§ 6 Abnahme");
            Para(column,
                "(1) Der Auftragnehmer zeigt dem Auftraggeber die Fertigstellung des Werkes " +
                "schriftlich an (Fertigstellungsanzeige). Der Auftraggeber ist verpflichtet, " +
                "das Werk innerhalb von 14 Werktagen nach Zugang der Fertigstellungsanzeige " +
                "zu prüfen und abzunehmen (§ 640 BGB).");
            Para(column,
                "(2) Die Abnahme erfolgt durch Unterzeichnung eines gemeinsamen " +
                "Abnahmeprotokolls. In diesem sind etwaige Mängel, offene Punkte und " +
                "Vorbehalte zu vermerken.");
            if (!string.IsNullOrWhiteSpace(data.AcceptanceCriteria))
                Para(column, $"(3) Abnahmekriterien: {data.AcceptanceCriteria}");
            var nextAbnahme = string.IsNullOrWhiteSpace(data.AcceptanceCriteria) ? 3 : 4;
            Para(column,
                $"({nextAbnahme}) Die Abnahme darf nicht wegen unwesentlicher Mängel verweigert werden " +
                "(§ 640 Abs. 1 Satz 2 BGB). Ein Mangel ist unwesentlich, wenn er die " +
                "Gebrauchstauglichkeit des Werkes nicht oder nur geringfügig beeinträchtigt " +
                "und mit vertretbarem Aufwand beseitigt werden kann.");
            Para(column,
                $"({nextAbnahme + 1}) Lässt der Auftraggeber die Abnahmefrist verstreichen, ohne Mängel " +
                "zu rügen, gilt das Werk als abgenommen (fiktive Abnahme). Der Auftragnehmer " +
                "weist den Auftraggeber in der Fertigstellungsanzeige auf diese Rechtsfolge hin.");

            SectionHeading(column, "§ 7 Mängelansprüche und Gewährleistung");
            Para(column,
                $"(1) Die Gewährleistungsfrist beträgt {data.WarrantyPeriod}. Die Frist " +
                "beginnt mit der Abnahme des Werkes (§ 634a Abs. 2 BGB). Für Nachbesserungen " +
                "beginnt die Frist hinsichtlich des nachgebesserten Teils erneut zu laufen.");
            Para(column,
                "(2) Im Falle eines Mangels hat der Auftraggeber zunächst Anspruch auf " +
                "Nacherfüllung (§ 635 BGB). Der Auftragnehmer hat das Wahlrecht zwischen " +
                "Mangelbeseitigung und Neuherstellung, soweit die gewählte Art der " +
                "Nacherfüllung nicht für den Auftraggeber unzumutbar ist.");
            Para(column,
                "(3) Scheitert die Nacherfüllung auch nach dem zweiten Versuch oder " +
                "verweigert der Auftragnehmer die Nacherfüllung, kann der Auftraggeber " +
                "nach seiner Wahl Minderung (§ 638 BGB), Rücktritt (§ 636 BGB) " +
                "oder Schadensersatz statt der Leistung (§ 634 Nr. 4 i.V.m. §§ 280, 281 BGB) " +
                "verlangen.");
            Para(column,
                "(4) Offensichtliche Mängel sind dem Auftragnehmer unverzüglich, spätestens " +
                "jedoch innerhalb von 10 Werktagen nach Abnahme, schriftlich und unter " +
                "genauer Beschreibung des Mangels anzuzeigen. Verdeckte Mängel sind unverzüglich " +
                "nach Entdeckung schriftlich anzuzeigen. Unterlässt der Auftraggeber die " +
                "rechtzeitige Mängelanzeige, verliert er seine Gewährleistungsansprüche " +
                "hinsichtlich des betreffenden Mangels, es sei denn, der Auftragnehmer hat " +
                "den Mangel arglistig verschwiegen.");

            SectionHeading(column, "§ 8 Mitwirkungspflichten des Auftraggebers");
            Para(column,
                "(1) Der Auftraggeber stellt dem Auftragnehmer alle für die Herstellung des " +
                "Werkes erforderlichen Informationen, Zugänge, technischen Voraussetzungen " +
                "und räumlichen Gegebenheiten rechtzeitig, vollständig und unentgeltlich " +
                "zur Verfügung.");
            Para(column,
                "(2) Der Auftraggeber benennt einen fachlich und organisatorisch verantwortlichen " +
                "Ansprechpartner, der zur Abgabe und Entgegennahme von Erklärungen im Rahmen " +
                "dieses Vertrages bevollmächtigt ist.");
            Para(column,
                "(3) Verzögert sich die Fertigstellung aufgrund unterlassener oder verspäteter " +
                "Mitwirkung des Auftraggebers, verlängert sich der vereinbarte " +
                "Fertigstellungstermin entsprechend. Dem Auftragnehmer stehen in diesem Fall " +
                "Ansprüche auf Ersatz der durch die Verzögerung entstandenen Mehrkosten zu.");
        }

        private void ComposeCommonClauses(ColumnDescriptor column, ContractData data)
        {
            var n = data.ContractType == ContractType.Werkvertrag ? 9 : 7;

            SectionHeading(column, $"§ {n} Haftungsbeschränkung");
            Para(column,
                "(1) Der Auftragnehmer haftet unbeschränkt für Schäden aus der Verletzung " +
                "des Lebens, des Körpers oder der Gesundheit, die auf einer vorsätzlichen " +
                "oder fahrlässigen Pflichtverletzung des Auftragnehmers, seiner gesetzlichen " +
                "Vertreter oder Erfüllungsgehilfen beruhen.");
            Para(column,
                "(2) Für sonstige Schäden haftet der Auftragnehmer nur bei Vorsatz und grober " +
                "Fahrlässigkeit sowie bei schuldhafter Verletzung wesentlicher Vertragspflichten " +
                "(Kardinalpflichten). Wesentliche Vertragspflichten sind solche, deren Erfüllung " +
                "die ordnungsgemäße Durchführung des Vertrages überhaupt erst ermöglicht und " +
                "auf deren Einhaltung der Auftraggeber regelmäßig vertrauen darf.");
            Para(column,
                "(3) Bei der Verletzung wesentlicher Vertragspflichten durch leichte " +
                "Fahrlässigkeit ist die Haftung auf den vertragstypischen, bei Vertragsschluss " +
                "vorhersehbaren Schaden begrenzt.");
            Para(column,
                "(4) Die Haftung nach dem Produkthaftungsgesetz (ProdHaftG), für die Übernahme " +
                "einer Garantie oder aus arglistigem Verschweigen eines Mangels bleibt von den " +
                "vorstehenden Beschränkungen unberührt.");
            Para(column,
                "(5) Soweit die Haftung des Auftragnehmers ausgeschlossen oder beschränkt ist, " +
                "gilt dies auch zugunsten seiner Organe, Mitarbeiter, Vertreter und " +
                "Erfüllungsgehilfen.");
            n++;

            SectionHeading(column, $"§ {n} Vertraulichkeit und Geheimhaltung");
            Para(column,
                "(1) Die Parteien verpflichten sich, sämtliche im Zusammenhang mit diesem " +
                "Vertrag erlangten vertraulichen Informationen, Geschäfts- und " +
                "Betriebsgeheimnisse der jeweils anderen Partei streng vertraulich zu " +
                "behandeln, nicht an Dritte weiterzugeben und ausschließlich für die " +
                "Zwecke dieses Vertrages zu verwenden. Als vertraulich gelten alle " +
                "Informationen, die als solche gekennzeichnet sind oder deren vertraulicher " +
                "Charakter sich aus den Umständen ergibt.");
            Para(column,
                "(2) Die Geheimhaltungspflicht gilt nicht für Informationen, die " +
                "a) zum Zeitpunkt der Mitteilung bereits öffentlich bekannt waren, " +
                "b) nach der Mitteilung ohne Verschulden der empfangenden Partei öffentlich " +
                "werden, c) der empfangenden Partei bereits vor der Mitteilung " +
                "rechtmäßig bekannt waren, d) der empfangenden Partei von einem Dritten " +
                "ohne Vertraulichkeitsbeschränkung rechtmäßig mitgeteilt werden, oder " +
                "e) aufgrund gesetzlicher Verpflichtung oder behördlicher/gerichtlicher " +
                "Anordnung offengelegt werden müssen.");
            Para(column,
                "(3) Die Geheimhaltungspflicht besteht während der Vertragslaufzeit und " +
                "für einen Zeitraum von 5 (fünf) Jahren nach Vertragsbeendigung fort. " +
                "Auf Verlangen der offenlegenden Partei sind alle vertraulichen Unterlagen " +
                "und Kopien unverzüglich zurückzugeben oder nachweislich zu vernichten.");
            n++;

            SectionHeading(column, $"§ {n} Datenschutz");
            Para(column,
                "(1) Die Parteien verpflichten sich, die Bestimmungen der Verordnung (EU) " +
                "2016/679 (Datenschutz-Grundverordnung – DSGVO) sowie des " +
                "Bundesdatenschutzgesetzes (BDSG) in der jeweils geltenden Fassung einzuhalten.");
            Para(column,
                "(2) Soweit der Auftragnehmer im Rahmen der Vertragserfüllung personenbezogene " +
                "Daten im Auftrag des Auftraggebers verarbeitet, schließen die Parteien vor " +
                "Beginn der Verarbeitung eine gesonderte Auftragsverarbeitungsvereinbarung " +
                "gemäß Art. 28 DSGVO ab.");
            Para(column,
                "(3) Jede Partei benennt, soweit gesetzlich erforderlich, einen " +
                "Datenschutzbeauftragten und teilt der anderen Partei dessen Kontaktdaten mit.");
            n++;

            SectionHeading(column, $"§ {n} Geistiges Eigentum und Nutzungsrechte");
            if (data.ContractType == ContractType.Werkvertrag)
            {
                Para(column,
                    "(1) Mit vollständiger Bezahlung der vereinbarten Vergütung gehen " +
                    "sämtliche ausschließlichen, örtlich, zeitlich und inhaltlich " +
                    "unbeschränkten Nutzungs- und Verwertungsrechte an den im Rahmen dieses " +
                    "Vertrages erstellten Arbeitsergebnissen auf den Auftraggeber über. " +
                    "Dies umfasst insbesondere das Recht zur Vervielfältigung, Bearbeitung, " +
                    "Verbreitung und öffentlichen Zugänglichmachung.");
            }
            else
            {
                Para(column,
                    "(1) Arbeitsergebnisse, die der Auftragnehmer im Rahmen der " +
                    "Vertragserfüllung erstellt, stehen dem Auftraggeber nach vollständiger " +
                    "Bezahlung zur vertragsgemäßen Nutzung zu. Die Übertragung " +
                    "weitergehender Nutzungsrechte bedarf einer gesonderten schriftlichen " +
                    "Vereinbarung.");
            }
            Para(column,
                "(2) Vorbestehende Schutzrechte (Background-IP) des Auftragnehmers bleiben " +
                "unberührt. Soweit vorbestehende Schutzrechte in die Arbeitsergebnisse " +
                "einfließen, räumt der Auftragnehmer dem Auftraggeber ein nicht-ausschließliches, " +
                "zeitlich unbeschränktes Nutzungsrecht im vertraglich vereinbarten Umfang ein.");
            Para(column,
                "(3) Der Auftragnehmer gewährleistet, dass die erbrachten Leistungen und " +
                "Arbeitsergebnisse keine Schutzrechte Dritter (insbesondere Urheber-, Patent- " +
                "oder Markenrechte) verletzen. Er stellt den Auftraggeber von allen Ansprüchen " +
                "Dritter frei, die aus einer Verletzung solcher Rechte entstehen.");
            n++;

            SectionHeading(column, $"§ {n} Höhere Gewalt (Force Majeure)");
            Para(column,
                "(1) Keine Partei haftet für die Nichterfüllung oder verspätete Erfüllung " +
                "ihrer vertraglichen Pflichten, soweit dies auf höhere Gewalt zurückzuführen " +
                "ist. Höhere Gewalt umfasst insbesondere Naturkatastrophen, Epidemien/Pandemien, " +
                "behördliche Anordnungen, Krieg, Terror, Aufruhr, Streik, Aussperrung, " +
                "Lieferkettenunterbrechungen sowie großflächige Ausfälle der " +
                "Telekommunikationsinfrastruktur oder Energieversorgung.");
            Para(column,
                "(2) Die betroffene Partei hat die andere Partei unverzüglich, spätestens " +
                "jedoch innerhalb von 5 Werktagen, schriftlich über den Eintritt und die " +
                "voraussichtliche Dauer des Hindernisses zu informieren. Vertragliche Fristen " +
                "verlängern sich um die Dauer der Behinderung.");
            Para(column,
                "(3) Dauert die höhere Gewalt länger als 90 Kalendertage an, ist jede Partei " +
                "berechtigt, den Vertrag mit sofortiger Wirkung schriftlich zu kündigen. " +
                "Bereits erbrachte Leistungen sind in diesem Fall anteilig zu vergüten.");
            n++;

            SectionHeading(column, $"§ {n} Compliance und Anti-Korruption");
            Para(column,
                "Die Parteien verpflichten sich, alle anwendbaren gesetzlichen Bestimmungen " +
                "einzuhalten, insbesondere die Vorschriften zur Bekämpfung von Korruption " +
                "und Bestechung (§§ 299, 331–335 StGB), das Geldwäschegesetz (GwG) sowie " +
                "das Lieferkettensorgfaltspflichtengesetz (LkSG), soweit anwendbar. " +
                "Verstöße berechtigen zur fristlosen Kündigung.");
            n++;

            if (!string.IsNullOrWhiteSpace(data.AdditionalClauses))
            {
                SectionHeading(column, $"§ {n} Besondere Vereinbarungen");
                Para(column, data.AdditionalClauses);
                n++;
            }

            SectionHeading(column, $"§ {n} Schlussbestimmungen");
            Para(column,
                "(1) Änderungen, Ergänzungen und die Aufhebung dieses Vertrages bedürfen " +
                "der Schriftform (§ 126 BGB). Dies gilt auch für die Aufhebung dieses " +
                "Schriftformerfordernisses. Mündliche Nebenabreden bestehen nicht.");
            Para(column,
                "(2) Sollten einzelne Bestimmungen dieses Vertrages ganz oder teilweise " +
                "unwirksam oder undurchführbar sein oder werden, wird hierdurch die " +
                "Wirksamkeit der übrigen Bestimmungen nicht berührt (salvatorische Klausel). " +
                "An die Stelle der unwirksamen oder undurchführbaren Bestimmung tritt eine " +
                "wirksame und durchführbare Regelung, die dem wirtschaftlichen Zweck der " +
                "unwirksamen Bestimmung am nächsten kommt. Gleiches gilt für etwaige " +
                "Vertragslücken.");
            Para(column,
                "(3) Die Abtretung von Rechten und Pflichten aus diesem Vertrag an Dritte " +
                "bedarf der vorherigen schriftlichen Zustimmung der jeweils anderen Partei. " +
                "§ 354a HGB bleibt unberührt.");
            if (!string.IsNullOrWhiteSpace(data.Jurisdiction))
            {
                Para(column,
                    $"(4) Ausschließlicher Gerichtsstand für alle Streitigkeiten aus oder " +
                    $"im Zusammenhang mit diesem Vertrag ist {data.Jurisdiction}, sofern " +
                    "beide Parteien Kaufleute im Sinne des HGB, juristische Personen des " +
                    "öffentlichen Rechts oder öffentlich-rechtliche Sondervermögen sind " +
                    "(§ 38 Abs. 1 ZPO).");
            }
            var lastSub = string.IsNullOrWhiteSpace(data.Jurisdiction) ? 4 : 5;
            Para(column,
                $"({lastSub}) Auf diesen Vertrag findet ausschließlich das Recht der " +
                "Bundesrepublik Deutschland Anwendung unter Ausschluss des Übereinkommens " +
                "der Vereinten Nationen über Verträge über den internationalen Warenkauf " +
                "(UN-Kaufrecht / CISG) sowie der Verweisungsnormen des internationalen " +
                "Privatrechts.");
        }

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
                row.ConstantItem(12).PaddingTop(4).Text("•")
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
            amount.ToString("N2", De) + " €";
    }
}
