using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace EmployeeTimeTracker.Utilities
{
    public static class SmartTimeHelper
    {
        /// <summary>
        /// Converts user input to properly formatted time string
        /// Examples: "8" → "8:00 AM", "1430" → "2:30 PM", "830p" → "8:30 PM"
        /// </summary>
        public static string FormatTimeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Clean input - remove spaces and convert to uppercase
            string cleanInput = input.Trim().ToUpper();

            // Check if user explicitly typed AM/PM
            bool hasAmPm = cleanInput.Contains("AM") || cleanInput.Contains("PM") ||
                          cleanInput.Contains("A") || cleanInput.Contains("P");
            bool isPm = cleanInput.Contains("PM") || cleanInput.Contains("P");

            // Extract only digits
            string digits = new string(cleanInput.Where(char.IsDigit).ToArray());

            if (string.IsNullOrEmpty(digits))
                return input; // Return original if no digits

            try
            {
                return ConvertDigitsToTime(digits, hasAmPm, isPm);
            }
            catch
            {
                return input; // Return original if conversion fails
            }
        }

        private static string ConvertDigitsToTime(string digits, bool hasAmPm, bool isPm)
        {
            int hour, minute;

            switch (digits.Length)
            {
                case 1: // "8" → "8:00"
                    hour = int.Parse(digits);
                    minute = 0;
                    break;

                case 2: // "08" → "8:00" or "14" → "2:00 PM"
                    hour = int.Parse(digits);
                    minute = 0;
                    break;

                case 3: // "830" → "8:30"
                    hour = int.Parse(digits.Substring(0, 1));
                    minute = int.Parse(digits.Substring(1, 2));
                    break;

                case 4: // "0830" → "8:30" or "1430" → "2:30 PM"
                    hour = int.Parse(digits.Substring(0, 2));
                    minute = int.Parse(digits.Substring(2, 2));
                    break;

                default:
                    throw new ArgumentException("Invalid time format");
            }

            // Validate hour and minute ranges
            if (hour < 0 || hour > 23 || minute < 0 || minute > 59)
                throw new ArgumentException("Invalid time values");

            // Determine AM/PM
            if (hasAmPm)
            {
                // User specified AM/PM, use their preference
                return FormatTo12Hour(hour, minute, isPm);
            }
            else
            {
                // Smart detection for common business hours
                return SmartAmPmDetection(hour, minute);
            }
        }

        private static string SmartAmPmDetection(int hour, int minute)
        {
            // Business logic for typical work shifts:
            // 1-7: Usually means PM (1 PM - 7 PM) unless very early morning
            // 8-11: Could be AM (8 AM - 11 AM) or PM (8 PM - 11 PM)
            // 12: Usually noon or midnight context
            // 13-23: Convert to PM (1 PM - 11 PM)
            // 0: Midnight (12:00 AM)

            if (hour == 0)
            {
                return $"12:{minute:D2} AM"; // Midnight
            }
            else if (hour >= 1 && hour <= 7)
            {
                // For early hours, default to PM (common work end times)
                // Unless it's very early (1-5 might be AM for overnight shifts)
                if (hour >= 1 && hour <= 5)
                {
                    // Could be early morning - let's default to AM for these
                    return $"{hour}:{minute:D2} AM";
                }
                else
                {
                    // 6-7 more likely to be PM
                    return $"{hour}:{minute:D2} PM";
                }
            }
            else if (hour >= 8 && hour <= 11)
            {
                // Common work hours - default to AM
                return $"{hour}:{minute:D2} AM";
            }
            else if (hour == 12)
            {
                // Noon
                return $"12:{minute:D2} PM";
            }
            else // hour >= 13 && hour <= 23
            {
                // Convert 24-hour to 12-hour PM
                int displayHour = hour - 12;
                return $"{displayHour}:{minute:D2} PM";
            }
        }

        private static string FormatTo12Hour(int hour, int minute, bool forcePm)
        {
            if (hour == 0)
            {
                return $"12:{minute:D2} AM";
            }
            else if (hour >= 1 && hour <= 11)
            {
                string ampm = forcePm ? "PM" : "AM";
                return $"{hour}:{minute:D2} {ampm}";
            }
            else if (hour == 12)
            {
                string ampm = forcePm ? "PM" : "AM";
                return $"12:{minute:D2} {ampm}";
            }
            else // hour >= 13 && hour <= 23
            {
                // Convert to 12-hour format
                int displayHour = hour - 12;
                return $"{displayHour}:{minute:D2} PM";
            }
        }

        /// <summary>
        /// Validates if a string represents a valid time
        /// </summary>
        public static bool IsValidTimeFormat(string timeInput)
        {
            if (string.IsNullOrWhiteSpace(timeInput))
                return false;

            // Try to parse the formatted time
            return DateTime.TryParse(timeInput, out _) || TimeSpan.TryParse(timeInput, out _);
        }

        /// <summary>
        /// Converts formatted time string to TimeSpan for calculations
        /// </summary>
        public static bool TryParseToTimeSpan(string timeInput, out TimeSpan timeSpan)
        {
            timeSpan = TimeSpan.Zero;

            if (string.IsNullOrWhiteSpace(timeInput))
                return false;

            // Try parsing as DateTime first (handles AM/PM)
            if (DateTime.TryParse(timeInput, out DateTime dateTime))
            {
                timeSpan = dateTime.TimeOfDay;
                return true;
            }

            // Try parsing as TimeSpan (handles 24-hour format)
            return TimeSpan.TryParse(timeInput, out timeSpan);
        }

        /// <summary>
        /// Gets suggested format examples for user guidance
        /// </summary>
        public static string GetFormatExamples()
        {
            return "Examples: 8, 830, 8:30, 8:30 AM, 1430, 2:30 PM";
        }

        /// <summary>
        /// Calculates total hours worked with automatic lunch deduction based on business rules.
        /// </summary>
        /// <param name="timeIn">The clock-in time.</param>
        /// <param name="timeOut">The clock-out time.</param>
        /// <param name="lunchThreshold">The minimum work duration before lunch is automatically deducted.</param>
        /// <param name="lunchDuration">The duration of lunch break to deduct.</param>
        /// <returns>The total hours worked after applying lunch deductions.</returns>
        public static double CalculateTotalHours(DateTime timeIn, DateTime timeOut, TimeSpan lunchThreshold, TimeSpan lunchDuration)
        {
            if (timeOut <= timeIn)
            {
                throw new ArgumentException("Clock out time must be after clock in time.");
            }

            var totalTime = timeOut - timeIn;
            var totalHours = totalTime.TotalHours;

            // Apply lunch deduction if work time exceeds threshold
            if (totalTime >= lunchThreshold)
            {
                totalHours -= lunchDuration.TotalHours;
            }

            // Ensure we don't return negative hours
            return Math.Max(0, totalHours);
        }

        /// <summary>
        /// Calculates total hours worked using TimeSpan values with lunch deduction.
        /// </summary>
        /// <param name="timeIn">The clock-in time as TimeSpan.</param>
        /// <param name="timeOut">The clock-out time as TimeSpan.</param>
        /// <param name="lunchThreshold">The minimum work duration before lunch is automatically deducted.</param>
        /// <param name="lunchDuration">The duration of lunch break to deduct.</param>
        /// <returns>The total hours worked after applying lunch deductions.</returns>
        public static double CalculateTotalHours(TimeSpan timeIn, TimeSpan timeOut, TimeSpan lunchThreshold, TimeSpan lunchDuration)
        {
            // Handle overnight shifts (clock out next day)
            var totalTime = timeOut < timeIn ? 
                (TimeSpan.FromDays(1) - timeIn) + timeOut : 
                timeOut - timeIn;

            var totalHours = totalTime.TotalHours;

            // Apply lunch deduction if work time exceeds threshold
            if (totalTime >= lunchThreshold)
            {
                totalHours -= lunchDuration.TotalHours;
            }

            // Ensure we don't return negative hours
            return Math.Max(0, totalHours);
        }

        /// <summary>
        /// Checks if the given time falls within business hours, including support for overnight shifts.
        /// </summary>
        /// <param name="time">The time to check.</param>
        /// <param name="businessStart">Start of business hours (default: 6:00 AM).</param>
        /// <param name="businessEnd">End of business hours (default: 10:00 PM). If earlier than start time, indicates overnight shift.</param>
        /// <returns>True if within business hours, false otherwise.</returns>
        public static bool IsWithinBusinessHours(DateTime time, TimeSpan? businessStart = null, TimeSpan? businessEnd = null)
        {
            var start = businessStart ?? new TimeSpan(6, 0, 0);  // 6:00 AM
            var end = businessEnd ?? new TimeSpan(22, 0, 0);     // 10:00 PM

            var timeOfDay = time.TimeOfDay;

            // Check if this is an overnight shift (end time is earlier than start time)
            if (end < start)
            {
                // Overnight shift: valid if time is after start OR before end
                // Example: 10:00 PM to 3:00 AM allows 11:00 PM or 1:00 AM
                return timeOfDay >= start || timeOfDay <= end;
            }
            else
            {
                // Normal same-day shift: valid if time is between start and end
                // Example: 6:00 AM to 10:00 PM
                return timeOfDay >= start && timeOfDay <= end;
            }
        }

        /// <summary>
        /// Checks if the given time falls within extended business hours for 24-hour operations.
        /// This method provides more flexible validation for businesses that operate nearly 24 hours.
        /// </summary>
        /// <param name="time">The time to check.</param>
        /// <param name="restrictedStart">Start of restricted hours (default: 2:00 AM).</param>
        /// <param name="restrictedEnd">End of restricted hours (default: 5:00 AM).</param>
        /// <returns>True if within allowed hours (outside restricted period), false otherwise.</returns>
        public static bool IsWithinExtendedBusinessHours(DateTime time, TimeSpan? restrictedStart = null, TimeSpan? restrictedEnd = null)
        {
            // For 24-hour operations, it's easier to define when clocking is NOT allowed
            // Default restricted period: 2:00 AM to 5:00 AM (maintenance/cleanup time)
            var restrictStart = restrictedStart ?? new TimeSpan(2, 0, 0);   // 2:00 AM
            var restrictEnd = restrictedEnd ?? new TimeSpan(5, 0, 0);       // 5:00 AM

            var timeOfDay = time.TimeOfDay;

            // If restricted period doesn't cross midnight (normal case)
            if (restrictEnd > restrictStart)
            {
                // Time is valid if it's NOT within the restricted period
                return !(timeOfDay >= restrictStart && timeOfDay <= restrictEnd);
            }
            else
            {
                // If restricted period crosses midnight (e.g., 11 PM to 2 AM)
                // Time is valid if it's NOT (after start OR before end)
                return !(timeOfDay >= restrictStart || timeOfDay <= restrictEnd);
            }
        }

        /// <summary>
        /// Rounds time to the nearest specified interval (e.g., 15 minutes).
        /// </summary>
        /// <param name="time">The time to round.</param>
        /// <param name="interval">The rounding interval in minutes (default: 15).</param>
        /// <returns>The rounded time.</returns>
        public static DateTime RoundToNearestInterval(DateTime time, int interval = 15)
        {
            var minutes = time.Minute;
            var roundedMinutes = (int)Math.Round(minutes / (double)interval) * interval;
            
            if (roundedMinutes >= 60)
            {
                return new DateTime(time.Year, time.Month, time.Day, time.Hour + 1, 0, 0);
            }
            
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, roundedMinutes, 0);
        }

        /// <summary>
        /// Formats a duration into a human-readable string.
        /// </summary>
        /// <param name="duration">The duration to format.</param>
        /// <returns>A formatted string (e.g., "8.5 hours", "45 minutes").</returns>
        public static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                return $"{duration.TotalHours:F1} hours";
            }
            else
            {
                return $"{duration.TotalMinutes:F0} minutes";
            }
        }
    }
}