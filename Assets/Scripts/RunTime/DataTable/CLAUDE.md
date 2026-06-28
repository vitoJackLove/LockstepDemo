# DataTable

> 📍 位置: `Assets/Scripts/RunTime/DataTable/`
> 🔗 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 📊 完善度: 🟢 基础

## 模块职责

这一层只做数据表索引和访问辅助，不承载表结构本身。

## 目录结构

```text
DataTable/
└── DataTableHelper.cs
```

## 核心内容

- `DataTableHelper.DataTableNames` 保存当前项目约定的数据表名。
- 这些名称应与 `GameEntry.DataTable.GetDataTable<T>()` 的可用表保持一致。

## 注意事项

- 增删数据表时要同步更新这里的表名列表。
- 这个目录更像索引层，不要在这里塞业务逻辑。

---
<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-05-15
completeness: basic
-->
