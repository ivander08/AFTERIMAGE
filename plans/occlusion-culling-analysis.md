# Occlusion Culling Analysis — AFTERIMAGE

## Project Profile

| Factor | Status |
|--------|--------|
| **Render Pipeline** | URP (Universal Render Pipeline) — fully supports occlusion culling |
| **Visual Style** | Low-poly polygon assets (SciFi Worlds, Cyber City, Samurai) |
| **Level Design** | Room-based combat arenas with doors between rooms |
| **Lighting** | Level3 has baked lighting data + reflection probes |
| **Target Platforms** | Both PC and Mobile render pipeline assets exist |
| **Camera** | Cinemachine-based, 3rd person with zoom |

## Key Finding: Room-Based Architecture

The game uses a [`Room`](Assets/Scripts/Rooms/Room.cs) system where levels are divided into combat rooms connected by doors. Each room locks/unlocks doors during combat. **Frustum culling already eliminates most off-screen geometry** — objects in adjacent rooms are outside the camera frustum ~90% of the time. The solid door/room geometry also blocks line-of-sight naturally.

## Recommendation: **Probably Not Worth The Setup Cost**

### Reasons Against

1. **Low polygon counts** — Polygon-style assets are inherently low-triangle. Even if Unity renders objects behind walls within the same room, the GPU cost is negligible.
2. **Room isolation** — Doors naturally segment levels; you can't see much of one room from another.
3. **Baking overhead** — Occlusion data must be rebaked every time geometry changes (walls, props, etc.). Adds friction to iteration.
4. **Memory cost** — Occlusion data consumes memory. For low-poly scenes this can outweigh GPU savings.
5. **Mobile concern** — Mobile render pipeline exists. Occlusion culling adds CPU lookup overhead that can hurt performance on lower-end devices.

### When It Would Be Worth It

- Any level has **large open areas** with dense object counts (warehouse with crates, etc.)
- Rooms with **complex interior geometry** where walls within the same room hide many objects
- Measurable **overdraw** issues in the Frame Debugger

### Better Alternatives To Try First

| Technique | Effort | Impact |
|-----------|--------|--------|
| **LOD groups** on complex props | Medium | Reduces triangle count at distance |
| **Camera far clip plane** tuning | Low | Limits draw distance appropriately |
| **Opaque texture/LOD bias** in URP | Low | Reduces overdraw |
| **Manual culling** via Room (disable renderers in non-active rooms) | High | Maximum control but complex |

### How To Test If Curious

1. Open a level scene (e.g., Level3 which already has baked lighting)
2. **Window → Rendering → Occlusion Culling**
3. Click **"Bake"**
4. Set visibility lines (small colored arrows) at common player positions
5. Switch to **Visualization** tab in Game view to see what's culled
6. Compare **Stats panel** triangle/draw call count before and after

## Bottom Line

Given the room-based combat design and low-poly style, occlusion culling will likely have **diminishing returns**. Profile with the **Unity Profiler** first to find real bottlenecks (enemy AI scripts, URP post-processing, particle VFX).
