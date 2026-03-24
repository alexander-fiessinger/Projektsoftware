using System.Windows;
using System.Windows.Input;

namespace Projektsoftware.Views
{
    public partial class PasswordDialog : Window
    {
        // Master-Passwort für kritische Operationen
        private const string MASTER_PASSWORD = "Admin2024!";
        
        public bool IsAuthenticated { get; private set; }

        public PasswordDialog()
        {
            InitializeComponent();
            PasswordBox.Focus();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            ValidatePassword();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            IsAuthenticated = false;
            DialogResult = false;
            Close();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Enter-Taste für schnelle Bestätigung
            if (e.Key == Key.Enter)
            {
                ValidatePassword();
            }
            // Fehler zurücksetzen bei neuer Eingabe
            else
            {
                ErrorTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void ValidatePassword()
        {
            string enteredPassword = PasswordBox.Password;

            if (enteredPassword == MASTER_PASSWORD)
            {
                IsAuthenticated = true;
                DialogResult = true;
                Close();
            }
            else
            {
                // Falsches Passwort
                IsAuthenticated = false;
                ErrorTextBlock.Text = "❌ Falsches Passwort! Zugriff verweigert.";
                ErrorTextBlock.Visibility = Visibility.Visible;
                
                // Passwort-Feld leeren und Fokus setzen
                PasswordBox.Password = "";
                PasswordBox.Focus();
                
                // Optional: Fenster vibrieren lassen (visuelles Feedback)
                var anim = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 10,
                    Duration = System.TimeSpan.FromMilliseconds(50),
                    AutoReverse = true,
                    RepeatBehavior = new System.Windows.Media.Animation.RepeatBehavior(3)
                };
                
                var transform = new System.Windows.Media.TranslateTransform();
                this.RenderTransform = transform;
                transform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, anim);
            }
        }
    }
}
