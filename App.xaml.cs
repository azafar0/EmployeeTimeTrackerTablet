using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        /// Gets the service provider for dependency injection access.
        /// </summary>
        public IServiceProvider? Services => _host?.Services;

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
                        
                        // ?? DEVELOPMENT/TESTING ONLY - Register TestDataResetService
                        System.Diagnostics.Debug.WriteLine("Registering TestDataResetService (DEVELOPMENT ONLY)...");
                        services.AddScoped<TestDataResetService>();
                        
                        System.Diagnostics.Debug.WriteLine("Registering MainViewModel with explicit dependency injection...");
                        services.AddTransient<MainViewModel>(provider => 
                            new MainViewModel(
                                provider.GetRequiredService<EmployeeRepository>(),
                                provider.GetRequiredService<TimeEntryRepository>(),
                                provider.GetRequiredService<TabletTimeService>(),
                                provider.GetRequiredService<TestDataResetService>(), // ?? DEVELOPMENT ONLY
                                provider.GetRequiredService<PhotoCaptureService>() // For camera settings integration
                            ));
                        
                        System.Diagnostics.Debug.WriteLine("Registering AdminMainViewModel...");
                        services.AddTransient<AdminMainViewModel>();
                        
                        System.Diagnostics.Debug.WriteLine("Registering MainWindow...");
                        services.AddTransient<Views.MainWindow>(); // Use fully qualified name to be explicit
                        
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
                System.Diagnostics.Debug.WriteLine("? Database initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Database initialization failed: {ex.Message}");
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