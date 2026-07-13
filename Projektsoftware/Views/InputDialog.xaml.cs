using System.Windows;
using System.Windows.Input;

namespace Projektsoftware.Views
{
    public partial class InputDialog : Window
    {
        public string Result { get; private set; }

        public InputDialog(string title, string message, string defaultValue = "")
        {
            InitializeComponent();

            TitleText.Text = title;
            MessageText.Text = message;
            InputTextBox.Text = defaultValue;

            // Fokus auf Eingabefeld und Text markieren
            Loaded += (s, e) =>
            {
                InputTextBox.Focus();
                InputTextBox.SelectAll();
            };
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Result = InputTextBox.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OK_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                Cancel_Click(sender, e);
            }
        }
    }
}
