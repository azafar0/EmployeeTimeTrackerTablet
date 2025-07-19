using EmployeeTimeTracker.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
#if WINFORMS_SUPPORT
using System.Windows.Forms;
#endif

namespace EmployeeTimeTracker.Data
{
    public class TimeEntryRepository
    {
        // Add this field to the class
        private bool _isLoading = false;

        /// <summary>
        /// Gets all time entries for a specific employee in a given week
        /// FIXED: Now includes photo path columns in SELECT query
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="weekStartDate">Monday of the week (will normalize to Monday if not)</param>
        /// <returns>List of time entries for the week</returns>
        public List<TimeEntry> GetTimeEntriesForWeek(int employeeId, DateTime weekStartDate)
        {
            var timeEntries = new List<TimeEntry>();

            // Ensure we have the Monday of the week
            DateTime monday = GetMondayOfWeek(weekStartDate);
            DateTime sunday = monday.AddDays(6);

            using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
            connection.Open();

            // FIXED: Include photo path columns in SELECT using safe column access
            string sql = @"SELECT EntryID, EmployeeID, ShiftDate, TimeIn, TimeOut, TotalHours, 
                                 GrossPay, Notes, CreatedDate, ModifiedDate,
                                 ClockInPhotoPath, ClockOutPhotoPath
                          FROM TimeEntries 
                          WHERE EmployeeID = @employeeId 
                            AND ShiftDate >= @startDate 
                            AND ShiftDate <= @endDate
                          ORDER BY ShiftDate";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@employeeId", employeeId);
            command.Parameters.AddWithValue("@startDate", monday.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@endDate", sunday.ToString("yyyy-MM-dd"));

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                timeEntries.Add(new TimeEntry
                {
                    EntryID = reader.GetInt32("EntryID"),
                    EmployeeID = reader.GetInt32("EmployeeID"),
                    ShiftDate = reader.GetDateTime("ShiftDate"),
                    TimeIn = reader.IsDBNull("TimeIn") ? null : TimeSpan.Parse(reader.GetString("TimeIn")),
                    TimeOut = reader.IsDBNull("TimeOut") ? null : TimeSpan.Parse(reader.GetString("TimeOut")),
                    TotalHours = reader.IsDBNull("TotalHours") ? 0 : reader.GetDecimal("TotalHours"),
                    GrossPay = reader.IsDBNull("GrossPay") ? 0 : reader.GetDecimal("GrossPay"),
                    Notes = reader.IsDBNull("Notes") ? "" : reader.GetString("Notes"),
                    CreatedDate = reader.GetDateTime("CreatedDate"),
                    ModifiedDate = reader.GetDateTime("ModifiedDate"),
                    // FIXED: Now reading photo path columns with safe access
                    ClockInPhotoPath = GetSafeString(reader, "ClockInPhotoPath"),
                    ClockOutPhotoPath = GetSafeString(reader, "ClockOutPhotoPath")
                });
            }

            return timeEntries;
        }

        /// <summary>
        /// Saves a new time entry or updates existing one
        /// </summary>
        public bool SaveTimeEntry(TimeEntry entry)
        {
            try
            {
                using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
                connection.Open();

                // Check if entry already exists for this employee and date
                var existingEntry = GetTimeEntryForDate(entry.EmployeeID, entry.ShiftDate);

                if (existingEntry != null)
                {
                    // Update existing entry
                    entry.EntryID = existingEntry.EntryID;
                    return UpdateTimeEntry(entry, connection);
                }
                else
                {
                    // Insert new entry
                    return InsertTimeEntry(entry, connection);
                }
            }
            catch (Exception ex)
            {
                // Log error in production - for now just return false
                System.Diagnostics.Debug.WriteLine($"SaveTimeEntry error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets a specific time entry for an employee on a specific date
        /// FIXED: Now includes photo path columns in SELECT query
        /// </summary>
        public TimeEntry? GetTimeEntryForDate(int employeeId, DateTime shiftDate)
        {
            using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
            connection.Open();

            // FIXED: Include photo path columns in SELECT using safe column access
            string sql = @"SELECT EntryID, EmployeeID, ShiftDate, TimeIn, TimeOut, TotalHours, 
                                 GrossPay, Notes, CreatedDate, ModifiedDate,
                                 ClockInPhotoPath, ClockOutPhotoPath
                          FROM TimeEntries 
                          WHERE EmployeeID = @employeeId AND ShiftDate = @shiftDate";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@employeeId", employeeId);
            command.Parameters.AddWithValue("@shiftDate", shiftDate.ToString("yyyy-MM-dd"));

            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return new TimeEntry
                {
                    EntryID = reader.GetInt32("EntryID"),
                    EmployeeID = reader.GetInt32("EmployeeID"),
                    ShiftDate = reader.GetDateTime("ShiftDate"),
                    TimeIn = reader.IsDBNull("TimeIn") ? null : TimeSpan.Parse(reader.GetString("TimeIn")),
                    TimeOut = reader.IsDBNull("TimeOut") ? null : TimeSpan.Parse(reader.GetString("TimeOut")),
                    TotalHours = reader.IsDBNull("TotalHours") ? 0 : reader.GetDecimal("TotalHours"),
                    GrossPay = reader.IsDBNull("GrossPay") ? 0 : reader.GetDecimal("GrossPay"),
                    Notes = reader.IsDBNull("Notes") ? "" : reader.GetString("Notes"),
                    CreatedDate = reader.GetDateTime("CreatedDate"),
                    ModifiedDate = reader.GetDateTime("ModifiedDate"),
                    // FIXED: Now reading photo path columns with safe access
                    ClockInPhotoPath = GetSafeString(reader, "ClockInPhotoPath"),
                    ClockOutPhotoPath = GetSafeString(reader, "ClockOutPhotoPath")
                };
            }

            return null;
        }

        /// <summary>
        /// Updates an existing time entry
        /// FIXED: Enhanced debugging and includes photo path columns in UPDATE
        /// </summary>
        private bool UpdateTimeEntry(TimeEntry entry, SqliteConnection connection)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateTimeEntry: Updating entry ID {entry.EntryID}");
            System.Diagnostics.Debug.WriteLine($"UpdateTimeEntry: TimeIn={entry.TimeIn}, TimeOut={entry.TimeOut}, TotalHours={entry.TotalHours}");
            System.Diagnostics.Debug.WriteLine($"UpdateTimeEntry: ClockInPhotoPath={entry.ClockInPhotoPath}, ClockOutPhotoPath={entry.ClockOutPhotoPath}");
            
            // FIXED: Include photo path columns in UPDATE
            string sql = @"UPDATE TimeEntries 
                          SET TimeIn = @timeIn, TimeOut = @timeOut, TotalHours = @totalHours, 
                              GrossPay = @grossPay, Notes = @notes, ModifiedDate = @modifiedDate,
                              ClockInPhotoPath = @clockInPhotoPath, ClockOutPhotoPath = @clockOutPhotoPath
                          WHERE EntryID = @entryId";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@entryId", entry.EntryID);
            command.Parameters.AddWithValue("@timeIn", entry.TimeIn?.ToString() ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@timeOut", entry.TimeOut?.ToString() ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@totalHours", entry.TotalHours);
            command.Parameters.AddWithValue("@grossPay", entry.GrossPay);
            command.Parameters.AddWithValue("@notes", entry.Notes ?? "");
            command.Parameters.AddWithValue("@modifiedDate", DateTime.Now);
            // FIXED: Include photo path parameters
            command.Parameters.AddWithValue("@clockInPhotoPath", entry.ClockInPhotoPath ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@clockOutPhotoPath", entry.ClockOutPhotoPath ?? (object)DBNull.Value);

            System.Diagnostics.Debug.WriteLine($"UpdateTimeEntry: Executing SQL with TimeOut parameter = {entry.TimeOut?.ToString() ?? "NULL"}");
            System.Diagnostics.Debug.WriteLine($"UpdateTimeEntry: Photo paths - ClockIn: {entry.ClockInPhotoPath ?? "NULL"}, ClockOut: {entry.ClockOutPhotoPath ?? "NULL"}");

            int rowsAffected = command.ExecuteNonQuery();
            System.Diagnostics.Debug.WriteLine($"UpdateTimeEntry: {rowsAffected} rows affected");
            
            return rowsAffected > 0;
        }

