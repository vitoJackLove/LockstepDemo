---
work_id: "{work_id}"
subtask_id: "{subtask_id}"
report_type: "test"
attempt: {attempt}
source_report: "{build_or_fix_report_path}"
passed: false
created_at: "{created_at}"
---

# {subtask_id} Test 报告 第 {attempt} 次

## 测试对象文档

`{build_or_fix_report_path}`

## 测试结论

passed: {true_or_false}

## 验收标准逐项结果

- [ ] {acceptance_criterion}: {result_and_evidence}

## Definition of Done 验证结果

- [ ] 验收标准证据：{result}
- [ ] 构建或测试：{result}
- [ ] 中文注释：{result}
- [ ] AgentTest 日志：{result}
- [ ] 剩余风险：{result}

## CodeGraph 查询记录

- {codegraph_symbol_file_or_call_relation}

## 执行的测试命令

| 命令 | 结果 | 摘要 |
|------|------|------|
| `{command}` | {passed_or_failed} | {summary} |

## Agent 游戏入口验证

- 是否使用：{agent_entry_used_yes_or_no}
- 服务端命令：`{server_command_or_none}`
- Unity 入口：{menu_or_batch_entry_or_none}
- 关键日志：{agent_entry_log_or_none}
- 结果：{agent_entry_result_or_none}

## AgentTest 日志

- 是否检查 `GameLogChannel.AgentTest`：{agent_test_channel_checked_yes_or_no}
- `GameLogChannel.AgentTest` 是否存在阻断 Error：{agent_test_error_yes_or_no}
- 读取到的关键日志：{agent_test_log_summary_or_none}
- 缺失或异常日志：{agent_test_log_gap_or_none}
- 其他频道 Error 背景记录：{other_channel_error_context_or_none}

## 中文注释检查结果

- 类：{class_comment_check}
- 方法：{method_comment_check}
- 属性：{property_comment_check}
- 结论：{comment_check_result}

## 通过项

- {passed_item}

## 失败项

- {failed_item}

## 复现步骤

1. {step}

## 期望结果

{expected}

## 实际结果

{actual}

## 建议 development-master 优先检查

- `{path_or_module}`

## 剩余风险

- {risk}
