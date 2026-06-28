# AstarPathfindingProject 路徑確定性審查

本文梳理 AstarPathfindingProject 中**會影響路徑確定性**的 API 使用點，並決定在**幀同步邏輯**中允許／禁止的部分。  
目標：同一幀序列與地圖數據在不同機器上產生完全一致的尋路結果。

---

## 一、非確定性來源總覽

| 類別 | 說明 | 在幀同步邏輯中 |
|------|------|----------------|
| **Time / 時間** | `Time.deltaTime`, `Time.time`, `Time.realtimeSinceStartup`, `Time.frameCount` | ❌ 禁止 |
| **Random** | `Random.value`, `Random.Range` | ❌ 禁止（除非改為注入種子） |
| **DateTime** | `DateTime.UtcNow` | ❌ 禁止 |
| **Physics** | `Physics.Raycast`, `CheckSphere`, `SphereCast` 等 | ⚠️ 見下文 |
| **Transform** | 即時讀取場景中 Transform 位置 | ⚠️ 僅在圖構建／可視化時可接受 |
| **MonoBehaviour / 回調** | `Update`、協程、異步回調 | ❌ 邏輯層不依賴 |

---

## 二、按檔案／模組的 API 使用點

### 2.1 核心：AstarPath.cs

| 位置 | API | 用途 | 是否影響路徑結果 | 幀同步邏輯建議 |
|------|-----|------|------------------|----------------|
| 約 1106 | `Time.realtimeSinceStartup` | 圖更新批次計時 `lastGraphUpdate` | 否（僅排程） | **禁止**：圖更新不應在邏輯幀內用真實時間排程 |
| 約 1121 | `WaitForSeconds` + `Time.realtimeSinceStartup` | 延遲圖更新協程 | 否 | **禁止**：同上 |
| 約 1199 | `Time.realtimeSinceStartup` | 是否批次圖更新 | 否 | **禁止** |
| 約 1849, 1859 | `Time.frameCount` | 非 Pro 版禁止異步掃描（拋錯） | 否 | **禁止**：掃描應在邏輯外或單幀同步完成 |
| 類繼承 | `VersionedMonoBehaviour` | 編輯器／生命週期 | 否 | 幀同步不直接依賴 AstarPath 的 MonoBehaviour 生命週期 |

**結論**：  
- 幀同步邏輯中**不要**透過 `AstarPath` 的 `UpdateGraphs(ob, delay)` 或依賴 `Time`/協程的圖更新。  
- 圖應在**世界初始化時**一次性構建並鎖定，或圖更新由**與 tick 綁定**的可預測事件驅動。

---

### 2.2 路徑計算：Pathfinders/ABPath.cs

| 位置 | API | 用途 | 是否影響路徑結果 | 幀同步邏輯建議 |
|------|-----|------|------------------|----------------|
| 約 673, 747 | `System.DateTime.UtcNow.Ticks`（**已註解**） | 分幀尋路時限 | 若啟用會影響「何時中斷」 | **保持註解**；若將來做分幀尋路，應改為傳入**邏輯 tick** 而非 `DateTime` |
| 其餘 | 純圖遍歷、堆、啟發式 | 無 Time/Random/Physics | 不影響 | **允許**：核心 A* 搜尋可作為純函數使用 |

**結論**：  
- 當前 `CalculateStep` 內時間檢查已註解，**單次同步計算整條路徑**本身是確定性的。  
- 若未來啟用「按時間片計算」，必須用**幀同步提供的 tick** 取代 `DateTime.UtcNow.Ticks`。

---

### 2.3 路徑 ID：AstarPath.cs / Path.cs

| 位置 | API | 用途 | 是否影響路徑結果 | 幀同步邏輯建議 |
|------|-----|------|------------------|----------------|
| AstarPath 約 741 | `GetNextPathID()`，內部 `nextFreePathID++` | 路徑唯一 ID | 否（不影響節點選擇） | **允許**：只要請求順序在兩端一致，ID 序列一致即確定性 |

---

### 2.4 AI 移動與重力：Core/AI/AIBase.cs

| 位置 | API | 用途 | 是否影響路徑結果 | 幀同步邏輯建議 |
|------|-----|------|------------------|----------------|
| 約 331（已註解） | `Time.deltaTime` | 舊版 MovementUpdate | 影響位置積分 | **禁止**（已由 LogicDeltaTime 取代） |
| 約 346 | `fpmath1.LogicDeltaTime` | 定點 deltaTime | 由外部注入，可確定 | **允許**（需保證由幀同步提供） |
| 約 560 | `RaycastPosition` → `Physics.Raycast` | 重力時貼地 | 影響**移動位置**，不影響路徑節點 | **禁止在邏輯層**：不同機器物理狀態可能不一致 |
| 約 581 | `Time.frameCount` | UpdateVelocity 前後幀比對 | 僅速度顯示／除錯 | **禁止**：邏輯層不應依賴 frameCount |

