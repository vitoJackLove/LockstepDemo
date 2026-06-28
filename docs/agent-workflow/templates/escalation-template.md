---
work_id: "{work_id}"
subtask_id: "{subtask_id}"
report_type: "escalation"
fix_attempts: 3
created_at: "{created_at}"
---

# {subtask_id} 升级处理文档

当前子任务已达到修复次数上限，仍未通过 Test 或 Review，需要用户或人工开发者介入。

## 语言要求

本文档正文必须使用中文。路径、状态值、Agent 名称、命令和代码标识可以保留原文。

## 状态文件

`docs/agent-workflow/runs/{work_id}/state.yaml`

## 子任务文档

`{subtask_doc_path}`

## 最后阻断类型

{test_failed_or_review_rejected_or_blocked}

## Build/Fix 链路

- `{build_or_fix_report_path}`

## Test 失败链路

- `{test_report_path_or_none}`

## Review 拒绝链路

- `{review_report_path_or_none}`

## 已排除或已尝试方向

- {attempted_direction}

## 需要人工判断的问题

- {manual_question}

## 建议下一步

- {next_step}
