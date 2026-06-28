# CheatEditorWindow 恢复说明

> **状态：已于 2026-06-27 从 Cursor retrieval checkpoint 恢复。**

源文件路径：`Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`

菜单入口：`Tool/金手指工具`

## 若入口仍不可见

1. 回到 Unity Editor，等待脚本编译完成（右下角无转圈）
2. 若仍无菜单，菜单栏点击 **Assets → Refresh** 或按 `Ctrl+R`
3. 确认 Console 无编译错误

## 单机 Buff 补丁

已集成 `EntitySystem.TryResolveCheatOperationTargets`，单机模式不再要求 Authority 实体配对。
