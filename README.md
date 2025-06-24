# Interactive Image Grid

##  Project Description

This Unity project implements an interactive, scrollable **3×3 grid of images** designed to showcase responsive UI structure, user interaction handling, and efficient state management.

Key interaction features include:
- Smooth **scrolling in all four directions**
- **Interactive resizing** of individual grid cells
- **Drag-and-drop logic** with long-press activation
- Snap-to-cell scrolling for a clean and intuitive experience

The project highlights best practices in Unity UI architecture and user experience design, with performance-optimized code suitable for both mobile and desktop platforms.

##  Installation

This project includes a ready-to-use `.unitypackage` file for quick integration into your own Unity projects.

###  Importing the Package

1. Open your Unity project.
2. Go to `Assets` → `Import Package` → `Custom Package...`
3. Select the provided `.unitypackage` file.
4. In the import dialog, ensure all items are checked and click `Import`.

###  Requirements

- **Unity 2022.3 LTS** or newer
- **[DOTween](http://dotween.demigiant.com/)** (Free version)

##  Algorithm and Project Logic

### 1. Infinite Scrolling (Horizontal & Vertical)

This system enables smooth, infinite scrolling in all directions by recycling and repositioning grid elements based on their logical positions. Instead of instantiating new objects, the grid continuously updates the positions of a fixed set of UI elements, giving the illusion of an endless layout.

####  Involved Components

- **`FullScreenGridLayout`**  
  Automatically arranges all `GridCell` elements according to their `gridPosition (x, y)`. When a cell’s position is updated, this layout ensures the cell is visually placed at the correct anchored position.

- **`GridManager`**  
  Manages the logical state of the grid:
  - Calculates scrollable bounds via `CalculateBounds()`
  - Handles cell relocation via `GridUpdate()`
  - Tracks each `GridCell`’s `gridPosition` and ensures it's valid

- **`InfiniteScroll`**  
  Detects user scroll events using `OnScroll` and triggers boundary checks by calling `GridManager.Instance.GridUpdate()`.

####  Scrolling Algorithm

1. **Scroll Detection**  
   `InfiniteScroll` listens to the `ScrollRect.OnScroll` event. On every scroll frame, it calls `GridManager.Instance.GridUpdate()`.

2. **Boundary Calculation**  
   `GridManager.CalculateBounds()` defines the visible area of the scrollable grid using the current scroll position and content size.

3. **Cell Validation**  
   For each `GridCell`, `GridUpdate()` checks if the cell has moved outside the allowed bounds.

4. **Logical Repositioning**  
   If a cell crosses the boundary, its `gridPosition` is recalculated (e.g., moved from `(4, 1)` to `(-4, 1)`).

5. **Visual Repositioning**  
   `FullScreenGridLayout` immediately repositions the cell based on its new `gridPosition`, keeping the grid visually continuous.

####  Result

This method creates a highly performant infinite grid system:
- No object instantiation/destruction
- Full control over cell positions
- Clean integration with Unity UI and ScrollRect

### 2. Snap-to-Step Logic

This system ensures that after scrolling stops, the content **snaps to the nearest aligned grid position**, creating a clean, predictable, and grid-consistent user experience.



####  Involved Components

- **`InfiniteScroll`**  
  Monitors scroll velocity and user interaction during `LateUpdate`. If scrolling has stopped and velocity falls below a defined threshold (`snapThreshold`), it triggers snapping via `PerformSnapStep()`.

- **`GridManager`**  
  Provides snapping offset data through `GetSnapOffset()`. This method calculates the precise distance between the current position of the top-left visible cell and the origin `(0,0)` in logical grid space.

- **`DOTween`**  
  Animates the content snapping movement smoothly to the calculated aligned position.

####  Snapping Algorithm

1. **Velocity Check**  
   In `LateUpdate`, `InfiniteScroll` checks:
   - Whether the user is currently interacting with the scroll
   - Whether the scroll velocity is below `snapThreshold`

2. **Trigger Snap**  
   If conditions are met, it calls `PerformSnapStep()`.

3. **Calculate Snap Offset**  
   `GridManager.Instance.GetSnapOffset()` returns the required offset needed to align the content such that the top-left visible cell is snapped to its grid origin.

4. **Animate Snap**  
   The content's anchored position is tweened (animated) toward the target snapped position using DOTween, ensuring a smooth transition.

####  Result

This logic guarantees:
- Grid-aligned content positions after each scroll
- Smooth and visually appealing transitions
- A consistent and clear structure regardless of scroll momentum

### 3. Drag-and-Drop System

Drag-and-drop logic is handled inside the `GridCell` class using Unity’s EventSystems interfaces. It enables long-hold interaction and animated cell swapping.

> 🎯 **Author’s Note**  
> I believe it would be more appropriate to move the drag-and-drop logic into a separate script  
> for better separation of concerns...  
> But hey — there’s no such thing as perfect code!  
> Don’t judge too harshly 😊

####  How It Works

- `OnPointerDown` starts the `HoldCheck()` coroutine.
  - If the user holds for 1 second **without scrolling**, drag mode activates.
  - ScrollRect is temporarily disabled.

- In `OnBeginDrag`, the cell is scaled and moved to the front.
- While dragging (`OnDrag`):
  - The cell follows the pointer.
  - The nearest cell is detected using `GetClosestCollidingCell()`.
  - If found, `SwapCells()` animates a position swap using DOTween.

- `OnEndDrag` snaps the cell back with animation and re-enables layout.

####  Highlights

- Long-press activation prevents accidental drags.
- Real-time swap logic based on proximity.
- Smooth transitions and layout-aware behavior.

### 4. Interactive Resizing

Grid cells can be expanded or collapsed on click to emphasize content or create focus.

####  How It Works

- `OnPointerClick` triggers only if the cell isn’t currently moving.
- The logic is delegated to `GridManager`, ensuring only one cell is expanded at a time.
- `Collapse(float width)` handles:
  - Directional shift (left or right) based on `gridPosition`
  - Smooth scaling (2.1x width) and horizontal offset using DOTween
  - Toggle state via `m_isColapsed`

####  Highlights

- Prevents overlapping interactions during animation (`m_isMoved`)
- Animated scale and shift using DOTween
- Clean and responsive single-cell expansion behavior

---

### 5. Mouse Input Tracking

This system globally captures user mouse actions and determines the dominant movement axis (horizontal or vertical) to control scroll direction dynamically.

#### How It Works

- On `MouseButtonDown`, the system stores the initial mouse position.
- On the next frame, it calculates the delta and determines the dominant axis (`XAxis` or `YAxis`).
- If the movement exceeds a threshold (10px), the axis is confirmed and sent to listeners via `SendAxis`.
- On `MouseButtonUp`, `ForceStopMouseTrackingAction` is invoked to finalize and reset the interaction.

#### Highlights

- **Clean gesture detection** with one-frame delay to eliminate noise.
- **Global control logic** that decouples input from scroll and layout logic.
- Emits events to downstream systems for direction-aware behavior.

---

### 6. Integrated Scroll System

A coordinated architecture between input tracking, scroll behavior, and grid logic.

#### Components

- **MouseInputTracker**  
  Captures mouse gestures and determines scroll direction.

- **InfiniteScroll**  
  - Receives axis data from `MouseInputTracker`.
  - Enables scrolling in the selected axis only.
  - Resets the scroll position after gesture completion.

- **GridManager**  
  - Based on the selected `GridCell` and axis:
    - Picks the corresponding row or column.
    - Moves those cells to the scrollable area for interaction.
  - Once scrolling ends, returns them to the static container and resets their logical grid position.

#### Workflow Summary

1. User clicks on a `GridCell` and begins dragging.
2. `MouseInputTracker` detects the gesture and determines scroll axis.
3. `InfiniteScroll` locks scrolling to that axis.
4. `GridManager` selects the affected row or column and injects it into the scrollable container.
5. After gesture completion:
   - `InfiniteScroll` resets the scroll position.
   - `GridManager` returns the moved cells to the static container and restores layout.

#### Key Design Advantages

- Axis-aware gesture detection and movement.
- Clean separation between input, scrolling logic, and grid structure.
- Smooth row/column-based drag interactions with auto-reset behavior.


>  **Author’s Note**  
> This project does **not** use architectural patterns like **MVC** or **State Machine**,  
> as the scope and goals did not require it.  
> However, in my other projects you can find full implementations of such patterns  
> where they are appropriate and beneficial.