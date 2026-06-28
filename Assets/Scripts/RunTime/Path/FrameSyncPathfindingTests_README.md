# 幀同步尋路測試說明

## 單機確定性（已自動化）

- 位置：`Assets/Scripts/Test/FrameSyncPathfindingTests.cs`
- 在 Unity 中打開 **Window > General > Test Runner**，切換到 **EditMode**，執行 `FrameSyncPathfindingTests`。
- 驗證內容：
  - 相同起終點多次尋路結果完全一致（`TryFindPath_SameInput_Twice_ReturnsIdenticalPath_Determinism`）。
  - 起終點相同、相鄰格子、空路徑等邊界情況。
  - 回滾後重算路徑：用「模擬位置 + 目標」再次尋路，結果有效且終點一致（`Rollback_ReFindPath_AfterSamePositionAndTarget_ConsistentWithAuthority`）。

## 雙機同步（手動驗證）

- 兩端使用**相同輸入序列**與**相同地圖/網格**。
- 驗證：同一 tick 怪物位置、路徑節點索引、目標位置一致；`VerifyForecast` 不應觸發回滾（無丟包時）。
- 建議：錄製一局輸入，在兩台機器或兩個 Build 上回放，比對日誌中權威/本地實體位置與尋路相關快照字串。

## 高回滾場景（手動驗證）

- 在 `BaseWorld` 或測試中提高 `_lossPacket` / 模擬延遲，使回滾頻繁觸發。
- 驗證：怪物不應在節點間抖動或卡住；回滾後 `PathFindingComponent` 以還原的位置與目標重新尋路，行為與權威一致。
- 建議：觀察回滾前後 `PathFindingComponent` 的 `_currentWaypointIndex` 與實體位置是否與權威快照一致。
