using System.Reflection;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using STPDebug.Debugging;
using STPDebug.Utility;

namespace STPDebug;

public class DebugWindow : SimpleWindow
{
    public DebugWindow() : base("STPDebug Debug Window")
    {
        WindowName = $"STPDebug [{Assembly.GetExecutingAssembly().GetName().Version}] - Client Structs#{Common.ClientStructsVersion}###stDebugMenu";

        Size          = ImGuiHelpers.ScaledVector2(500, 350);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void PreDraw()
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = ImGuiHelpers.ScaledVector2(350),
            MaximumSize = ImGuiHelpers.ScaledVector2(2000)
        };
    }

    public override void Draw()
    {
        base.Draw();
        DebugManager.DrawDebugWindow();
    }
}
