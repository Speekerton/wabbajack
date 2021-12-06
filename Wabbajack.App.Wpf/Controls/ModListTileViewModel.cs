﻿

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Wabbajack.App.Wpf.Messages;
using Wabbajack.App.Wpf.Screens;
using Wabbajack.App.Wpf.Support;
using Wabbajack.Common;
using Wabbajack.Downloaders;
using Wabbajack.DTOs;
using Wabbajack.DTOs.JsonConverters;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using Wabbajack.RateLimiter;
using Wabbajack.Services.OSIntegrated;
using Wabbajack.VFS;

namespace Wabbajack.App.Wpf.Controls
{
    
    public enum ModListState
    {
        Downloaded,
        NotDownloaded,
        Downloading,
        Disabled
    }

    public struct ModListTag
    {
        public ModListTag(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    public class ModListTileViewModel : ViewModel
    {
        private ModlistMetadata _metadata;
        public ModlistMetadata Metadata => _metadata;
        
        private ModListGalleryViewModel _parent;

        public ICommand OpenWebsiteCommand { get; }
        public ICommand ExecuteCommand { get; }
        
        public ICommand ModListContentsCommend { get; }
        
        [Reactive]
        public ModListState State { get; set; }
        
        [Reactive]
        public bool Exists { get; set; }
        

        public AbsolutePath Location { get; }

        [Reactive]
        public List<ModListTag> ModListTagList { get; private set; }

        [Reactive]
        public bool IsBroken { get; private set; }
        
        [Reactive]
        public bool IsDownloading { get; private set; }

        [Reactive]
        public string DownloadSizeText { get; private set; }

        [Reactive]
        public string InstallSizeText { get; private set; }
        
        [Reactive]
        public string VersionText { get; private set; }

        //[Reactive]
        //public IErrorResponse Error { get; private set; }

        
        [Reactive]
        public BitmapSource Image { get; set; }
        
        public Uri ImageUri => new(_metadata.Links.ImageUri);
        
        [Reactive]
        public bool LoadingImage { get; set; }
        
        [Reactive]
        public Percent Progress { get; set; } = Percent.Zero;

        private Subject<bool> IsLoadingIdle;
        private readonly Configuration _configuration;
        private readonly FileHashCache _hashCache;
        private readonly ImageCache _imageCache;
        private readonly DownloadDispatcher _dispatcher;
        private readonly IResource<DownloadDispatcher> _downloadLimiter;
        private readonly ILogger _logger;
        private readonly DTOSerializer _dtos;

        public AbsolutePath ModListLocation => _configuration.ModListsDownloadLocation.Combine(_metadata.Links.MachineURL)
            .WithExtension(Ext.Wabbajack);

        public ModListTileViewModel(ModListGalleryViewModel parent, ModlistMetadata metadata, Configuration configuration,
            FileHashCache hashCache, ImageCache imageCache, DownloadDispatcher dispatcher, IResource<DownloadDispatcher> downloadLimiter,
            DTOSerializer dtos, ILogger logger)
        {
            _parent = parent;
            _configuration = configuration;
            _hashCache = hashCache;
            _imageCache = imageCache;
            _metadata = metadata;
            _dispatcher = dispatcher;
            _downloadLimiter = downloadLimiter;
            _logger = logger;
            _dtos = dtos;
            //Location = LauncherUpdater.CommonFolder.Value.Combine("downloaded_mod_lists", Metadata.Links.MachineURL + (string)Consts.ModListExtension);
            ModListTagList = new List<ModListTag>();

            _metadata.tags.ForEach(tag =>
            {
                ModListTagList.Add(new ModListTag(tag));
            });
            ModListTagList.Add(new ModListTag(metadata.Game.MetaData().HumanFriendlyGameName));

            DownloadSizeText = "Download size : " + UIUtils.FormatBytes(_metadata.DownloadMetadata!.SizeOfArchives);
            InstallSizeText = "Installation size : " + UIUtils.FormatBytes(_metadata.DownloadMetadata!.SizeOfInstalledFiles);
            VersionText = "Modlist version : " + _metadata.Version;
            IsBroken = metadata.ValidationSummary.HasFailures || metadata.ForceDown;
            //https://www.wabbajack.org/#/modlists/info?machineURL=eldersouls
            OpenWebsiteCommand = ReactiveCommand.Create(() => UIUtils.OpenWebsite(new Uri($"https://www.wabbajack.org/#/modlists/info?machineURL={_metadata.Links.MachineURL}")));

            IsLoadingIdle = new Subject<bool>();

            this.WhenAny(vm => vm.State)
                .Select(s => s == ModListState.Downloaded)
                .StartWith(false)
                .BindTo(this, vm => vm.Exists)
                .DisposeWith(CompositeDisposable);
            
            UpdateState().FireAndForget();
            LoadImage().FireAndForget();
            
            ExecuteCommand = ReactiveCommand.Create(() =>
                {
                    if (State == ModListState.Downloaded)
                    {
                        //MessageBus.Current.SendMessage(new StartInstallConfiguration(ModListLocation));
                        //MessageBus.Current.SendMessage(NavigateTo.Create(new NavigateTo(typeof(InstallConfigurationViewModel))));
                    }
                    else
                    {
                        DownloadModList().FireAndForget();
                    }
                },
                this.ObservableForProperty(t => t.State)
                    .Select(c => c.Value != ModListState.Downloading && c.Value != ModListState.Disabled)
                    .StartWith(true));

            /*
            ModListContentsCommend = ReactiveCommand.Create(async () =>
            {
                _parent.MWVM.ModListContentsVM.Value.Name = metadata.Title;
                IsLoadingIdle.OnNext(false);
                try
                {
                    var status = await ClientAPIEx.GetDetailedStatus(metadata.Links.MachineURL);
                    var coll = _parent.MWVM.ModListContentsVM.Value.Status;
                    coll.Clear();
                    coll.AddRange(status.Archives);
                    _parent.MWVM.NavigateTo(_parent.MWVM.ModListContentsVM.Value);
                }
                finally
                {
                    IsLoadingIdle.OnNext(true);
                }
            }, IsLoadingIdle.StartWith(true));
            ExecuteCommand = ReactiveCommand.CreateFromObservable<Unit, Unit>(
                canExecute: this.WhenAny(x => x.IsBroken).Select(x => !x),
                execute: (unit) =>
                Observable.Return(unit)
                .WithLatestFrom(
                    this.WhenAny(x => x.Exists),
                    (_, e) => e)
                // Do any download work on background thread
                .ObserveOn(RxApp.TaskpoolScheduler)
                .SelectTask(async (exists) =>
                {
                    if (!exists)
                    {
                        try
                        {
                            var success = await Download();
                            if (!success)
                            {
                                Error = ErrorResponse.Fail("Download was marked unsuccessful");
                                return false;
                            }
                        }
                        catch (Exception ex)
                        {
                            Error = ErrorResponse.Fail(ex);
                            return false;
                        }
                        // Return an updated check on exists
                        return Location.Exists;
                    }
                    return exists;
                })
                .Where(exists => exists)
                // Do any install page swap over on GUI thread
                .ObserveOnGuiThread()
                .Select(_ =>
                {
                    _parent.MWVM.OpenInstaller(Location);

                    // Wait for modlist member to be filled, then open its readme
                    return _parent.MWVM.Installer.Value.WhenAny(x => x.ModList)
                        .NotNull()
                        .Take(1)
                        .Do(modList =>
                        {
                            try
                            {
                                modList.OpenReadme();
                            }
                            catch (Exception ex)
                            {
                                Utils.Error(ex);
                            }
                        });
                })
                .Switch()
                .Unit());



            var imageObs = Observable.Return(Metadata.Links.ImageUri)
                .DownloadBitmapImage((ex) => Utils.Log($"Error downloading modlist image {Metadata.Title}"));

            _Image = imageObs
                .ToGuiProperty(this, nameof(Image));

            _LoadingImage = imageObs
                .Select(x => false)
                .StartWith(true)
                .ToGuiProperty(this, nameof(LoadingImage));
                */
        }



        private async Task DownloadModList()
        {
            State = ModListState.Downloading;
            var state = _dispatcher.Parse(new Uri(_metadata.Links.Download));
            var archive = new Archive
            {
                State = state!,
                Hash = _metadata.DownloadMetadata?.Hash ?? default,
                Size = _metadata.DownloadMetadata?.Size ?? 0,
                Name = ModListLocation.FileName.ToString()
            };

            using var job = await _downloadLimiter.Begin(state!.PrimaryKeyString, archive.Size, CancellationToken.None);

            var hashTask = _dispatcher.Download(archive, ModListLocation, job, CancellationToken.None);

            while (!hashTask.IsCompleted)
            {
                Progress = Percent.FactoryPutInRange(job.Current, job.Size ?? 0);
                await Task.Delay(100);
            }

            var hash = await hashTask;
            if (hash != _metadata.DownloadMetadata?.Hash)
            {
                _logger.LogWarning("Hash files didn't match after downloading modlist, deleting modlist");
                if (ModListLocation.FileExists())
                    ModListLocation.Delete();
            }

            _hashCache.FileHashWriteCache(ModListLocation, hash);

            var metadataPath = ModListLocation.WithExtension(Ext.MetaData);
            await metadataPath.WriteAllTextAsync(_dtos.Serialize(_metadata));

            Progress = Percent.Zero;
            await UpdateState();
        }
        
                
        public async Task LoadImage()
        {
            LoadingImage = true;
            Image = await _imageCache.From(ImageUri, 540, 300);
            LoadingImage = false;
        }
        
        public async Task<ModListState> GetState()
        {
            if (_metadata.ForceDown || _metadata.ValidationSummary.HasFailures)
                return ModListState.Disabled;
        
            var file = ModListLocation;
            if (!file.FileExists())
                return ModListState.NotDownloaded;

            return await _hashCache.FileHashCachedAsync(file, CancellationToken.None) !=
                   _metadata.DownloadMetadata?.Hash
                ? ModListState.NotDownloaded
                : ModListState.Downloaded;
        }
        
        public async Task UpdateState()
        {
            State = await GetState();
        }

    }
    
}
