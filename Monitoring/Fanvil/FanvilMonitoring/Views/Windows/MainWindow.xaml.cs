using System.Windows;
using FanvilMonitoring.Services;

namespace FanvilMonitoring.Views.Windows;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly Services.NavigationService _navigationService;
    public MainWindow(NavigationService navigationService)
    {
        InitializeComponent();
        _navigationService = navigationService;
        _navigationService.Initialize(MainFrame);

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _navigationService.Navigate<Pages.LoginPage>();
    }
}