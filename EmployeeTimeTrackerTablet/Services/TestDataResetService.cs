using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using EmployeeTimeTracker.Data;

namespace EmployeeTimeTrackerTablet.Services
{
    /// <summary>
    /// ?? DEVELOPMENT AND TESTING ONLY ??
    /// Service for clearing test data from the TimeEntry table to facilitate repeated testing
    /// of clock-in/out operations without business rule conflicts.
    /// 
    /// WARNING: This service is designed for development/testing environments only.
    /// Do NOT expose this functionality in production UI or deployment.
    /// </summary>
    public class TestDataResetService
    {
        private readonly TimeEntryRepository _timeEntryRepository;

        /// <summary>
        /// Initializes a new instance of the TestDataResetService.
        /// </summary>
        /// <param name="timeEntryRepository">The repository for TimeEntry operations</param>
        public TestDataResetService(TimeEntryRepository timeEntryRepository)
        {
            _timeEntryRepository = timeEntryRepository ?? throw new ArgumentNullException(nameof(timeEntryRepository));
        }

        /// <summary>
        /// ?? DEVELOPMENT/TESTING ONLY ??
        /// Clears all TimeEntry records from the database to reset testing state.
        /// This allows repeated testing of clock-in/out operations without the "one clock-in per day" restriction.
        /// 
        /// SAFETY: Only affects TimeEntry table - Employee data remains intact.
        /// </summary>
        /// <returns>Task containing the number of records deleted</returns>
        public async Task<int> ClearAllTimeEntriesAsync()
        {
            System.Diagnostics.Debug.WriteLine("=== TestDataResetService: ClearAllTimeEntriesAsync BEGIN ===");
            System.Diagnostics.Debug.WriteLine("?? WARNING: DELETING ALL TIME ENTRY DATA FOR TESTING ??");

            try
            {
                int deletedCount = await Task.Run(() =>
                {
                    using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
                    connection.Open();

                    // First, count existing records for logging
                    string countSql = "SELECT COUNT(*) FROM TimeEntries";
                    using var countCommand = new SqliteCommand(countSql, connection);
                    int totalRecords = Convert.ToInt32(countCommand.ExecuteScalar());

                    System.Diagnostics.Debug.WriteLine($"Found {totalRecords} TimeEntry records to delete");

                    if (totalRecords == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("No TimeEntry records found - database already clean");
                        return 0;
                    }

                    // Delete all TimeEntry records
                    string deleteSql = "DELETE FROM TimeEntries";
                    using var deleteCommand = new SqliteCommand(deleteSql, connection);
                    int deleted = deleteCommand.ExecuteNonQuery();

                    System.Diagnostics.Debug.WriteLine($"Successfully deleted {deleted} TimeEntry records");
                    return deleted;
                });

                System.Diagnostics.Debug.WriteLine($"=== TestDataResetService: ClearAllTimeEntriesAsync COMPLETE - {deletedCount} records deleted ===");
                return deletedCount;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== TestDataResetService ERROR ===");
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw new InvalidOperationException($"Failed to clear TimeEntry data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ?? DEVELOPMENT/TESTING ONLY ??
        /// Clears TimeEntry records for a specific employee to reset their testing state.
        /// Less destructive than clearing all records.
        /// </summary>
        /// <param name="employeeId">The ID of the employee whose time entries should be cleared</param>
        /// <returns>Task containing the number of records deleted for the employee</returns>
        public async Task<int> ClearEmployeeTimeEntriesAsync(int employeeId)
        {
            System.Diagnostics.Debug.WriteLine($"=== TestDataResetService: ClearEmployeeTimeEntriesAsync for Employee ID {employeeId} BEGIN ===");

            try
            {
                int deletedCount = await Task.Run(() =>
                {
                    using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
                    connection.Open();

                    // First, count existing records for this employee
                    string countSql = "SELECT COUNT(*) FROM TimeEntries WHERE EmployeeID = @employeeId";
                    using var countCommand = new SqliteCommand(countSql, connection);
                    countCommand.Parameters.AddWithValue("@employeeId", employeeId);
                    int totalRecords = Convert.ToInt32(countCommand.ExecuteScalar());

                    System.Diagnostics.Debug.WriteLine($"Found {totalRecords} TimeEntry records for Employee ID {employeeId}");

                    if (totalRecords == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"No TimeEntry records found for Employee ID {employeeId}");
                        return 0;
                    }

                    // Delete TimeEntry records for this employee
                    string deleteSql = "DELETE FROM TimeEntries WHERE EmployeeID = @employeeId";
                    using var deleteCommand = new SqliteCommand(deleteSql, connection);
                    deleteCommand.Parameters.AddWithValue("@employeeId", employeeId);
                    int deleted = deleteCommand.ExecuteNonQuery();

                    System.Diagnostics.Debug.WriteLine($"Successfully deleted {deleted} TimeEntry records for Employee ID {employeeId}");
                    return deleted;
                });

                System.Diagnostics.Debug.WriteLine($"=== TestDataResetService: ClearEmployeeTimeEntriesAsync COMPLETE - {deletedCount} records deleted ===");
                return deletedCount;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== TestDataResetService ERROR for Employee ID {employeeId} ===");
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw new InvalidOperationException($"Failed to clear TimeEntry data for Employee ID {employeeId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ?? DEVELOPMENT/TESTING ONLY ??
        /// Clears only today's TimeEntry records to reset daily testing state.
        /// Most targeted option for testing daily clock-in restrictions.
        /// </summary>
        /// <returns>Task containing the number of today's records deleted</returns>
        public async Task<int> ClearTodaysTimeEntriesAsync()
        {
            System.Diagnostics.Debug.WriteLine("=== TestDataResetService: ClearTodaysTimeEntriesAsync BEGIN ===");

            try
            {
                int deletedCount = await Task.Run(() =>
                {
                    using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
                    connection.Open();

                    string today = DateTime.Today.ToString("yyyy-MM-dd");

                    // First, count existing records for today
                    string countSql = "SELECT COUNT(*) FROM TimeEntries WHERE ShiftDate = @today";
                    using var countCommand = new SqliteCommand(countSql, connection);
                    countCommand.Parameters.AddWithValue("@today", today);
                    int totalRecords = Convert.ToInt32(countCommand.ExecuteScalar());

                    System.Diagnostics.Debug.WriteLine($"Found {totalRecords} TimeEntry records for today ({today})");

                    if (totalRecords == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"No TimeEntry records found for today ({today})");
                        return 0;
                    }

                    // Delete today's TimeEntry records
                    string deleteSql = "DELETE FROM TimeEntries WHERE ShiftDate = @today";
                    using var deleteCommand = new SqliteCommand(deleteSql, connection);
                    deleteCommand.Parameters.AddWithValue("@today", today);
                    int deleted = deleteCommand.ExecuteNonQuery();

                    System.Diagnostics.Debug.WriteLine($"Successfully deleted {deleted} TimeEntry records for today ({today})");
                    return deleted;
                });

                System.Diagnostics.Debug.WriteLine($"=== TestDataResetService: ClearTodaysTimeEntriesAsync COMPLETE - {deletedCount} records deleted ===");
                return deletedCount;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== TestDataResetService ERROR for today's records ===");
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw new InvalidOperationException($"Failed to clear today's TimeEntry data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ?? DEVELOPMENT/TESTING ONLY ??
        /// Gets a count of current TimeEntry records for verification purposes.
        /// </summary>
        /// <returns>Task containing the total number of TimeEntry records</returns>
        public async Task<int> GetTimeEntryCountAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
                    connection.Open();

                    string countSql = "SELECT COUNT(*) FROM TimeEntries";
                    using var command = new SqliteCommand(countSql, connection);
                    return Convert.ToInt32(command.ExecuteScalar());
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting TimeEntry count: {ex.Message}");
                return -1; // Indicate error
            }
        }
    }
}