using System.Windows;
using Grl.App.Presentation;

namespace Grl.App;

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var selection = FakeScenarioParser.Parse(e.Args);
        var session = new WizardSession(
            selection,
            new FakeClock(),
            new PreviewDelay(TimeSpan.FromMilliseconds(80)),
            WizardAdapters.CreateFake());
        var window = new MainWindow { DataContext = session };
        MainWindow = window;
        window.Show();
    }
}
