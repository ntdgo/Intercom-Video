using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FanvilMonitoring.Services;

public class NavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private Frame _frame = null!;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Initialize(Frame frame)
    {
        _frame = frame;
    }

    public void Navigate<TPage>() where TPage : Page
    {
        if (_frame == null)
        {
            throw new InvalidOperationException("Frame is not initialized.");
        }

        var page = _serviceProvider.GetService(typeof(TPage));
        if (page == null)
        {
            throw new InvalidOperationException($"Page of type {typeof(TPage).Name} not found.");
        }
        _frame.Navigate(page);
    }
}