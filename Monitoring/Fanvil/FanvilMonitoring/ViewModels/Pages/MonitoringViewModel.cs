using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FanvilMonitoring.Services;

namespace FanvilMonitoring.ViewModels.Pages;

public partial class MonitoringViewModel : ObservableObject
{
    private readonly Services.FanvilService _fanvilService;

    [ObservableProperty]
    private bool _input0;

    [ObservableProperty]
    private bool _input1;

    [ObservableProperty]
    private bool _input2;

    [ObservableProperty]
    private ObservableCollection<string> _receivedMessages = new ObservableCollection<string>();


    public MonitoringViewModel(FanvilService fanvilService)
    {
        _fanvilService = fanvilService;
        _fanvilService.OnInputChanged += FanvilServiceOnOnInputChanged;
        _fanvilService.OnMessageReceived += FanvilService_OnMessageReceived;
    }

    private void FanvilService_OnMessageReceived(object? sender, string e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ReceivedMessages.Add(e);
            if (ReceivedMessages.Count > 100)
            {
                ReceivedMessages.RemoveAt(0);
            }
        });
    }

    private void FanvilServiceOnOnInputChanged(object? sender, (bool status, string input) e)
    {
        switch (e.input)
        {
            case "Input0":
                Input0 = e.status;
                break;
            case "Input1":
                Input1 = e.status;
                break;
            case "Input2":
                Input2 = e.status;
                if(e.status)
                {
                    _fanvilService.SetOutput(0);
                    _fanvilService.SetOutput(1);
                }
                break;
        }
    }

    [RelayCommand]
    public async void Output0()
    {
        await _fanvilService.SetOutput(0);
    }

    [RelayCommand]
    public async void Output1()
    {
        await _fanvilService.SetOutput(1);
    }
}