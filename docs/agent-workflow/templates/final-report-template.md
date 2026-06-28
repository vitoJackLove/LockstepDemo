---
work_id: "{work_id}"
doc_type: "final_report"
phase: "done"
created_at: "{created_at}"
---

# {work_id} Final Report

## 工作单结论

{summary}

## 状态文件

`docs/agent-workflow/runs/{work_id}/state.yaml`

## Spec 与 Plan

- Spec：`docs/agent-workflow/runs/{work_id}/spec.md`
- Plan：`docs/agent-workflow/runs/{work_id}/plan.md`

## 已完成子任务

| 子任务 | Build/Fix | Test | Review | 结论 |
|--------|-----------|------|--------|------|
| `{subtask_id}` | `{build_or_fix_report}` | `{test_report}` | `{review_report}` | done |

## 最终门禁结果

- 所有子任务测试通过：{yes_or_no}
- 所有子任务审查通过：{yes_or_no}
- 未处理阻断问题：{none_or_list}
- 剩余风险：{risk_summary}

## 后续建议

- {follow_up_or_none}
