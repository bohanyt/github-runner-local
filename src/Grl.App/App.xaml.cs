using System.Windows;
using Grl.App.Presentation;
using Grl.Integration;

namespace Grl.App;

public partial class App : Application
{
    private LiveWizardComposition? live;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        if (e.Args.Contains("--live", StringComparer.Ordinal))
        {
            try
            {
                live = LiveWizardComposition.Create(LiveRuntimeOptions.Parse(e.Args));
                var liveWindow = new MainWindow { DataContext = live.Session, Title = "GitHub Runner Local — LIVE" };
                liveWindow.Closed += (_, _) => live.Dispose();
                MainWindow = liveWindow;
                liveWindow.Show();
            }
            catch (ArgumentException error)
            {
                MessageBox.Show(error.Message, "LIVE setup blocked", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(2);
            }
            return;
        }
        var selection = FakeScenarioParser.Parse(e.Args);
        var session = new WizardSession(
            selection,
            new FakeClock(),
            new PreviewDelay(TimeSpan.FromMilliseconds(80)),
            WizardAdapters.CreateFake(selection.Scenario));
        var window = new MainWindow { DataContext = session };
        MainWindow = window;
        window.Show();
    }
}
