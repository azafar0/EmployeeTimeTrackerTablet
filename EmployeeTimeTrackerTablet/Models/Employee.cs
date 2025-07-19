using System;

namespace EmployeeTimeTracker.Models
{
    /// <summary>
    /// Employee data model with enhanced personal details
    /// Includes Phone Number, Date of Birth, and Social Security Number fields
    /// </summary>
    public class Employee
    {
        /// <summary>
        /// Primary key for the employee
        /// </summary>
        public int EmployeeID { get; set; }

        /// <summary>
        /// Employee's first name (required)
        /// </summary>
        public string FirstName { get; set; } = "";

        /// <summary>
        /// Employee's last name (required)
        /// </summary>
        public string LastName { get; set; } = "";

        /// <summary>
        /// Employee's hourly pay rate (required)
        /// </summary>
        public decimal PayRate { get; set; }

        /// <summary>
        /// Employee's job title (optional)
        /// </summary>
        public string JobTitle { get; set; } = "";

        /// <summary>
        /// Whether the employee is active in the system
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Date the employee was hired (optional)
        /// </summary>
        public DateTime? DateHired { get; set; }

        /// <summary>
        /// Date the employee record was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // NEW PERSONAL DETAILS FIELDS

        /// <summary>
        /// Employee's phone number (required, 10-digit format)
        /// Format: (XXX) XXX-XXXX
        /// </summary>
        public string PhoneNumber { get; set; } = "";

        /// <summary>
        /// Employee's date of birth (optional, no future dates allowed)
        /// </summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>
        /// Employee's Social Security Number (required)
        /// Format: XXX-XX-XXXX
        /// WARNING: This is stored as plain text - implement encryption for production use
        /// </summary>
        public string SocialSecurityNumber { get; set; } = "";

        /// <summary>
        /// Gets the employee's full name for display
        /// </summary>
        public string FullName => $"{FirstName} {LastName}";

        /// <summary>
        /// Gets the employee's display name in "LastName, FirstName" format
        /// </summary>
        public string DisplayName => $"{LastName}, {FirstName}";

        /// <summary>
        /// Gets the employee's display name in "LastName, FirstName" format
        /// FIXED: Added missing property for DateRangeReportForm compatibility
        /// </summary>
        public string FullNameLastFirst => $"{LastName}, {FirstName}";

        /// <summary>
        /// Gets the employee's age based on date of birth (if provided)
        /// </summary>
        public int? Age
        {
            get
            {
                if (!DateOfBirth.HasValue)
                    return null;

                var today = DateTime.Today;
                var age = today.Year - DateOfBirth.Value.Year;

                // Check if birthday hasn't occurred this year yet
                if (DateOfBirth.Value.Date > today.AddYears(-age))
                    age--;

                return age;
            }
        }

        /// <summary>
        /// Gets a masked version of the SSN for display (XXX-XX-1234)
        /// </summary>
        public string MaskedSSN
        {
            get
            {
                if (string.IsNullOrEmpty(SocialSecurityNumber) || SocialSecurityNumber.Length < 4)
                    return "***-**-****";

                return $"***-**-{SocialSecurityNumber.Substring(SocialSecurityNumber.Length - 4)}";
            }
        }

        /// <summary>
        /// Validates that all required fields are properly filled
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(FirstName) &&
                   !string.IsNullOrWhiteSpace(LastName) &&
                   PayRate > 0 &&
                   !string.IsNullOrWhiteSpace(PhoneNumber) &&
                   !string.IsNullOrWhiteSpace(SocialSecurityNumber);
        }
    }

}