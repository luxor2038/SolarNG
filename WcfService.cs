using System.Linq;

namespace SolarNG;

internal class WcfService : IWcfService
{
    public void PassArguments(string[] args)
    {
        ProcessCommandLineArguments(App.ParseParameters(args));
    }

    private void ProcessCommandLineArguments(Options opts)
    {
        App.mainWindows.First((MainWindow t) => !t.IsWindowClosed)?.ProcessCommandLineArguments(opts);
    }
}
