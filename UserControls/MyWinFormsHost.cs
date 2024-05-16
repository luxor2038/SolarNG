using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace SolarNG.UserControls;

public class MyWinFormsHost : WindowsFormsHost
{
    public static readonly DependencyProperty ChildProperty = DependencyProperty.Register("Child", typeof(Control), typeof(MyWinFormsHost), new PropertyMetadata(PropertyChanged));

    public new Control Child
    {
        get
        {
            if (base.Dispatcher.CheckAccess())
            {
                return (Control)GetValue(ChildProperty);
            }
            return base.Dispatcher.Invoke(() => ReturnChild());
        }
        set
        {
            SetValue(ChildProperty, value);
        }
    }

    private static void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WindowsFormsHost windowsFormsHost)
        {
            windowsFormsHost.Child = e.NewValue as Panel;
        }
    }

    private Control ReturnChild()
    {
        return Child;
    }
}
