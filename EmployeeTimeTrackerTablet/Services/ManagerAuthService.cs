using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;

namespace EmployeeTimeTrackerTablet.Services
{
    /// <summary>
    /// Service for managing manager authentication using PIN-based security.
    /// Provides secure authentication with session timeout management.
    /// </summary>
    public class ManagerAuthService
    {
        private const string MANAGER_PIN = "9999";
        private const int AUTH_TIMEOUT_MINUTES = 1;
        
        private DateTime? _lastAuthTimestamp = null;
        private bool _isAuthenticated = false;

        /// <summary>
        /// Authenticates a manager using the provided PIN (async version).
        /// </summary>
        /// <param name="pin">The PIN entered by the manager.</param>
        /// <returns>True if authentication successful, false otherwise.</returns>
        public async Task<bool> AuthenticateAsync(string pin)
        {
            try
            {
                Debug.WriteLine($"ManagerAuthService: Authentication attempt with PIN: {new string('*', pin?.Length ?? 0)}");
                
                // Simulate processing time for security
                await Task.Delay(500);
                
                if (string.IsNullOrWhiteSpace(pin))
                {
                    Debug.WriteLine("ManagerAuthService: Authentication failed - empty PIN");
                    return false;
                }
                
                if (pin.Trim() == MANAGER_PIN)
                {
                    _isAuthenticated = true;
                    _lastAuthTimestamp = DateTime.Now;
                    Debug.WriteLine("ManagerAuthService: Authentication successful");
                    return true;
                }
                else
                {
                    Debug.WriteLine("ManagerAuthService: Authentication failed - incorrect PIN");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ManagerAuthService: Authentication error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Authenticates a manager using the provided PIN (synchronous version for backward compatibility).
        /// </summary>
        /// <param name="pin">The PIN entered by the manager.</param>
        /// <returns>True if authentication successful, false otherwise.</returns>
        public bool AuthenticateManager(string pin)
        {
            try
            {
                Debug.WriteLine($"ManagerAuthService: Sync authentication attempt with PIN: {new string('*', pin?.Length ?? 0)}");
                
                if (string.IsNullOrWhiteSpace(pin))
                {
                    Debug.WriteLine("ManagerAuthService: Authentication failed - empty PIN");
                    return false;
                }
                
                if (pin.Trim() == MANAGER_PIN)
                {
                    _isAuthenticated = true;
                    _lastAuthTimestamp = DateTime.Now;
                    Debug.WriteLine("ManagerAuthService: Authentication successful");
                    return true;
                }
                else
                {
                    Debug.WriteLine("ManagerAuthService: Authentication failed - incorrect PIN");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ManagerAuthService: Authentication error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if the manager is currently authenticated and session hasn't expired.
        /// </summary>
        /// <returns>True if authenticated and session valid, false otherwise.</returns>
        public bool IsAuthenticatedAndValid()
        {
            try
            {
                if (!_isAuthenticated || !_lastAuthTimestamp.HasValue)
                {
                    return false;
                }

                var timeSinceAuth = DateTime.Now - _lastAuthTimestamp.Value;
                var isValid = timeSinceAuth.TotalMinutes <= AUTH_TIMEOUT_MINUTES;
                
                if (!isValid)
                {
                    Debug.WriteLine("ManagerAuthService: Session expired, clearing authentication");
                    ClearAuthentication();
                }
                
                return isValid;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ManagerAuthService: Error checking authentication: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the remaining time in minutes before authentication expires.
        /// </summary>
        /// <returns>Minutes remaining, or 0 if not authenticated.</returns>
        public int GetRemainingMinutes()
        {
            try
            {
                if (!_isAuthenticated || !_lastAuthTimestamp.HasValue)
                {
                    return 0;
                }

                var timeSinceAuth = DateTime.Now - _lastAuthTimestamp.Value;
                var remainingMinutes = AUTH_TIMEOUT_MINUTES - (int)timeSinceAuth.TotalMinutes;
                
                return Math.Max(0, remainingMinutes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ManagerAuthService: Error calculating remaining time: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the remaining time as a TimeSpan for precise countdown display.
        /// </summary>
        /// <returns>TimeSpan representing remaining time, or TimeSpan.Zero if not authenticated.</returns>
        public TimeSpan GetRemainingTimeSpan()
        {
            try
            {
                if (!_isAuthenticated || !_lastAuthTimestamp.HasValue)
                {
                    return TimeSpan.Zero;
                }

                var timeSinceAuth = DateTime.Now - _lastAuthTimestamp.Value;
                var timeoutDuration = TimeSpan.FromMinutes(AUTH_TIMEOUT_MINUTES);
                var remainingTime = timeoutDuration - timeSinceAuth;
                
                return remainingTime > TimeSpan.Zero ? remainingTime : TimeSpan.Zero;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ManagerAuthService: Error calculating remaining TimeSpan: {ex.Message}");
                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Extends the current authentication session by resetting the timestamp.
        /// Only works if currently authenticated.
        /// </summary>
        /// <returns>True if session extended, false if not authenticated.</returns>
        public bool ExtendSession()
        {
            try
            {
                if (IsAuthenticatedAndValid())
                {
                    _lastAuthTimestamp = DateTime.Now;
                    Debug.WriteLine("ManagerAuthService: Session extended");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ManagerAuthService: Error extending session: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clears the current authentication session.
        /// </summary>
        public void ClearAuthentication()
        {
            try
            {
                _isAuthenticated = false;
                _lastAuthTimestamp = null;
                Debug.WriteLine("ManagerAuthService: Authentication cleared");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ManagerAuthService: Error clearing authentication: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the authentication status message based on current state.
        /// </summary>
        /// <returns>Status message describing authentication state.</returns>
        public string GetAuthStatusMessage()
        {
            try
            {
                if (!_isAuthenticated)
                {
                    return "Manager authentication required";
                }

                if (!IsAuthenticatedAndValid())
                {
                    return "Manager session expired";
                }

                var remainingMinutes = GetRemainingMinutes();
                return $"Manager authenticated ({remainingMinutes} min remaining)";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ManagerAuthService: Error getting status message: {ex.Message}");
                return "Authentication status unknown";
            }
        }

        /// <summary>
        /// Gets the authentication status message with precise countdown.
        /// </summary>
        /// <returns>Status message describing authentication state with precise timing.</returns>
        public string GetPreciseAuthStatusMessage()
        {
            try
            {
                if (!_isAuthenticated)
                {
                    return "Manager authentication required";
                }

                if (!IsAuthenticatedAndValid())
                {
                    return "Manager session expired";
                }

                var remainingTime = GetRemainingTimeSpan();
                
                if (remainingTime.TotalSeconds <= 0)
                {
                    return "Manager session expired";
                }
                else if (remainingTime.TotalMinutes >= 1)
                {
                    return $"Manager authenticated ({remainingTime.Minutes} min {remainingTime.Seconds} sec remaining)";
                }
                else
                {
                    return $"Manager authenticated ({remainingTime.Seconds} sec remaining)";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ManagerAuthService: Error getting precise status message: {ex.Message}");
                return "Authentication status unknown";
            }
        }

        /// <summary>
        /// Gets the timestamp of the last successful authentication.
        /// </summary>
        /// <returns>DateTime of last auth, or null if not authenticated.</returns>
        public DateTime? GetLastAuthTimestamp()
        {
            return _lastAuthTimestamp;
        }

        /// <summary>
        /// Validates that the provided PIN meets security requirements.
        /// </summary>
        /// <param name="pin">The PIN to validate.</param>
        /// <returns>True if PIN format is valid, false otherwise.</returns>
        public bool ValidatePinFormat(string pin)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pin))
                {
                    return false;
                }

                var cleanPin = pin.Trim();
                
                // PIN must be exactly 4 digits
                if (cleanPin.Length != 4)
                {
                    return false;
                }

                // PIN must contain only digits
                return cleanPin.All(char.IsDigit);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ManagerAuthService: Error validating PIN format: {ex.Message}");
                return false;
            }
        }
    }
}