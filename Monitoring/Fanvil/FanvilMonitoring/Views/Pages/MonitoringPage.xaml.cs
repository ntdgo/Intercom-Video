using System.Windows.Controls;

namespace FanvilMonitoring.Views.Pages;

/// <summary>
/// Interaction logic for MonitoringPage.xaml
/// </summary>
public partial class MonitoringPage : Page
{
    public ViewModels.Pages.MonitoringViewModel ViewModel { get; }
    public MonitoringPage(ViewModels.Pages.MonitoringViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel;
        DataContext = this;
    }
}