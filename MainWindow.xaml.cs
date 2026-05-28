using System.Windows;
using System.Windows.Input;

namespace Organizer;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OrganizerTab_Checked(object sender, RoutedEventArgs e)
    {
        if (OrganizerPanel is null) return;
        OrganizerPanel.Visibility = Visibility.Visible;
        CleanerPanel.Visibility   = Visibility.Collapsed;
        GamingPanel.Visibility    = Visibility.Collapsed;
        OrganizerLogScroll.Visibility = Visibility.Visible;
        CleanerLogScroll.Visibility   = Visibility.Collapsed;
        GamingLogScroll.Visibility    = Visibility.Collapsed;
        TitleLabel.Text = Loc.UI.TitleOrganizer;
    }

    private void CleanerTab_Checked(object sender, RoutedEventArgs e)
    {
        if (OrganizerPanel is null) return;
        OrganizerPanel.Visibility = Visibility.Collapsed;
        CleanerPanel.Visibility   = Visibility.Visible;
        GamingPanel.Visibility    = Visibility.Collapsed;
        OrganizerLogScroll.Visibility = Visibility.Collapsed;
        CleanerLogScroll.Visibility   = Visibility.Visible;
        GamingLogScroll.Visibility    = Visibility.Collapsed;
        TitleLabel.Text = Loc.UI.TitleCleaner;
    }

    private void GamingTab_Checked(object sender, RoutedEventArgs e)
    {
        if (OrganizerPanel is null) return;
        OrganizerPanel.Visibility = Visibility.Collapsed;
        CleanerPanel.Visibility   = Visibility.Collapsed;
        GamingPanel.Visibility    = Visibility.Visible;
        OrganizerLogScroll.Visibility = Visibility.Collapsed;
        CleanerLogScroll.Visibility   = Visibility.Collapsed;
        GamingLogScroll.Visibility    = Visibility.Visible;
        TitleLabel.Text = Loc.UI.TitleGaming;
    }

    private void ClearLogBtn_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.MainViewModel vm) return;

        if (OrganizerPanel.Visibility == Visibility.Visible)
            vm.ClearOrganizerLogCommand.Execute(null);
        else if (CleanerPanel.Visibility == Visibility.Visible)
            vm.ClearCleanerLogCommand.Execute(null);
        else
            vm.ClearGamingLogCommand.Execute(null);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) ToggleMaximize();
        else DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e)
        => ToggleMaximize();

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();

    private void ToggleMaximize()
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
}
