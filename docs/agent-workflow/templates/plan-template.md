---
work_id: "{work_id}"
doc_type: "plan"
created_at: "{created_at}"
updated_at: "{updated_at}"
---

# {work_id} Plan

## 任务拆分

| 子任务 | 状态 | 目标 | 前置依赖 | 交付物 |
|--------|------|------|----------|--------|
| `{subtask_id}` | pending | {objective} | {dependency_or_none} | build/test/review reports |

## 推进顺序

1. {subtask_id}

## 并行限制

- 默认按顺序执行；除非子任务无共享修改范围和依赖关系，否则不要并行。

## 子任务 Definition of Done

- `{subtask_id}`：{definition_of_done_summary}

## 建议验证方式

- `{subtask_id}`：{verification_summary}

## 审查关注点

- `{subtask_id}`：{review_focus}
