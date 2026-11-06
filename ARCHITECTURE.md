# Architecture

Please read this before changing code. The structure is new and still being cleaned up, so keeping to these rules saves us both from merge conflicts and hidden bugs.

## Pipeline

Every frame, input flows through four steps, in this order:

```
Measure  →  Decide  →  Context  →  React
(Platform)  (Core)     (Core)      (Features)
```

1. **Measure**: input handlers read the microphone and camera and return plain numbers (peak, RMS, brightness).
2. **Decide**: `SensorHandler` turns those numbers into results (clap, blow, light).
3. **Context**: the results are written into one `SensorContext` object.
4. **React**: `ShapeReactionDispatcher` passes the context to the current shape, and the shape decides what to do (e.g. the candle ignites or goes out).

## Folders

```
Assets/Project/
├── Core/       Pure logic: sensors, SensorHandler, SensorContext, interfaces
├── Platform/   Talks to hardware: microphone and camera handlers, jslib bridge
├── App/        Wires everything together, runs the pipeline each frame
│               (currently still RunSensor() in ShapeSwitcher)
├── UI/         Everything drawn on screen: buttons, debug display
└── Features/   The individual models and how they react
```

## Rules

### Core
- **No Unity code in Core.** No `using UnityEngine;`, no `Mathf`, no `Time`, no `Debug.Log`. The Core assembly definition has *No Engine References* turned on, so this is a compile error anyway.
- Use `System.MathF` instead of `Mathf`. Pass time in as a parameter instead of using `Time.time`.

### Platform
- Input handlers **only measure** and return values. They never decide whether something is a clap or a blow.
- New input source: implement the existing interface (e.g. `IMicrophoneInputHandler`).

### Platform-specific code
- `#if UNITY_WEBGL` / `#if UNITY_EDITOR` only where the handlers are **created**. Nowhere else.
- The Editor uses `EditorMicrophoneInputHandler` (Unity's `Microphone`). The web build uses `WebMicrophoneInputHandler` (jslib). Both cameras use `WebLightInputHandler`.

### Reactions (Features)
- Shapes **only read** the `SensorContext`. Never change its values.
- New model: add its reaction script under `Features/`. The pipeline doesn't need to change.

### Update loop
- **Avoid `Update()` in other scripts.** The whole pipeline runs from one place, `RunSensor()` in `ShapeSwitcher`, so the order of steps stays visible and predictable. Several `Update()` methods would run in an unclear order and could work with values from the previous frame.
- New logic goes into a **plain C# class** that `RunSensor()` calls, not into a new MonoBehaviour with its own `Update()`.

### UI
- UI code goes into the `UI/` folder, as plain classes with a `Draw(...)` method.
- For now, these `Draw(...)` methods are called from `OnGUI()` in `ShapeSwitcher`. This is the current state and will be cleaned up later, so please don't add a separate `OnGUI()` elsewhere.

### WebSocket communication
- In the current state, call it from `RunSensor()`. **Do not add an `Update()` for it.**
- Keep `RunSensor()` short: put as much of the WebSocket logic as possible into **its own class**, and call that class from `RunSensor()`.
- Incoming messages are only collected when they arrive and processed in `RunSensor()`, at the start of the frame. Never wait for a network reply inside `RunSensor()`.
- Remote input goes into the same `SensorContext` as local input.

### Thresholds
- No magic numbers. Thresholds are named constants or constructor parameters.

### Unity
- **Move and rename files in Unity's Project window, not in Visual Studio or Windows Explorer.** Otherwise the `.meta` files get lost and scenes show "Missing Script".
- Never name a folder `Editor` for code the game uses. Unity excludes those folders from builds.
- Don't commit WebGL build output (`Build/`, `TemplateData/`, `index.html`) to this repo.

## Work in progress

- `ShapeSwitcher` is being split up. Please don't add new logic to it. New code goes into the folders above.
- Sensor calibration (auto-calibration with ratios) is being reworked in `SensorHandler`. Please check with me before changing sensor thresholds or logic.

## Before merging

Work on a branch and open a pull request. The other person takes a quick look before it goes to `main`.