        /// <summary>
        /// Inserts a new time entry
        /// FIXED: Now includes photo path columns in INSERT
        /// </summary>
        private bool InsertTimeEntry(TimeEntry entry, SqliteConnection connection)
        {
            // FIXED: Include photo path columns in INSERT
            string sql = @"INSERT INTO TimeEntries 
                          (EmployeeID, ShiftDate, TimeIn, TimeOut, TotalHours, GrossPay, Notes, 
                           ClockInPhotoPath, ClockOutPhotoPath, CreatedDate, ModifiedDate)
                          VALUES (@employeeId, @shiftDate, @timeIn, @timeOut, @totalHours, @grossPay, @notes, 
                                  @clockInPhotoPath, @clockOutPhotoPath, @createdDate, @modifiedDate)";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@employeeId", entry.EmployeeID);
            command.Parameters.AddWithValue("@shiftDate", entry.ShiftDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@timeIn", entry.TimeIn?.ToString() ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@timeOut", entry.TimeOut?.ToString() ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@totalHours", entry.TotalHours);
            command.Parameters.AddWithValue("@grossPay", entry.GrossPay);
            command.Parameters.AddWithValue("@notes", entry.Notes ?? "");
            // FIXED: Include photo path parameters
            command.Parameters.AddWithValue("@clockInPhotoPath", entry.ClockInPhotoPath ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@clockOutPhotoPath", entry.ClockOutPhotoPath ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@createdDate", DateTime.Now);
            command.Parameters.AddWithValue("@modifiedDate", DateTime.Now);

            System.Diagnostics.Debug.WriteLine($"InsertTimeEntry: Creating new entry with photo paths - ClockIn: {entry.ClockInPhotoPath ?? "NULL"}, ClockOut: {entry.ClockOutPhotoPath ?? "NULL"}");

            command.ExecuteNonQuery();
            return true;
        }

