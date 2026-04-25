using System.Reflection;

namespace AudioCarousel;

public static class AppVersion
{
    public static string Display => Format(
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString());

    public static string Format(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "dev";
        int plus = raw.IndexOf('+');
        return plus > 0 ? raw[..plus] : raw;
    }
}
