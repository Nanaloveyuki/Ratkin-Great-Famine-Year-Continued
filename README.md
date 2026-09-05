# 鼠灾-大荒年 (Continued)

RimWorld 1.6 的“鼠灾-大荒年”延续维护版本。

## 当前改动

- 将大型流民冲击、大灾荒、鼠疫大灾荒和流民商队的大批 Pawn 生成分散到多个游戏 tick。
- 每个渲染帧最多生成一个 Pawn，降低事件触发时的单帧耗时峰值。
- 生成队列写入存档，完成后再统一建立队伍关系、Lord 与事件信件。
- 与原版包冲突时仅加载独立提示组件，阻止 Continued 的主体程序集、Def 与 Patch 重复加载。

原项目归属及维护授权状态见 [NOTICE](NOTICE)。

## 构建与部署

退出 RimWorld 后运行 `powershell -ExecutionPolicy Bypass -File .\scripts\build-and-deploy.ps1`，脚本会构建主体与冲突保护 DLL、同步运行所需文件并逐文件校验 SHA-256。
