using Microsoft.Data.Sqlite;
using System;
using System.Data;
using System.IO;

namespace EmployeeTimeTracker.Data
{
    public class DatabaseHelper
    {
        private static string GetDatabasePath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "EmployeeTimeTracker");

            // Create directory if it doesn't exist
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);

            return Path.Combine(appFolder, "timetracker.db");
        }

        public static string ConnectionString => $"Data Source={GetDatabasePath()}";

        public static void InitializeDatabase()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            // Check database version and perform migrations
            int currentVersion = GetDatabaseVersion(connection);
            
            if (currentVersion == 0)
            {
                // Fresh database - create all tables
                CreateAllTables(connection);
                SetDatabaseVersion(connection, 3); // Version 3 includes photo path columns
            }
            else if (currentVersion == 1)
            {
                // Migrate from version 1 to 2 (add security tables)
                MigrateDatabaseToVersion2(connection);
                // Continue to version 3
                MigrateDatabaseToVersion3(connection);
                SetDatabaseVersion(connection, 3);
            }
            else if (currentVersion == 2)
            {
                // Migrate from version 2 to 3 (add photo path columns)
                MigrateDatabaseToVersion3(connection);
                SetDatabaseVersion(connection, 3);
            }
            
            // Create default admin user if no users exist
            CreateDefaultAdminUser(connection);
        }

        private static void CreateAllTables(SqliteConnection connection)
        {
            // Create Employees table with ALL fields (including personal details)
            string createEmployeesTable = @"
                CREATE TABLE IF NOT EXISTS Employees (
                    EmployeeID INTEGER PRIMARY KEY AUTOINCREMENT,
                    FirstName TEXT NOT NULL,
                    LastName TEXT NOT NULL,
                    PayRate DECIMAL(10,2) NOT NULL,
                    JobTitle TEXT,
                    Active BOOLEAN DEFAULT 1,
                    DateHired DATE,
                    PhoneNumber TEXT,
                    DateOfBirth DATE,
                    SocialSecurityNumber TEXT,
                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

            // Create TimeEntries table WITH PHOTO PATH COLUMNS
            string createTimeEntriesTable = @"
                CREATE TABLE IF NOT EXISTS TimeEntries (
                    EntryID INTEGER PRIMARY KEY AUTOINCREMENT,
                    EmployeeID INTEGER NOT NULL,
                    ShiftDate DATE NOT NULL,
                    TimeIn TIME,
                    TimeOut TIME,
                    TotalHours DECIMAL(4,2),
                    GrossPay DECIMAL(10,2),
                    Notes TEXT,
                    ClockInPhotoPath TEXT,
                    ClockOutPhotoPath TEXT,
                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (EmployeeID) REFERENCES Employees(EmployeeID)
                )";

            // Create Users table for authentication
            string createUsersTable = @"
                CREATE TABLE IF NOT EXISTS Users (
                    UserID INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT UNIQUE NOT NULL,
                    Email TEXT UNIQUE NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    Salt TEXT NOT NULL,
                    FirstName TEXT NOT NULL,
                    LastName TEXT NOT NULL,
                    Role INTEGER NOT NULL DEFAULT 1,
                    IsActive BOOLEAN DEFAULT 1,
                    LastLoginDate DATETIME,
                    FailedLoginAttempts INTEGER DEFAULT 0,
                    AccountLockedUntil DATETIME NULL,
                    PasswordLastChanged DATETIME DEFAULT CURRENT_TIMESTAMP,
                    TwoFactorEnabled BOOLEAN DEFAULT 0,
                    TwoFactorSecret TEXT,
                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    CreatedBy INTEGER,
                    FOREIGN KEY (CreatedBy) REFERENCES Users(UserID)
                )";

            // Create UserSessions table for session management
            string createUserSessionsTable = @"
                CREATE TABLE IF NOT EXISTS UserSessions (
                    SessionID TEXT PRIMARY KEY,
                    UserID INTEGER NOT NULL,
                    LoginDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    LastActivityDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    IPAddress TEXT,
                    UserAgent TEXT,
                    IsActive BOOLEAN DEFAULT 1,
                    LogoutDate DATETIME,
                    SessionTimeout INTEGER DEFAULT 480,
                    FOREIGN KEY (UserID) REFERENCES Users(UserID)
                )";

            // Create SecurityAuditLog table for security logging
            string createSecurityAuditLogTable = @"
                CREATE TABLE IF NOT EXISTS SecurityAuditLog (
                    LogID INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserID INTEGER,
                    Action TEXT NOT NULL,
                    Details TEXT,
                    IPAddress TEXT,
                    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                    Success BOOLEAN NOT NULL,
                    FOREIGN KEY (UserID) REFERENCES Users(UserID)
                )";

            // Create database version table
            string createVersionTable = @"
                CREATE TABLE IF NOT EXISTS DatabaseVersion (
                    Version INTEGER PRIMARY KEY
                )";

            using var command = connection.CreateCommand();

            // Execute table creation commands
            command.CommandText = createEmployeesTable;
            command.ExecuteNonQuery();

            command.CommandText = createTimeEntriesTable;
            command.ExecuteNonQuery();

            command.CommandText = createUsersTable;
            command.ExecuteNonQuery();

            command.CommandText = createUserSessionsTable;
            command.ExecuteNonQuery();

            command.CommandText = createSecurityAuditLogTable;
            command.ExecuteNonQuery();

            command.CommandText = createVersionTable;
            command.ExecuteNonQuery();

            Console.WriteLine("Database tables created successfully");
        }

        private static void MigrateDatabaseToVersion2(SqliteConnection connection)
        {
            using var transaction = connection.BeginTransaction();

            try
            {
                var command = connection.CreateCommand();
                command.Transaction = transaction;

                // First, migrate existing employee table if needed
                if (NeedsEmployeeMigration(connection))
                {
                    MigrateEmployeeTable(connection, transaction);
                }

                // Create new security tables
                string createUsersTable = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        UserID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT UNIQUE NOT NULL,
                        Email TEXT UNIQUE NOT NULL,
                        PasswordHash TEXT NOT NULL,
                        Salt TEXT NOT NULL,
                        FirstName TEXT NOT NULL,
                        LastName TEXT NOT NULL,
                        Role INTEGER NOT NULL DEFAULT 1,
                        IsActive BOOLEAN DEFAULT 1,
                        LastLoginDate DATETIME,
                        FailedLoginAttempts INTEGER DEFAULT 0,
                        AccountLockedUntil DATETIME NULL,
                        PasswordLastChanged DATETIME DEFAULT CURRENT_TIMESTAMP,
                        TwoFactorEnabled BOOLEAN DEFAULT 0,
                        TwoFactorSecret TEXT,
                        CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                        ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                        CreatedBy INTEGER,
                        FOREIGN KEY (CreatedBy) REFERENCES Users(UserID)
                    )";

                string createUserSessionsTable = @"
                    CREATE TABLE IF NOT EXISTS UserSessions (
                        SessionID TEXT PRIMARY KEY,
                        UserID INTEGER NOT NULL,
                        LoginDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                        LastActivityDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                        IPAddress TEXT,
                        UserAgent TEXT,
                        IsActive BOOLEAN DEFAULT 1,
                        LogoutDate DATETIME,
                        SessionTimeout INTEGER DEFAULT 480,
                        FOREIGN KEY (UserID) REFERENCES Users(UserID)
                    )";

                string createSecurityAuditLogTable = @"
                    CREATE TABLE IF NOT EXISTS SecurityAuditLog (
                        LogID INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserID INTEGER,
                        Action TEXT NOT NULL,
                        Details TEXT,
                        IPAddress TEXT,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                        Success BOOLEAN NOT NULL,
                        FOREIGN KEY (UserID) REFERENCES Users(UserID)
                    )";

                command.CommandText = createUsersTable;
                command.ExecuteNonQuery();

                command.CommandText = createUserSessionsTable;
                command.ExecuteNonQuery();

                command.CommandText = createSecurityAuditLogTable;
                command.ExecuteNonQuery();

                transaction.Commit();
                Console.WriteLine("Database migration to version 2 completed successfully");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Database migration to version 2 failed: {ex.Message}", ex);
            }
        }

        private static void MigrateDatabaseToVersion3(SqliteConnection connection)
        {
            using var transaction = connection.BeginTransaction();

            try
            {
                var command = connection.CreateCommand();
                command.Transaction = transaction;

                // Add photo path columns to TimeEntries table
                try
                {
                    command.CommandText = "ALTER TABLE TimeEntries ADD COLUMN ClockInPhotoPath TEXT";
                    command.ExecuteNonQuery();
                    Console.WriteLine("Added ClockInPhotoPath column to TimeEntries table");
                }
                catch (Exception ex)
                {
                    // Column might already exist
                    Console.WriteLine($"ClockInPhotoPath column may already exist: {ex.Message}");
                }

                try
                {
                    command.CommandText = "ALTER TABLE TimeEntries ADD COLUMN ClockOutPhotoPath TEXT";
                    command.ExecuteNonQuery();
                    Console.WriteLine("Added ClockOutPhotoPath column to TimeEntries table");
                }
                catch (Exception ex)
                {
                    // Column might already exist
                    Console.WriteLine($"ClockOutPhotoPath column may already exist: {ex.Message}");
                }

                transaction.Commit();
                Console.WriteLine("Database migration to version 3 (photo path support) completed successfully");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Database migration to version 3 failed: {ex.Message}", ex);
            }
        }

        private static bool NeedsEmployeeMigration(SqliteConnection connection)
        {
            try
            {
                // Check if personal details columns exist
                string checkSql = "PRAGMA table_info(Employees)";
                using var command = new SqliteCommand(checkSql, connection);
                using var reader = command.ExecuteReader();

                bool hasPhoneNumber = false;
                bool hasDateOfBirth = false;
                bool hasSSN = false;

                while (reader.Read())
                {
                    string columnName = reader.GetString("name");
                    if (columnName == "PhoneNumber") hasPhoneNumber = true;
                    if (columnName == "DateOfBirth") hasDateOfBirth = true;
                    if (columnName == "SocialSecurityNumber") hasSSN = true;
                }

                // Need migration if any of the new columns are missing
                return !hasPhoneNumber || !hasDateOfBirth || !hasSSN;
            }
            catch
            {
                return false; // Table doesn't exist, no migration needed
            }
        }

        private static void MigrateEmployeeTable(SqliteConnection connection, SqliteTransaction transaction)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;

            // Add new personal details columns if they don't exist
            try
            {
                command.CommandText = "ALTER TABLE Employees ADD COLUMN PhoneNumber TEXT";
                command.ExecuteNonQuery();
            }
            catch { /* Column might already exist */ }

            try
            {
                command.CommandText = "ALTER TABLE Employees ADD COLUMN DateOfBirth DATE";
                command.ExecuteNonQuery();
            }
            catch { /* Column might already exist */ }

            try
            {
                command.CommandText = "ALTER TABLE Employees ADD COLUMN SocialSecurityNumber TEXT";
                command.ExecuteNonQuery();
            }
            catch { /* Column might already exist */ }
        }

        private static void CreateDefaultAdminUser(SqliteConnection connection)
        {
            try
            {
                // Check if any users exist
                using var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = "SELECT COUNT(*) FROM Users";
                int userCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                if (userCount == 0)
                {
                    // Create default admin user
                    // Use BCrypt to hash the default password
                    string defaultPassword = "Admin@123456"; // Strong default password
                    string passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(defaultPassword, 13);
                    string salt = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO Users (
                            Username, Email, PasswordHash, Salt, FirstName, LastName, 
                            Role, IsActive, CreatedDate, ModifiedDate, PasswordLastChanged
                        ) VALUES (
                            'admin', 'admin@company.com', @PasswordHash, @Salt, 'System', 'Administrator',
                            4, 1, @CreatedDate, @ModifiedDate, @PasswordLastChanged
                        )";

                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    command.Parameters.AddWithValue("@Salt", salt);
                    command.Parameters.AddWithValue("@CreatedDate", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@ModifiedDate", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@PasswordLastChanged", DateTime.UtcNow);

                    command.ExecuteNonQuery();

                    Console.WriteLine("Default admin user created: username='admin', password='Admin@123456'");
                    Console.WriteLine("*** IMPORTANT: Change the default password immediately after first login! ***");
                }
                
                // Also create sample employees if none exist
                CreateSampleEmployeesIfNeeded(connection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create default admin user: {ex.Message}");
            }
        }

        private static void CreateSampleEmployeesIfNeeded(SqliteConnection connection)
        {
            try
            {
                // Check if any employees exist
                using var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = "SELECT COUNT(*) FROM Employees";
                int employeeCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                if (employeeCount == 0)
                {
                    // Create sample employees for testing
                    var sampleEmployees = new[]
                    {
                        new { FirstName = "John", LastName = "Doe", PayRate = 25.00m, JobTitle = "Developer", PhoneNumber = "(555) 123-4567" },
                        new { FirstName = "Jane", LastName = "Smith", PayRate = 22.50m, JobTitle = "Designer", PhoneNumber = "(555) 234-5678" },
                        new { FirstName = "Mike", LastName = "Johnson", PayRate = 30.00m, JobTitle = "Manager", PhoneNumber = "(555) 345-6789" },
                        new { FirstName = "Sarah", LastName = "Wilson", PayRate = 24.00m, JobTitle = "Analyst", PhoneNumber = "(555) 456-7890" },
                        new { FirstName = "David", LastName = "Brown", PayRate = 20.00m, JobTitle = "Tester", PhoneNumber = "(555) 567-8901" },
                        new { FirstName = "Emily", LastName = "Davis", PayRate = 26.50m, JobTitle = "Developer", PhoneNumber = "(555) 678-9012" },
                        new { FirstName = "Robert", LastName = "Garcia", PayRate = 28.00m, JobTitle = "Senior Developer", PhoneNumber = "(555) 789-0123" },
                        new { FirstName = "Lisa", LastName = "Martinez", PayRate = 23.75m, JobTitle = "UI/UX Designer", PhoneNumber = "(555) 890-1234" },
                        new { FirstName = "Christopher", LastName = "Rodriguez", PayRate = 31.00m, JobTitle = "Team Lead", PhoneNumber = "(555) 901-2345" },
                        new { FirstName = "Amanda", LastName = "Thompson", PayRate = 21.50m, JobTitle = "Junior Developer", PhoneNumber = "(555) 012-3456" },
                        new { FirstName = "Kevin", LastName = "White", PayRate = 25.75m, JobTitle = "Business Analyst", PhoneNumber = "(555) 123-0987" },
                        new { FirstName = "Nicole", LastName = "Lee", PayRate = 27.25m, JobTitle = "Project Manager", PhoneNumber = "(555) 234-1098" },
                        new { FirstName = "Daniel", LastName = "Taylor", PayRate = 22.00m, JobTitle = "QA Engineer", PhoneNumber = "(555) 345-2109" }
                    };

                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO Employees (
                            FirstName, LastName, PayRate, JobTitle, Active, DateHired, 
                            PhoneNumber, CreatedDate
                        ) VALUES (
                            @FirstName, @LastName, @PayRate, @JobTitle, 1, @DateHired,
                            @PhoneNumber, @CreatedDate
                        )";

                    foreach (var emp in sampleEmployees)
                    {
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@FirstName", emp.FirstName);
                        command.Parameters.AddWithValue("@LastName", emp.LastName);
                        command.Parameters.AddWithValue("@PayRate", emp.PayRate);
                        command.Parameters.AddWithValue("@JobTitle", emp.JobTitle);
                        command.Parameters.AddWithValue("@DateHired", DateTime.Now.AddDays(-new Random().Next(30, 365)));
                        command.Parameters.AddWithValue("@PhoneNumber", emp.PhoneNumber);
                        command.Parameters.AddWithValue("@CreatedDate", DateTime.UtcNow);

                        command.ExecuteNonQuery();
                    }

                    Console.WriteLine($"Created {sampleEmployees.Length} sample employees for testing");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create sample employees: {ex.Message}");
            }
        }

        private static int GetDatabaseVersion(SqliteConnection connection)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT Version FROM DatabaseVersion ORDER BY Version DESC LIMIT 1";
                var result = command.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch
            {
                return 0; // Table doesn't exist, assume version 0
            }
        }

        private static void SetDatabaseVersion(SqliteConnection connection, int version)
        {
            try
            {
                // Create version table if it doesn't exist
                using var createCommand = connection.CreateCommand();
                createCommand.CommandText = @"
                    CREATE TABLE IF NOT EXISTS DatabaseVersion (
                        Version INTEGER PRIMARY KEY
                    )";
                createCommand.ExecuteNonQuery();

                // Insert or update version
                using var command = connection.CreateCommand();
                command.CommandText = "INSERT OR REPLACE INTO DatabaseVersion (Version) VALUES (@Version)";
                command.Parameters.AddWithValue("@Version", version);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not set database version: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the current database version
        /// </summary>
        /// <returns>Current database version</returns>
        public static int GetCurrentDatabaseVersion()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            return GetDatabaseVersion(connection);
        }

        /// <summary>
        /// Performs database integrity check
        /// </summary>
        /// <returns>True if database is healthy</returns>
        public static bool CheckDatabaseIntegrity()
        {
            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA integrity_check";
                var result = command.ExecuteScalar();

                return result?.ToString() == "ok";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets database file information
        /// </summary>
        /// <returns>Database file info</returns>
        public static DatabaseInfo GetDatabaseInfo()
        {
            var dbPath = GetDatabasePath();
            var fileInfo = new FileInfo(dbPath);

            return new DatabaseInfo
            {
                FilePath = dbPath,
                FileSize = fileInfo.Exists ? fileInfo.Length : 0,
                LastModified = fileInfo.Exists ? fileInfo.LastWriteTime : DateTime.MinValue,
                Version = GetCurrentDatabaseVersion(),
                IsHealthy = CheckDatabaseIntegrity()
            };
        }
    }

    /// <summary>
    /// Database information model
    /// </summary>
    public class DatabaseInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
        public int Version { get; set; }
        public bool IsHealthy { get; set; }

        public string FileSizeFormatted => FormatFileSize(FileSize);

        private static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:N1} {suffixes[counter]}";
        }
    }
}