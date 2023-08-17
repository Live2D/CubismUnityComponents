[English](NOTICE.md) / [日本語](NOTICE.ja.md)

---

# お知らせ

## [注意事項] Apple Silicon版 Unity Editor での動作について (2023-01-26)

Apple Silicon版Unity Editorでの動作につきまして、macOS向けのCubism Coreを利用するには `Assets/Live2D/Cubism/Plugins/macOS` 以下にある `Live2DCubismCore.bundle` をインスペクタから操作する必要があります。
手順は以下の通りとなります。

1. `Live2DCubismCore.bundle` を選択状態にし、インスペクタを表示する。
1. `Platform settings` の `Editor` を選択し、`Apple Silicon` または `Any CPU` を選択する。
1. Unity Editorを再起動する。


## [注意事項] Windows 11の対応状況について (2021-12-09)

Windows 11対応につきまして、Windows 11上にて成果物の動作を確認しております。
ただし、Windows 11を利用したビルドにつきましては動作を保証しておりません、ご了承ください。
対応バージョンや時期につきましては今後のリリースをもってお知らせいたします。


## [注意事項] macOS Catalina 以降での `.bundle` と `.dylib` の利用について

macOS Catalina 以降で `.bundle` と `.dylib` を利用する際、公証の確認のためオンライン環境に接続している必要があります。

詳しくは、Apple社 公式ドキュメントをご確認ください。

* [Apple社 公式ドキュメント](https://developer.apple.com/documentation/security/notarizing_your_app_before_distribution)
---

©Live2D
