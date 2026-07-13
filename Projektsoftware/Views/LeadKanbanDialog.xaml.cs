using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Projektsoftware.Models;
using Projektsoftware.Services;

namespace Projektsoftware.Views
{
    public partial class LeadKanbanDialog : Window
    {
        private readonly DatabaseService _db = new DatabaseService();
        private Point _dragStart;
        private SalesLead? _draggedLead;

        public LeadKanbanDialog()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                var leads = await _db.GetSalesLeadsAsync();
                ListNeu.ItemsSource = leads.Where(l => l.Status == LeadStatus.Neu).ToList();
                ListBearbeitung.ItemsSource = leads.Where(l => l.Status == LeadStatus.InBearbeitung).ToList();
                ListQualifiziert.ItemsSource = leads.Where(l => l.Status == LeadStatus.Qualifiziert).ToList();
                ListAbgelehnt.ItemsSource = leads.Where(l => l.Status == LeadStatus.Abgelehnt).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Item_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(null);
            if (sender is ListBox lb && lb.SelectedItem is SalesLead lead)
                _draggedLead = lead;
        }

        private void Item_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _draggedLead == null) return;

            var diff = _dragStart - e.GetPosition(null);
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                if (sender is ListBox lb)
                    DragDrop.DoDragDrop(lb, _draggedLead, DragDropEffects.Move);
            }
        }

        private async void Column_Drop(object sender, DragEventArgs e)
        {
            if (sender is not ListBox target || target.Tag is not string statusName) return;
            if (_draggedLead == null) return;

            if (!Enum.TryParse<LeadStatus>(statusName, out var newStatus)) return;
            if (_draggedLead.Status == newStatus) { _draggedLead = null; return; }

            try
            {
                _draggedLead.Status = newStatus;
                await _db.UpdateSalesLeadAsync(_draggedLead);
                _draggedLead = null;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Aktualisieren:\n{ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
    }
}
