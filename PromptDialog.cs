using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SolarNG;

public partial class PromptDialog : Window
{
    private bool Password;

    public bool Canceled { get; set; }

    public PromptDialog(MainWindow window)
    {
        InitializeComponent();
        base.Owner = window;
    }

    public PromptDialog(MainWindow window, string windowtitle, string title, string input, bool password = false)
    {
        InitializeComponent();
        base.Owner = window;
        base.Title = windowtitle;
        TitleTextBox.Text = title;
        Password = password;
        if (password)
        {
            InputTextBox.Visibility = Visibility.Collapsed;
            MyPassword.Visibility = Visibility.Visible;
            MyPassword.Password = input;
        }
        else
        {
            InputTextBox.Text = input;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Canceled = true;
        base.DialogResult = false;
        Close();
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        Canceled = false;
        base.DialogResult = true;
        Close();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (Password)
        {
            FocusManager.SetFocusedElement(this, MyPassword);
            MyPassword.SelectAll();
        }
        else
        {
            FocusManager.SetFocusedElement(this, InputTextBox);
            InputTextBox.SelectAll();
        }
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
                Canceled = true;
                base.DialogResult = false;
            }
            else
            {
                Canceled = false;
                base.DialogResult = true;
            }
            e.Handled = true;
            Close();
        }
    }
}
