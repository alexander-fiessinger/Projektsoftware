using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace Projektsoftware.Views
{
    public partial class MeetingDialog : Window
    {
        public Meeting? Meeting { get; private set; }

        private readonly List<Project> projects;
        private readonly Meeting? existingMeeting;
        private readonly WebexService webexService;
        private bool webexMeetingCreated = false;

        public MeetingDialog(List<Project> projects, Meeting? meetingToEdit = null)
        {
            InitializeComponent();
            this.projects = projects;
            this.existingMeeting = meetingToEdit;
            this.webexService = new WebexService();

            PopulateProjects();

            if (meetingToEdit != null)
            {
                HeaderTextBlock.Text = "Meeting bearbeiten";
                Title = "Meeting bearbeiten";
                LoadMeeting(meetingToEdit);
            }
            else
            {
                var defaultStart = new DateTime(
                    DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                    DateTime.Now.Hour + 1, 0, 0);
                MeetingDatePicker.SelectedDate = defaultStart.Date;
                StartTimeBox.Text = defaultStart.ToString("HH:mm");
                EndTimeBox.Text = defaultStart.AddHours(1).ToString("HH:mm");
                UpdateDurationPreview();
            }

            if (!webexService.IsConfigured)
            {
                WebexCheckBox.IsEnabled = false;
                WebexCheckBox.ToolTip = "Webex ist nicht konfiguriert. Bitte unter Einstellungen > Webex konfigurieren.";
            }
        }

        private void PopulateProjects()
        {
            var emptyProject = new Project { Id = 0, Name = "(Kein Projekt)" };
            var projectList = new List<Project> { emptyProject };
            projectList.AddRange(projects);

            ProjectComboBox.ItemsSource = projectList;
            ProjectComboBox.SelectedIndex = 0;
        }

        private void LoadMeeting(Meeting m)
        {
            TitleTextBox.Text = m.Title;
            MeetingDatePicker.SelectedDate = m.StartTime.Date;
            StartTimeBox.Text = m.StartTime.ToString("HH:mm");
            EndTimeBox.Text = m.EndTime.ToString("HH:mm");
            LocationTextBox.Text = m.Location ?? "";
            ParticipantsTextBox.Text = m.Participants ?? "";
            DescriptionTextBox.Text = m.Description ?? "";

            if (m.ProjectId.HasValue)
            {
                foreach (var item in ProjectComboBox.Items)
                {
                    if (item is Project p && p.Id == m.ProjectId.Value)
                    {
                        ProjectComboBox.SelectedItem = p;
                        break;
                    }
                }
            }

            if (m.IsWebexMeeting && !string.IsNullOrEmpty(m.WebexMeetingId))
            {
                WebexCheckBox.IsChecked = true;
                webexMeetingCreated = true;
                ShowWebexInfo(m.WebexJoinLink, m.WebexPassword, m.WebexHostKey);
                WebexStatusTextBlock.Text = "✅ Webex-Meeting bereits erstellt.";
            }

            UpdateDurationPreview();
        }

        private void DateTime_Changed(object sender, object e)
        {
            UpdateDurationPreview();
        }

        private void UpdateDurationPreview()
        {
            if (DurationPreviewTextBlock == null) return;
            if (!TryGetDateTimes(out var start, out var end))
            {
                DurationPreviewTextBlock.Text = "";
                return;
            }

            if (end <= start)
            {
                DurationPreviewTextBlock.Text = "⚠️ Endzeit muss nach der Startzeit liegen.";
                return;
            }

            var dur = end - start;
            if (dur.TotalHours >= 1)
                DurationPreviewTextBlock.Text = $"Dauer: {(int)dur.TotalHours}h {dur.Minutes:D2}m";
            else
                DurationPreviewTextBlock.Text = $"Dauer: {dur.Minutes}m";
        }

        private bool TryGetDateTimes(out DateTime start, out DateTime end)
        {
            start = DateTime.MinValue;
            end = DateTime.MinValue;

            if (MeetingDatePicker.SelectedDate == null) return false;
            if (!TryParseTime(StartTimeBox.Text, out var startTime)) return false;
            if (!TryParseTime(EndTimeBox.Text, out var endTime)) return false;

            var date = MeetingDatePicker.SelectedDate.Value.Date;
            start = date + startTime;
            end = date + endTime;
            return true;
        }

        private static bool TryParseTime(string text, out TimeSpan result)
        {
            result = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(text)) return false;

            if (TimeSpan.TryParse(text.Trim(), out result)) return true;

            // Try HH:mm
            if (text.Contains(':'))
            {
                var parts = text.Split(':');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int h) &&
                    int.TryParse(parts[1], out int m))
                {
                    if (h >= 0 && h <= 23 && m >= 0 && m <= 59)
                    {
                        result = new TimeSpan(h, m, 0);
                        return true;
                    }
                }
            }
            return false;
        }

        private void WebexCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            WebexDetailsPanel.Visibility = Visibility.Visible;

            if (!webexMeetingCreated)
            {
                WebexStatusTextBlock.Text = "📅 Webex-Meeting wird beim Speichern erstellt.";
            }
        }

        private void WebexCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            WebexDetailsPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowWebexInfo(string? joinLink, string? password, string? hostKey)
        {
            WebexInfoBorder.Visibility = Visibility.Visible;
            WebexLinkTextBlock.Text = joinLink ?? "";
            WebexPasswordTextBlock.Text = password ?? "—";
            WebexHostKeyTextBlock.Text = hostKey ?? "—";
        }

        private void WebexLink_Click(object sender, MouseButtonEventArgs e)
        {
            var url = WebexLinkTextBlock.Text;
            if (!string.IsNullOrEmpty(url))
            {
                try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                catch { }
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie einen Titel ein.", "Validierung",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryGetDateTimes(out var start, out var end))
            {
                MessageBox.Show("Bitte geben Sie gültige Datum/Uhrzeit-Werte ein (Format: HH:mm).",
                    "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (end <= start)
            {
                MessageBox.Show("Das Ende muss nach dem Beginn liegen.", "Validierung",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (WebexCheckBox.IsChecked == true && start <= DateTime.Now)
            {
                MessageBox.Show(
                    "Die Startzeit muss in der Zukunft liegen, um ein Webex-Meeting zu erstellen.",
                    "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedProject = ProjectComboBox.SelectedItem as Project;

            var meeting = new Meeting
            {
                Id = existingMeeting?.Id ?? 0,
                Title = TitleTextBox.Text.Trim(),
                StartTime = start,
                EndTime = end,
                Location = LocationTextBox.Text.Trim(),
                Participants = ParticipantsTextBox.Text.Trim(),
                Description = DescriptionTextBox.Text.Trim(),
                ProjectId = (selectedProject != null && selectedProject.Id > 0) ? selectedProject.Id : null,
                ProjectName = (selectedProject != null && selectedProject.Id > 0) ? selectedProject.Name : null,
                IsWebexMeeting = WebexCheckBox.IsChecked == true,
                WebexMeetingId = existingMeeting?.WebexMeetingId,
                WebexJoinLink = existingMeeting?.WebexJoinLink,
                WebexHostKey = existingMeeting?.WebexHostKey,
                WebexPassword = existingMeeting?.WebexPassword,
                WebexSipAddress = existingMeeting?.WebexSipAddress,
                CreatedAt = existingMeeting?.CreatedAt ?? DateTime.Now
            };

            // Webex meeting creation/update
            if (meeting.IsWebexMeeting && webexService.IsConfigured)
            {
                SaveButton.IsEnabled = false;
                SaveButton.Content = "Webex-Meeting wird erstellt...";

                try
                {
                    if (!webexMeetingCreated || string.IsNullOrEmpty(meeting.WebexMeetingId))
                    {
                        WebexStatusTextBlock.Text = "⏳ Erstelle Webex-Meeting...";
                        var response = await webexService.CreateMeetingAsync(meeting);
                        meeting.WebexMeetingId = response.Id;
                        meeting.WebexJoinLink = response.JoinLink ?? response.WebLink;
                        meeting.WebexHostKey = response.HostKey;
                        meeting.WebexPassword = response.Password;
                        meeting.WebexSipAddress = response.SipAddress;
                        ShowWebexInfo(meeting.WebexJoinLink, meeting.WebexPassword, meeting.WebexHostKey);
                        WebexStatusTextBlock.Text = "✅ Webex-Meeting erfolgreich erstellt!";
                    }
                    else
                    {
                        // Update existing Webex meeting
                        await webexService.UpdateMeetingAsync(meeting.WebexMeetingId!, meeting);
                        WebexStatusTextBlock.Text = "✅ Webex-Meeting aktualisiert.";
                    }
                }
                catch (Exception ex)
                {
                    var choice = MessageBox.Show(
                        $"Webex-Meeting konnte nicht erstellt werden:\n\n{ex.Message}\n\n" +
                        "Möchten Sie das Meeting trotzdem lokal speichern?",
                        "Webex-Fehler", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    meeting.IsWebexMeeting = false;
                    meeting.WebexMeetingId = null;

                    if (choice != MessageBoxResult.Yes)
                    {
                        SaveButton.IsEnabled = true;
                        SaveButton.Content = "Speichern";
                        return;
                    }
                }
                finally
                {
                    SaveButton.IsEnabled = true;
                    SaveButton.Content = "Speichern";
                }
            }

            Meeting = meeting;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
