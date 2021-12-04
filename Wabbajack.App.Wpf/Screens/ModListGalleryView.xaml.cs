﻿using System.Reactive.Disposables;
using ReactiveUI;
using Wabbajack.App.Wpf.Support;

namespace Wabbajack.App.Wpf.Screens
{
    public partial class ModListGalleryView
    {
        public ModListGalleryView(ModListGalleryViewModel vm) : base(vm)
        {
            InitializeComponent();

            this.WhenActivated(dispose =>
            {
                
                this.WhenAny(x => x.ViewModel.BackCommand)
                    .BindToStrict(this, x => x.BackButton.Command)
                    .DisposeWith(dispose);
                
                /*

                this.WhenAny(x => x.ViewModel.ModLists)
                    .BindToStrict(this, x => x.ModListGalleryControl.ItemsSource)
                    .DisposeWith(dispose);
                Observable.CombineLatest(
                        this.WhenAny(x => x.ViewModel.Error),
                        this.WhenAny(x => x.ViewModel.Loaded),
                        resultSelector: (err, loaded) =>
                        {
                            if (!err?.Succeeded ?? false) return true;
                            return !loaded;
                        })
                    .DistinctUntilChanged()
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .StartWith(Visibility.Collapsed)
                    .BindToStrict(this, x => x.LoadingRing.Visibility)
                    .DisposeWith(dispose);
                Observable.CombineLatest(
                        this.WhenAny(x => x.ViewModel.ModLists.Count)
                            .Select(x => x > 0),
                        this.WhenAny(x => x.ViewModel.Loaded),
                        resultSelector: (hasContent, loaded) =>
                        {
                            return !hasContent && loaded;
                        })
                    .DistinctUntilChanged()
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .StartWith(Visibility.Collapsed)
                    .BindToStrict(this, x => x.NoneFound.Visibility)
                    .DisposeWith(dispose);
                this.WhenAny(x => x.ViewModel.Error)
                    .Select(e => (e?.Succeeded ?? true) ? Visibility.Collapsed : Visibility.Visible)
                    .StartWith(Visibility.Collapsed)
                    .BindToStrict(this, x => x.ErrorIcon.Visibility)
                    .DisposeWith(dispose);

                this.BindStrict(ViewModel, vm => vm.Search, x => x.SearchBox.Text)
                    .DisposeWith(dispose);

                this.BindStrict(ViewModel, vm => vm.OnlyInstalled, x => x.OnlyInstalledCheckbox.IsChecked)
                    .DisposeWith(dispose);
                this.BindStrict(ViewModel, vm => vm.ShowNSFW, x => x.ShowNSFW.IsChecked)
                    .DisposeWith(dispose);
                this.BindStrict(ViewModel, vm => vm.ShowUtilityLists, x => x.ShowUtilityLists.IsChecked)
                    .DisposeWith(dispose);

                this.WhenAny(x => x.ViewModel.ClearFiltersCommand)
                    .BindToStrict(this, x => x.ClearFiltersButton.Command)
                    .DisposeWith(dispose);
                    */
            });
        }
    }
}
