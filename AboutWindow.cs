using System.Reflection;
using System.Windows;

namespace SolarNG;

public partial class AboutWindow : Window
{
    public AboutWindow(MainWindow window)
    {
        InitializeComponent();
        base.Owner = window;
        Product.Content = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product + " " + Assembly.GetExecutingAssembly().GetName().Version;
        Copyright.Content = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
    }
}
