---
work_id: "{work_id}"
subtask_id: "{subtask_id}"
report_type: "fix"
attempt: {attempt}
build_status: "done"
source_report: "{test_or_review_report_path}"
created_at: "{created_at}"
---

# {subtask_id} Fix 报告 第 {attempt} 次

## 失败来源报告

`{test_or_review_report_path}`

## 失败来源类型

{test_failed_or_review_rejected}

## 问题原因

{root_cause}

## 修复方案

{fix_solution}

## 使用的技能和工具

- CodeGraph：{codegraph_used}
- unity-developer：{unity_developer_used}
- 其他：{other_tools_or_none}

## CodeGraph 查询记录

- {codegraph_symbol_file_or_call_relation}

## 修改的代码脚本

- `{script_path}`

## 修改的资源或配置

- `{asset_or_config_path}`

## 中文注释覆盖情况

- 类：{class_comment_coverage}
- 方法：{method_comment_coverage}
- 属性：{property_comment_coverage}
- 未覆盖说明：{comment_gap_or_none}

## 功能影响范围

- {impact_scope}

## 自检命令和结果

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

- 是否新增或依赖 `GameLogChannel.AgentTest` 日志：{agent_test_log_used_yes_or_no}
- 日志前缀：{agent_test_log_prefix_or_none}
- 关键日志摘要：{agent_test_log_summary_or_none}

## Definition of Done 自查

- 验收标准覆盖：{acceptance_coverage}
- 修改范围控制：{scope_control}
- 注释要求：{comment_requirement}
- 日志要求：{logging_requirement}
- 未覆盖项：{dod_gap_or_none}

## 回归测试重点

- {regression_focus}

## 回滚或恢复说明

{rollback_or_restore_note}
