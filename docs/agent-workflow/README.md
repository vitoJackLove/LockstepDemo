# Agent Workflow

本目录保存项目级多 Agent 阶段门禁工作流产生的规格、计划、状态、子任务、开发、测试、审查、修复、升级和最终归档文档。

## 新工作流结构

新工作流只写入 `runs/{work_id}/`：

```text
docs/agent-workflow/
  runs/
    {work_id}/
      state.yaml
      spec.md
      plan.md
      final-report.md
      tasks/
        001-{short-name}.md
      artifacts/
        001-{short-name}/
          build-attempt-1.md
          test-attempt-1.md
          review-attempt-1.md
          fix-attempt-1.md
          escalation.md
  templates/
```

旧目录 `tasks/` 和 `reports/` 是历史产物目录，只保留查阅。新工作流不要继续写入旧目录。

## 生命周期

```text
Spec -> Plan -> Build -> Test -> Review -> Final Gate
```

阶段门禁：

- 没有 `spec.md` 和 `plan.md` 不进入 Build。
- 没有验收标准和 Definition of Done 的子任务不进入 Build。
- 测试不通过不进入 Review。
- Review 不通过不进入 Final Gate。
- Review 拒绝后必须先 Fix，再 Test，再 Review。
- `state.yaml` 是唯一状态源。

## 角色

- workflow-controller：协议控制器和最终门禁；维护 `state.yaml`，不开发、不测试、不审查代码。
- designer：生成 `spec.md`、`plan.md`、`state.yaml` 和子任务文档。
- development-master：执行 Build/Fix，输出 build/fix 报告。
- test-engineer：执行 Test，输出 test 报告和 `PASSED`。
- code-reviewer：执行 Review，输出 review 报告和 `APPROVED`。

## 工作流入口

完整工作流定义：

```text
.codex/workflows/multi-agent-development-workflow.md
```

Agent 定义：

```text
.codex/agents/
```

模板：

```text
docs/agent-workflow/templates/
```

## 常用启动提示

标准启动：

```text
使用 .codex/agents/master-coordinator.md 中的 workflow-controller 工作流处理这个需求。
需求：{完整需求}
```

指定工作单：

```text
使用多 Agent 阶段门禁工作流处理这个任务。
work_id: {YYYYMMDD-short-name}
任务：{完整需求}
```

只生成 Spec 和 Plan：

```text
只使用 .codex/agents/designer.md 为这个复杂任务生成 Spec 和 Plan，不启动开发、测试或审查。
任务：{完整需求}
```

继续已有工作单：

```text
使用 .codex/agents/master-coordinator.md 继续这个工作单。
状态文件：docs/agent-workflow/runs/{work_id}/state.yaml
从 state.yaml 当前 phase 继续推进。
```
