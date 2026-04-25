using System.Text.Json.Serialization;

namespace AudioCarousel.Config;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(ConfigSchema))]
internal partial class ConfigJsonContext : JsonSerializerContext
{
}
