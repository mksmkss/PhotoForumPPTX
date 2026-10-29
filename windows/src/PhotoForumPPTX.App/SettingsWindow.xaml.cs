using System.Windows;
using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.App;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
