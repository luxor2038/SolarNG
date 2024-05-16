using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Shell;
using SolarNG.Configs;
using SolarNG.Sessions;

namespace SolarNG.Utilities;

internal class JumpListManager
{
    private static void ClearJumpList()
    {
        JumpList jumpList = JumpList.GetJumpList(Application.Current);
        if (jumpList == null)
        {
            return;
        }
        jumpList.JumpItems.Clear();
        jumpList.Apply();
    }

    internal static void SetNewJumpList(ObservableCollection<Session> sessions)
    {
        ClearJumpList();
        JumpList jumpList = new JumpList();
        foreach (Session item2 in (from s in sessions
                where (s.SessionTypeIsNormal || s.Type == "app" || s.Type == "proxy") && (s.iFlags2 & ProgramConfig.FLAG_NOTINOVERVIEW) == 0
                orderby s.OpenCounter descending
                select s).Take(10))
        {
            string arguments = $"-i {item2.Id}";
            JumpTask item = new JumpTask
            {
                ApplicationPath = Assembly.GetExecutingAssembly().CodeBase,
                Arguments = arguments,
                IconResourcePath = Assembly.GetExecutingAssembly().Location,
                Title = item2.Name,
                Description = item2.Ip,
                CustomCategory = (string)Application.Current.Resources["Frequent"]
            };
            jumpList.JumpItems.Add(item);
        }
        jumpList.ShowFrequentCategory = false;
        jumpList.ShowRecentCategory = false;
        JumpList.SetJumpList(Application.Current, jumpList);
    }
}
