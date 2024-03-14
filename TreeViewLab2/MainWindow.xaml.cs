using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TreeViewLab2
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TreeViewItem Root { get; set; }
        private TreeViewItem draggedItem;
        public MainWindow()
        {
            InitializeComponent();
            GetDatabase();
        }
        public void GetDatabase()
        {
            Root = new TreeViewItem();
            Root.Header = "Companies Database";
            mainTreeView.Items.Add(Root);
            AddContextMenuToRoot();

            using (var conn = GetConnection())
            {
                conn.Open();
                string query =  "SELECT c.company_id, c.company_name, c.company_country, d.department_id, d.department_name, e.employee_id, e.name, e.position " +
                                "FROM public.companies c " +
                                "LEFT JOIN public.departments d ON c.company_id = d.company_id " +
                                "LEFT JOIN public.employees e ON e.department_id = d.department_id ";

                var cmd = new NpgsqlCommand(query, conn);
                List<TreeViewItem> presentCompanies = new List<TreeViewItem>(), presentDepartments = new List<TreeViewItem>();
                TreeViewItem company = null, department = null, employee = null;

                using (var reader = cmd.ExecuteReader()) 
                {
                    while (reader.Read())
                    {
                        int compId = -1, compIndex = -1, depId = -1, depIndex = -1;

                        compId = (int)reader["company_id"];
                        compIndex = Find(compId, presentCompanies);

                        Debug.WriteLine(reader["department_id"]);

                        if (reader["department_id"].ToString().Length != 0)
                        {
                            depId = (int)reader["department_id"];
                            depIndex = Find(depId, presentDepartments);
                        }

                        if (compIndex == -1 && depIndex == -1)
                        {
                            company = new TreeViewItem();
                            company.Tag = reader["company_id"];
                            company.Header = reader["company_name"].ToString() + ": " + reader["company_country"];
                            Root.Items.Add(company);
                            presentCompanies.Add(company);
                            AddContextMenuToCompany(company);

                            if (reader["department_id"].ToString().Length != 0)
                            {
                                department = new TreeViewItem();
                                department.Tag = reader["department_id"];
                                department.Header = reader["department_name"];
                                company.Items.Add(department);
                                presentDepartments.Add(department);
                                AddContextMenuToDepartment(department);
                            }
                        }
                        else if (depIndex == -1 && compIndex != -1)
                        {
                            if (reader["department_id"].ToString().Length != 0)
                            {
                                company = presentCompanies[compIndex];

                                department = new TreeViewItem();
                                department.Tag = reader["department_id"];
                                department.Header = reader["department_name"];
                                company.Items.Add(department);
                                AddContextMenuToDepartment(department);

                                presentDepartments.Add(department);
                            }
                        }
                        else
                        {
                            department = presentDepartments[depIndex];
                        }

                        if (department != null && reader["employee_id"].ToString().Length != 0)
                        {
                            employee = new TreeViewItem();
                            employee.Tag = reader["employee_id"];
                            employee.Header = reader["position"].ToString() + " | " + reader["name"].ToString();
                            department.Items.Add(employee);
                            AddContextMenuToEmployee(employee);
                        }
                    }
                }
            }
        }

        private void AddCompany(object sender, RoutedEventArgs e)
        {
            if (sender == null) return;
            var parent = GetTreeItem(sender);

            var dlg = new AddCompanyDialogWindow();
            dlg.Owner = this;

            if (!(bool)dlg.ShowDialog())
                return;

            using (var conn = GetConnection())
            {
                conn.Open();
                string query = @"INSERT INTO public.companies (company_name, company_country) VALUES(@company_name, @company_country) RETURNING company_id;";
                var cmd = new NpgsqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@company_name", dlg.companyNameTextBox.Text);
                cmd.Parameters.AddWithValue("@company_country", dlg.countryTextBox.Text);

                object id = cmd.ExecuteScalar();

                var company = new TreeViewItem();
                company.Tag = id;
                company.Header = dlg.companyNameTextBox.Text + ": " + dlg.countryTextBox.Text;
                parent.Items.Add(company);
                AddContextMenuToCompany(company);
            }
        }
        private void AddDepartment(object sender, RoutedEventArgs e)
        {
            if (sender == null) return;
            var parent = GetTreeItem(sender);

            var dlg = new AddDepartmentDialogWindow();
            dlg.Owner = this;

            if (!(bool)dlg.ShowDialog()) return;

            using (var conn = GetConnection())
            {
                conn.Open();
                string query = @"INSERT INTO public.departments (department_name, company_id) VALUES(@department_name, @company_id) RETURNING department_id;";
                var cmd = new NpgsqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@department_name", dlg.departmentNameTextBox.Text);
                cmd.Parameters.AddWithValue("@company_id", (int)parent.Tag);

                object id = cmd.ExecuteScalar();

                var department = new TreeViewItem();
                department.Tag = id;
                department.Header = dlg.departmentNameTextBox.Text;
                parent.Items.Add(department);
                AddContextMenuToDepartment(department);
            }
        }
        private void AddEmployee(object sender, RoutedEventArgs e)
        {
            if (sender == null) return;
            var parent = GetTreeItem(sender);

            var dlg = new AddEmployeeDialogWindow();
            dlg.Owner = this;

            if (!(bool)dlg.ShowDialog()) return;

            using (var conn = GetConnection())
            {
                conn.Open();
                string query = @"INSERT INTO public.employees (name, position, department_id) VALUES(@name, @position, @department_id) RETURNING employee_id;";
                var cmd = new NpgsqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@name", dlg.employeeNameTextBox.Text);
                cmd.Parameters.AddWithValue("@position", dlg.positionTextBox.Text);
                cmd.Parameters.AddWithValue("@department_id", (int)parent.Tag);

                object id = cmd.ExecuteScalar();

                var employee = new TreeViewItem();
                employee.Tag = id;
                employee.Header = dlg.positionTextBox.Text + " | " + dlg.employeeNameTextBox.Text;
                parent.Items.Add(employee);
                AddContextMenuToEmployee(employee);
            }
        }

        private void EditCompany(object sender, RoutedEventArgs e)
        {
            if (sender == null) return;
            var parent = GetTreeItem(sender);

            var dlg = new EditCompanyDialogWindow();
            dlg.Owner = this;

            string[] delimiterStrings = { ": " };
            string[] words = parent.Header.ToString().Split(delimiterStrings, System.StringSplitOptions.RemoveEmptyEntries);
            string companyName = words[0];
            string companyCountry = words[1];

            dlg.companyNameTextBox.Text = companyName;
            dlg.countryTextBox.Text = companyCountry;

            if (!(bool)dlg.ShowDialog())
                return;

            using (var conn = GetConnection())
            {
                conn.Open();
                string query = @"UPDATE public.companies SET company_name=@company_name, company_country=@company_country WHERE company_id=@company_id;";

                var cmd = new NpgsqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@company_name", dlg.companyNameTextBox.Text);
                cmd.Parameters.AddWithValue("@company_country", dlg.countryTextBox.Text);
                cmd.Parameters.AddWithValue("@company_id", (int)parent.Tag);

                cmd.ExecuteNonQuery();

                parent.Header = dlg.companyNameTextBox.Text + ": " + dlg.countryTextBox.Text;
            }
        }
        private void EditDepartment(object sender, RoutedEventArgs e)
        {
            if (sender == null) return;
            var parent = GetTreeItem(sender);

            var dlg = new EditDepartmentDialogWindow();
            dlg.Owner = this;

            dlg.departmentNameTextBox.Text = parent.Header.ToString();

            if (!(bool)dlg.ShowDialog())
                return;

            using (var conn = GetConnection())
            {
                conn.Open();
                string query = @"UPDATE public.departments SET department_name=@department_name WHERE department_id=@department_id;";

                var cmd = new NpgsqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@department_name", dlg.departmentNameTextBox.Text);
                cmd.Parameters.AddWithValue("@department_id", (int)parent.Tag);

                cmd.ExecuteNonQuery();

                parent.Header = dlg.departmentNameTextBox.Text;
            }
        }
        private void EditEmployee(object sender, RoutedEventArgs e)
        {
            if (sender == null) return;
            var parent = GetTreeItem(sender);

            var dlg = new EditEmployeeDialogWindow();
            dlg.Owner = this;

            string[] delimiterStrings = { " | " };
            string[] words = parent.Header.ToString().Split(delimiterStrings, System.StringSplitOptions.RemoveEmptyEntries);
            string position = words[0];
            string employeeName = words[1];

            dlg.positionTextBox.Text = position;
            dlg.employeeNameTextBox.Text = employeeName;

            if (!(bool)dlg.ShowDialog())
                return;

            using (var conn = GetConnection())
            {
                conn.Open();
                string query = @"UPDATE public.employees SET name=@name, position=@position WHERE employee_id=@employee_id;";

                var cmd = new NpgsqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@name", dlg.employeeNameTextBox.Text);
                cmd.Parameters.AddWithValue("@position", dlg.positionTextBox.Text);
                cmd.Parameters.AddWithValue("@employee_id", (int)parent.Tag);

                cmd.ExecuteNonQuery();

                parent.Header = dlg.positionTextBox.Text + " | " + dlg.employeeNameTextBox.Text;
            }
        }

        private void DeleteCompany(object sender, RoutedEventArgs e)
        {
            if (sender == null) return;
            var parent = GetTreeItem(sender);

            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Вы уверены, что хотите удалить компанию " + parent.Header.ToString() + "?", 
                "Подтверждение удаления", 
                System.Windows.MessageBoxButton.YesNo);

            if (messageBoxResult == MessageBoxResult.No)
                return;

            using (var conn = GetConnection())
            {
                conn.Open();
                string query = @"DELETE FROM public.companies WHERE company_id=@company_id;";

                var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@company_id", (int)parent.Tag);

                cmd.ExecuteNonQuery();

                TreeViewItem root = parent.Parent as TreeViewItem;
                root.Items.Remove(parent);
            }
        }
        private void DeleteDepartment(object sender, RoutedEventArgs e)
        {
            if (sender == null) return;
            var parent = GetTreeItem(sender);

            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Вы уверены, что хотите удалить отдел " + parent.Header.ToString() + "?",
               "Подтверждение удаления",
               System.Windows.MessageBoxButton.YesNo);

            if (messageBoxResult == MessageBoxResult.No)
                return;

            using (var conn = GetConnection())
            {
                conn.Open();
                string query = @"DELETE FROM public.departments WHERE department_id=@department_id;";

                var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@department_id", (int)parent.Tag);

                cmd.ExecuteNonQuery();

                TreeViewItem root = parent.Parent as TreeViewItem;
                root.Items.Remove(parent);
            }
        }
        private void DeleteEmployee(object sender, RoutedEventArgs e)
        {
            if (sender == null) return;
            var parent = GetTreeItem(sender);

            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Вы уверены, что хотите удалить сотрудника " + parent.Header.ToString() + "?",
               "Подтверждение удаления",
               System.Windows.MessageBoxButton.YesNo);

            if (messageBoxResult == MessageBoxResult.No)
                return;

            using (var conn = GetConnection())
            {
                conn.Open();
                string query = @"DELETE FROM public.employees WHERE employee_id=@employee_id;";

                var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@employee_id", (int)parent.Tag);

                cmd.ExecuteNonQuery();

                TreeViewItem root = parent.Parent as TreeViewItem;
                root.Items.Remove(parent);
            }
        }

        private void AddContextMenuToRoot()
        {
            var rootContextMenu = new ContextMenu();
            Root.ContextMenu = rootContextMenu;
            var mi = new MenuItem();
            mi.Header = "Добавить компанию...";
            rootContextMenu.Items.Add(mi);
            mi.Click += AddCompany;
        }
        private void AddContextMenuToCompany(TreeViewItem company)
        {
            var companyContextMenu = new ContextMenu();
            company.ContextMenu = companyContextMenu;
            var mi1 = new MenuItem();
            mi1.Header = "Добавить отдел...";
            var mi2 = new MenuItem();
            mi2.Header = "Изменить компанию...";
            var mi3 = new MenuItem();
            mi3.Header = "Удалить компанию";

            companyContextMenu.Items.Add(mi1);
            companyContextMenu.Items.Add(mi2);
            companyContextMenu.Items.Add(mi3);

            mi1.Click += AddDepartment;
            mi2.Click += EditCompany;
            mi3.Click += DeleteCompany; 
        }
        private void AddContextMenuToDepartment(TreeViewItem department)
        {
            var departmentContextMenu = new ContextMenu();
            department.ContextMenu = departmentContextMenu;
            var mi1 = new MenuItem();
            mi1.Header = "Добавить сотрудника...";
            var mi2 = new MenuItem();
            mi2.Header = "Изменить отдел...";
            var mi3 = new MenuItem();
            mi3.Header = "Удалить отдел";

            departmentContextMenu.Items.Add(mi1);
            departmentContextMenu.Items.Add(mi2);
            departmentContextMenu.Items.Add(mi3);

            mi1.Click += AddEmployee;
            mi2.Click += EditDepartment;
            mi3.Click += DeleteDepartment;
        }
        private void AddContextMenuToEmployee(TreeViewItem employee)
        {
            var employeeContextMenu = new ContextMenu();
            employee.ContextMenu = employeeContextMenu;
            var mi2 = new MenuItem();
            mi2.Header = "Изменить сотрудника...";
            var mi3 = new MenuItem();
            mi3.Header = "Удалить сотрудника";

            employeeContextMenu.Items.Add(mi2);
            employeeContextMenu.Items.Add(mi3);

            mi2.Click += EditEmployee;
            mi3.Click += DeleteEmployee;
        }

        public int Find(int toCheck, List<TreeViewItem> collection)
        {
            for (int i = 0; i < collection.Count; i++) if ((int)collection[i].Tag == toCheck) return i;
            return -1;
        }
        public TreeViewItem GetTreeItem(object sender)
        {
            MenuItem btn = sender as MenuItem;
            var ctm = btn.Parent as ContextMenu;
            var parent = ctm.PlacementTarget as TreeViewItem;

            return parent;
        }
        private static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(@"");
        }

        private void mainTreeView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                draggedItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
            }
        }

        private void mainTreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // Если нажата левая кнопка мыши и есть перетаскиваемый элемент
            if (e.LeftButton == MouseButtonState.Pressed && draggedItem != null)
            {
                // Начинаем операцию перетаскивания
                DragDrop.DoDragDrop(mainTreeView, draggedItem, DragDropEffects.Move);
            }
        }

        private void mainTreeView_Drop(object sender, DragEventArgs e)
        {
            // Получаем целевой объект, над которым был сброшен элемент
            var targetItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);

            if (draggedItem == Root || targetItem == Root || draggedItem == targetItem) 
            {
                draggedItem = null;
                return;
            }

            int draggedItemDepth = GetDepth(draggedItem);
            int targetItemDepth = GetDepth(targetItem);

            if (draggedItemDepth - targetItemDepth != 1 || draggedItemDepth < 3)
            {
                draggedItem = null;
                return;
            }

            int draggedId = (int)draggedItem.Tag;
            int targetId = (int)targetItem.Tag;

            TreeViewItem prevDraggedParent = draggedItem.Parent as TreeViewItem;
            prevDraggedParent.Items.Remove(draggedItem);
            targetItem.Items.Add(draggedItem);
            string query = "";

            switch (draggedItemDepth)
            {
                case 3:
                    query = $"UPDATE public.departments SET company_id={(int)targetItem.Tag} WHERE department_id={(int)draggedItem.Tag};";
                break;
                case 4:
                    query = $"UPDATE public.employees SET department_id={(int)targetItem.Tag} WHERE employee_id={(int)draggedItem.Tag};";
                break;
            }

            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new NpgsqlCommand(query, conn);
                cmd.ExecuteNonQuery();
            }

            draggedItem = null;
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }
        private int GetDepth(TreeViewItem elem)
        {
            int depth = 0;
            while (elem != null)
            {
                depth++;
                elem = elem.Parent as TreeViewItem;
            }
            return depth;
        }
    }
}
