# EnemyScatter Rotation Fix

## Root Cause

**NavMeshAgent fights manual rotation during attack sequence.**

In [`EnemyScatter.AttackRoutine()`](Assets/Scripts/Enemies/EnemyScatter.cs:75), the tracking phase (lines 83-101) smoothly rotates the enemy to face the player using `Quaternion.Slerp` over multiple frames. However:

1. [`_agent.updateRotation`](https://docs.unity3d.com/ScriptReference/AI.NavMeshAgent-updateRotation.html) defaults to `true`
2. The NavMeshAgent still has a pending destination from line 62
3. Even though `_agent.isStopped = true` (line 66), the agent continues trying to rotate towards its last destination
4. This creates a **feedback loop** every frame: manual Slerp rotation vs. NavMeshAgent automatic rotation
5. The transform gets pushed in unpredictable directions, potentially off the NavMesh

## Why Other Enemies Don't Have This Issue

| Enemy | Rotation Method | Why It Works |
|-------|----------------|--------------|
| [`EnemyGeist`](Assets/Scripts/Enemies/EnemyGeist.cs:85) | `transform.LookAt()` — **single snap** | No multi-frame conflict |
| [`EnemyWeaver`](Assets/Scripts/Enemies/EnemyWeaver.cs:66) | `transform.rotation = Quaternion.LookRotation()` — **direct set** | No multi-frame conflict |

The Scatter is **unique** in using `Slerp` (multi-frame smooth rotation) which allows the NavMeshAgent to interfere every frame.

## Fix

Add `_agent.updateRotation = false` before the tracking phase and restore it after.

### Changes to [`EnemyScatter.cs`](Assets/Scripts/Enemies/EnemyScatter.cs):

**1. Beginning of tracking phase (around line 80):**
```csharp
_agent.updateRotation = false;  // Stop NavMeshAgent from fighting manual rotation
```

**2. End of attack (around line 152):**
```csharp
_agent.updateRotation = true;   // Restore default behavior
```

**3. In abort paths (around lines 105, 128):**
```csharp
_agent.updateRotation = true;   // Restore on abort
```
