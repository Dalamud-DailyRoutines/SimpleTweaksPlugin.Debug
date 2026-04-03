using Dalamud.Configuration;

namespace SimpleTweaksPlugin;

public partial class SimpleTweaksPluginConfig : IPluginConfiguration {
    public int Version { get; set; } = 1;

    public bool DisableAutoOpenDebug;
    public bool ShowInDevMenu;
    public bool NoCallerInLog;

    public Debugging.DebugConfig Debugging { get; set; } = new();

    public void Init(SimpleTweaksPlugin plugin) {
    }

    public void Save() {
#if !TEST
        Service.PluginInterface.SavePluginConfig(this);
#endif
    }
}
