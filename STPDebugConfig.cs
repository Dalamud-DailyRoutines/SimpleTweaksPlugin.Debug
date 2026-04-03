using Dalamud.Configuration;
using STPDebug.Debugging;

namespace STPDebug;

public class STPDebugConfig : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool DisableAutoOpenDebug;
    public bool ShowInDevMenu;
    public bool NoCallerInLog;

    public DebugConfig Debugging { get; set; } = new();

    public void Init()
    {
    }

    public void Save()
    {
#if !TEST
        Service.PluginInterface.SavePluginConfig(this);
#endif
    }
}
