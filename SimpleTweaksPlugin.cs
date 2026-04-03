using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using SimpleTweaksPlugin.Debugging;
using SimpleTweaksPlugin.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
#if DEBUG
using System.IO;
#endif

namespace SimpleTweaksPlugin;

public sealed class SimpleTweaksPlugin : IDalamudPlugin {
    public string Name => "STPDebug";

    public static SimpleTweaksPlugin Plugin { get; private set; } = null!;

    public SimpleTweaksPluginConfig PluginConfig { get; }
    public DebugWindow DebugWindow { get; } = new();
    public WindowSystem WindowSystem { get; } = new("STPDebug");

    internal bool ShowErrorWindow { get; private set; }
    internal List<CaughtError> ErrorList { get; } = [];

    public SimpleTweaksPlugin(IDalamudPluginInterface pluginInterface) {
        Plugin = this;
        pluginInterface.Create<Service>();
        pluginInterface.Create<SimpleLog>();
        pluginInterface.Create<Common>();

        PluginConfig = Service.PluginInterface.GetPluginConfig() as SimpleTweaksPluginConfig ?? new SimpleTweaksPluginConfig();
        PluginConfig.Init(this);

#if DEBUG
        SimpleLog.SetupBuildPath();
#endif

        Task.Run(() => Service.Framework.RunOnFrameworkThread(Initialize));
    }

    public void Dispose() {
        SimpleLog.Debug("开始释放调试器插件。");
        PluginConfig.Save();

        Service.Framework.Update -= FrameworkOnUpdate;
        Service.PluginInterface.UiBuilder.Draw -= BuildUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfig;
        RemoveCommands();

        DebugManager.Dispose();

        foreach (var hook in Common.HookList.Where(hook => !hook.IsDisposed)) {
            if (hook.IsEnabled) {
                hook.Disable();
            }

            hook.Dispose();
        }

        Common.HookList.Clear();
        Common.Shutdown();
        TooltipManager.Destroy();
        SimpleEvent.Destroy();
        Service.Dispose();
    }

    private void Initialize() {
        UiHelper.Setup(Service.SigScanner);
        DebugManager.SetPlugin(this);
        Common.Setup();

        WindowSystem.AddWindow(DebugWindow);

        Service.PluginInterface.UiBuilder.Draw += BuildUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfig;

        SetupCommands();

        DebugWindow.IsOpen = !PluginConfig.DisableAutoOpenDebug;
        DebugManager.Reload();

        Service.Framework.Update += FrameworkOnUpdate;
    }

    private static void FrameworkOnUpdate(IFramework framework) => Common.InvokeFrameworkUpdate();

    private void SetupCommands() {
        Service.Commands.AddHandler("/stpd", new Dalamud.Game.Command.CommandInfo(OnCommand) {
            HelpMessage = "打开 Simple Tweaks 调试窗口。",
            ShowInHelp = true
        });
    }

    private static void RemoveCommands() {
        Service.Commands.RemoveHandler("/stpd");
    }

    private static void OnOpenConfig() => OpenDebugWindow();

    private static void OnCommand(string command, string arguments) => OpenDebugWindow();

    private static void OpenDebugWindow() => Plugin.DebugWindow.UnCollapseOrToggle();

    private void BuildUI() {
        foreach (var error in ErrorList.Where(error => error.IsNew)) {
            error.IsNew = false;
            ShowErrorWindow = true;
        }

        WindowSystem.Draw();

        if (ShowErrorWindow) {
            DrawErrorWindow();
        }

        if (Service.PluginInterface.IsDevMenuOpen && (Service.PluginInterface.IsDev || PluginConfig.ShowInDevMenu)) {
            if (ImGui.BeginMainMenuBar()) {
                if (ImGui.MenuItem("Simple Tweaks 调试器")) {
                    OpenDebugWindow();
                }

                ImGui.EndMainMenuBar();
            }
        }
    }

    private void DrawErrorWindow() {
        if (ErrorList.Count == 0) {
            ShowErrorWindow = false;
            return;
        }

        var isOpen = true;
        if (ImGui.Begin($"{Name}：错误", ref isOpen, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize)) {
            for (var index = 0; index < ErrorList.Count && index < 5; index++) {
                var error = ErrorList[index];
                ImGui.Text("调试器内部发生异常：");
                if (!string.IsNullOrWhiteSpace(error.Message)) {
                    ImGui.TextWrapped(error.Message);
                }

                ImGui.TextWrapped($"{error.Exception}");

                if (ImGui.Button($"关闭此项###closeError_{index}")) {
                    error.Closed = true;
                }

                if (error.Count > 1) {
                    ImGui.SameLine();
                    ImGui.Text($"已重复出现 {error.Count} 次。");
                }

                ImGui.Separator();
            }

            if (ErrorList.Count > 5) {
                ImGui.TextColored(ImGuiColors.DalamudRed, $"还有 {ErrorList.Count - 5} 条额外错误未展开。");
            }
        }

        ImGui.End();

        ErrorList.RemoveAll(error => error.Closed);
        if (!isOpen) {
            ErrorList.Clear();
            ShowErrorWindow = false;
        }
    }

#if DEBUG
    public void Error(Exception exception, string message = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = "") {
        if (string.IsNullOrWhiteSpace(message)) {
            SimpleLog.Error("调试器内部发生异常。", callerFilePath, callerMemberName, callerLineNumber);
        } else {
            SimpleLog.Error($"调试器内部发生异常：{message}", callerFilePath, callerMemberName, callerLineNumber);
        }

        SimpleLog.Error($"{exception}", callerFilePath, callerMemberName, callerLineNumber);
        AddError(exception, message, allowContinue: false);
    }
#else
    public void Error(Exception exception, string message = "") {
        if (string.IsNullOrWhiteSpace(message)) {
            SimpleLog.Error("调试器内部发生异常。");
        } else {
            SimpleLog.Error($"调试器内部发生异常：{message}");
        }

        SimpleLog.Error($"{exception}");
        AddError(exception, message, allowContinue: false);
    }
#endif

    private void AddError(Exception exception, string message, bool allowContinue) {
        var error = new CaughtError {
            Exception = exception,
            IsNew = !allowContinue,
            Message = message
        };

        var index = ErrorList.IndexOf(error);
        if (index >= 0) {
            ErrorList[index].Count++;
            ErrorList[index].IsNew |= error.IsNew;
            return;
        }

        ErrorList.Insert(0, error);
        if (ErrorList.Count > 50) {
            ErrorList.RemoveRange(50, ErrorList.Count - 50);
        }
    }

    internal sealed class CaughtError {
        public Exception Exception { get; init; } = null!;
        public bool IsNew { get; set; } = true;
        public bool Closed { get; set; }
        public string Message { get; init; } = string.Empty;
        public ulong Count { get; set; } = 1;

        public override bool Equals(object? obj) {
            return obj is CaughtError other
                   && other.Message == Message
                   && $"{other.Exception}" == $"{Exception}";
        }

        public override int GetHashCode() => HashCode.Combine(Message, $"{Exception}");
    }
}
