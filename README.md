# 鼠灾-大荒年 (Continued)

RimWorld 1.6 的“鼠灾-大荒年”延续维护版本。

## 当前改动

- 将大型流民冲击、大灾荒、鼠疫大灾荒和流民商队的大批 Pawn 生成分散到多个游戏 tick。
- 每个渲染帧最多生成一个 Pawn，降低事件触发时的单帧耗时峰值。
- 生成队列写入存档，完成后再统一建立队伍关系、Lord 与事件信件。
- 与原版包冲突时仅加载独立提示组件，阻止 Continued 的主体程序集、Def 与 Patch 重复加载。
- Mod 设置中提供可即时生效的新内容总开关；关闭后不再触发新事件或生成新 Pawn，已有 Pawn 及其工作逻辑继续运行。

原项目归属及维护授权状态见 [NOTICE](NOTICE)。

## 维护入口

- `1.6/Source/Utilities/`：按职责拆分的工具实现及共享函数；保留原类名与存档字段。
- `1.6/Source/Incidents/`：按接济、情报、鼠疫、围攻等事件族组织的扩展事件。
- `MouseDisasterIncidentCatalog.cs`：事件开关分类、原作分组和广播资格的统一目录。
- `Utilities/MouseDisasterPlagueUtility.cs`：独立的鼠疫感染与传播逻辑；原 Phase2 公共入口仅保留转发。
- 玩家文本统一从 `Languages/ChineseSimplified/` 维护；目录与回退规则见 [文案维护](docs/localization.md)。

只验证、不部署：使用 PowerShell 7 运行 `scripts/verify-narrative.ps1`、`scripts/verify-pawn-components.ps1` 和 `scripts/verify-refactor.ps1`。静态检查不能替代游戏内存档、寻路和界面验证。

## 构建与部署

当前存档停用、卸载用副本导出及验证边界见 [存档停用与卸载](docs/recovery.md)。

逐事件态度设置、寻食缓存与性能验证边界见 [事件行为与性能](docs/event-behavior-performance.md)。

退出 RimWorld 后运行 `powershell -ExecutionPolicy Bypass -File .\scripts\build-and-deploy.ps1`，脚本会构建主体与冲突保护 DLL、同步运行所需文件并逐文件校验 SHA-256。
