namespace Ost.Common;

internal static class Debug
{
    internal static void Log(object message)
    {
        UnityEngine.Debug.Log($"[OST] {message}");
    }
}