using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Projektsoftware.Views
{
    public partial class MeetingCalendarView : UserControl
    {
        private int currentYear;
        private int currentMonth;
        private DateTime selectedDate;
        private List<Meeting> monthMeetings = new();
        private List<Project> projects = new();

        private readonly DatabaseService dbService;

        private static readonly string[] MonthNames =
        {
            "Januar", "Februar", "März", "April", "Mai", "Juni",
            "Juli", "August", "September", "Oktober", "November", "Dezember"
        };

        public MeetingCalendarView()
        {
            InitializeComponent();
            dbService = new DatabaseService();
            currentYear = DateTime.Today.Year;
            currentMonth = DateTime.Today.Month;
            selectedDate = DateTime.Today;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public async System.Threading.Tasks.Task LoadAsync()
        {
            await LoadProjectsAsync();
            await RefreshCalendarAsync();
        }

        public async System.Threading.Tasks.Task SetProjectsAsync(List<Project> proj)
        {
            projects = proj;
        }

        // ── Data Loading ──────────────────────────────────────────────────────

        private async System.Threading.Tasks.Task LoadProjectsAsync()
        {
            try
            {
                var all = await dbService.GetAllProjectsAsync();
                projects = all;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Projekte laden fehlgeschlagen: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task RefreshCalendarAsync()
        {
            try
            {
                monthMeetings = await dbService.GetMeetingsByMonthAsync(currentYear, currentMonth);
            }
            catch (Exception ex)
            {
                monthMeetings = new List<Meeting>();
                System.Diagnostics.Debug.WriteLine($"Meetings laden fehlgeschlagen: {ex.Message}");
            }

            MonthYearTextBlock.Text = $"{MonthNames[currentMonth - 1]} {currentYear}";
            BuildCalendarGrid();
            ShowDayDetail(selectedDate);
        }

        // ── Calendar Grid Builder ─────────────────────────────────────────────

        private void BuildCalendarGrid()
        {
            CalendarGrid.Children.Clear();
            CalendarGrid.RowDefinitions.Clear();
            CalendarGrid.ColumnDefinitions.Clear();

            for (int c = 0; c < 7; c++)
                CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int r = 0; r < 6; r++)
                CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) });

            // First day of month (Mon=0 .. Sun=6)
            var firstDay = new DateTime(currentYear, currentMonth, 1);
            int startOffset = ((int)firstDay.DayOfWeek + 6) % 7; // Mon=0

            int daysInMonth = DateTime.DaysInMonth(currentYear, currentMonth);

            for (int cell = 0; cell < 42; cell++)
            {
                int row = cell / 7;
                int col = cell % 7;
                int dayNum = cell - startOffset + 1;

                var cellBorder = new Border
                {
                    Margin = new Thickness(1),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6)
                };

                if (dayNum < 1 || dayNum > daysInMonth)
                {
                    cellBorder.Background = new SolidColorBrush(Color.FromRgb(250, 250, 250));
                    cellBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                }
                else
                {
                    var date = new DateTime(currentYear, currentMonth, dayNum);
                    bool isToday = date == DateTime.Today;
                    bool isSelected = date == selectedDate.Date;
                    bool isWeekend = col >= 5;

                    cellBorder.BorderBrush = isSelected
                        ? (Brush)FindResource("PrimaryBrush")
                        : new SolidColorBrush(Color.FromRgb(224, 224, 224));
                    cellBorder.BorderThickness = isSelected ? new Thickness(2) : new Thickness(1);

                    cellBorder.Background = isSelected
                        ? new SolidColorBrush(Color.FromRgb(227, 242, 253))
                        : isToday
                            ? new SolidColorBrush(Color.FromRgb(255, 243, 224))
                            : isWeekend
                                ? new SolidColorBrush(Color.FromRgb(253, 253, 253))
                                : Brushes.White;

                    var meetings = monthMeetings.Where(m => m.StartTime.Date == date).ToList();

                    var cellContent = BuildDayCell(dayNum, date, isToday, meetings);
                    cellBorder.Child = cellContent;

                    var capturedDate = date;
                    cellBorder.MouseLeftButtonDown += (s, e) => DayCell_Click(capturedDate);
                    cellBorder.Cursor = Cursors.Hand;
                    cellBorder.MouseEnter += (s, e) =>
                    {
                        if (capturedDate != selectedDate.Date)
                            cellBorder.BorderBrush = (Brush)FindResource("PrimaryBrush");
                    };
                    cellBorder.MouseLeave += (s, e) =>
                    {
                        if (capturedDate != selectedDate.Date)
                            cellBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                    };
                }

                Grid.SetRow(cellBorder, row);
                Grid.SetColumn(cellBorder, col);
                CalendarGrid.Children.Add(cellBorder);
            }
        }

        private UIElement BuildDayCell(int dayNum, DateTime date, bool isToday, List<Meeting> meetings)
        {
            var panel = new StackPanel { Margin = new Thickness(4) };

            // Day number
            var dayText = new TextBlock
            {
                Text = dayNum.ToString(),
                FontSize = 13,
                FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                Foreground = isToday
                    ? new SolidColorBrush(Color.FromRgb(230, 81, 0))
                    : new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                Margin = new Thickness(0, 0, 0, 3)
            };

            if (isToday)
            {
                var todayBg = new Border
                {
                    Width = 24, Height = 24,
                    CornerRadius = new CornerRadius(12),
                    Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                var todayNum = new TextBlock
                {
                    Text = dayNum.ToString(),
                    FontSize = 12, FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                todayBg.Child = todayNum;
                panel.Children.Add(todayBg);
            }
            else
            {
                panel.Children.Add(dayText);
            }

            // Meeting chips (max 3 visible)
            int shown = Math.Min(meetings.Count, 3);
            for (int i = 0; i < shown; i++)
            {
                var m = meetings[i];
                var chip = new Border
                {
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(3, 1, 3, 1),
                    Margin = new Thickness(0, 1, 0, 0),
                    Background = m.IsWebexMeeting
                        ? new SolidColorBrush(Color.FromRgb(33, 150, 243))
                        : new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    Cursor = Cursors.Hand
                };
                chip.Child = new TextBlock
                {
                    Text = $"{m.StartTime:HH:mm} {m.Title}",
                    FontSize = 10,
                    Foreground = Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                panel.Children.Add(chip);
            }

            if (meetings.Count > 3)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"+{meetings.Count - 3} weitere",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    Margin = new Thickness(2, 1, 0, 0)
                });
            }

            return panel;
        }

        // ── Day Detail Panel ──────────────────────────────────────────────────

        private void DayCell_Click(DateTime date)
        {
            selectedDate = date;
            BuildCalendarGrid();
            ShowDayDetail(date);
        }

        private void ShowDayDetail(DateTime date)
        {
            bool isToday = date.Date == DateTime.Today;
            DayDetailHeaderTextBlock.Text = isToday
                ? $"Heute – {date:dddd, dd. MMMM yyyy}"
                : date.ToString("dddd, dd. MMMM yyyy");

            DayMeetingsPanel.Children.Clear();

            var dayMeetings = monthMeetings.Where(m => m.StartTime.Date == date.Date)
                                            .OrderBy(m => m.StartTime)
                                            .ToList();

            if (dayMeetings.Count == 0)
            {
                DayMeetingsPanel.Children.Add(new TextBlock
                {
                    Text = "Keine Meetings an diesem Tag.",
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                    FontStyle = FontStyles.Italic,
                    FontSize = 13,
                    Margin = new Thickness(0, 10, 0, 0)
                });
                return;
            }

            foreach (var meeting in dayMeetings)
            {
                DayMeetingsPanel.Children.Add(BuildMeetingCard(meeting));
            }
        }

        private UIElement BuildMeetingCard(Meeting meeting)
        {
            var card = new Border
            {
                BorderThickness = new Thickness(0, 0, 0, 3),
                BorderBrush = meeting.IsWebexMeeting
                    ? new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    : new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var panel = new StackPanel();

            // Title row
            var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            titleRow.Children.Add(new TextBlock
            {
                Text = meeting.Title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            if (meeting.IsWebexMeeting)
            {
                titleRow.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(4, 1, 4, 1),
                    Margin = new Thickness(6, 0, 0, 0),
                    Child = new TextBlock
                    {
                        Text = "Webex",
                        FontSize = 10,
                        Foreground = Brushes.White
                    }
                });
            }
            panel.Children.Add(titleRow);

            // Time + duration
            panel.Children.Add(new TextBlock
            {
                Text = $"🕐 {meeting.StartTime:HH:mm} – {meeting.EndTime:HH:mm}  ({meeting.DurationText})",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                Margin = new Thickness(0, 0, 0, 2)
            });

            if (!string.IsNullOrEmpty(meeting.Location))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"📍 {meeting.Location}",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                    Margin = new Thickness(0, 0, 0, 2)
                });
            }

            if (!string.IsNullOrEmpty(meeting.ProjectName))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"📁 {meeting.ProjectName}",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                    Margin = new Thickness(0, 0, 0, 2)
                });
            }

            // Webex join link
            if (meeting.IsWebexMeeting && !string.IsNullOrEmpty(meeting.WebexJoinLink))
            {
                var linkText = new TextBlock
                {
                    Text = "🔗 Meeting beitreten",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                    TextDecorations = TextDecorations.Underline,
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(0, 2, 0, 2)
                };
                var url = meeting.WebexJoinLink;
                linkText.MouseLeftButtonDown += (s, e) =>
                {
                    try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                    catch { }
                };
                panel.Children.Add(linkText);
            }

            // Action buttons
            var buttonRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var editBtn = new Button
            {
                Content = "Bearbeiten",
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 6, 0),
                Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 11
            };
            editBtn.Click += (s, e) => EditMeeting_Click(meeting);

            var deleteBtn = new Button
            {
                Content = "Löschen",
                Padding = new Thickness(8, 4, 8, 4),
                Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 11
            };
            deleteBtn.Click += (s, e) => DeleteMeeting_Click(meeting);

            buttonRow.Children.Add(editBtn);
            buttonRow.Children.Add(deleteBtn);
            panel.Children.Add(buttonRow);

            card.Child = panel;
            return card;
        }

        // ── Event Handlers ────────────────────────────────────────────────────

        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            var d = new DateTime(currentYear, currentMonth, 1).AddMonths(-1);
            currentYear = d.Year;
            currentMonth = d.Month;
            _ = RefreshCalendarAsync();
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            var d = new DateTime(currentYear, currentMonth, 1).AddMonths(1);
            currentYear = d.Year;
            currentMonth = d.Month;
            _ = RefreshCalendarAsync();
        }

        private void Today_Click(object sender, RoutedEventArgs e)
        {
            currentYear = DateTime.Today.Year;
            currentMonth = DateTime.Today.Month;
            selectedDate = DateTime.Today;
            _ = RefreshCalendarAsync();
        }

        private async void AddMeeting_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MeetingDialog(projects)
            {
                Owner = Window.GetWindow(this)
            };
            dialog.Owner.IsEnabled = false;
            try
            {
                if (dialog.ShowDialog() == true && dialog.Meeting != null)
                {
                    var meeting = dialog.Meeting;
                    meeting.Id = await dbService.AddMeetingAsync(meeting);

                    // Navigate to the month of the new meeting
                    currentYear = meeting.StartTime.Year;
                    currentMonth = meeting.StartTime.Month;
                    selectedDate = meeting.StartTime.Date;
                    await RefreshCalendarAsync();
                }
            }
            finally
            {
                dialog.Owner.IsEnabled = true;
            }
        }

        private async void EditMeeting_Click(Meeting meeting)
        {
            var dialog = new MeetingDialog(projects, meeting)
            {
                Owner = Window.GetWindow(this)
            };
            var owner = dialog.Owner;
            if (owner != null) owner.IsEnabled = false;
            try
            {
                if (dialog.ShowDialog() == true && dialog.Meeting != null)
                {
                    await dbService.UpdateMeetingAsync(dialog.Meeting);
                    await RefreshCalendarAsync();
                }
            }
            finally
            {
                if (owner != null) owner.IsEnabled = true;
            }
        }

        private async void DeleteMeeting_Click(Meeting meeting)
        {
            var result = MessageBox.Show(
                $"Möchten Sie das Meeting \"{meeting.Title}\" am {meeting.StartTime:dd.MM.yyyy} wirklich löschen?",
                "Meeting löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // Delete Webex meeting if it has one
                if (meeting.IsWebexMeeting && !string.IsNullOrEmpty(meeting.WebexMeetingId))
                {
                    var webexService = new WebexService();
                    if (webexService.IsConfigured)
                    {
                        try { await webexService.DeleteMeetingAsync(meeting.WebexMeetingId!); }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Webex-Meeting konnte nicht gelöscht werden: {ex.Message}");
                        }
                    }
                }

                await dbService.DeleteMeetingAsync(meeting.Id);
                await RefreshCalendarAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Löschen: {ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
