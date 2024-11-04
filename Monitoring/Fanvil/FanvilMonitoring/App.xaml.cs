using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Navigation;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace FanvilMonitoring;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static readonly IHost Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
        .ConfigureServices((_, services) =>
        {
            ConfigService(services);
        })
        .Build();

    public App()
    {
        var procName = Process.GetCurrentProcess().ProcessName;
        var processes = Process.GetProcessesByName(procName);
        if (processes.Length > 1)
        {
            MessageBox.Show("프로그램이 이미 실행중입니다.", "알람 메세지");
            Application.Current.Shutdown();
            return;
        }
        Startup += OnStartup;
        Exit += OnExit;
    }

    public static void ConfigService(IServiceCollection services)
    {
        services.AddHostedService<Services.ApplicationHostService>();
        services.AddSingleton<Services.NavigationService>();
        services.AddSingleton<Services.FanvilService>();
        //services.AddSingleton<Services.ConnectionService>();
        ////services.AddDbContext<Database.PosDbContext>(options =>
        ////{
        ////    options.UseSqlite($"Data Source={Path.Combine(Helpers.AppData.LocalAppData, "Database.db")}");
        ////});
        ////services
        ////    .AddScoped<Database.Repositories.IProductRepository,
        ////        Database.Repositories.ProductRepository>();
        ////services
        ////    .AddScoped<Database.Repositories.ICategoryRepository,
        ////        Database.Repositories.CategoryRepository>();


        services.AddSingleton<Views.Windows.MainWindow>();
        //services.AddSingleton<ViewModels.Windows.MainWindowViewModel>();

        services.AddSingleton<Views.Pages.LoginPage>();
        services.AddSingleton<ViewModels.Pages.LoginViewModel>();


        services.AddSingleton<Views.Pages.MonitoringPage>();
        services.AddSingleton<ViewModels.Pages.MonitoringViewModel>();

        //services.AddSingleton<Views.Pages.HistoryDetail>();

        //services.AddSingleton<Views.Pages.SettingsPage>();
        //services.AddSingleton<ViewModels.Pages.SettingsViewModel>();

        //services.AddSingleton<Views.Pages.SettingPage>();
        //services.AddSingleton<Views.Pages.SettingAppearance>();
        //services.AddSingleton<Views.Pages.SettingProduct>();
        //services.AddSingleton<Views.Pages.SettingSecurity>();
        //services.AddSingleton<Views.Pages.SettingAboutUs>();

    }

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        //var vCulture = new CultureInfo("ko-KR");

        //Thread.CurrentThread.CurrentCulture = vCulture;
        //Thread.CurrentThread.CurrentUICulture = vCulture;
        //CultureInfo.DefaultThreadCurrentCulture = vCulture;
        //CultureInfo.DefaultThreadCurrentUICulture = vCulture;

        //FrameworkElement.LanguageProperty.OverrideMetadata(
        //    typeof(FrameworkElement),
        //    new FrameworkPropertyMetadata(
        //        XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        await Host.StartAsync();
    }

    private async void OnExit(object sender, ExitEventArgs e)
    {
        await Host.StopAsync();
        Host.Dispose();
    }
}

