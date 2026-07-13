using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Projektsoftware.Views
{
    public partial class SalesView : UserControl
    {
        private readonly DatabaseService _db = new();
        private readonly SalesCalendarService _sales = new();
        private List<SalesAppointment> _appointments = new();
        private List<SalesLead> _leads = new();
        private Timer? _pollTimer;

        public SalesView()
        {
            InitializeComponent();
            Loaded += async (_, _) =>
            {
                await LoadAsync();
                StartPolling();
            };
        }

        public async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                _appointments = await _db.GetAllSalesAppointmentsAsync();
                AppointmentsGrid.ItemsSource = null;
                AppointmentsGrid.ItemsSource = _appointments;

                _leads = await _db.GetSalesLeadsAsync();
                LeadsGrid.ItemsSource = null;
                LeadsGrid.ItemsSource = _leads;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SalesView Load Fehler: {ex.Message}");
            }
        }

        private void StartPolling()
        {
            if (!_sales.IsConfigured) return;
            // Alle 10 Minuten RSVP-Antworten prüfen
            _pollTimer = new Timer(async _ =>
            {
                await CheckRsvpAsync(silent: true);
            }, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10));
        }

        // ── Events ──────────────────────────────────────────────────────────

        private async void NewAppointment_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SalesAppointmentDialog { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true) return;

            var appt = dlg.Result;
            appt.CreatedBy = "Sales";

            if (dlg.SendInvite && _sales.IsConfigured)
            {
                try
                {
                    var webexWarn = await _sales.SendInvitationAsync(appt, createWebexMeeting: dlg.CreateWebex);
                    var webexInfo = string.IsNullOrEmpty(appt.WebexJoinLink) ? "" : $"\n🎦 Webex: {appt.WebexJoinLink}";
                    var warnLine  = webexWarn != null ? $"\n⚠ {webexWarn}" : "";
                    MessageBox.Show($"✅ Einladung wurde an {appt.ContactEmail} gesendet.{webexInfo}{warnLine}",
                        "Einladung gesendet", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Termin gespeichert, aber E-Mail konnte nicht gesendet werden:\n{ex.Message}",
                        "E-Mail Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else if (dlg.SendInvite && !_sales.IsConfigured)
            {
                MessageBox.Show("Die Sales-EWS-Konfiguration fehlt oder ist unvollständig.\n\nBitte über den ⚙ EWS-Button in der Sales-Toolbar die EWS-Zugangsdaten für das Sales-Postfach hinterlegen.",
                    "EWS nicht konfiguriert", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            await _db.AddSalesAppointmentAsync(appt);
            await LoadAsync();
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not SalesAppointment appt) return;
            var dlg = new SalesAppointmentDialog(appt) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true) return;

            await _db.UpdateSalesAppointmentAsync(dlg.Result);

            if (dlg.SendInvite && _sales.IsConfigured)
            {
                try { await _sales.SendInvitationAsync(dlg.Result, createWebexMeeting: false); }
                catch (Exception ex)
                {
                    MessageBox.Show($"Aktualisierungs-Einladung konnte nicht gesendet werden:\n{ex.Message}",
                        "E-Mail Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            await LoadAsync();
        }

        private async void ResendInvite_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not SalesAppointment appt) return;
            if (!_sales.IsConfigured)
            {
                MessageBox.Show("SMTP ist nicht konfiguriert.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                var webexWarn = await _sales.SendInvitationAsync(appt);
                await _db.UpdateSalesAppointmentAsync(appt); // UID persistieren
                var warnLine = webexWarn != null ? $"\n⚠ {webexWarn}" : "";
                MessageBox.Show($"✅ Einladung erneut gesendet an {appt.ContactEmail}.{warnLine}",
                    "Gesendet", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Senden:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not SalesAppointment appt) return;
            if (MessageBox.Show($"Termin '{appt.Title}' löschen?", "Löschen",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            if (_sales.IsConfigured && !string.IsNullOrEmpty(appt.ICalUid))
            {
                try { await _sales.SendCancellationAsync(appt); }
                catch { /* Absage-Mail optional */ }
            }

            await _db.DeleteSalesAppointmentAsync(appt.Id);
            await LoadAsync();
        }

        private void EwsSettings_Click(object sender, RoutedEventArgs e)
        {
            new SalesEwsSettingsDialog().ShowDialog();
        }

        private async void PollRsvp_Click(object sender, RoutedEventArgs e)
        {
            PollRsvpBtn.IsEnabled = false;
            PollRsvpBtn.Content = "⏳ Prüfe...";
            try
            {
                await CheckRsvpAsync(silent: false);
            }
            finally
            {
                PollRsvpBtn.IsEnabled = true;
                PollRsvpBtn.Content = "🔄 RSVP prüfen";
            }
        }

        private async System.Threading.Tasks.Task CheckRsvpAsync(bool silent)
        {
            if (!_sales.IsConfigured) return;
            try
            {
                var replies = await _sales.PollRsvpRepliesAsync();
                int updated = 0;

                foreach (var (icalUid, status, email) in replies)
                {
                    var match = _appointments.FirstOrDefault(a =>
                        !string.IsNullOrEmpty(a.ICalUid) &&
                        !string.IsNullOrEmpty(icalUid) &&
                        (string.Equals(a.ICalUid, icalUid, StringComparison.OrdinalIgnoreCase) ||
                         icalUid.IndexOf(a.ICalUid, StringComparison.OrdinalIgnoreCase) >= 0 ||
                         a.ICalUid.IndexOf(icalUid, StringComparison.OrdinalIgnoreCase) >= 0));
                    if (match == null) continue;

                    // Pro-Person Status merken
                    var resolvedEmail = email?.Trim();

                    // Fallback: Wenn EWS keine Absender-E-Mail liefert, versuchen wir einen
                    // Teilnehmer aus dem Termin zu ermitteln der noch keinen Status hat
                    if (string.IsNullOrEmpty(resolvedEmail))
                    {
                        var contactEmails = match.ContactEmail
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        // Nimm den ersten Teilnehmer ohne eigenen Eintrag; bei nur einem immer diesen
                        resolvedEmail = contactEmails.FirstOrDefault(e =>
                            !match.RsvpDetails.ContainsKey(e.ToLowerInvariant()))
                            ?? (contactEmails.Length == 1 ? contactEmails[0] : null);
                    }

                    if (!string.IsNullOrEmpty(resolvedEmail))
                        match.RsvpDetails[resolvedEmail.ToLowerInvariant()] = status;

                    // Gesamt-Status: ALLE Teilnehmer-E-Mails einbeziehen.
                    // Wer noch keinen Eintrag hat, gilt als Pending.
                    // Accepted nur wenn wirklich ALLE zugestimmt haben.
                    var allContactEmails = match.ContactEmail
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var allStatuses = allContactEmails
                        .Select(e => match.RsvpDetails.TryGetValue(e.ToLowerInvariant(), out var s) ? s : RsvpStatus.Pending)
                        .ToList();
                    // Wenn keine E-Mails bekannt sind, direkt den empfangenen Status übernehmen
                    var newOverall = allStatuses.Count == 0
                        ? status
                        : allStatuses.Any(v => v == RsvpStatus.Declined)  ? RsvpStatus.Declined
                        : allStatuses.Any(v => v == RsvpStatus.Tentative) ? RsvpStatus.Tentative
                        : allStatuses.All(v => v == RsvpStatus.Accepted)  ? RsvpStatus.Accepted
                        : RsvpStatus.Pending;

                    var detailKey = resolvedEmail?.ToLowerInvariant() ?? string.Empty;
                    if (match.RsvpStatus == newOverall &&
                        (!string.IsNullOrEmpty(detailKey) && match.RsvpDetails.ContainsKey(detailKey))) continue;

                    match.RsvpStatus = newOverall;
                    await _db.UpdateSalesAppointmentRsvpAsync(match.Id, newOverall, match.RsvpDetails);
                    updated++;
                }

                if (updated > 0)
                {
                    await Dispatcher.InvokeAsync(async () => await LoadAsync());
                    if (!silent)
                        MessageBox.Show($"✅ {updated} Termin-Antwort(en) aktualisiert.",
                            "RSVP aktualisiert", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (!silent)
                {
                    MessageBox.Show("Keine neuen Antworten gefunden.",
                        "RSVP prüfen", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                if (!silent)
                    Dispatcher.Invoke(() =>
                        MessageBox.Show($"RSVP-Prüfung fehlgeschlagen:\n{ex.Message}",
                            "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning));
            }
        }

        private void Grid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AppointmentsGrid.SelectedItem is SalesAppointment appt)
                ShowDetail(appt);
        }

        private void AppointmentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AppointmentsGrid.SelectedItem is SalesAppointment appt)
                ShowDetail(appt);
        }

        // ── Detail Panel ────────────────────────────────────────────────────

        private void ShowDetail(SalesAppointment a)
        {
            DetailPanel.Children.Clear();

            void AddRow(string label, string value)
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
                sp.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)) });
                sp.Children.Add(new TextBlock { Text = value, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)), TextWrapping = TextWrapping.Wrap });
                DetailPanel.Children.Add(sp);
            }

            // RSVP Badge
            var rsvpColor = a.RsvpStatus switch
            {
                RsvpStatus.Accepted  => Color.FromRgb(22, 163, 74),
                RsvpStatus.Declined  => Color.FromRgb(220, 38, 38),
                RsvpStatus.Tentative => Color.FromRgb(202, 138, 4),
                _                    => Color.FromRgb(100, 116, 139)
            };
            var badge = new Border
            {
                Background = new SolidColorBrush(rsvpColor),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 0, 16),
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = $"{a.RsvpIcon} {a.RsvpText}",
                    Foreground = Brushes.White,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold
                }
            };
            DetailPanel.Children.Add(badge);

            AddRow("Betreff", a.Title);
            AddRow("Datum", a.AppointmentDate.ToString("dddd, dd. MMMM yyyy"));
            AddRow("Uhrzeit", $"{a.AppointmentDate:HH:mm} – {a.AppointmentEnd:HH:mm}  ({a.DurationText})");
            AddRow("Kontakt", a.ContactName);
            AddRow("Unternehmen", a.ContactCompany);
            AddRow("E-Mail", a.ContactEmail);
            AddRow("Telefon", a.ContactPhone);
            AddRow("Ort", a.Location);
            AddRow("Notizen", a.Notes);
            if (a.RsvpAnsweredAt.HasValue)
                AddRow("Geantwortet am", a.RsvpAnsweredAt.Value.ToString("dd.MM.yyyy HH:mm"));

            // RSVP pro Teilnehmer
            var emails = a.ContactEmail
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (emails.Length > 0)
            {
                var rsvpSp = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
                rsvpSp.Children.Add(new TextBlock
                {
                    Text = "RSVP Teilnehmer",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    Margin = new Thickness(0, 0, 0, 4)
                });

                foreach (var email in emails)
                {
                    if (!a.RsvpDetails.TryGetValue(email.ToLowerInvariant(), out var personStatus))
                        personStatus = RsvpStatus.Pending; // Noch keine Antwort von dieser Person
                    var (icon, color) = personStatus switch
                    {
                        RsvpStatus.Accepted  => ("✅ Angenommen", Color.FromRgb(22, 163, 74)),
                        RsvpStatus.Declined  => ("❌ Abgesagt",   Color.FromRgb(220, 38, 38)),
                        RsvpStatus.Tentative => ("❓ Vielleicht", Color.FromRgb(202, 138, 4)),
                        _                    => ("⏳ Ausstehend", Color.FromRgb(100, 116, 139))
                    };

                    var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var emailTb = new TextBlock
                    {
                        Text = email,
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                        VerticalAlignment = VerticalAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    Grid.SetColumn(emailTb, 0);

                    var statusBadge = new Border
                    {
                        Background = new SolidColorBrush(color),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(6, 2, 6, 2),
                        Child = new TextBlock
                        {
                            Text = icon,
                            FontSize = 11,
                            Foreground = Brushes.White
                        }
                    };
                    Grid.SetColumn(statusBadge, 1);

                    row.Children.Add(emailTb);
                    row.Children.Add(statusBadge);
                    rsvpSp.Children.Add(row);
                }
                DetailPanel.Children.Add(rsvpSp);
            }

            // Webex-Link als klickbarer Button
            if (!string.IsNullOrEmpty(a.WebexJoinLink))
            {
                var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
                sp.Children.Add(new TextBlock
                {
                    Text = "Webex-Meeting",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139))
                });
                var btn = new Button
                {
                    Content = "🎦 Webex-Meeting beitreten",
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(10, 6, 10, 6),
                    Background = new SolidColorBrush(Color.FromRgb(3, 105, 161)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    FontSize = 13,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = a.WebexJoinLink
                };
                btn.Click += (_, _) =>
                {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(a.WebexJoinLink) { UseShellExecute = true }); }
                    catch { }
                };
                sp.Children.Add(btn);
                DetailPanel.Children.Add(sp);
            }
        }

        // ── Tab-Umschaltung ─────────────────────────────────────────────────

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AppointmentToolbar == null || LeadToolbar == null) return;
            bool isLeads = MainTabControl.SelectedIndex == 1;
            AppointmentToolbar.Visibility = isLeads ? Visibility.Collapsed : Visibility.Visible;
            LeadToolbar.Visibility = isLeads ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── Lead-Events ─────────────────────────────────────────────────────

        private async void NewLead_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SalesLeadDialog { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true) return;
            dlg.Result.FileData = dlg.SelectedFileBytes;
            dlg.Result.CreatedBy = "Sales";
            await _db.AddSalesLeadAsync(dlg.Result);
            await LoadAsync();
        }

        private async void EditLead_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not SalesLead lead) return;
            var dlg = new SalesLeadDialog(lead) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true) return;
            if (dlg.SelectedFileBytes != null)
                dlg.Result.FileData = dlg.SelectedFileBytes;
            await _db.UpdateSalesLeadAsync(dlg.Result);
            await LoadAsync();
        }

        private async void LeadsGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (LeadsGrid.SelectedItem is SalesLead lead)
            {
                var dlg = new SalesLeadDialog(lead) { Owner = Window.GetWindow(this) };
                if (dlg.ShowDialog() != true) return;
                if (dlg.SelectedFileBytes != null)
                    dlg.Result.FileData = dlg.SelectedFileBytes;
                await _db.UpdateSalesLeadAsync(dlg.Result);
                await LoadAsync();
            }
        }

        private void OpenLead_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not SalesLead lead) return;
            if (!lead.HasFile)
            {
                MessageBox.Show("Kein Leadbogen vorhanden. Bitte zuerst eine Datei hochladen.",
                    "Keine Datei", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                var ext = System.IO.Path.GetExtension(lead.OriginalFileName);
                if (string.IsNullOrEmpty(ext)) ext = ".pdf";
                var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ProjektsoftwareLeads");
                System.IO.Directory.CreateDirectory(tempDir);
                var safeName = string.Concat((lead.OriginalFileName ?? $"Lead{ext}")
                    .Select(c => System.IO.Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
                var tempPath = System.IO.Path.Combine(tempDir, safeName);
                System.IO.File.WriteAllBytes(tempPath, lead.FileData!);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(tempPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Datei konnte nicht geöffnet werden:\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteLead_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not SalesLead lead) return;
            if (MessageBox.Show($"Lead '{lead.Title}' löschen?", "Löschen",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            await _db.DeleteSalesLeadAsync(lead.Id);
            await LoadAsync();
        }
    }
}
