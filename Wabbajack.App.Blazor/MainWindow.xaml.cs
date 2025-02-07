﻿using System;
using Wabbajack.App.Blazor.State;

namespace Wabbajack.App.Blazor;

public partial class MainWindow
{
    public MainWindow(IServiceProvider serviceProvider, IStateContainer stateContainer)
    {
        stateContainer.TaskBarStateObservable.Subscribe(state =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                TaskBarItem.Description = state.Description;
                TaskBarItem.ProgressState = state.State;
                TaskBarItem.ProgressValue = state.ProgressValue;
            });
        });
        
        InitializeComponent();
        BlazorWebView.Services = serviceProvider;
    }
}

// Required so compiler doesn't complain about not finding the type. [MC3050]
public partial class Main { }
