# Fix: MainMenu Buttons Not Working After Returning from Game

## Root Cause Analysis

### Bug A — Dead Button Disconnect (Critical)

**What happens:**
1. MainMenu scene has a `GameProgressManager` GameObject. Buttons "New Game" and "Continue" are wired via the Inspector to call methods on this GameObject.
2. Player starts a game. `GameProgressManager.StartNewGame()` loads Level0. The `GameProgressManager` is set to `DontDestroyOnLoad`, so it survives the transition.
3. Player finishes level and returns to MainMenu.
4. Unity loads a **new** MainMenu scene. This new scene has its own `GameProgressManager` GameObject.
5. The new `GameProgressManager.Awake()` runs: it checks `Instance != null && Instance != this`, finds the **old** instance still alive, and calls `Destroy(gameObject)` — killing itself.
6. The "New Game" and "Continue" buttons in the new scene are still referencing the **destroyed** `GameProgressManager`. Clicks go nowhere.

**Why `QuitGame()` works:** It's a method on `MainMenuUI` (a non-singleton, scene-local component), so it survives correctly.

**Fix:** Add `StartNewGame()` and `ContinueGame()` wrapper methods to `MainMenuUI.cs`, delegating to `GameProgressManager.Instance`. Then re-wire the buttons in the Inspector to point to `MainMenuUI` instead.

### Bug B — Soft-Lock (Game Freezes)

**What happens:**
1. Player enters a level with a flashback cutscene.
2. `FlashbackPanelController.Awake()` sets `IsPlaying = true`.
3. Before the flashback finishes, player opens Pause Menu → Exit to Main Menu.
4. Scene unloads. `FlashbackPanelController` GameObject is destroyed.
5. **No `OnDestroy()` exists** to reset `IsPlaying = false`. The static flag stays `true` **forever**.
6. Next time the player clicks "New Game" or "Continue", the level loads. `PreGamePanel.Start()` runs:
   ```csharp
   while (FlashbackPanelController.IsPlaying) { yield return null; }
   ```
7. `IsPlaying` is permanently `true` → **infinite loop** → game soft-locked.

**Same issue exists with `PreGamePanel.IsPlaying`** — if the scene unloads while the panel is typing, the flag stays stuck, blocking `FinishPanelController`, `PlayerMovement`, `PlayerDash`, etc.

**Fix:** Add `OnDestroy()` to both `FlashbackPanelController` and `PreGamePanel` to reset their respective `IsPlaying` flags.

---

## Implementation Plan

### Step 1: Fix Bug A — `MainMenuUI.cs`

Add two public wrapper methods and wire them to the buttons.

**Changes to [`Assets/Scripts/UI/MainMenuUI.cs`](Assets/Scripts/UI/MainMenuUI.cs):**

1. Add `StartNewGame()` method:
   ```csharp
   public void StartNewGame()
   {
       if (GameProgressManager.Instance != null)
           GameProgressManager.Instance.StartNewGame();
   }
   ```

2. Add `ContinueGame()` method:
   ```csharp
   public void ContinueGame()
   {
       if (GameProgressManager.Instance != null)
           GameProgressManager.Instance.ContinueGame();
   }
   ```

3. **In the Unity Inspector**: Re-wire the "New Game" button's `OnClick()` → `MainMenuUI.StartNewGame()` and the "Continue" button's `OnClick()` → `MainMenuUI.ContinueGame()`. These now reference the local `MainMenuUI` component which is never destroyed on scene load, while internally using the static singleton accessor `GameProgressManager.Instance` which always returns the surviving instance.

### Step 2: Fix Bug B — `FlashbackPanelController.cs`

**Changes to [`Assets/Scripts/UI/FlashbackPanelController.cs`](Assets/Scripts/UI/FlashbackPanelController.cs):**

Add an `OnDestroy()` method:
```csharp
private void OnDestroy()
{
    IsPlaying = false;
}
```

### Step 3: Fix Bug B — `PreGamePanel.cs`

**Changes to [`Assets/Scripts/UI/PreGamePanel.cs`](Assets/Scripts/UI/PreGamePanel.cs):**

Add an `OnDestroy()` method:
```csharp
private void OnDestroy()
{
    IsPlaying = false;
}
```

---

## Files Affected

| File | Change |
|---|---|
| `Assets/Scripts/UI/MainMenuUI.cs` | Add `StartNewGame()` + `ContinueGame()` wrapper methods |
| `Assets/Scripts/UI/FlashbackPanelController.cs` | Add `OnDestroy()` to reset `IsPlaying` |
| `Assets/Scripts/UI/PreGamePanel.cs` | Add `OnDestroy()` to reset `IsPlaying` |
| `Assets/Scenes/MainMenu.unity` | Re-wire button OnClick events in Inspector |

---

## Verification

1. **Bug A**: Play a level to completion, return to MainMenu, click "New Game" → should load Level0 without errors.
2. **Bug A**: Return to MainMenu, check "Continue" button is properly enabled/disabled based on save data.
3. **Bug B**: Start a level with a flashback, immediately pause → Exit to Main Menu → Start New Game → should not hang.
