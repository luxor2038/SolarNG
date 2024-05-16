using System.Reflection;

namespace GalaSoft.MvvmLight.Helpers;

internal static class FeatureDetection
{
    private class ReflectionDetectionClass
    {
        private void Method()
        {
        }
    }

    private static bool? _isPrivateReflectionSupported;

    public static bool IsPrivateReflectionSupported
    {
        get
        {
            if (!_isPrivateReflectionSupported.HasValue)
            {
                _isPrivateReflectionSupported = ResolveIsPrivateReflectionSupported();
            }
            return _isPrivateReflectionSupported.Value;
        }
    }

    private static bool ResolveIsPrivateReflectionSupported()
    {
        ReflectionDetectionClass obj = new ReflectionDetectionClass();
        try
        {
            typeof(ReflectionDetectionClass).GetTypeInfo().GetDeclaredMethod("Method").Invoke(obj, null);
        }
        catch
        {
            return false;
        }
        return true;
    }
}
