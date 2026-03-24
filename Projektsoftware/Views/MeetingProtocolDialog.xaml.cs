using Projektsoftware.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class MeetingProtocolDialog : Window
    {
        public MeetingProtocol Protocol { get; private set; }
#pragma warning disable IDE0052 // Nicht gelesene private Member entfernen
        private bool isReadOnly;
#pragma warning restore IDE0052 // Nicht gelesene private Member entfernen

        public MeetingProtocolDialog(ObservableCollection<Project> projects)
        {
            InitializeComponent();
            Protocol = new MeetingProtocol();

            // KRITISCH: Erstelle eine KOPIE der Collection
            var projectsCopy = new ObservableCollection<Project>(projects);
            ProjectComboBox.ItemsSource = projectsCopy;

            MeetingDatePicker.SelectedDate = DateTime.Now;
            isReadOnly = false;

            // Verhindere, dass die Auswahl beim Öffnen der DropDown verloren geht
            ProjectComboBox.DropDownClosed += PreserveSelection;
        }

        private void PreserveSelection(object sender, EventArgs e)
        {
            // Diese Methode sorgt dafür, dass die Auswahl erhalten bleibt
        }

        public MeetingProtocolDialog(ObservableCollection<Project> projects, MeetingProtocol protocol) : this(projects)
        {
            Protocol = protocol;
            LoadProtocolData();
            isReadOnly = true;
            SetReadOnlyMode();
        }

        private void LoadProtocolData()
        {
            TitleTextBox.Text = Protocol.Title;
            ProjectComboBox.SelectedValue = Protocol.ProjectId;
            MeetingDatePicker.SelectedDate = Protocol.MeetingDate.Date;
            MeetingTimeTextBox.Text = Protocol.MeetingDate.ToString("HH:mm");
            LocationTextBox.Text = Protocol.Location;
            ParticipantsTextBox.Text = Protocol.Participants;
            AgendaTextBox.Text = Protocol.Agenda;
            DiscussionTextBox.Text = Protocol.Discussion;
            DecisionsTextBox.Text = Protocol.Decisions;
            ActionItemsTextBox.Text = Protocol.ActionItems;
            NextMeetingTextBox.Text = Protocol.NextMeetingDate;
        }

        private void SetReadOnlyMode()
        {
            TitleTextBox.IsReadOnly = true;
            ProjectComboBox.IsEnabled = false;
            MeetingDatePicker.IsEnabled = false;
            MeetingTimeTextBox.IsReadOnly = true;
            LocationTextBox.IsReadOnly = true;
            ParticipantsTextBox.IsReadOnly = true;
            AgendaTextBox.IsReadOnly = true;
            DiscussionTextBox.IsReadOnly = true;
            DecisionsTextBox.IsReadOnly = true;
            ActionItemsTextBox.IsReadOnly = true;
            NextMeetingTextBox.IsReadOnly = true;
            SaveButton.Visibility = Visibility.Collapsed;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectComboBox.SelectedValue == null)
            {
                MessageBox.Show("Bitte wählen Sie ein Projekt.", "Validierung", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie einen Titel ein.", "Validierung", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!MeetingDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Bitte wählen Sie ein Datum.", "Validierung", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var time = DateTime.ParseExact(MeetingTimeTextBox.Text, "HH:mm", null).TimeOfDay;
            var meetingDateTime = MeetingDatePicker.SelectedDate.Value.Date + time;

            Protocol.ProjectId = (int)ProjectComboBox.SelectedValue;
            Protocol.ProjectName = (ProjectComboBox.SelectedItem as Project)?.Name;
            Protocol.Title = TitleTextBox.Text;
            Protocol.MeetingDate = meetingDateTime;
            Protocol.Location = LocationTextBox.Text;
            Protocol.Participants = ParticipantsTextBox.Text;
            Protocol.Agenda = AgendaTextBox.Text;
            Protocol.Discussion = DiscussionTextBox.Text;
            Protocol.Decisions = DecisionsTextBox.Text;
            Protocol.ActionItems = ActionItemsTextBox.Text;
            Protocol.NextMeetingDate = NextMeetingTextBox.Text;

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
