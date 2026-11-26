[English](Description.md) / [日本語](Description.ja.md)

---

# Rendering Interceptor

このサンプルは、描画割り込み機能を利用します。

## RenderingInterceptor

各Maoモデルの描画に描画割り込み機能でCubismモデルやSpriteを描画します。

画面の左から順に下記のようになっています。

- Koharuモデルを描画割り込み機能で描画
- Z Depthを基準に描画割り込み機能で描画
- 特定のDrawableを基準に描画割り込み機能で描画
- Sorting Orderを基準に描画割り込み機能で描画

## RenderingInterceptorModels

実行時にスクリプトからモデルを配置し、Koharuモデルの描画の途中にRiceモデルを描画割り込み機能で描画します。

配置と設定は `RenderingInterceptorModelsSetting.cs` で行います。
Inspectorで下記を設定してください。

- `Normal Model Prefab`：通常描画するモデルのPrefab（デフォルト: Koharu）
- `Interceptor Model Prefab`：描画割り込み機能で描画するモデルのPrefab（デフォルト: Rice）
- `Interceptor Model Animator Controller`：描画割り込みで描画するモデルに設定するAnimator Controller（デフォルト: `Rice.controller`）
- `Default Sorting Mode`：両方のモデルに設定する `SortingMode`（デフォルト: `BackToFrontOrder`）

スクリプトは `Awake` で次の処理を行います。

- 2つのPrefabをインスタンス化
- 両方のモデルの `CubismRenderController` に下記を設定
  - `SortingMode`：`Default Sorting Mode` の値
  - `GroupedSortingIndex`：`0`
- 描画割り込みで描画するモデルにAnimator Controllerを設定（`Animator` コンポーネントが無ければ追加）
- 描画割り込みで描画するモデルに `CubismRenderingInterceptController` を設定（無ければ追加）し、下記を設定
  - `InvokeTimingSetting`：`PostRendering`
  - `Mode`：`SortingMode` が深度ソート（`BackToFrontZ` / `FrontToBackZ`）のときは `ZDepth`、それ以外のときは `SortingOrder`
  - `GroupSortingOrder`：`0`
- 描画割り込みで描画するモデルを右に少しずらして配置
