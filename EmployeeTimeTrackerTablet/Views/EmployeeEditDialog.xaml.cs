#nullable enable
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using EmployeeTimeTracker.Models;
using MessageBox = System.Windows.MessageBox;

namespace EmployeeTimeTrackerTablet.Views
{
    /// <summary>
    /// Employee Add/Edit Dialog for CRUD operations
    /// </summary>
    public partial class EmployeeEditDialog : Window, INotifyPropertyChanged
    {
        private Employee _employee;
        private bool _isNewEmployee;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// The employee being edited
        /// </summary>
        public Employee Employee
        {
            get => _employee;
            set
            {
                _employee = value;
                OnPropertyChanged();
                LoadEmployeeData();
            }
        }

        /// <summary>
        /// True if this is a new employee, false if editing existing
        /// </summary>
        public bool IsNewEmployee
        {
            get => _isNewEmployee;
            set
            {
                _isNewEmployee = value;
                UpdateWindowTitle();
            }
        }

        /// <summary>
        /// Constructor for the Employee Edit Dialog
        /// </summary>
        /// <param name="employee">Employee to edit (or new employee template)</param>
        /// <param name="isNewEmployee">True if adding new employee, false if editing existing</param>
        public EmployeeEditDialog(Employee employee, bool isNewEmployee = false)
        {
            InitializeComponent();
            
            IsNewEmployee = isNewEmployee;
            Employee = employee ?? new Employee
            {
                Active = true,
                PayRate = 15.00m,
                DateHired = DateTime.Now
            };
            
            // Set focus to first name field when window loads
            Loaded += (s, e) => FirstNameTextBox.Focus();
        }

        /// <summary>
        /// Load employee data into the form fields
        /// </summary>
        private void LoadEmployeeData()
        {
            if (Employee == null) return;

            FirstNameTextBox.Text = Employee.FirstName ?? "";
            LastNameTextBox.Text = Employee.LastName ?? "";
            PhoneNumberTextBox.Text = Employee.PhoneNumber ?? "";
            JobTitleTextBox.Text = Employee.JobTitle ?? "";
            PayRateTextBox.Text = Employee.PayRate.ToString("F2");
            DateHiredPicker.SelectedDate = Employee.DateHired;
            ActiveCheckBox.IsChecked = Employee.Active;
        }

        /// <summary>
        /// Update window title based on new/edit mode
        /// </summary>
        private void UpdateWindowTitle()
        {
            if (IsNewEmployee)
            {
                Title = "Add New Employee";
                HeaderTextBlock.Text = "Add New Employee";
            }
            else
            {
                Title = $"Edit Employee - {Employee?.FullName ?? "Unknown"}";
                HeaderTextBlock.Text = "Edit Employee Information";
            }
        }

        /// <summary>
        /// Save button click handler
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateAndSaveData())
            {
                DialogResult = true;
                Close();
            }
        }

        /// <summary>
        /// Cancel button click handler
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Validates input and saves data to Employee object
        /// </summary>
        /// <returns>True if validation passes and data is saved</returns>
        private bool ValidateAndSaveData()
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                MessageBox.Show("First Name is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FirstNameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                MessageBox.Show("Last Name is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                LastNameTextBox.Focus();
                return false;
            }

            // Validate pay rate
            if (!decimal.TryParse(PayRateTextBox.Text, out decimal payRate) || payRate <= 0)
            {
                MessageBox.Show("Please enter a valid pay rate greater than $0.00.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PayRateTextBox.Focus();
                return false;
            }

            if (payRate > 1000)
            {
                MessageBox.Show("Pay rate seems unreasonably high. Please verify.", "Validation Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PayRateTextBox.Focus();
                return false;
            }

            // Validate phone number if provided
            if (!string.IsNullOrWhiteSpace(PhoneNumberTextBox.Text))
            {
                string cleanPhone = PhoneNumberTextBox.Text.Replace("(", "").Replace(")", "")
                    .Replace("-", "").Replace(" ", "").Replace(".", "");

                if (cleanPhone.Length != 10 || !cleanPhone.All(char.IsDigit))
                {
                    MessageBox.Show("Phone Number must be 10 digits (format: (555) 123-4567).", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    PhoneNumberTextBox.Focus();
                    return false;
                }

                // Auto-format the phone number
                PhoneNumberTextBox.Text = $"({cleanPhone.Substring(0, 3)}) {cleanPhone.Substring(3, 3)}-{cleanPhone.Substring(6, 4)}";
            }

            // Save data to Employee object
            Employee.FirstName = FirstNameTextBox.Text.Trim();
            Employee.LastName = LastNameTextBox.Text.Trim();
            Employee.PhoneNumber = PhoneNumberTextBox.Text.Trim();
            Employee.JobTitle = JobTitleTextBox.Text.Trim();
            Employee.PayRate = payRate;
            Employee.DateHired = DateHiredPicker.SelectedDate;
            Employee.Active = ActiveCheckBox.IsChecked ?? true;

            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}