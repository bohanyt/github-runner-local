using System.Windows;
using Grl.App.Presentation;
using Grl.Core;

namespace Grl.App;

public partial class MainWindow : Window
{
    private readonly System.Windows.Threading.DispatcherTimer statusTimer = new()
        { Interval = TimeSpan.FromSeconds(5) };

    public MainWindow()
    {
        InitializeComponent();
        statusTimer.Tick += async (_, _) =>
        {
            if (DataContext is WizardSession session) await session.RefreshRunnerStatusAsync();
        };
        Closed += (_, _) => statusTimer.Stop();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        AcknowledgeBox.Focus();
        if (DataContext is WizardSession { IsLive: true }) statusTimer.Start();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is WizardSession { IsLive: true } session &&
            (session.HasOwnedRunnerProcess || session.ActiveError is null &&
                session.State is WizardState.InstallingDownloading or WizardState.InstallingVerifying or
                    WizardState.InstallingExtracting or WizardState.InstallingConfiguring))
        {
            e.Cancel = true;
            MessageBox.Show("Drain, stop now, or unregister the portable runner before closing LIVE setup.",
                "LIVE runner active", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
