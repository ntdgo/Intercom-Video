using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FanvilMonitoring.Properties;
using FanvilMonitoring.Services;

namespace FanvilMonitoring.ViewModels.Pages;

public partial class LoginViewModel : ObservableObject
{
    private readonly Services.FanvilService _fanvilService;
    private readonly Services.NavigationService _navigationService;
    [ObservableProperty]
    private string _address = Settings.Default.Address;

    [ObservableProperty]
    private string _username = Settings.Default.Username;

    [ObservableProperty]
    private string _password = Settings.Default.Password;

    public LoginViewModel(FanvilService fanvilService, NavigationService navigationService)
    {
        _fanvilService = fanvilService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public async void Login()
    {
        var isLogin = await _fanvilService.Login(Address, Username, Password);
        if(isLogin)
        {
            Settings.Default.Address = _address;
            Settings.Default.Username = _username;
            Settings.Default.Password = _password;
            Settings.Default.Save();
            _navigationService.Navigate<Views.Pages.MonitoringPage>();
        }
        else
        {
            MessageBox.Show("Login failed.");
        }

        _fanvilService.Start();
    }
}