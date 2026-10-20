[English](Description.md) / [日本語](Description.ja.md)

---

# MultipleModels

このサンプルは、複数のモデルを重ねて配置したときの描画順を設定したものになります。
`CubismRenderController.GroupedSortingIndex` を設定してモデルの前後関係を調整しており、このシーンではKoharuモデルに大きい値を設定して、Clippingモデルよりも前面に描画しています。

また、他の2D要素と簡単に混ぜられるように、CubismモデルのパーツはデフォルトでZオフセットを使用して並べ替えられます。これにより、UnityのSortingLayerやSortingOrderのいずれかで複数のモデルを並べ替える必要があります。どちらも、`CubismRenderController.SortingLayer`および`CubismRenderController.SortingOrder`設定を介して簡単に実現できます。

モデルの並べ替えを明確に分離することで、期待される目に見える結果が得られるだけでなく、UnityのDynamic Batchingを機能させることもできます（もちろん、有効になっている場合のみ）。
