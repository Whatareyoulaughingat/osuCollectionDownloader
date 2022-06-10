using osuCollectionDownloader.Global;
using osuCollectionDownloader.ViewModels;
using ReactiveUI;
using Serilog;
using System;
using System.Globalization;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;

namespace osuCollectionDownloader.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainWindowViewModel();

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Async(x => x.RichTextBox(Logger, formatProvider: CultureInfo.InvariantCulture))
            .WriteTo.Async(x => x.File(Path.GetFullPath("log.txt", Paths.LogsDirectory), rollingInterval: RollingInterval.Day))
            .CreateLogger();

        this.WhenActivated(disposableRegistration =>
        {
            // Titlebar bindings.
            this.OneWayBind(
                ViewModel,
                vm => vm.TitlebarName,
                view => view.TitlebarName.Text)
            .DisposeWith(disposableRegistration);

            // Osu directory path bindings.
            this.Bind(
                ViewModel,
                vm => vm.OsuDirectory,
                view => view.OsuDirectory.Text)
            .DisposeWith(disposableRegistration);

            this.OneWayBind(
                ViewModel,
                vm => vm.IsDownloading,
                view => view.OsuDirectory.IsEnabled,
                value => !value)
            .DisposeWith(disposableRegistration);

            this.BindCommand(
                ViewModel,
                vm => vm.OpenOsuFolderDialog,
                view => view.OpenOsuFolderDialog)
            .DisposeWith(disposableRegistration);

            this.OneWayBind(
                ViewModel,
                vm => vm.IsDownloading,
                view => view.OpenOsuFolderDialog.IsEnabled,
                value => !value)
            .DisposeWith(disposableRegistration);

            // Collection ID bindings.
            this.Bind(
                ViewModel,
                vm => vm.CollectionId,
                view => view.CollectionId.Text)
            .DisposeWith(disposableRegistration);

            this.OneWayBind(
                ViewModel,
                vm => vm.IsDownloading,
                view => view.CollectionId.IsEnabled,
                value => !value)
            .DisposeWith(disposableRegistration);

            // Start/StartCollection.db download bindings.
            this.BindCommand(
                ViewModel,
                vm => vm.StartDownload,
                view => view.StartDownload)
            .DisposeWith(disposableRegistration);

            this.OneWayBind(
                ViewModel,
                vm => vm.IsDownloading,
                view => view.StartDownload.IsEnabled,
                value => !value)
            .DisposeWith(disposableRegistration);

            this.BindCommand(
                ViewModel,
                vm => vm.StartCollectionDbDownload,
                view => view.StartCollectionDbDownload)
            .DisposeWith(disposableRegistration);

            this.OneWayBind(
                ViewModel,
                vm => vm.IsDownloading,
                view => view.StartCollectionDbDownload.IsEnabled,
                value => !value)
            .DisposeWith(disposableRegistration);

            // Collection.db output file bindings.
            this.Bind(
                ViewModel,
                vm => vm.CollectionDbOutput,
                view => view.CollectionDbOutput.Text)
            .DisposeWith(disposableRegistration);

            this.OneWayBind(
                ViewModel,
                vm => vm.IsDownloading,
                view => view.CollectionDbOutput.IsEnabled,
                value => !value)
            .DisposeWith(disposableRegistration);

            this.BindCommand(
                ViewModel,
                vm => vm.OpenCollectionDbOutputFolderDialog,
                view => view.OpenCollectionDbOutputFolderDialog)
            .DisposeWith(disposableRegistration);

            this.OneWayBind(
                ViewModel,
                vm => vm.IsDownloading,
                view => view.OpenCollectionDbOutputFolderDialog.IsEnabled,
                value => !value)
            .DisposeWith(disposableRegistration);

            // Events setup.
            Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(
                handler => MinimizeWindow.Click += handler,
                handler => MinimizeWindow.Click -= handler)
            .Subscribe(x => WindowState = WindowState.Minimized);

            Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(
                handler => CloseWindow.Click += handler,
                handler => CloseWindow.Click -= handler)
            .Subscribe(x => Close());

            Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(
                handler => AlternativeDownloadOptions.Click += handler,
                handler => AlternativeDownloadOptions.Click -= handler)
            .Subscribe(x => StartCollectionDbDownload.Visibility = StartCollectionDbDownload.Visibility == Visibility.Visible ? StartCollectionDbDownload.Visibility = Visibility.Hidden : Visibility.Visible);

            Observable.FromEventPattern<TextChangedEventHandler, TextChangedEventArgs>(
                handler => Logger.TextChanged += handler,
                handler => Logger.TextChanged -= handler)
            .Subscribe(x => Logger.ScrollToEnd());
        });
    }
}
