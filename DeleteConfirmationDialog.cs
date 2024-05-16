using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SolarNG;

public partial class DeleteConfirmationDialog : Window
{
    public string TitleText
    {
        get
        {
            return TitleTextBox.Text;
        }
        set
        {
            TitleTextBox.Text = value;
        }
    }

    public bool Confirmed { get; set; }

    public DeleteConfirmationDialog(MainWindow window)
    {
        InitializeComponent();
        base.Owner = window;
    }

    public DeleteConfirmationDialog(MainWindow window, string title, bool hideCancelButton = false)
    {
        Confirmed = false;
        InitializeComponent();
        base.Owner = window;
        if (hideCancelButton)
        {
            BtnCancel.Visibility = Visibility.Collapsed;
            BtnOk.Content = Application.Current.Resources["OK"];
        }
        BtnOk.Focus();
        TitleText = title;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        Close();
    }

    private void On_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return)
        {
            e.Handled = true;
        }
    }

    private void On_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return)
        {
            if (sender is Button button && button.Name == "BtnCancel")
            {
                Confirmed = false;
            }
            else
            {
                Confirmed = true;
            }
            e.Handled = true;
            Close();
        }
    }
}
