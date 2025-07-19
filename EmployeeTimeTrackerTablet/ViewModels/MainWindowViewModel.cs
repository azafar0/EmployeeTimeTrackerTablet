using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using EmployeeTimeTracker.Models;

namespace EmployeeTimeTrackerTablet.ViewModels
{
    /// <summary>
    /// ViewModel for the main tablet interface
    /// </summary>
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string _searchText = "";
        private Employee? _selectedEmployee;
        private string _statusMessage = "Welcome! Please select an employee to begin.";
        private string _employeeStatusMessage = "";
        private bool _canClockIn = false;
        private bool _canClockOut = false;

        public MainWindowViewModel()
        {
            // Initialize collections
            EmployeeSuggestions = new ObservableCollection<Employee>();
            
            // Initialize commands (these would need proper implementations)
            ClockInCommand = new RelayCommand(ClockIn, () => CanClockIn);
            ClockOutCommand = new RelayCommand(ClockOut, () => CanClockOut);
            AdminAccessCommand = new RelayCommand(AdminAccess);
            
            // Load initial data
            LoadEmployeeSuggestions();
        }

        #region Properties

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterEmployeeSuggestions();
                }
            }
        }

        public Employee? SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                if (SetProperty(ref _selectedEmployee, value))
                {
                    UpdateEmployeeStatus();
                    UpdateClockingAvailability();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string EmployeeStatusMessage
        {
            get => _employeeStatusMessage;
            set => SetProperty(ref _employeeStatusMessage, value);
        }

        public bool CanClockIn
        {
            get => _canClockIn;
            set => SetProperty(ref _canClockIn, value);
        }

        public bool CanClockOut
        {
            get => _canClockOut;
            set => SetProperty(ref _canClockOut, value);
        }

        public ObservableCollection<Employee> EmployeeSuggestions { get; }

        #endregion

        #region Commands

        public ICommand ClockInCommand { get; }
        public ICommand ClockOutCommand { get; }
        public ICommand AdminAccessCommand { get; }

        #endregion

        #region Methods

        private void LoadEmployeeSuggestions()
        {
            // TODO: Load actual employees from repository
            // For now, add some sample data
            EmployeeSuggestions.Clear();
            
            // This would typically come from your EmployeeRepository
            var sampleEmployees = new[]
            {
                new Employee { EmployeeID = 1, FirstName = "John", LastName = "Doe", JobTitle = "Developer" },
                new Employee { EmployeeID = 2, FirstName = "Jane", LastName = "Smith", JobTitle = "Designer" },
                new Employee { EmployeeID = 3, FirstName = "Mike", LastName = "Johnson", JobTitle = "Manager" },
            };

            foreach (var employee in sampleEmployees)
            {
                EmployeeSuggestions.Add(employee);
            }
        }

        private void FilterEmployeeSuggestions()
        {
            // TODO: Implement filtering based on SearchText
            // This would filter the EmployeeSuggestions collection
        }

        private void UpdateEmployeeStatus()
        {
            if (SelectedEmployee != null)
            {
                EmployeeStatusMessage = $"Job Title: {SelectedEmployee.JobTitle}";
                StatusMessage = $"Employee {SelectedEmployee.FullName} selected. Ready to clock in/out.";
            }
            else
            {
                EmployeeStatusMessage = "";
                StatusMessage = "Welcome! Please select an employee to begin.";
            }
        }

        private void UpdateClockingAvailability()
        {
            if (SelectedEmployee != null)
            {
                // TODO: Check actual clock status from TimeEntryRepository
                // For now, allow both actions (this should be based on actual time entry status)
                CanClockIn = true;
                CanClockOut = true;
            }
            else
            {
                CanClockIn = false;
                CanClockOut = false;
            }
        }

        private void ClockIn()
        {
            if (SelectedEmployee != null)
            {
                // TODO: Implement actual clock-in logic using TimeEntryRepository
                StatusMessage = $"{SelectedEmployee.FullName} clocked in at {DateTime.Now:h:mm tt}";
                CanClockIn = false;
                CanClockOut = true;
            }
        }

        private void ClockOut()
        {
            if (SelectedEmployee != null)
            {
                // TODO: Implement actual clock-out logic using TimeEntryRepository
                StatusMessage = $"{SelectedEmployee.FullName} clocked out at {DateTime.Now:h:mm tt}";
                CanClockIn = true;
                CanClockOut = false;
            }
        }

        private void AdminAccess()
        {
            // TODO: Implement admin access functionality
            StatusMessage = "Admin access requested...";
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// Simple RelayCommand implementation for commands
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }
}