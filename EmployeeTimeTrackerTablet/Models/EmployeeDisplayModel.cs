using EmployeeTimeTracker.Models;

namespace EmployeeTimeTracker.Models
{
    /// <summary>
    /// Display model for Employee Management DataGridView
    /// Contains formatted data for grid display with proper sorting support
    /// </summary>
    public class EmployeeDisplayModel
    {
        public int EmployeeID { get; set; }
        public string FullName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public decimal PayRate { get; set; }
        public string JobTitle { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string DateOfBirthFormatted { get; set; } = "";
        public string SocialSecurityNumberMasked { get; set; } = "";
        public string Active { get; set; } = "";
        public string DateHiredFormatted { get; set; } = "";
        public string CreatedDateFormatted { get; set; } = "";

        /// <summary>
        /// Reference to the original Employee object for editing
        /// </summary>
        public Employee OriginalEmployee { get; set; } = new Employee();
    }
}