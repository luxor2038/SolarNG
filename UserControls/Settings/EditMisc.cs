using System.Windows;
using System.Windows.Controls;
using SolarNG.ViewModel.Settings;

namespace SolarNG.UserControls.Settings;

public partial class EditMisc : UserControl
{
    public EditMisc()
    {
        InitializeComponent();
        base.DataContext = new EditMiscViewModel();
    }

    private void OnCheckBoxChecked(object sender, RoutedEventArgs e)
    {
        App.hotKeys.HotKeysDisabled = true;
        App.Config.GUI.Hotkey = false;
    }

    private void OnCheckBoxUnchecked(object sender, RoutedEventArgs e)
    {
        App.hotKeys.HotKeysDisabled = false;
        App.Config.GUI.Hotkey = true;
    }
}
