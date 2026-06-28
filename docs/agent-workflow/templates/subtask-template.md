---
work_id: "{work_id}"
subtask_id: "{subtask_id}"
status: "pending"
attempt_budget: 3
created_at: "{created_at}"
updated_at: "{updated_at}"
---

# {subtask_id} {title}

## 原始需求摘要

{summary}

## 子任务目标

{objective}

## 非目标

- {non_goal}

## 开发范围

- {scope_item}

## 禁止修改范围

- {forbidden_item}

## 上下文入口

- 根文档：`CLAUDE.md`
- 模块文档：`{module_claude_path}`
- 推荐 CodeGraph 查询：`{codegraph_query}`

## 前置依赖

- {dependency_or_none}

## 验收标准

- [ ] {acceptance_criterion}

## Definition of Done

- [ ] 验收标准全部有验证证据。
- [ ] 关键构建或测试通过。
- [ ] 修改范围不超出本子任务。
- [ ] 新增或修改后形成的类、方法、属性具备中文注释。
- [ ] 涉及 Agent 判断的关键日志使用 `GameLogChannel.AgentTest`。
- [ ] 未留下无关调试代码、无关重构或未说明风险。

## 建议验证方式

- {verification_step}

## 审查关注点

- {review_focus}

## 交付物要求

- development-master 必须生成 build 或 fix 报告。
- test-engineer 必须生成 test 报告并返回 `PASSED: true|false`。
- code-reviewer 必须生成 review 报告并返回 `APPROVED: true|false`。

## 状态记录

| 时间 | 状态 | 来源 | 关联文档 |
|------|------|------|----------|
| {created_at} | pending | designer | - |
