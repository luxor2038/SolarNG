using System.Threading.Tasks;

namespace GalaSoft.MvvmLight.Helpers;

public static class Empty
{
    private static readonly Task ConcreteTask = new Task(delegate
    {
    });

    public static Task Task => ConcreteTask;
}
