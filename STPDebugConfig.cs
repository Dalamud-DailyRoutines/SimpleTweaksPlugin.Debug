using Dalamud.Configuration;
using STPDebug.Debugging;

namespace STPDebug;

public class STPDebugConfig : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public DebugConfig Debugging { get; set; } = new();

    public void Save() =>
        Service.PluginInterface.SavePluginConfig(this);
}
