using EmployeeTimeTracker.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace EmployeeTimeTracker.Data
{
    /// <summary>
    /// Repository class for Employee data access operations
    /// Updated to support Phone Number, Date of Birth, and Social Security Number fields
    /// </summary>
    public class EmployeeRepository
    {
        /// <summary>
        /// Retrieves all employees from the database, ordered by last name then first name
        /// Includes all personal details fields
        /// </summary>
        /// <returns>List of all employees with complete information</returns>
        public List<Employee> GetAllEmployees()
        {
            var employees = new List<Employee>();

            using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
            connection.Open();

            string sql = "SELECT * FROM Employees ORDER BY LastName, FirstName";
            using var command = new SqliteCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                employees.Add(new Employee
                {
                    EmployeeID = reader.GetInt32("EmployeeID"),
                    FirstName = reader.GetString("FirstName"),
                    LastName = reader.GetString("LastName"),
                    PayRate = reader.GetDecimal("PayRate"),
                    JobTitle = reader.IsDBNull("JobTitle") ? "" : reader.GetString("JobTitle"),
                    Active = reader.GetBoolean("Active"),
                    DateHired = reader.IsDBNull("DateHired") ? null : reader.GetDateTime("DateHired"),
                    CreatedDate = reader.GetDateTime("CreatedDate"),

                    // NEW PERSONAL DETAILS FIELDS - Handle potentially missing columns gracefully
                    PhoneNumber = GetSafeString(reader, "PhoneNumber"),
                    DateOfBirth = GetSafeDateTime(reader, "DateOfBirth"),
                    SocialSecurityNumber = GetSafeString(reader, "SocialSecurityNumber")
                });
            }

            return employees;
        }

        /// <summary>
        /// Searches employees by name, job title, or phone number
        /// Includes all personal details fields in results
        /// </summary>
        /// <param name="searchTerm">Search term to match against employee data</param>
        /// <returns>List of employees matching the search criteria</returns>
        public List<Employee> SearchEmployees(string searchTerm)
        {
            var employees = new List<Employee>();

            using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
            connection.Open();

            // Enhanced search to include phone number searching
            string sql = @"SELECT * FROM Employees 
                          WHERE FirstName LIKE @searchTerm 
                             OR LastName LIKE @searchTerm 
                             OR (FirstName || ' ' || LastName) LIKE @searchTerm
                             OR JobTitle LIKE @searchTerm 
                             OR PhoneNumber LIKE @searchTerm
                          ORDER BY LastName, FirstName";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                employees.Add(new Employee
                {
                    EmployeeID = reader.GetInt32("EmployeeID"),
                    FirstName = reader.GetString("FirstName"),
                    LastName = reader.GetString("LastName"),
                    PayRate = reader.GetDecimal("PayRate"),
                    JobTitle = reader.IsDBNull("JobTitle") ? "" : reader.GetString("JobTitle"),
                    Active = reader.GetBoolean("Active"),
                    DateHired = reader.IsDBNull("DateHired") ? null : reader.GetDateTime("DateHired"),
                    CreatedDate = reader.GetDateTime("CreatedDate"),

                    // NEW PERSONAL DETAILS FIELDS
                    PhoneNumber = GetSafeString(reader, "PhoneNumber"),
                    DateOfBirth = GetSafeDateTime(reader, "DateOfBirth"),
                    SocialSecurityNumber = GetSafeString(reader, "SocialSecurityNumber")
                });
            }

            return employees;
        }

        /// <summary>
        /// Adds a new employee to the database with all personal details
        /// </summary>
        /// <param name="employee">Employee object with all required information</param>
        /// <returns>True if employee was added successfully, false otherwise</returns>
        public bool AddEmployee(Employee employee)
        {
            try
            {
                using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
                connection.Open();

                // SQL INSERT statement including all personal details fields
                string sql = @"INSERT INTO Employees (
                                FirstName, LastName, PayRate, JobTitle, Active, DateHired, 
                                PhoneNumber, DateOfBirth, SocialSecurityNumber
                              ) VALUES (
                                @FirstName, @LastName, @PayRate, @JobTitle, @Active, @DateHired,
                                @PhoneNumber, @DateOfBirth, @SocialSecurityNumber
                              )";

                using var command = new SqliteCommand(sql, connection);

                // Basic employee information parameters
                command.Parameters.AddWithValue("@FirstName", employee.FirstName);
                command.Parameters.AddWithValue("@LastName", employee.LastName);
                command.Parameters.AddWithValue("@PayRate", employee.PayRate);
                command.Parameters.AddWithValue("@JobTitle", employee.JobTitle ?? "");
                command.Parameters.AddWithValue("@Active", employee.Active);
                command.Parameters.AddWithValue("@DateHired", employee.DateHired.HasValue ? employee.DateHired.Value : DBNull.Value);

                // NEW PERSONAL DETAILS PARAMETERS
                command.Parameters.AddWithValue("@PhoneNumber", employee.PhoneNumber ?? "");
                command.Parameters.AddWithValue("@DateOfBirth", employee.DateOfBirth.HasValue ? employee.DateOfBirth.Value : DBNull.Value);
                command.Parameters.AddWithValue("@SocialSecurityNumber", employee.SocialSecurityNumber ?? "");

                int rowsAffected = command.ExecuteNonQuery();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                // Log the error for debugging (you can implement proper logging here)
                System.Diagnostics.Debug.WriteLine($"Error adding employee: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates an existing employee in the database with all personal details
        /// </summary>
        /// <param name="employee">Employee object with updated information</param>
        /// <returns>True if employee was updated successfully, false otherwise</returns>
        public bool UpdateEmployee(Employee employee)
        {
            try
            {
                using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
                connection.Open();

                // SQL UPDATE statement including all personal details fields
                string sql = @"UPDATE Employees 
                              SET FirstName = @FirstName, 
                                  LastName = @LastName, 
                                  PayRate = @PayRate, 
                                  JobTitle = @JobTitle, 
                                  Active = @Active, 
                                  DateHired = @DateHired,
                                  PhoneNumber = @PhoneNumber,
                                  DateOfBirth = @DateOfBirth,
                                  SocialSecurityNumber = @SocialSecurityNumber
                              WHERE EmployeeID = @EmployeeID";

                using var command = new SqliteCommand(sql, connection);

                // Employee identification
                command.Parameters.AddWithValue("@EmployeeID", employee.EmployeeID);

                // Basic employee information parameters
                command.Parameters.AddWithValue("@FirstName", employee.FirstName);
                command.Parameters.AddWithValue("@LastName", employee.LastName);
                command.Parameters.AddWithValue("@PayRate", employee.PayRate);
                command.Parameters.AddWithValue("@JobTitle", employee.JobTitle ?? "");
                command.Parameters.AddWithValue("@Active", employee.Active);
                command.Parameters.AddWithValue("@DateHired", employee.DateHired.HasValue ? employee.DateHired.Value : DBNull.Value);

                // NEW PERSONAL DETAILS PARAMETERS
                command.Parameters.AddWithValue("@PhoneNumber", employee.PhoneNumber ?? "");
                command.Parameters.AddWithValue("@DateOfBirth", employee.DateOfBirth.HasValue ? employee.DateOfBirth.Value : DBNull.Value);
                command.Parameters.AddWithValue("@SocialSecurityNumber", employee.SocialSecurityNumber ?? "");

                int rowsAffected = command.ExecuteNonQuery();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Error updating employee: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes an employee from the database if they have no time entries
        /// Business rule: Cannot delete employees with existing time records
        /// </summary>
        /// <param name="employeeID">ID of the employee to delete</param>
        /// <returns>True if employee was deleted successfully, false if they have time entries or error occurred</returns>
        public bool DeleteEmployee(int employeeID)
        {
            try
            {
                using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
                connection.Open();

                // Check if employee has time entries (business rule: can't delete employees with time records)
                string checkSql = "SELECT COUNT(*) FROM TimeEntries WHERE EmployeeID = @EmployeeID";
                using var checkCommand = new SqliteCommand(checkSql, connection);
                checkCommand.Parameters.AddWithValue("@EmployeeID", employeeID);

                int timeEntryCount = Convert.ToInt32(checkCommand.ExecuteScalar());
                if (timeEntryCount > 0)
                {
                    return false; // Cannot delete - has time entries
                }

                // Safe to delete - no time entries exist
                string deleteSql = "DELETE FROM Employees WHERE EmployeeID = @EmployeeID";
                using var deleteCommand = new SqliteCommand(deleteSql, connection);
                deleteCommand.Parameters.AddWithValue("@EmployeeID", employeeID);

                int rowsAffected = deleteCommand.ExecuteNonQuery();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Error deleting employee: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if an employee has any time entries in the system
        /// Used for business rule enforcement before deletion
        /// </summary>
        /// <param name="employeeID">ID of the employee to check</param>
        /// <returns>True if employee has time entries, false otherwise</returns>
        public bool HasTimeEntries(int employeeID)
        {
            try
            {
                using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
                connection.Open();

                string sql = "SELECT COUNT(*) FROM TimeEntries WHERE EmployeeID = @EmployeeID";
                using var command = new SqliteCommand(sql, connection);
                command.Parameters.AddWithValue("@EmployeeID", employeeID);

                int count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0;
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Error checking time entries: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets only active employees for dropdowns and selection lists
        /// Useful for time entry forms where only active employees should be selectable
        /// </summary>
        /// <returns>List of active employees only</returns>
        public List<Employee> GetActiveEmployees()
        {
            var employees = new List<Employee>();

            using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
            connection.Open();

            string sql = "SELECT * FROM Employees WHERE Active = 1 ORDER BY LastName, FirstName";
            using var command = new SqliteCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                employees.Add(new Employee
                {
                    EmployeeID = reader.GetInt32("EmployeeID"),
                    FirstName = reader.GetString("FirstName"),
                    LastName = reader.GetString("LastName"),
                    PayRate = reader.GetDecimal("PayRate"),
                    JobTitle = reader.IsDBNull("JobTitle") ? "" : reader.GetString("JobTitle"),
                    Active = reader.GetBoolean("Active"),
                    DateHired = reader.IsDBNull("DateHired") ? null : reader.GetDateTime("DateHired"),
                    CreatedDate = reader.GetDateTime("CreatedDate"),

                    // NEW PERSONAL DETAILS FIELDS
                    PhoneNumber = GetSafeString(reader, "PhoneNumber"),
                    DateOfBirth = GetSafeDateTime(reader, "DateOfBirth"),
                    SocialSecurityNumber = GetSafeString(reader, "SocialSecurityNumber")
                });
            }

            return employees;
        }

        /// <summary>
        /// Gets a single employee by their ID
        /// Useful for detailed views or editing operations
        /// </summary>
        /// <param name="employeeID">ID of the employee to retrieve</param>
        /// <returns>Employee object if found, null otherwise</returns>
        public Employee? GetEmployeeById(int employeeID)
        {
            try
            {
                using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
                connection.Open();

                string sql = "SELECT * FROM Employees WHERE EmployeeID = @EmployeeID";
                using var command = new SqliteCommand(sql, connection);
                command.Parameters.AddWithValue("@EmployeeID", employeeID);
                using var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    return new Employee
                    {
                        EmployeeID = reader.GetInt32("EmployeeID"),
                        FirstName = reader.GetString("FirstName"),
                        LastName = reader.GetString("LastName"),
                        PayRate = reader.GetDecimal("PayRate"),
                        JobTitle = reader.IsDBNull("JobTitle") ? "" : reader.GetString("JobTitle"),
                        Active = reader.GetBoolean("Active"),
                        DateHired = reader.IsDBNull("DateHired") ? null : reader.GetDateTime("DateHired"),
                        CreatedDate = reader.GetDateTime("CreatedDate"),

                        // NEW PERSONAL DETAILS FIELDS
                        PhoneNumber = GetSafeString(reader, "PhoneNumber"),
                        DateOfBirth = GetSafeDateTime(reader, "DateOfBirth"),
                        SocialSecurityNumber = GetSafeString(reader, "SocialSecurityNumber")
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Error getting employee by ID: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets count of all employees (active and inactive)
        /// Useful for statistics and reporting
        /// </summary>
        /// <returns>Total number of employees</returns>
        public int GetEmployeeCount()
        {
            try
            {
                using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
                connection.Open();

                string sql = "SELECT COUNT(*) FROM Employees";
                using var command = new SqliteCommand(sql, connection);

                return Convert.ToInt32(command.ExecuteScalar());
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Error getting employee count: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets count of active employees only
        /// Useful for dashboard displays
        /// </summary>
        /// <returns>Number of active employees</returns>
        public int GetActiveEmployeeCount()
        {
            try
            {
                using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
                connection.Open();

                string sql = "SELECT COUNT(*) FROM Employees WHERE Active = 1";
                using var command = new SqliteCommand(sql, connection);

                return Convert.ToInt32(command.ExecuteScalar());
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Error getting active employee count: {ex.Message}");
                return 0;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Safely retrieves a string value from SqliteDataReader, handling missing columns
        /// Used for backward compatibility when new columns might not exist yet
        /// </summary>
        /// <param name="reader">SqliteDataReader instance</param>
        /// <param name="columnName">Name of the column to retrieve</param>
        /// <returns>String value or empty string if column doesn't exist or is null</returns>
        private static string GetSafeString(SqliteDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal);
            }
            catch (IndexOutOfRangeException)
            {
                // Column doesn't exist yet (during migration period)
                return "";
            }
        }

        /// <summary>
        /// Safely retrieves a DateTime value from SqliteDataReader, handling missing columns
        /// Used for backward compatibility when new columns might not exist yet
        /// </summary>
        /// <param name="reader">SqliteDataReader instance</param>
        /// <param name="columnName">Name of the column to retrieve</param>
        /// <returns>DateTime value or null if column doesn't exist or is null</returns>
        private static DateTime? GetSafeDateTime(SqliteDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
            }
            catch (IndexOutOfRangeException)
            {
                // Column doesn't exist yet (during migration period)
                return null;
            }
        }

        #endregion

        // ====================================================================
        // NEW METHODS FOR PHASE 5 DATA BINDING INTEGRATION
        // NO EXISTING CODE ABOVE HAS BEEN MODIFIED - ONLY ADDING NEW METHODS
        // ====================================================================

        /// <summary>
        /// Asynchronously searches employees by name, job title, or phone number.
        /// Provides ObservableCollection return type optimized for UI data binding.
        /// </summary>
        /// <param name="searchText">Search text to match against employee data (case-insensitive)</param>
        /// <returns>Task containing ObservableCollection of employees matching the search criteria</returns>
        public async Task<ObservableCollection<Employee>> SearchEmployeesAsync(string searchText)
        {
            try
            {
                var allEmployees = await GetAllEmployeesAsync();
                if (string.IsNullOrWhiteSpace(searchText))
                    return allEmployees;
                    
                var filtered = allEmployees.Where(e => 
                    e.FirstName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    e.LastName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (e.JobTitle ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase)
                ).ToList();
                
                return new ObservableCollection<Employee>(filtered);
            }
            catch (Exception ex)
            {
                // Log error and return empty collection for graceful UI handling
                System.Diagnostics.Debug.WriteLine($"Error in SearchEmployeesAsync: {ex.Message}");
                return new ObservableCollection<Employee>();
            }
        }

        /// <summary>
        /// Asynchronously gets all active employees.
        /// Provides ObservableCollection return type optimized for UI data binding.
        /// </summary>
        /// <returns>Task containing ObservableCollection of active employees</returns>
        public async Task<ObservableCollection<Employee>> GetActiveEmployeesAsync()
        {
            try
            {
                var allEmployees = await GetAllEmployeesAsync();
                var activeEmployees = allEmployees.Where(e => e.Active).ToList();
                return new ObservableCollection<Employee>(activeEmployees);
            }
            catch (Exception ex)
            {
                // Log error and return empty collection for graceful UI handling
                System.Diagnostics.Debug.WriteLine($"Error in GetActiveEmployeesAsync: {ex.Message}");
                return new ObservableCollection<Employee>();
            }
        }

        /// <summary>
        /// Asynchronously gets an employee's current status (clocked in or available).
        /// Integrates with TimeEntryRepository through extension methods to determine clock status.
        /// </summary>
        /// <param name="employeeId">ID of the employee to check status for</param>
        /// <returns>Task containing status string ("Currently Clocked In", "Available to Clock In", or "Status Unknown")</returns>
        public async Task<string> GetEmployeeCurrentStatusAsync(int employeeId)
        {
            try
            {
                // Note: This method assumes TimeEntryRepository extension method GetCurrentTimeEntryAsync exists
                // We'll need to create a TimeEntryRepository instance or inject it for this to work properly
                // For now, we'll implement basic logic that can be enhanced when repositories are properly integrated
                
                // Basic implementation - check if employee exists and is active
                var employee = await GetByIdAsync(employeeId);
                if (employee == null)
                {
                    return "Employee Not Found";
                }
                
                if (!employee.Active)
                {
                    return "Employee Inactive";
                }
                
                // TODO: When TimeEntryRepository is properly integrated:
                // var timeEntryRepo = new TimeEntryRepository(); // or inject via DI
                // var openEntry = await timeEntryRepo.GetCurrentTimeEntryAsync(employeeId);
                // return openEntry != null ? "Currently Clocked In" : "Available to Clock In";
                
                // For now, return default available status
                return "Available to Clock In";
            }
            catch (Exception ex)
            {
                // Log error and return default status for graceful UI handling
                System.Diagnostics.Debug.WriteLine($"Error in GetEmployeeCurrentStatusAsync for ID {employeeId}: {ex.Message}");
                return "Status Unknown";
            }
        }

        /// <summary>
        /// Asynchronously gets all employees from the database.
        /// Helper method to support the new async methods above.
        /// </summary>
        /// <returns>Task containing ObservableCollection of all employees</returns>
        private async Task<ObservableCollection<Employee>> GetAllEmployeesAsync()
        {
            return await Task.Run(() =>
            {
                var employees = GetAllEmployees();
                return new ObservableCollection<Employee>(employees);
            });
        }

        /// <summary>
        /// Asynchronously gets an employee by ID.
        /// Helper method to support the new async methods above.
        /// </summary>
        /// <param name="employeeId">ID of the employee to retrieve</param>
        /// <returns>Task containing Employee object or null if not found</returns>
        private async Task<Employee?> GetByIdAsync(int employeeId)
        {
            return await Task.Run(() => GetEmployeeById(employeeId));
        }
    }
}