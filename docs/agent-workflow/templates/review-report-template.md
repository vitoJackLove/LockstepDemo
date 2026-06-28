---
work_id: "{work_id}"
subtask_id: "{subtask_id}"
report_type: "review"
attempt: {attempt}
source_report: "{build_or_fix_report_path}"
test_report: "{test_report_path}"
approved: false
created_at: "{created_at}"
---

# {subtask_id} Review 报告 第 {attempt} 次

## 审查对象

- Build/Fix 报告：`{build_or_fix_report_path}`
- Test 报告：`{test_report_path}`

## 审查结论

approved: {true_or_false}

## CodeGraph 查询记录

- {codegraph_symbol_file_or_call_relation}

## 修改范围检查

- {scope_check}

## 质量门禁检查

- 正确性：{correctness_check}
- 可维护性：{maintainability_check}
- 重复和复杂度：{duplication_complexity_check}
- 注释质量：{comment_quality_check}

## 项目规范检查

- 固定点和确定性：{determinism_check}
- 帧同步和回滚：{rollback_check}
- Runtime/Editor 分离：{runtime_editor_check}
- 资源和 `.meta`：{asset_meta_check}
- 协议兼容：{protocol_check}

## 测试证据充分性

- {test_evidence_check}

## 阻断问题

| 严重级别 | 位置 | 问题 | 修复建议 |
|----------|------|------|----------|
| {severity} | `{path_or_symbol}` | {issue} | {recommendation} |

## 非阻断建议

- {non_blocking_suggestion}

## 建议 development-master 修复顺序

1. {fix_priority}

## 剩余风险

- {risk}
