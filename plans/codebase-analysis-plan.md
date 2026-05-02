# AFTERIMAGE Codebase Analysis & Refinement Plan

## 1. Analysis Summary

After reviewing 16+ scripts across the entire project, here's a categorized breakdown of findings.

---

### 1.1 Folder Structure

| Area | Verdict |
|------|---------|
| `Enemies/` - 8 enemy types + base | ✅ Well organized |
| `Projectiles/` - 5 projectile types | ✅ Well organized |
| `Rooms/` - Room, RoomManager, Doors, Captions | ✅ Well organized |
| `UI/` - 12 UI scripts | ✅ Well organized |
| `Utilities/` - 6 utility scripts | ✅ Well organized |
| `Loadout/` - Loadout system | ✅ Well organized |
| `Audio/` - 3 audio scripts | ✅ Well organized |
| **`Camera/`** folder exists but is **empty** - CameraShakeService.cs sits in root | ❌ Empty folder |
| **`Editor/`** folder exists but is **empty** | ❌ Empty folder |
| **Root Scripts/** - PlayerDash, PlayerMovement, ThrowableObject, etc. at root | ⚠️ Could be organized into subfolders |

**File-level issues:**
- `Assets/Scripts/Utilities/HoloLure.cs` vs `HoloLureUtility.cs` - duplicate files?

---

### 1.2 Comments & Headers

| Issue | Example |
|-------|---------|
| **No file headers** | No script has a header comment describing purpose |
| **Leftover dev comments** | `// <-- ADDED: Allow the agent to move again` in EnemyScatter.cs:56 |
| **Uneven comment quality** | Some methods well-documented, others have zero comments |
| **Redundant comments** | `// Update walking animation` on a method literally named `SetWalkingAnimation` |
| **Path-only headers** | `// Assets/Scripts/UI/UtilityHUDController.cs` on 2 files only |

---

### 1.3 Regions

**No `#region` blocks exist anywhere** in the codebase. This makes large files harder to navigate:
- `PlayerDash.cs` (482 lines) - desperately needs regions
- `EnemyBase.cs` (381 lines) - would benefit from regions

---

### 1.4 God Scripts / Monolithic Classes

| Script | Lines | Issues |
|--------|-------|--------|
| **PlayerDash.cs** | **482** | Handles: dashing, camera zoom, enemy highlights, door breaking, projectile destruction, player animation, weapon switching, audio, slow-mo recovery, environment collision. **Primary target for refactoring.** |
| **EnemyBase.cs** | **381** | Handles: patrol, aggro, death, knockback, stun, highlights, blood pools, animation, audio, physics state. Reasonable for a base class but edge of too-large. |

---

### 1.5 Unity Best Practices Violations

#### CRITICAL: Static State Check Chain (Anti-Pattern)
The exact same boolean chain appears in **3+ scripts**:
```csharp
CaptionManager.IsFrozen || TutorialUIManager.IsOpen || PreGamePanel.IsPlaying || FinishPanelController.IsFinished
```
Found in: `PlayerMovement.cs:36`, `PlayerDash.cs:95`, `UtilityManager.cs:41`

This creates **tight coupling** between gameplay systems and every UI panel. Adding one new UI state requires touching 3+ files.

#### Debug.Log in Production
- `EnemyPhalanx.cs:86,106` - `Debug.Log($"[EnemyPhalanx] ... attacking player!")`
- `BaseProjectile.cs:82,87` - `Debug.Log($"[BaseProjectile] Shield broken ...")`
- `ThrowableObject.cs:60,69` - `Debug.Log("[ThrowableObject] No visible enemies found.")`
- `UtilityManager.cs:54` - `Debug.Log($"Switched to: {CurrentUtility?.UtilityName}")`

#### Exposed public fields that should be `[SerializeField] private`
BaseProjectile: `speed`, `lifetime` - exposed but other projectiles set values via Initialize()
EnemyBase: `health`, `detectRange`, `damage`, `scoreValue` - could be serialized private

#### String-based lookups in Update loops
- `transform.Find("Shield")` - string lookup in Awake (better, but still fragile)
- `CompareTag("Player")` - used in several update loops instead of cached layer comparison

#### OnGUI() / GUILayout in production code
- `UtilityManager.cs:66-75` - uses `OnGUI()` and `GUILayout` for debug HUD

#### Inconsistent initialization patterns
- `EnemyWeaver` uses `Start()` while all others use `Awake()`
- Some call `base.Awake()`, some don't

---

### 1.6 Duplicate Code

**Melee enemies share nearly identical attack patterns:**
- `EnemyGrunt.WaitForAttackAnimationEnd()` - identical to
- `EnemyPhalanx.WaitForAttackAnimationEnd()` - identical to
- `EnemyGeist.WaitForAttackAnimationEnd()`

All three follow the exact same `AttackRoutine` structure: `isAttacking = true` → `agent.isStopped = true` → `LookAt` → `SetKatanaVisible(true)` → `animator.SetTrigger("dashTrigger")` → `StartCoroutine(WaitForAttackAnimationEnd())` → `yield return new WaitForSeconds(attackWindup)` → abort check → range check → deal damage → cooldown.

This could be consolidated into a base class method.

---

### 1.7 Naming & Consistency

- **Protected fields**: consistent `_` prefix ✅
- **Private fields**: consistent `_` prefix ✅
- **Public fields**: no prefix, PascalCase ✅
- **Method names**: PascalCase ✅
- **File-scoped namespaces**: **Not used** - could be applied for cleaner code
- **Naming inconsistency**: `minSolidTime`/`maxSolidTime` vs `minPatrolWait`/`maxPatrolWait` - Geist uses `min`/`max` prefix while base uses `min`/`max` suffix pattern

---

## 2. Recommended Refinement Plan

### Phase 1: Quick Wins (Low Effort, High Impact)

1. **Add `#region` blocks** to large files (PlayerDash, EnemyBase)
2. **Remove debug `Debug.Log` statements** from production code
3. **Organize root scripts** into subfolders (e.g., `Player/`, `Camera/`)
4. **Add file header comments** with brief descriptions
5. **Remove leftover dev comments** (e.g., `// <-- ADDED:`)

### Phase 2: Structural Improvements (Medium Effort)

6. **Create a `GameStateService`** to replace the static state check chain
7. **Add `#region` blocks** to all medium+ files
8. **Consolidate duplicate melee attack code** into `EnemyBase`
9. **Replace magic numbers** with named constants
10. **Replace `OnGUI()`** with proper UI in UtilityManager

### Phase 3: Architecture (High Effort)

11. **Refactor `PlayerDash`** - extract camera, highlight, and door systems
12. **Convert public fields** to `[SerializeField] private` where appropriate
13. **Add event-based player reference** instead of `GameObject.FindGameObjectWithTag`
14. **Remove empty folders** (Camera, Editor)
15. **Add `[RequireComponent]` attributes** where missing

---

## 3. Priority Todo List

The following todo list is ordered by priority and grouped by phase.
