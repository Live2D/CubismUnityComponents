[English](Description.md) / [日本語](Description.ja.md)

---

# Model Sorting

このサンプルは、複数のモデルの描画順設定したものになります。
それぞれ Grouped Sorting Index を設定してモデルの前後関係を調整しています。

画面の左から順に下記のようになっています。

- Maoモデルを背後に描画する。
- Maoモデルを背景のClippingモデルと同じグループで描画する。
  - ClippingモデルのDrawableである `NoMask` と `Mask` のLocal Orderを変更し、モデルの間にモデルを挟む表現の描画を行う。
- Maoモデルを前面に描画する。
