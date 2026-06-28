# Subtask 002: Apply Cheats To Predicted And Local Entities

work_id: cheat-editor-refactor-20260624-000000
subtask_id: 002-apply-cheats-to-predicted-and-local-entities
status: done

## Original Request Summary

给实体增加 Buff 和修改属性功能要作用到预测和本地实体上，避免触发预测错误。

## Goal

重构 Buff 添加和属性修改逻辑，使编辑器金手指操作对选中的预测本地实体及其对应实体成对生效，避免只改一侧导致预测/回滚校验错误。

## Development Scope

- 基于子任务 001 的当前选中预测本地实体上下文，解析需要同步操作的实体集合。
- 明确并实现 Buff 添加的双侧/多侧应用规则：对预测实体和对应本地/权威实体使用一致的 Buff config id 和现有 `BuffComponent.CreateBuff(int buffConfigId)` 行为。
- 明确并实现属性修改的双侧/多侧应用规则：对预测实体和对应本地/权威实体使用一致的 property key、value type、set/delta/min/max 操作语义。
- 保持现有属性 API 语义：`TrySetProperty`、`TryChangePropertyValue`、`TryGetPropertyValue`、`GetPropertyDebugInfos()`，不改核心属性计算。
- 对无法解析对应实体、部分实体缺少 Buff 组件、部分实体缺少属性键、实体死亡或失效等场景给出明确消息。
- 操作后刷新选中实体信息和属性快照，提示实际影响的实体数量或实体标识。
- 保留现有失败路径的无异常行为和编辑器可见操作结果。

## Forbidden Scope

- 不修改 Buff 运行时规则、属性夹取规则、回滚系统或预测系统正式逻辑。
- 不新增进入正式包的 cheat-only runtime API。
- 不修改 Buff 配置、实体配置、服务器协议、生成代码、Prefab 或场景。
- 不用不同路径绕过现有 Buff/属性组件行为。

## Acceptance Criteria

- 在 Play Mode 中对预测本地实体添加 Buff 时，预测实体和对应本地/权威实体状态保持一致，不因只改单侧触发明显预测错误。
- 在 Play Mode 中设置属性当前值、增量修改、设置到 min/max 时，预测实体和对应实体使用相同语义更新。
- 若对应实体无法解析，工具阻止危险操作或明确降级，并显示不会造成误解的操作消息。
- 若某一侧操作失败，消息能说明失败侧和原因，且不会静默造成不一致。
- 无选中实体、非 Play Mode、无世界、非法 Buff config id、非法属性值等失败场景均无异常。
- 子任务 001 的简化选择流程不回退为复杂筛选流程。

## Suggested Verification

- 在 Play Mode 中对预测本地实体添加一个已知有效 Buff，观察预测实体和对应实体的 Buff/属性效果一致。
- 对 `Hp`、`Attack`、`Speed` 或当前实体实际拥有的属性执行 set、delta、min、max 操作，确认两侧快照同步刷新。
- 使用非法 Buff config id、不可用属性、实体失效状态验证失败消息。
- 观察 Unity Console，确认没有新增预测错误、空引用或未捕获异常。
- 可行时执行 Unity 脚本编译或 `dotnet build Roguelike_Master.sln -nologo`。

## Dependencies

- docs/agent-workflow/tasks/cheat-editor-refactor-20260624-000000/001-select-local-predicted-entity.md

## Deliverables

- 开发总结，说明实体对应关系解析策略和 Buff/属性双侧应用规则。
- 受影响文件清单。
- 验证记录，覆盖成功、部分失败、非法输入和实体失效场景。
- 若发现现有 runtime API 无法安全表达某类双侧操作，提供疑难说明并升级。

## Status Transition Log

- 2026-06-24: Created as pending.
- 2026-06-24: Marked ready after completion of docs/agent-workflow/tasks/cheat-editor-refactor-20260624-000000/001-select-local-predicted-entity.md.
- 2026-06-24: Marked done after main agent completion. Development report: docs/agent-workflow/reports/development/cheat-editor-refactor-20260624-000000/002-development.md. Test report: docs/agent-workflow/reports/tests/cheat-editor-refactor-20260624-000000/002-test-attempt-1.md. PASSED: true.
