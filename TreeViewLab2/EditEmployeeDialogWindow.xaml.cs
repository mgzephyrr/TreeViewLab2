using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TreeViewLab2
{
    /// <summary>
    /// Логика взаимодействия для EditEmployeeDialogWindow.xaml
    /// </summary>
    public partial class EditEmployeeDialogWindow : Window
    {
        public EditEmployeeDialogWindow()
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
            if (employeeNameTextBox.Text.Length == 0 || positionTextBox.Text.Length == 0 || IsEmpty(employeeNameTextBox.Text) || IsEmpty(positionTextBox.Text))
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
