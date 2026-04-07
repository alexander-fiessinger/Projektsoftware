using Projektsoftware.Models;
using System.Collections.Generic;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class NotificationsDialog : Window
    {
        public NotificationsDialog(List<AppNotification> notifications)
        {
            InitializeComponent();

            if (notifications.Count == 0)
            {
                NotificationsList.Visibility = Visibility.Collapsed;
                EmptyText.Visibility = Visibility.Visible;
            }
            else
            {
                NotificationsList.ItemsSource = notifications;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
