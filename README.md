# Simple Tweaks Debug

这是一个仅保留调试器能力的 `Simple Tweaks` 精简分支。

当前分支的目标是：

- 只保留插件启动、调试窗口和调试页面。
- 删除所有面向玩家的 tweak 功能、配置界面、统计与本地化链路。
- 尽量保留仓库顶层结构，方便后续同步上游调试器相关改动。

构建时请始终使用解决方案：

```powershell
dotnet build .\SimpleTweaksPlugin.sln -c Debug -p:Platform=x64
```
