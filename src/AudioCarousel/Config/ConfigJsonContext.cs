using System.Text.Json.Serialization;

namespace AudioCarousel.Config;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(ConfigSchema))]
public partial class ConfigJsonContext : JsonSerializerContext
{
}
