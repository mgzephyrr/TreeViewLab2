using System.Windows;

namespace TreeViewLab2
{
    /// <summary>
    /// Логика взаимодействия для AddCompanyDialogWindow.xaml
    /// </summary>
    public partial class AddCompanyDialogWindow : Window
    {
        public AddCompanyDialogWindow()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsValid()) return;
            this.DialogResult = true;
        }
        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
        bool IsValid()
        {
            if (countryTextBox.Text.Length == 0 || companyNameTextBox.Text.Length == 0 || IsEmpty(countryTextBox.Text) || IsEmpty(companyNameTextBox.Text))
            {
                MessageBox.Show("Проверьте, что вы заполнили все поля!");
                return false;
            }

            return true;
        }
        bool IsEmpty(string str)
        {
            foreach (char c in str)
            {
                if (c != ' ') return false;
            }

            return true;
        }
    }
}
