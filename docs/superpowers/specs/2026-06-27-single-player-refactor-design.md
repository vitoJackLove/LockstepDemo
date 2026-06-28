# 单机模式架构重构设计

> 日期：2026-06-27  
> 状态：已确认

## 目标

- 单机与联机共用帧同步管道（本地回环 Transport）
- 单机禁用快照与回滚（性能考虑）
- 消除散落 `if (IsSinglePlayer)` 分支
- 修复金手指 Buff 等调试工具在单机下的兼容问题

## 架构

四层抽象：IGameSessionProfile / IFrameSyncTransport / IEntitySyncPolicy / 统一 BaseWorld.FixedUpdate

## 单机帧时序

ForecastTick=0，Transport 当帧回环权威帧，跳过快照与 VerifyForecast。

## 联机行为

保持现有 TCP、快照、回滚逻辑不变。

## 不在范围

SurvivorWorld 单机、服务端 FrameSyncServer 改动
