using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EmployeeTimeTrackerTablet.Views;
using EmployeeTimeTrackerTablet.ViewModels;
using EmployeeTimeTracker.Data;
using EmployeeTimeTrackerTablet.Services;

namespace EmployeeTimeTrackerTablet
{
    public partial class App : System.Windows.Application
    {
        private IHost? _host;

        /// <summary>
        /// Provides access to the application's service provider for dependency injection.
        /// </summary>
        public IServiceProvider Services => _host?.Services ?? throw new InvalidOperationException("Services not available");

        /// <summary>
        /// Creates a DualTimeCorrectionDialog with dependency injection support.
        /// This helper method resolves the required services from DI and combines them with runtime parameters.
        /// </summary>
        /// <param name="employee">The employee whose time is being corrected</param>
        /// <param name="timeEntry">The time entry to be corrected</param>
        /// <returns>A fully configured DualTimeCorrectionDialog instance</returns>
        public Views.DualTimeCorrectionDialog CreateDualTimeCorrectionDialog(
            EmployeeTimeTracker.Models.Employee employee, 
            EmployeeTimeTracker.Models.TimeEntry timeEntry)
        {
            if (_host?.Services == null)
                throw new InvalidOperationException("Services not available");

            // Resolve dependencies from DI container
            var timeEntryRepository = _host.Services.GetRequiredService<TimeEntryRepository>();
            var logger = _host.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Views.DualTimeCorrectionDialog>>();

            // Create dialog with both DI services and runtime parameters
            return new Views.DualTimeCorrectionDialog(employee, timeEntry, timeEntryRepository, logger);
        }

        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            
            // Add additional startup diagnostics
            System.Diagnostics.Debug.WriteLine("=== App Constructor Called ===");
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== OnStartup Begin ===");
            
            try
            {
                System.Diagnostics.Debug.WriteLine("Creating host builder...");
                _host = CreateHostBuilder().Build();
                
                System.Diagnostics.Debug.WriteLine("Starting host...");
                await _host.StartAsync();

                System.Diagnostics.Debug.WriteLine("Initializing database...");
                await InitializeDatabaseAsync();

                System.Diagnostics.Debug.WriteLine("Getting MainWindow from DI container...");
                var mainWindow = _host.Services.GetRequiredService<Views.MainWindow>();
                
                System.Diagnostics.Debug.WriteLine("Showing MainWindow...");
                mainWindow.Show();
                
                System.Diagnostics.Debug.WriteLine("=== OnStartup Complete ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== STARTUP ERROR ===");
                System.Diagnostics.Debug.WriteLine($"Exception Type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                
                System.Windows.MessageBox.Show(
                    $"Application failed to start: {ex.Message}\n\nInner Exception: {ex.InnerException?.Message}\n\nSee debug output for details.",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                Current.Shutdown();
            }

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== OnExit Begin ===");
            
            try
            {
                if (_host != null)
                {
                    System.Diagnostics.Debug.WriteLine("Stopping host...");
                    await _host.StopAsync();
                    System.Diagnostics.Debug.WriteLine("Disposing host...");
                    _host.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App.OnExit Error]: {ex.Message}");
            }
            
            System.Diagnostics.Debug.WriteLine("=== OnExit Complete ===");
            base.OnExit(e);
        }

        private IHostBuilder CreateHostBuilder()
        {
            System.Diagnostics.Debug.WriteLine("Configuring services...");
            
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("Registering EmployeeRepository...");
                        services.AddScoped<EmployeeRepository>();
                        
                        System.Diagnostics.Debug.WriteLine("Registering TimeEntryRepository...");
                        services.AddScoped<TimeEntryRepository>();
                        
                        System.Diagnostics.Debug.WriteLine("Registering CameraSettingsService...");
                        services.AddSingleton<CameraSettingsService>();
                        
                        System.Diagnostics.Debug.WriteLine("Registering PhotoCaptureService...");
                        services.AddScoped<PhotoCaptureService>();
                        
                        System.Diagnostics.Debug.WriteLine("Registering TabletTimeService...");
                        services.AddScoped<TabletTimeService>();
                        
                        // SEGMENT 2: Register ManagerAuthService for time correction functionality
                        System.Diagnostics.Debug.WriteLine("Registering ManagerAuthService...");
                        services.AddSingleton<ManagerAuthService>();
                        
                        // ⚠️ DEVELOPMENT/TESTING ONLY - Register TestDataResetService
                        System.Diagnostics.Debug.WriteLine("Registering TestDataResetService (DEVELOPMENT ONLY)...");
                        services.AddScoped<TestDataResetService>();
                        
                        System.Diagnostics.Debug.WriteLine("Registering MainViewModel with explicit dependency injection...");
                        services.AddTransient<MainViewModel>(provider => 
                            new MainViewModel(
                                provider.GetRequiredService<EmployeeRepository>(),
                                provider.GetRequiredService<TimeEntryRepository>(),
                                provider.GetRequiredService<TabletTimeService>(),
                                provider.GetRequiredService<TestDataResetService>(), // ⚠️ DEVELOPMENT ONLY
                                provider.GetRequiredService<PhotoCaptureService>(), // For camera settings integration
                                provider.GetRequiredService<ManagerAuthService>() // For manager time correction
                            ));
                        
                        // CRITICAL FIX: Register AdminMainViewModel with dependency injection
                        System.Diagnostics.Debug.WriteLine("Registering AdminMainViewModel with explicit dependency injection...");
                        services.AddTransient<AdminMainViewModel>(provider => 
                            new AdminMainViewModel(
                                provider.GetRequiredService<TimeEntryRepository>(),
                                provider.GetRequiredService<EmployeeRepository>(),
                                provider.GetRequiredService<ILogger<AdminMainViewModel>>(),
                                provider.GetRequiredService<ManagerAuthService>() // Add manager authentication service
                            ));
                        
                        System.Diagnostics.Debug.WriteLine("Registering MainWindow...");
                        services.AddTransient<Views.MainWindow>(); // Use fully qualified name to be explicit
                        
                        // Register DualTimeCorrectionDialog for dependency injection
                        System.Diagnostics.Debug.WriteLine("Registering DualTimeCorrectionDialog...");
                        services.AddTransient<Views.DualTimeCorrectionDialog>();
                        
                        System.Diagnostics.Debug.WriteLine("Service registration complete.");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Service registration error: {ex.Message}");
                        throw;
                    }
                });
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting database initialization...");
                await Task.Run(() => DatabaseHelper.InitializeDatabase());
                System.Diagnostics.Debug.WriteLine("✅ Database initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Database initialization failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new InvalidOperationException($"Database initialization failed: {ex.Message}", ex);
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== UNHANDLED EXCEPTION ===");
                System.Diagnostics.Debug.WriteLine($"Exception Type: {e.Exception.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Message: {e.Exception.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {e.Exception.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"Inner Exception: {e.Exception.InnerException?.Message}");
                
                System.Windows.MessageBox.Show(
                    $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nThe application will continue running, but you may need to restart if issues persist.\n\nSee debug output for details.",
                    "Unexpected Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                e.Handled = true;
            }
            catch
            {
                e.Handled = false;
            }
        }
    }
}
