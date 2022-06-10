using osuCollectionDownloader.Global;
using ReactiveUI;
using Splat;
using System.IO;
using System.Reflection;
using System.Windows;

namespace osuCollectionDownloader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Directory.CreateDirectory(Paths.BaseDirectory);
            Directory.CreateDirectory(Paths.DownloadedBeatmapsDirectory);
            Directory.CreateDirectory(Paths.LogsDirectory);

            Locator.CurrentMutable.InitializeSplat();
            Locator.CurrentMutable.InitializeReactiveUI(RegistrationNamespace.Wpf);
            Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetExecutingAssembly());
        }
    }
}
