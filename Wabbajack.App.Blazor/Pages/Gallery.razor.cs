﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Shell;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Wabbajack.App.Blazor.State;
using Wabbajack.Common;
using Wabbajack.DTOs;
using Wabbajack.Paths.IO;
using Wabbajack.RateLimiter;
using Wabbajack.Services.OSIntegrated.Services;

namespace Wabbajack.App.Blazor.Pages;

public partial class Gallery
{
    [Inject] private ILogger<Gallery> Logger { get; set; } = default!;
    [Inject] private IStateContainer StateContainer { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ModListDownloadMaintainer Maintainer { get; set; } = default!;

    private Percent DownloadProgress { get; set; } = Percent.Zero;
    private ModlistMetadata? DownloadingMetaData { get; set; }

    private IEnumerable<ModlistMetadata> Modlists => StateContainer.Modlists;

    private bool _errorLoadingModlists;
    
    private bool _shouldRender;
    protected override bool ShouldRender() => _shouldRender;

    protected override async Task OnInitializedAsync()
    {
        if (!StateContainer.Modlists.Any())
        {
            var res = await StateContainer.LoadModlistMetadata();
            if (!res)
            {
                _errorLoadingModlists = true;
                _shouldRender = true;
                return;
            }
        }
        
        _shouldRender = true;
    }

    private async Task OnClickDownload(ModlistMetadata metadata)
    {
        // GlobalState.NavigationAllowed = !GlobalState.NavigationAllowed;
        await Download(metadata);
    }

    private async Task OnClickInformation(ModlistMetadata metadata)
    {
        // TODO: [High] Implement information modal.
    }

    private async Task Download(ModlistMetadata metadata)
    {
        StateContainer.NavigationAllowed = false;
        DownloadingMetaData = metadata;

        // TODO: download progress should be in ProgressBar component so it can refresh independently
        
        try
        {
            var (progress, task) = Maintainer.DownloadModlist(metadata);
            
            var dispose = progress
                .DistinctUntilChanged(p => p.Value)
                .Throttle(TimeSpan.FromMilliseconds(100))
                .Subscribe(p =>
            {
                DownloadProgress = p;
                StateContainer.TaskBarState = new TaskBarState
                {
                    Description = $"Downloading {metadata.Title}",
                    State = TaskbarItemProgressState.Indeterminate,
                    ProgressValue = p.Value
                };

                InvokeAsync(StateHasChanged);
                // Dispatcher.CreateDefault().InvokeAsync(StateHasChanged);
            }, () => { StateContainer.TaskBarState = new TaskBarState(); });

            await task;
            dispose.Dispose();
            
            var path = KnownFolders.EntryPoint.Combine("downloaded_mod_lists", metadata.Links.MachineURL).WithExtension(Ext.Wabbajack);
            StateContainer.ModlistPath = path;
            NavigationManager.NavigateTo(Configure.Route);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception downloading Modlist {Name}", metadata.Title);
        }
        finally
        {
            StateContainer.TaskBarState = new TaskBarState();
            StateContainer.NavigationAllowed = true;
        }
    }
}