**結論**：  
- 幀同步邏輯中**不要**使用依賴 `Physics.Raycast` 的 `usingGravity`/`RaycastPosition`。  
- 移動應基於**路徑點 + 定點運算**，不依賴實時物理查詢。

---

### 2.5 自動重算路徑策略：Core/Misc/AutoRepathPolicy.cs

| 位置 | API | 用途 | 是否影響路徑結果 | 幀同步邏輯建議 |
|------|-----|------|------------------|----------------|
| 約 75, 102 | `Time.time` | `lastRepathTime`、是否該重算路徑 | **是**：決定何時請求新路徑 | **禁止**：必須改為基於 **邏輯 tick** 或「每 N 邏輯幀」 |

**結論**：  
- 幀同步中「何時請求路徑」須由**邏輯 tick / 狀態機**決定，不可用 `Time.time`。

---

### 2.6 圖掃描與碰撞：Generators/Base.cs (GraphCollision)

| 位置 | API | 用途 | 是否影響路徑結果 | 幀同步邏輯建議 |
|------|-----|------|------------------|----------------|
| 約 640–656 | `Physics.CheckCapsule`, `CheckSphere`, `Raycast` | 節點是否可走、碰撞檢查 | **是**：決定節點 walkable | **僅在構圖時**：若圖在**世界創建時**用相同場景與層級一次掃描，結果可重現 |
| 約 681–717 | `Physics.Raycast` / `SphereCast` | 高度檢查 `CheckHeight` | **是**：節點高度與 walkable | 同上 |

**結論**：  
- **禁止**在幀同步邏輯幀內**動態**呼叫圖掃描（依賴當前 Physics 狀態）。  
- **允許**：在**離線或世界初始化時**用同一套參數與場景掃描一次，將圖序列化或鎖定後，在運行時只做查詢與 A* 搜尋。

---

### 2.7 圖節點上的隨機：影響 GetNearest / 路徑結果

| 檔案 | 位置 | API | 用途 | 是否影響路徑結果 | 幀同步邏輯建議 |
|------|------|-----|------|------------------|----------------|
| GridNodeBase.cs | 約 139 | `Random.value` | `RandomPointOnSurface()` 網格節點表面隨機點 | 僅當呼叫者用於尋路起終點時 | **禁止**：若幀同步需要「節點上取點」應使用**確定性映射**（如格子中心） |
| TriangleMeshNode.cs | 約 505–506 | `Random.value` | `RandomPointOnSurface()` 三角形內均勻取點 | 同上 | **禁止**：同上，或改為傳入種子並用確定性 RNG |

**結論**：  
- `GetNearest` 若只返回**節點**（不調用 `RandomPointOnSurface`），則不直接受影響。  
- 任何在幀同步邏輯中使用的「在節點上取點」邏輯，不得使用 `Random.value`，應改為確定性演算法或注入種子。

---

### 2.8 工具與輔助：PathUtilities.cs

| 位置 | API | 用途 | 是否影響路徑結果 | 幀同步邏輯建議 |
|------|-----|------|------------------|----------------|
| 約 564, 577, 609 | `Random.value`, `Random.Range`, `RandomPointOnSurface` | `GetPointsOnNodes` 在圖上取多個隨機點 | 若用於生成目標／起點則**是** | **禁止**：幀同步中不應使用此工具取得隨機點；目標應由邏輯／輸入給定 |

---

### 2.9 修飾器與射線：Modifiers

| 檔案 | API | 用途 | 是否影響路徑結果 | 幀同步邏輯建議 |
|------|-----|------|------------------|----------------|
| RaycastModifier.cs | `Physics.Linecast`, `CheckSphere`, `SphereCast`, `Physics2D.CircleCast` | 路徑簡化時用物理射線 | **是**：會改變路徑形狀 | **禁止**：物理查詢非確定性；可改用**圖射線**（useGraphRaycasting）並確保圖數據確定 |
| StartEndModifier.cs | `Physics.Linecast` | 起終點調整 | **是** | **禁止**：同上 |

---

### 2.10 NodeLink2 / NodeLink3（Transform 位置）

