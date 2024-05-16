using System.Windows;

namespace SolarNG;

public partial class ConfirmationDialog : Window
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

    public ConfirmationDialog(MainWindow window)
    {
        InitializeComponent();
        base.Owner = window;
    }

    public ConfirmationDialog(MainWindow window, string windowtitle, string title, bool hideCancelButton = false)
    {
        Confirmed = false;
        InitializeComponent();
        base.Owner = window;
        base.Title = windowtitle;
        if (hideCancelButton)
        {
            BtnCancel.Visibility = Visibility.Collapsed;
            BtnOk.Content = Application.Current.Resources["OK"];
        }
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
}
