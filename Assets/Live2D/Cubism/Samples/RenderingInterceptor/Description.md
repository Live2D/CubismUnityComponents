[English](Description.md) / [日本語](Description.ja.md)

---

# Rendering Interceptor

This sample utilizes the rendering interceptor feature.

## RenderingInterceptor

The rendering interceptor feature is used to render Cubism models and Sprites for each Mao model.

From left to right on the screen, the following are displayed:

- Koharu model rendered using the rendering interceptor feature.
- Rendered using the rendering interceptor feature based on Z Depth.
- Rendered using the rendering interceptor feature based on specific Drawables.
- Rendered using the rendering interceptor feature based on Sorting Order.

## RenderingInterceptorModels

Models are placed from a script at runtime, and the Rice model is rendered partway through the rendering of the Koharu model using the rendering interceptor feature.

The placement and settings are done by `RenderingInterceptorModelsSetting.cs`.
Set the following in the Inspector:

- `Normal Model Prefab`: Prefab of the model rendered normally (Default: Koharu).
- `Interceptor Model Prefab`: Prefab of the model rendered using the rendering interceptor feature (Default: Rice).
- `Interceptor Model Animator Controller`: Animator Controller set to the model rendered using the rendering interceptor feature (Default: `Rice.controller`).
- `Default Sorting Mode`: `SortingMode` set to both models (Default: `BackToFrontOrder`).

The script does the following in `Awake`:

- Instantiates both Prefabs.
- Sets the following on the `CubismRenderController` of both models:
  - `SortingMode`: The value of `Default Sorting Mode`
  - `GroupedSortingIndex`: `0`
- Sets the Animator Controller to the model rendered using the rendering interceptor feature (adds an `Animator` Component if it has none).
- Sets up `CubismRenderingInterceptController` on the model rendered using the rendering interceptor feature (adds it if it has none), and sets the following:
  - `InvokeTimingSetting`: `PostRendering`
  - `Mode`: `ZDepth` when `SortingMode` is a depth sorting mode (`BackToFrontZ` / `FrontToBackZ`), otherwise `SortingOrder`
  - `GroupSortingOrder`: `0`
- Places the model rendered using the rendering interceptor feature slightly to the right.

A model that `CubismRenderingInterceptController` is added to afterwards is taken out of the normal rendering and rendered only using the rendering interceptor feature.