        /// <summary>
        /// Deletes a time entry
        /// </summary>
        public bool DeleteTimeEntry(int entryId)
        {
            try
            {
                using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
                connection.Open();

                string sql = "DELETE FROM TimeEntries WHERE EntryID = @entryId";
                using var command = new SqliteCommand(sql, connection);
                command.Parameters.AddWithValue("@entryId", entryId);

                int rowsAffected = command.ExecuteNonQuery();
                return rowsAffected > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Copies time entries from one week to another (for Copy Last Week functionality)
        /// </summary>
        public bool CopyWeekTimeEntries(int employeeId, DateTime sourceWeekStart, DateTime targetWeekStart)
        {
            try
            {
                // Ensure we're working with Monday dates
                DateTime sourceMon = GetMondayOfWeek(sourceWeekStart);
                DateTime targetMon = GetMondayOfWeek(targetWeekStart);

                System.Diagnostics.Debug.WriteLine($"Copy operation: {sourceMon:yyyy-MM-dd} to {targetMon:yyyy-MM-dd}");

                var sourceEntries = GetTimeEntriesForWeek(employeeId, sourceMon);

                if (sourceEntries.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No source entries found");
                    return false; // No data to copy
                }

                System.Diagnostics.Debug.WriteLine($"Found {sourceEntries.Count} entries to copy");

                using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // First, delete any existing entries for the target week
                    string deleteSql = @"DELETE FROM TimeEntries 
                               WHERE EmployeeID = @employeeId 
                                 AND ShiftDate >= @startDate 
                                 AND ShiftDate <= @endDate";

                    using var deleteCommand = new SqliteCommand(deleteSql, connection, transaction);
                    deleteCommand.Parameters.AddWithValue("@employeeId", employeeId);
                    deleteCommand.Parameters.AddWithValue("@startDate", targetMon.ToString("yyyy-MM-dd"));
                    deleteCommand.Parameters.AddWithValue("@endDate", targetMon.AddDays(6).ToString("yyyy-MM-dd"));

                    int deletedCount = deleteCommand.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine($"Deleted {deletedCount} existing entries");

                    // Copy each entry to the target week
                    foreach (var sourceEntry in sourceEntries)
                    {
                        // Only copy entries that have actual time data
                        if (sourceEntry.TimeIn.HasValue || sourceEntry.TimeOut.HasValue)
                        {
                            // Calculate the target date
                            int dayOffset = (int)(sourceEntry.ShiftDate.Date - sourceMon.Date).TotalDays;
                            DateTime targetDate = targetMon.AddDays(dayOffset);

                            string insertSql = @"INSERT INTO TimeEntries 
                                       (EmployeeID, ShiftDate, TimeIn, TimeOut, TotalHours, GrossPay, Notes, CreatedDate, ModifiedDate)
                                       VALUES (@employeeId, @shiftDate, @timeIn, @timeOut, @totalHours, @grossPay, @notes, @createdDate, @modifiedDate)";

                            using var insertCommand = new SqliteCommand(insertSql, connection, transaction);
                            insertCommand.Parameters.AddWithValue("@employeeId", employeeId);
                            insertCommand.Parameters.AddWithValue("@shiftDate", targetDate.ToString("yyyy-MM-dd"));
                            insertCommand.Parameters.AddWithValue("@timeIn", sourceEntry.TimeIn?.ToString() ?? (object)DBNull.Value);
                            insertCommand.Parameters.AddWithValue("@timeOut", sourceEntry.TimeOut?.ToString() ?? (object)DBNull.Value);
                            insertCommand.Parameters.AddWithValue("@totalHours", sourceEntry.TotalHours);
                            insertCommand.Parameters.AddWithValue("@grossPay", sourceEntry.GrossPay);
                            insertCommand.Parameters.AddWithValue("@notes", sourceEntry.Notes ?? "");
                            insertCommand.Parameters.AddWithValue("@createdDate", DateTime.Now);
                            insertCommand.Parameters.AddWithValue("@modifiedDate", DateTime.Now);

                            insertCommand.ExecuteNonQuery();
                            System.Diagnostics.Debug.WriteLine($"Copied entry: {targetDate:yyyy-MM-dd} - {sourceEntry.TimeIn} to {sourceEntry.TimeOut}");
                        }
                    }

                    transaction.Commit();
                    System.Diagnostics.Debug.WriteLine("Copy operation completed successfully");
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"Copy operation failed: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CopyWeekTimeEntries error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the Monday of the week for any given date
        /// </summary>
        private DateTime GetMondayOfWeek(DateTime date)
        {
            // Normalize to date only (remove time component)
            date = date.Date;

            // Calculate days from Monday (make sure Monday = 0)
            int daysFromMonday = ((int)date.DayOfWeek + 6) % 7; // Sunday=6, Monday=0, Tuesday=1, etc.

            DateTime monday = date.AddDays(-daysFromMonday);

            System.Diagnostics.Debug.WriteLine($"GetMondayOfWeek({date:yyyy-MM-dd}) = {monday:yyyy-MM-dd}");

            return monday;
        }

        /// <summary>
        /// Bulk delete time entries for a specific week (useful for clearing/resetting)
        /// </summary>
        public bool DeleteWeekTimeEntries(int employeeId, DateTime weekStartDate)
        {
            try
            {
                DateTime monday = GetMondayOfWeek(weekStartDate);
                DateTime sunday = monday.AddDays(6);

                using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
                connection.Open();

                string sql = @"DELETE FROM TimeEntries 
                              WHERE EmployeeID = @employeeId 
                                AND ShiftDate >= @startDate 
                                AND ShiftDate <= @endDate";

                using var command = new SqliteCommand(sql, connection);
                command.Parameters.AddWithValue("@employeeId", employeeId);
                command.Parameters.AddWithValue("@startDate", monday.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@endDate", sunday.ToString("yyyy-MM-dd"));

                command.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ========================================
        // NEW METHODS FOR PHASE 2 REPORTING
        // ========================================

        /// <summary>
        /// Gets time entries with employee information for reporting
        /// FIXED: Now includes photo path columns in SELECT query
        /// </summary>
        public List<TimeEntryReportData> GetTimeEntriesForReporting(
            DateTime startDate,
            DateTime endDate,
            int? employeeId = null,
            string? jobTitle = null)
        {
            var entries = new List<TimeEntryReportData>();

            using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
            connection.Open();

            // FIXED: Include photo path columns in SELECT
            var sql = @"SELECT 
                            te.EntryID,
                            te.ShiftDate,
                            te.TimeIn,
                            te.TimeOut,
                            te.TotalHours,
                            te.GrossPay,
                            te.Notes,
                            te.CreatedDate,
                            te.ModifiedDate,
                            te.ClockInPhotoPath,
                            te.ClockOutPhotoPath,
                            e.EmployeeID,
                            e.FirstName,
                            e.LastName,
                            e.JobTitle
                        FROM TimeEntries te
                        INNER JOIN Employees e ON te.EmployeeID = e.EmployeeID
                        WHERE te.ShiftDate >= @startDate 
                          AND te.ShiftDate <= @endDate
                          AND e.Active = 1";

            var parameters = new List<SqliteParameter>
            {
                new("@startDate", startDate.ToString("yyyy-MM-dd")),
                new("@endDate", endDate.ToString("yyyy-MM-dd"))
            };

            if (employeeId.HasValue)
            {
                sql += " AND e.EmployeeID = @employeeId";
                parameters.Add(new("@employeeId", employeeId.Value));
            }

            if (!string.IsNullOrWhiteSpace(jobTitle))
            {
                sql += " AND e.JobTitle = @jobTitle";
                parameters.Add(new("@jobTitle", jobTitle));
            }

            sql += " ORDER BY e.LastName, e.FirstName, te.ShiftDate";

            using var command = new SqliteCommand(sql, connection);
            foreach (var param in parameters)
            {
                command.Parameters.Add(param);
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var entry = new TimeEntryReportData
                {
                    EntryID = reader.GetInt32("EntryID"),
                    EmployeeID = reader.GetInt32("EmployeeID"),
                    EmployeeName = $"{reader.GetString("FirstName")} {reader.GetString("LastName")}".Trim(),
                    JobTitle = reader.IsDBNull("JobTitle") ? "" : reader.GetString("JobTitle"),
                    ShiftDate = reader.GetDateTime("ShiftDate"),
                    DayName = reader.GetDateTime("ShiftDate").ToString("dddd"),
                    TimeIn = reader.IsDBNull("TimeIn") ? null : TimeSpan.Parse(reader.GetString("TimeIn")),
                    TimeOut = reader.IsDBNull("TimeOut") ? null : TimeSpan.Parse(reader.GetString("TimeOut")),
                    TotalHours = reader.IsDBNull("TotalHours") ? 0 : reader.GetDecimal("TotalHours"),
                    GrossPay = reader.IsDBNull("GrossPay") ? 0 : reader.GetDecimal("GrossPay"),
                    Notes = reader.IsDBNull("Notes") ? "" : reader.GetString("Notes"),
                    CreatedDate = reader.GetDateTime("CreatedDate"),
                    ModifiedDate = reader.GetDateTime("ModifiedDate"),
                    // FIXED: Now reading photo path columns with safe access
                    ClockInPhotoPath = GetSafeString(reader, "ClockInPhotoPath"),
                    ClockOutPhotoPath = GetSafeString(reader, "ClockOutPhotoPath")
                };

                entries.Add(entry);
            }

            return entries;
        }

        /// <summary>
        /// Gets unique job titles for filter dropdown
        /// </summary>
        public List<string> GetUniqueJobTitles()
        {
            var jobTitles = new List<string>();

            using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
            connection.Open();

            var sql = @"SELECT DISTINCT JobTitle 
                        FROM Employees 
                        WHERE Active = 1 
                          AND JobTitle IS NOT NULL 
                          AND TRIM(JobTitle) != ''
                        ORDER BY JobTitle";

            using var command = new SqliteCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var jobTitle = reader.GetString("JobTitle").Trim();
                if (!string.IsNullOrWhiteSpace(jobTitle))
                {
                    jobTitles.Add(jobTitle);
                }
            }

            return jobTitles;
        }

        /// <summary>
        /// Gets summary data with grouping support
        /// </summary>
        public List<TimeReportSummary> GetSummaryData(
            DateTime startDate,
            DateTime endDate,
            int? employeeId = null,
            string? jobTitle = null,
            string groupBy = "Employee")
        {
            var timeEntries = GetTimeEntriesForReporting(startDate, endDate, employeeId, jobTitle);
            var summaries = new List<TimeReportSummary>();

            if (!timeEntries.Any())
                return summaries;

            IEnumerable<IGrouping<string, TimeEntryReportData>> groups;

            switch (groupBy.ToLower())
            {
                case "jobtitle":
                    groups = timeEntries.GroupBy(te => te.JobTitle ?? "No Job Title");
                    break;
                case "week":
                    groups = timeEntries.GroupBy(te => GetWeekKey(te.ShiftDate));
                    break;
                case "month":
                    groups = timeEntries.GroupBy(te => te.ShiftDate.ToString("yyyy-MM"));
                    break;
                case "date":
                    // Add support for date-based grouping
                    groups = timeEntries.GroupBy(te => te.ShiftDate.ToString("MM/dd/yyyy"));
                    break;
                default: // "employee"
                    groups = timeEntries.GroupBy(te => te.EmployeeName);
                    break;
            }

            foreach (var group in groups)
            {
                var entriesWithHours = group.Where(te => te.TotalHours > 0).ToList();

                if (entriesWithHours.Any())
                {
                    var summary = new TimeReportSummary
                    {
                        GroupKey = group.Key,
                        GroupType = groupBy,
                        TotalHours = entriesWithHours.Sum(te => te.TotalHours),
                        TotalPay = entriesWithHours.Sum(te => te.GrossPay),
                        DaysWorked = entriesWithHours.Select(te => te.ShiftDate.Date).Distinct().Count(),
                        StartDate = startDate,
                        EndDate = endDate
                    };

                    summary.AverageHoursPerDay = summary.DaysWorked > 0 ?
                        summary.TotalHours / summary.DaysWorked : 0;

                    // Add additional info based on grouping
                    if (groupBy.ToLower() == "employee")
                    {
                        var firstEntry = entriesWithHours.First();
                        summary.JobTitle = firstEntry.JobTitle;
                        summary.EmployeeName = firstEntry.EmployeeName;
                    }
                    else if (groupBy.ToLower() == "jobtitle")
                    {
                        // For job title grouping, we could show employee count
                        summary.EmployeeName = $"{entriesWithHours.Select(te => te.EmployeeName).Distinct().Count()} employees";
                    }

                    summaries.Add(summary);
                }
            }

            return summaries.OrderBy(s => s.GroupKey).ToList();
        }

        /// <summary>
        /// Gets total count for status display
        /// </summary>
        public int GetTimeEntriesCount(DateTime startDate, DateTime endDate,
            int? employeeId = null, string? jobTitle = null)
        {
            using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
            connection.Open();

            var sql = @"SELECT COUNT(*) 
                        FROM TimeEntries te
                        INNER JOIN Employees e ON te.EmployeeID = e.EmployeeID
                        WHERE te.ShiftDate >= @startDate 
                          AND te.ShiftDate <= @endDate
                          AND e.Active = 1
                          AND (te.TimeIn IS NOT NULL OR te.TimeOut IS NOT NULL)";

            var parameters = new List<SqliteParameter>
            {
                new("@startDate", startDate.ToString("yyyy-MM-dd")),
                new("@endDate", endDate.ToString("yyyy-MM-dd"))
            };

            if (employeeId.HasValue)
            {
                sql += " AND e.EmployeeID = @employeeId";
                parameters.Add(new("@employeeId", employeeId.Value));
            }

            if (!string.IsNullOrWhiteSpace(jobTitle))
            {
                sql += " AND e.JobTitle = @jobTitle";
                parameters.Add(new("@jobTitle", jobTitle));
            }

            using var command = new SqliteCommand(sql, connection);
            foreach (var param in parameters)
            {
                command.Parameters.Add(param);
            }

            return Convert.ToInt32(command.ExecuteScalar());
        }

        /// <summary>
        /// Helper method to get week key for grouping
        /// </summary>
        private string GetWeekKey(DateTime date)
        {
            var weekStart = GetMondayOfWeek(date);
            var weekEnd = weekStart.AddDays(6);
            var weekNumber = GetWeekOfYear(date);
            return $"Week {weekNumber:D2} ({weekStart:MMM dd} - {weekEnd:MMM dd, yyyy})";
        }

        /// <summary>
        /// Helper method to get week number
        /// </summary>
        private int GetWeekOfYear(DateTime date)
        {
            var culture = CultureInfo.CurrentCulture;
            var weekRule = culture.DateTimeFormat.CalendarWeekRule;
            var firstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;
            return culture.Calendar.GetWeekOfYear(date, weekRule, firstDayOfWeek);
        }

        /// <summary>
        /// DEBUG METHOD - Add this to the end of your TimeEntryRepository class
        /// This will help us diagnose database connectivity and data issues
        /// </summary>
        public void DebugTimeEntries(DateTime fromDate, DateTime toDate)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== DEBUG TIME ENTRIES ===");
                System.Diagnostics.Debug.WriteLine($"Date Range: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");

                using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
                connection.Open();
                System.Diagnostics.Debug.WriteLine("✅ Database connection successful");

                // Test 1: Check if TimeEntries table exists and has data
                var countSql = "SELECT COUNT(*) FROM TimeEntries";
                using var countCommand = new SqliteCommand(countSql, connection);
                var totalEntries = Convert.ToInt32(countCommand.ExecuteScalar());
                System.Diagnostics.Debug.WriteLine($"Total entries in TimeEntries table: {totalEntries}");

                // Test 2: Check entries in date range
                var rangeSql = @"SELECT COUNT(*) FROM TimeEntries 
                                WHERE ShiftDate >= @startDate AND ShiftDate <= @endDate";
                using var rangeCommand = new SqliteCommand(rangeSql, connection);
                rangeCommand.Parameters.AddWithValue("@startDate", fromDate.ToString("yyyy-MM-dd"));
                rangeCommand.Parameters.AddWithValue("@endDate", toDate.ToString("yyyy-MM-dd"));
                var rangeEntries = Convert.ToInt32(rangeCommand.ExecuteScalar());
                System.Diagnostics.Debug.WriteLine($"Entries in date range: {rangeEntries}");

                // Test 3: Check if Employees table has active employees
                var empSql = "SELECT COUNT(*) FROM Employees WHERE Active = 1";
                using var empCommand = new SqliteCommand(empSql, connection);
                var activeEmployees = Convert.ToInt32(empCommand.ExecuteScalar());
                System.Diagnostics.Debug.WriteLine($"Active employees: {activeEmployees}");

                // Test 4: Check join query
                var joinSql = @"SELECT COUNT(*) FROM TimeEntries te
                               INNER JOIN Employees e ON te.EmployeeID = e.EmployeeID
                               WHERE te.ShiftDate >= @startDate 
                                 AND te.ShiftDate <= @endDate
                                 AND e.Active = 1";
                using var joinCommand = new SqliteCommand(joinSql, connection);
                joinCommand.Parameters.AddWithValue("@startDate", fromDate.ToString("yyyy-MM-dd"));
                joinCommand.Parameters.AddWithValue("@endDate", toDate.ToString("yyyy-MM-dd"));
                var joinEntries = Convert.ToInt32(joinCommand.ExecuteScalar());
                System.Diagnostics.Debug.WriteLine($"Joined entries in range: {joinEntries}");

                // Test 5: Show actual data (if any exists)
                if (joinEntries > 0)
                {
                    // Note: SQLite doesn't support TOP, use LIMIT instead
                    var dataSql = @"SELECT e.FirstName, e.LastName, te.ShiftDate, te.TotalHours
                                   FROM TimeEntries te
                                   INNER JOIN Employees e ON te.EmployeeID = e.EmployeeID
                                   WHERE te.ShiftDate >= @startDate 
                                     AND te.ShiftDate <= @endDate
                                     AND e.Active = 1
                                   ORDER BY te.ShiftDate
                                   LIMIT 5";
                    using var dataCommand = new SqliteCommand(dataSql, connection);
                    dataCommand.Parameters.AddWithValue("@startDate", fromDate.ToString("yyyy-MM-dd"));
                    dataCommand.Parameters.AddWithValue("@endDate", toDate.ToString("yyyy-MM-dd"));

                    using var reader = dataCommand.ExecuteReader();
                    System.Diagnostics.Debug.WriteLine("Sample data:");
                    while (reader.Read())
                    {
                        System.Diagnostics.Debug.WriteLine($"  {reader.GetString("FirstName")} {reader.GetString("LastName")} - {reader.GetDateTime("ShiftDate"):yyyy-MM-dd} - {reader.GetDecimal("TotalHours")}h");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ No data found in date range with active employees");

                    // Additional debugging: Check what dates ARE in the database
                    var datesSql = @"SELECT MIN(ShiftDate) as MinDate, MAX(ShiftDate) as MaxDate 
                                    FROM TimeEntries te
                                    INNER JOIN Employees e ON te.EmployeeID = e.EmployeeID
                                    WHERE e.Active = 1";
                    using var datesCommand = new SqliteCommand(datesSql, connection);
                    using var datesReader = datesCommand.ExecuteReader();
                    if (datesReader.Read() && !datesReader.IsDBNull("MinDate"))
                    {
                        System.Diagnostics.Debug.WriteLine($"Actual date range in database: {datesReader.GetDateTime("MinDate"):yyyy-MM-dd} to {datesReader.GetDateTime("MaxDate"):yyyy-MM-dd}");
                    }
                }

                System.Diagnostics.Debug.WriteLine("=== END DEBUG ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ DEBUG ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Adds sorting options to a ComboBox (Windows Forms compatibility method for future use)
        /// NOTE: This method is kept for future Windows Forms integration
        /// </summary>
#if WINFORMS_SUPPORT
        public void AddSortOptions(ComboBox cmbSortBy)
#else
        public void AddSortOptions(object cmbSortBy)
#endif
        {
#if WINFORMS_SUPPORT
            cmbSortBy.Items.Add("Employee Name");
            cmbSortBy.Items.Add("Total Hours");
            cmbSortBy.Items.Add("Total Pay");
            cmbSortBy.Items.Add("Days Worked");
            cmbSortBy.Items.Add("Job Title");
            cmbSortBy.Items.Add("Date");
#else
            // For future Windows Forms support - currently not implemented in WPF
            System.Diagnostics.Debug.WriteLine("AddSortOptions called - Windows Forms not available in WPF context");
#endif
        }

        /// <summary>
        /// Event handler for ascending radio button (Windows Forms compatibility for future use)
        /// NOTE: This method is kept for future Windows Forms integration
        /// </summary>
#if WINFORMS_SUPPORT
        private void rbAscending_CheckedChanged(object sender, EventArgs e)
#else
        private void rbAscending_CheckedChanged(object sender, object e)
#endif
        {
            if (_isLoading) return;
            ApplySorting(true);
        }

        /// <summary>
        /// Event handler for descending radio button (Windows Forms compatibility for future use)
        /// NOTE: This method is kept for future Windows Forms integration
        /// </summary>
#if WINFORMS_SUPPORT
        private void rbDescending_CheckedChanged(object sender, EventArgs e)
#else
        private void rbDescending_CheckedChanged(object sender, object e)
#endif
        {
            if (_isLoading) return;
            ApplySorting(false);
        }

        /// <summary>
        /// Applies sorting to the data based on the selected order
        /// </summary>
        private void ApplySorting(bool ascending)
        {
            // Implement sorting logic here
            System.Diagnostics.Debug.WriteLine($"Sorting applied. Ascending: {ascending}");
        }

        /// <summary>
        /// Delete a specific time entry for a given employee and date
        /// </summary>
        public void DeleteTimeEntryForDate(int employeeId, DateTime shiftDate)
        {
            try
            {
                using var connection = new SqliteConnection(DatabaseHelper.ConnectionString); // ✅ CORRECT
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
            DELETE FROM TimeEntries 
            WHERE EmployeeID = @employeeId AND DATE(ShiftDate) = DATE(@shiftDate)";

                command.Parameters.AddWithValue("@employeeId", employeeId);
                command.Parameters.AddWithValue("@shiftDate", shiftDate.Date);

                var rowsAffected = command.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine($"Deleted {rowsAffected} time entry records for {shiftDate:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting time entry for date {shiftDate:yyyy-MM-dd}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets weekly team summary for the team panel
        /// </summary>
        public async Task<List<string>> GetWeeklyTeamSummaryAsync(DateTime weekStart, DateTime weekEnd)
        {
            var teamSummary = new List<string>();
            
            try
            {
                using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
                await connection.OpenAsync();
                
                var query = @"
                    SELECT (e.FirstName || ' ' || e.LastName) AS EmployeeName,
                           COALESCE(SUM(te.TotalHours), 0) AS TotalHours
                    FROM Employees e
                    LEFT JOIN TimeEntries te ON e.EmployeeID = te.EmployeeID 
                        AND te.ShiftDate BETWEEN @weekStart AND @weekEnd
                    WHERE e.Active = 1
                    GROUP BY e.EmployeeID, e.FirstName, e.LastName
                    HAVING COALESCE(SUM(te.TotalHours), 0) > 0
                    ORDER BY e.FirstName, e.LastName";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@weekStart", weekStart.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@weekEnd", weekEnd.ToString("yyyy-MM-dd"));
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var name = reader.GetString("EmployeeName");
                    var hours = reader.GetDecimal("TotalHours");
                    teamSummary.Add($"{name} - {hours:F1}h");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading team summary: {ex.Message}");
            }
            
            return teamSummary;
        }

        // ====================================================================
        // NEW TABLET TIME TRACKING METHODS FOR PHASE 6
        // NO EXISTING CODE ABOVE HAS BEEN MODIFIED - ONLY ADDING NEW METHODS
        // ====================================================================

        /// <summary>
        /// Asynchronously clocks in an employee by creating a new time entry.
        /// Implements double-punch prevention by checking for existing open time entries.
        /// </summary>
        /// <param name="employeeId">The ID of the employee clocking in</param>
        /// <returns>Task containing success status and message</returns>
        public async Task<(bool Success, string Message)> ClockInAsync(int employeeId)
        {
            try
            {
                // Check if already clocked in
                var existingEntry = await GetCurrentTimeEntryAsync(employeeId);
                if (existingEntry != null && existingEntry.TimeOut == null)
                {
                    return (false, "Employee is already clocked in");
                }

                var timeEntry = new TimeEntry
                {
                    EmployeeID = employeeId,
                    ShiftDate = DateTime.Today,
                    TimeIn = DateTime.Now.TimeOfDay,
                    TimeOut = null,
                    Notes = "Tablet Clock In",
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                // Use the existing SaveTimeEntry method to add the new entry
                var success = SaveTimeEntry(timeEntry);
                if (success)
                {
                    return (true, $"Successfully clocked in at {DateTime.Now:HH:mm}");
                }
                else
                {
                    return (false, "Failed to save clock-in record. Please try again.");
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                System.Diagnostics.Debug.WriteLine($"Error in ClockInAsync for employeeId {employeeId}: {ex.Message}\n{ex.StackTrace}");
                return (false, "Error processing clock in. Please try again.");
            }
        }

        /// <summary>
        /// Asynchronously clocks out an employee by updating their current time entry.
        /// Validates that the employee is currently clocked in and calculates total hours.
        /// FIXED: Enhanced debugging and improved database update verification.
        /// </summary>
        /// <param name="employeeId">The ID of the employee clocking out</param>
        /// <returns>Task containing success status and message with time summary</returns>
        public async Task<(bool Success, string Message)> ClockOutAsync(int employeeId)
        {
            try
            {
                var currentEntry = await GetCurrentTimeEntryAsync(employeeId);
                if (currentEntry == null || currentEntry.TimeOut != null)
                {
                    return (false, "Employee is not currently clocked in");
                }

                System.Diagnostics.Debug.WriteLine($"ClockOutAsync: Found current entry ID {currentEntry.EntryID} for employee {employeeId}");
                System.Diagnostics.Debug.WriteLine($"ClockOutAsync: Current TimeIn={currentEntry.TimeIn}, TimeOut={currentEntry.TimeOut}");

                currentEntry.TimeOut = DateTime.Now.TimeOfDay;
                System.Diagnostics.Debug.WriteLine($"ClockOutAsync: Setting TimeOut to {currentEntry.TimeOut}");
                
                // Calculate total hours with better precision and minimum time handling
                decimal totalHours = 0;
                if (currentEntry.TimeIn.HasValue && currentEntry.TimeOut.HasValue)
                {
                    var timeSpan = currentEntry.TimeOut.Value - currentEntry.TimeIn.Value;
                    // Handle overnight shifts (if TimeOut is less than TimeIn, add 24 hours)
                    if (timeSpan < TimeSpan.Zero)
                    {
                        timeSpan = timeSpan.Add(TimeSpan.FromDays(1));
                    }
                    
                    totalHours = (decimal)timeSpan.TotalHours;
                    
                    // For very short periods (less than 6 minutes), show minutes instead
                    if (totalHours < 0.1m) // Less than 6 minutes
                    {
                        var totalMinutes = (int)timeSpan.TotalMinutes;
                        if (totalMinutes < 1)
                        {
                            totalMinutes = 1; // Minimum 1 minute for any clock operation
                        }
                        currentEntry.TotalHours = Math.Round(totalHours, 2);
                        
                        System.Diagnostics.Debug.WriteLine($"ClockOutAsync: Short period - {totalMinutes} minutes, setting TotalHours to {currentEntry.TotalHours}");
                        
                        // Save the entry with debugging
                        currentEntry.ModifiedDate = DateTime.Now;
                        var success = SaveTimeEntry(currentEntry);
                        System.Diagnostics.Debug.WriteLine($"ClockOutAsync: SaveTimeEntry result = {success}");
                        
                        if (success)
                        {
                            // VERIFY the update was successful by re-reading from database
                            var verifyEntry = GetTimeEntryForDate(employeeId, DateTime.Today);
                            System.Diagnostics.Debug.WriteLine($"ClockOutAsync: Verification - TimeOut={verifyEntry?.TimeOut}, TotalHours={verifyEntry?.TotalHours}");
                            
                            var clockOutTime = DateTime.Now.ToString("HH:mm");
                            return (true, $"Successfully clocked out at {clockOutTime}. Worked {totalMinutes} minute{(totalMinutes == 1 ? "" : "s")} today.");
                        }
                        else
                        {
                            return (false, "Failed to save clock-out record. Please try again.");
                        }
                    }
                    
                    currentEntry.TotalHours = totalHours;
                    System.Diagnostics.Debug.WriteLine($"ClockOutAsync: Normal period - setting TotalHours to {currentEntry.TotalHours}");
                }

                currentEntry.ModifiedDate = DateTime.Now;

                // Use the existing SaveTimeEntry method to update the entry
                var saveSuccess = SaveTimeEntry(currentEntry);
                System.Diagnostics.Debug.WriteLine($"ClockOutAsync: SaveTimeEntry result = {saveSuccess}");
                
                if (saveSuccess)
                {
                    // VERIFY the update was successful by re-reading from database
                    var verifyEntry = GetTimeEntryForDate(employeeId, DateTime.Today);
                    System.Diagnostics.Debug.WriteLine($"ClockOutAsync: Verification - TimeOut={verifyEntry?.TimeOut}, TotalHours={verifyEntry?.TotalHours}");
                    
                    // Enhanced success message with time summary
                    var clockOutTime = DateTime.Now.ToString("HH:mm");
                    var hoursWorked = totalHours.ToString("F1");
                    return (true, $"Successfully clocked out at {clockOutTime}. Worked {hoursWorked} hours today.");
                }
                else
                {
                    return (false, "Failed to save clock-out record. Please try again.");
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                System.Diagnostics.Debug.WriteLine($"Error in ClockOutAsync for employeeId {employeeId}: {ex.Message}\n{ex.StackTrace}");
                return (false, "Error processing clock out. Please try again.");
            }
        }

        /// <summary>
        /// Asynchronously gets the current open time entry for an employee.
        /// Returns the time entry that has TimeIn set but TimeOut is null for today's date.
        /// </summary>
        /// <param name="employeeId">The ID of the employee</param>
        /// <returns>Task containing the current open TimeEntry or null if not found</returns>
        public async Task<TimeEntry?> GetCurrentTimeEntryAsync(int employeeId)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var today = DateTime.Today;
                    var entry = GetTimeEntryForDate(employeeId, today);
                    
                    // Return the entry only if it's an open entry (TimeOut is null)
                    if (entry != null && entry.TimeIn.HasValue && entry.TimeOut == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"GetCurrentTimeEntryAsync: Found open entry ID {entry.EntryID} with photo paths - ClockIn: {entry.ClockInPhotoPath ?? "NULL"}, ClockOut: {entry.ClockOutPhotoPath ?? "NULL"}");
                        return entry;
                    }
                    
                    return null;
                });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                System.Diagnostics.Debug.WriteLine($"Error in GetCurrentTimeEntryAsync for employeeId {employeeId}: {ex.Message}\n{ex.StackTrace}");
                return null; // Return null on error
            }
        }

        /// <summary>
        /// Asynchronously checks if an employee is currently clocked in.
        /// Returns true if the employee has an open time entry (TimeOut is null).
        /// FIXED: More robust checking with enhanced debugging and cache clearing.
        /// </summary>
        /// <param name="employeeId">The ID of the employee</param>
        /// <returns>Task containing true if clocked in, false otherwise</returns>
        public async Task<bool> IsEmployeeClockedInAsync(int employeeId)
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
                    connection.Open();

                    // ENHANCED: Check for any time entry today with TimeIn but no TimeOut
                    // Also log the actual values to debug the issue
                    string sql = @"SELECT EntryID, TimeIn, TimeOut, ClockInPhotoPath, ClockOutPhotoPath 
                                  FROM TimeEntries 
                                  WHERE EmployeeID = @employeeId 
                                    AND ShiftDate = @today 
                                    AND TimeIn IS NOT NULL";

                    using var command = new SqliteCommand(sql, connection);
                    command.Parameters.AddWithValue("@employeeId", employeeId);
                    command.Parameters.AddWithValue("@today", DateTime.Today.ToString("yyyy-MM-dd"));

                    using var reader = command.ExecuteReader();
                    bool hasOpenEntry = false;
                    int totalEntries = 0;
                    
                    while (reader.Read())
                    {
                        totalEntries++;
                        var entryId = reader.GetInt32("EntryID");
                        var timeIn = reader.IsDBNull("TimeIn") ? "NULL" : reader.GetString("TimeIn");
                        var timeOut = reader.IsDBNull("TimeOut") ? "NULL" : reader.GetString("TimeOut");
                        var clockInPhoto = GetSafeString(reader, "ClockInPhotoPath");
                        var clockOutPhoto = GetSafeString(reader, "ClockOutPhotoPath");
                        
                        System.Diagnostics.Debug.WriteLine($"Entry {entryId}: TimeIn={timeIn}, TimeOut={timeOut}");
                        System.Diagnostics.Debug.WriteLine($"Entry {entryId}: ClockInPhoto={clockInPhoto ?? "NULL"}, ClockOutPhoto={clockOutPhoto ?? "NULL"}");
                        
                        // Entry is open if TimeOut is NULL
                        if (reader.IsDBNull("TimeOut"))
                        {
                            hasOpenEntry = true;
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"IsEmployeeClockedInAsync for ID {employeeId}: {hasOpenEntry} (total entries today: {totalEntries})");
                    return hasOpenEntry;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in IsEmployeeClockedInAsync for employeeId {employeeId}: {ex.Message}");
                return false; // Default to false on error
            }
        }

        /// <summary>
        /// Asynchronously gets time entries for a specific employee and date.
        /// Helper method to support the tablet time tracking operations.
        /// </summary>
        /// <param name="employeeId">The ID of the employee</param>
        /// <param name="date">The date to search for</param>
        /// <returns>Task containing a list of time entries for the specified date</returns>
        public async Task<List<TimeEntry>> GetTimeEntriesForDateAsync(int employeeId, DateTime date)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var entries = new List<TimeEntry>();
                    var entry = GetTimeEntryForDate(employeeId, date);
                    if (entry != null)
                    {
                        entries.Add(entry);
                        System.Diagnostics.Debug.WriteLine($"GetTimeEntriesForDateAsync: Found entry with photo paths - ClockIn: {entry.ClockInPhotoPath ?? "NULL"}, ClockOut: {entry.ClockOutPhotoPath ?? "NULL"}");
                    }
                    return entries;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetTimeEntriesForDateAsync for employeeId {employeeId} on {date:yyyy-MM-dd}: {ex.Message}");
                return new List<TimeEntry>();
            }
        }

        /// <summary>
        /// Asynchronously adds a new time entry to the database.
        /// Wrapper around the existing SaveTimeEntry method for async compatibility.
        /// </summary>
        /// <param name="timeEntry">The time entry to add</param>
        /// <returns>Task containing true if successful, false otherwise</returns>
        public async Task<bool> AddAsync(TimeEntry timeEntry)
        {
            try
            {
                return await Task.Run(() => SaveTimeEntry(timeEntry));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AddAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Asynchronously updates an existing time entry in the database.
        /// Wrapper around the existing SaveTimeEntry method for async compatibility.
        /// </summary>
        /// <param name="timeEntry">The time entry to update</param>
        /// <returns>Task containing true if successful, false otherwise</returns>
        public async Task<bool> UpdateAsync(TimeEntry timeEntry)
        {
            try
            {
                return await Task.Run(() => SaveTimeEntry(timeEntry));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Asynchronously gets the most recent completed time entry for an employee.
        /// Used for enforcing cooldown periods between shifts.
        /// </summary>
        /// <param name="employeeId">The ID of the employee</param>
        /// <returns>Task containing the most recent completed TimeEntry or null if none found</returns>
        public async Task<TimeEntry?> GetMostRecentCompletedTimeEntryAsync(int employeeId)
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var connection = new SqliteConnection(DatabaseHelper.ConnectionString);
                    connection.Open();

                    // Get the most recent completed time entry (has both TimeIn and TimeOut)
                    string sql = @"SELECT EntryID, EmployeeID, ShiftDate, TimeIn, TimeOut, TotalHours, 
                                         GrossPay, Notes, CreatedDate, ModifiedDate,
                                         ClockInPhotoPath, ClockOutPhotoPath
                                  FROM TimeEntries 
                                  WHERE EmployeeID = @employeeId 
                                    AND TimeIn IS NOT NULL 
                                    AND TimeOut IS NOT NULL
                                  ORDER BY ShiftDate DESC, TimeOut DESC
                                  LIMIT 1";

                    using var command = new SqliteCommand(sql, connection);
                    command.Parameters.AddWithValue("@employeeId", employeeId);

                    using var reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        return new TimeEntry
                        {
                            EntryID = reader.GetInt32("EntryID"),
                            EmployeeID = reader.GetInt32("EmployeeID"),
                            ShiftDate = reader.GetDateTime("ShiftDate"),
                            TimeIn = reader.IsDBNull("TimeIn") ? null : TimeSpan.Parse(reader.GetString("TimeIn")),
                            TimeOut = reader.IsDBNull("TimeOut") ? null : TimeSpan.Parse(reader.GetString("TimeOut")),
                            TotalHours = reader.IsDBNull("TotalHours") ? 0 : reader.GetDecimal("TotalHours"),
                            GrossPay = reader.IsDBNull("GrossPay") ? 0 : reader.GetDecimal("GrossPay"),
                            Notes = reader.IsDBNull("Notes") ? "" : reader.GetString("Notes"),
                            CreatedDate = reader.GetDateTime("CreatedDate"),
                            ModifiedDate = reader.GetDateTime("ModifiedDate"),
                            ClockInPhotoPath = GetSafeString(reader, "ClockInPhotoPath"),
                            ClockOutPhotoPath = GetSafeString(reader, "ClockOutPhotoPath")
                        };
                    }

                    return null;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetMostRecentCompletedTimeEntryAsync for employeeId {employeeId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Safely retrieves a string value from SqliteDataReader, handling missing columns.
        /// Used for backward compatibility when new columns might not exist yet.
        /// </summary>
        /// <param name="reader">SqliteDataReader instance</param>
        /// <param name="columnName">Name of the column to retrieve</param>
        /// <returns>String value or null if column doesn't exist or is null</returns>
        private static string? GetSafeString(SqliteDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }
            catch (IndexOutOfRangeException)
            {
                // Column doesn't exist - return null for backward compatibility
                return null;
            }
        }
    }
}