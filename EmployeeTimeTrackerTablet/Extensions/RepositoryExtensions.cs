using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmployeeTimeTracker.Models;

namespace EmployeeTimeTracker.Data
{
    /// <summary>
    /// Extension methods to add async capabilities to existing repositories.
    /// </summary>
    public static class RepositoryExtensions
    {
        /// <summary>
        /// Asynchronously gets all active employees.
        /// </summary>
        /// <param name="repository">The employee repository.</param>
        /// <returns>A task that represents the asynchronous operation containing a list of active employees.</returns>
        public static async Task<List<Employee>> GetActiveEmployeesAsync(this EmployeeRepository repository)
        {
            return await Task.Run(() => repository.GetActiveEmployees());
        }

        /// <summary>
        /// Asynchronously gets an employee by ID.
        /// </summary>
        /// <param name="repository">The employee repository.</param>
        /// <param name="employeeId">The employee ID.</param>
        /// <returns>A task that represents the asynchronous operation containing the employee or null.</returns>
        public static async Task<Employee?> GetByIdAsync(this EmployeeRepository repository, int employeeId)
        {
            return await Task.Run(() => repository.GetEmployeeById(employeeId));
        }

        /// <summary>
        /// Asynchronously searches for employees by name or ID.
        /// </summary>
        /// <param name="repository">The employee repository.</param>
        /// <param name="searchText">The text to search for.</param>
        /// <returns>A task that represents the asynchronous operation containing a list of matching employees.</returns>
        public static async Task<List<Employee>> SearchEmployeesAsync(this EmployeeRepository repository, string searchText)
        {
            return await Task.Run(() =>
            {
                var allEmployees = repository.GetActiveEmployees();
                return allEmployees.Where(e =>
                    e.FirstName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    e.LastName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    e.FullName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    e.EmployeeID.ToString().Contains(searchText))
                    .ToList();
            });
        }

        /// <summary>
        /// Asynchronously gets the current active time entry for an employee.
        /// </summary>
        /// <param name="repository">The time entry repository.</param>
        /// <param name="employeeId">The employee ID.</param>
        /// <returns>A task that represents the asynchronous operation containing the current time entry or null.</returns>
        public static async Task<TimeEntry?> GetCurrentTimeEntryAsync(this TimeEntryRepository repository, int employeeId)
        {
            return await Task.Run(() => repository.GetTimeEntryForDate(employeeId, DateTime.Today));
        }

        /// <summary>
        /// Asynchronously gets time entries for a specific date.
        /// </summary>
        /// <param name="repository">The time entry repository.</param>
        /// <param name="employeeId">The employee ID.</param>
        /// <param name="date">The date to get entries for.</param>
        /// <returns>A task that represents the asynchronous operation containing a list of time entries.</returns>
        public static async Task<List<TimeEntry>> GetTimeEntriesForDateAsync(this TimeEntryRepository repository, int employeeId, DateTime date)
        {
            return await Task.Run(() =>
            {
                var entry = repository.GetTimeEntryForDate(employeeId, date);
                return entry != null ? new List<TimeEntry> { entry } : new List<TimeEntry>();
            });
        }

        /// <summary>
        /// Asynchronously adds a new time entry.
        /// </summary>
        /// <param name="repository">The time entry repository.</param>
        /// <param name="timeEntry">The time entry to add.</param>
        /// <returns>A task that represents the asynchronous operation containing true if successful.</returns>
        public static async Task<bool> AddAsync(this TimeEntryRepository repository, TimeEntry timeEntry)
        {
            return await Task.Run(() => repository.SaveTimeEntry(timeEntry));
        }

        /// <summary>
        /// Asynchronously updates an existing time entry.
        /// </summary>
        /// <param name="repository">The time entry repository.</param>
        /// <param name="timeEntry">The time entry to update.</param>
        /// <returns>A task that represents the asynchronous operation containing true if successful.</returns>
        public static async Task<bool> UpdateAsync(this TimeEntryRepository repository, TimeEntry timeEntry)
        {
            return await Task.Run(() => repository.SaveTimeEntry(timeEntry));
        }
    }
}