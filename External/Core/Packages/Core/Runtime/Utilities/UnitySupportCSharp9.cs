// IsExternalInit needs to be manually declared for init-only properties to work in Unity.
// It is normally only available in .NET 5.0.
namespace System.Runtime.CompilerServices
{
    public static class IsExternalInit { }
}
