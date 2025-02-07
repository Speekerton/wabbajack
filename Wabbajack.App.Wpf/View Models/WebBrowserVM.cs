﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.Wpf;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Wabbajack;
using Wabbajack.LibCefHelpers;
using Wabbajack.Messages;
using Wabbajack.Models;
using Wabbajack.WebAutomation;

namespace Wabbajack
{
    public class WebBrowserVM : ViewModel, IBackNavigatingVM, IDisposable
    {
        private readonly ILogger<WebBrowserVM> _logger;
        private readonly CefService _cefService;

        [Reactive]
        public string Instructions { get; set; }

        public IWebBrowser Browser { get; }
        public CefSharpWrapper Driver { get; set; }

        [Reactive]
        public ViewModel NavigateBackTarget { get; set; }

        [Reactive]
        public ReactiveCommand<Unit, Unit> BackCommand { get; set; }

        public Subject<bool> IsBackEnabledSubject { get; } = new Subject<bool>();
        public IObservable<bool> IsBackEnabled { get; }

        public WebBrowserVM(ILogger<WebBrowserVM> logger, CefService cefService)
        {
            // CefService is required so that Cef is initalized
            _logger = logger;
            _cefService = cefService;
            Instructions = "Wabbajack Web Browser";
            
            BackCommand = ReactiveCommand.Create(NavigateBack.Send);
            Browser = cefService.CreateBrowser();
            Driver = new CefSharpWrapper(_logger, Browser, cefService);

        }

        public override void Dispose()
        {
            Browser.Dispose();
            base.Dispose();
        }
    }
}
