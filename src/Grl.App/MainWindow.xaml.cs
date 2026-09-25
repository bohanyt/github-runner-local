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
        if (DataContext is WizardSession { IsLive: true } session && session.HasOwnedRunnerProcess)
        {
            e.Cancel = true;
            MessageBox.Show("A product-owned runner process is active. Safe Pause is unavailable. Explicit Stop Now may cancel an active or newly assigned job. Closing or crashing the app does not guarantee that the runner stops.",
                "LIVE runner active", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else if (DataContext is WizardSession { IsLive: true } inProgress &&
            inProgress.ActiveError is null &&
            inProgress.State is WizardState.InstallingDownloading or WizardState.InstallingVerifying or
                WizardState.InstallingExtracting or WizardState.InstallingConfiguring)
        {
            e.Cancel = true;
            MessageBox.Show("LIVE setup has not reached a safe closing point. Registration state may still be unresolved. Use the in-app recovery path when available, or leave the runner root untouched for inspection.",
                "LIVE setup incomplete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else if (DataContext is WizardSession { IsLive: true, HasPendingRecovery: true, State: not WizardState.DisconnectDone })
        {
            MessageBox.Show("Registration or removal may remain pending. Closing this window does not clean it up; reopen the same root for exact-identity recovery. A process from an earlier app session may still be running.",
                "LIVE recovery pending", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