| 位置 | API | 用途 | 是否影響路徑結果 | 幀同步邏輯建議 |
|------|-----|------|------------------|----------------|
| NodeLink2.cs, NodeLink3.cs | `StartTransform.position`, `EndTransform.position` | 連結端點位置與 cost | **是**：影響圖邊與 cost | **禁止**：運行時不應依賴場景 Transform；應在構圖時將連結端點寫入圖數據，運行時只讀圖 |

---

### 2.11 僅編輯器／可視化（不影響路徑結果）

以下僅影響編輯器或 Gizmo，**不參與路徑計算**，在幀同步邏輯中**不呼叫即可**，無需改庫內實作：

- **AstarPathEditor.cs**：`Time.realtimeSinceStartup`、`Guid.NewGuid`（編輯器 UI／快取）
- **GridGeneratorEditor.cs**：`Time.realtimeSinceStartup`、Editor 繪製
- **AIPath.cs**：`Time.realtimeSinceStartup`、Gizmo 淡出
- **RetainedGizmos.cs**：`Time.realtimeSinceStartup.GetHashCode()` 用於 Gizmo 唯一性
- **Path.cs**：`Environment.StackTrace`（僅在 `ASTAR_POOL_DEBUG` 下）
- **AstarProfiler.cs**：`DateTime.UtcNow`（性能統計）
- **AstarData.cs** / **UnityReferenceHelper.cs**：`Guid.NewGuid`（圖／資源 ID，構圖或資源管理用）

---

## 三、幀同步邏輯中允許／禁止總結

### ✅ 允許（在純邏輯層使用）

- **圖結構與查詢**：已鎖定的 `NavGraph`、`GraphNode`、`GridGraph` 等，僅讀取節點與連通性。
- **路徑搜尋**：`ABPath` 的建構、`Prepare`、`Initialize`、`CalculateStep`（整條路徑一次算完，且不啟用時間片邏輯）。
- **路徑表示**：`Path.vectorPath`、節點序列，轉為 `fp3[]` 供移動使用。
- **GetNearest**：僅用「返回最近節點」的介面，且不依賴 `RandomPointOnSurface` 或隨機點。
- **Path ID**：`GetNextPathID()`，只要請求順序一致即可。
- **定點數**：所有輸入輸出為 `fp`/`fp3`，運算在邏輯層用定點。

### ❌ 禁止（不得在幀同步邏輯中依賴）

- **Time**：`Time.deltaTime`、`Time.time`、`Time.realtimeSinceStartup`、`Time.frameCount`。
- **Random**：`Random.value`、`Random.Range`（除非改為注入確定性種子且僅在可控處使用）。
- **DateTime**：`DateTime.UtcNow`（含未來若啟用的分幀尋路時限）。
- **Physics**：`Physics.Raycast`、`CheckSphere`、`SphereCast`、`Linecast` 等（邏輯幀內不查詢）。
- **運行時 Transform**：動態讀取場景中連結或單位的 `Transform.position` 作為圖／路徑依據。
- **自動重算策略**：基於 `Time.time` 的 `AutoRepathPolicy`；改為基於 tick 或狀態機。
- **重力貼地**：依賴 `Physics.Raycast` 的 `RaycastPosition`；移動改為沿路徑定點計算。
- **路徑修飾器**：使用 `Physics` 做射線的 `RaycastModifier` / `StartEndModifier`；若需簡化，僅用圖射線且圖數據確定。

### ⚠️ 僅在「圖構建階段」允許（運行時邏輯不依賴）

- **GraphCollision**：`Physics` 用於掃描時節點 walkable／高度；僅在世界初始化或離線掃描時使用，且不在邏輯幀內動態掃描。
- **圖更新**：若要做圖更新，須與 **tick 或確定性事件** 綁定，且不用 `Time`/協程排程。

---

## 四、與計劃的對應

- **不在幀同步邏輯中直接使用**：`AIPath`、`Seeker` 的 MonoBehaviour 更新與回調；改為由 `BaseWorld.FixedUpdate` 驅動，在 `OnFixedUpdate(fp, WorldUpdateType)` 中呼叫**純邏輯的尋路介面**。
- **僅使用**：圖結構（`GraphNode`、`GridGraph` 等）、`ABPath` 的搜尋、以及可轉為定點輸出的路徑數據；並透過 **FrameSync 尋路服務**（如 `IFrameSyncPathfinder`）包裝，只接受/返回 `fp3` 與 tick。
- **地圖與圖**：在世界創建時構建並鎖定圖，或將圖更新設計為與 tick 綁定的確定性操作，避免運行中依賴 Time/Physics/Transform。

此審查結果可直接用於「步驟 2：設計 FrameSync 尋路介面」與「步驟 3：基於 AStarPathfinder 編寫純邏輯 wrapper」的邊界劃分。
