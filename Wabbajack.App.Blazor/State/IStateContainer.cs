﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wabbajack.DTOs;
using Wabbajack.Paths;

namespace Wabbajack.App.Blazor.State;

public interface IStateContainer
{
    IEnumerable<ModlistMetadata> Modlists { get; }
    Task<bool> LoadModlistMetadata();
    
    IObservable<bool> NavigationAllowedObservable { get; }
    bool NavigationAllowed { get; set; }
    
    IObservable<AbsolutePath> ModlistPathObservable { get; }
    AbsolutePath ModlistPath { get; set; }
    
    IObservable<ModList?> ModlistObservable { get; }
    ModList? Modlist { get; set; }
    
    IObservable<InstallState> InstallStateObservable { get; }
    InstallState InstallState { get; set; }
    
    IObservable<TaskBarState> TaskBarStateObservable { get; }
    TaskBarState TaskBarState { get; set; }
}
