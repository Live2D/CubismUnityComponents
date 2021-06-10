## [制限事項] Apple製品及びmacOS Big Surへの対応状況について (2021-06-10)
本 Cubism SDK につきまして、 `macOS Big Sur 11.2.3` 環境にてビルドが通過できることを確認しております。
Apple Sillicon版のmacにつきましては、引き続き全ての Cubism 製品において対応しておりません。ご了承ください。


## [注意事項] macOS Catalina 以降での `.bundle` と `.dylib` の利用について

macOS Catalina 以降で `.bundle` と `.dylib` を利用する際、公証の確認のためオンライン環境に接続している必要があります。

詳しくは、Apple社 公式ドキュメントをご確認ください。

* [Apple社 公式ドキュメント](https://developer.apple.com/documentation/security/notarizing_your_app_before_distribution)

## [制限事項] Unity 2021 での動作について (2021-06-10)

本 Cubism SDK につきまして、 `Unity 2021.1.7f1` にて Unity Editor 上での動作を確認しております。

ただし試験的な動作確認であり、 Unity Editor からの書き出し機能等含む全ての機能が正しく動作することを保証するものではございません。
アプリケーションに組み込み製品として利用する際には、 Unity 2020 等 LTSバージョンのご利用をご検討ください。

対応するUnityのバージョンに関してはReadmeの[Development environment](Readme.md#Development-environment)をご参照ください。

## [注意事項] Unity 2018 での動作について (2021-06-10)

* `.unitypackage` 内にある `Prefab` の `Material` の参照が切れる

macOS Catalina 以降で Cubism の `Prefab` が同梱された `.unitypackage` を、
2Dモードで作成した Unity 2018 のプロジェクトにインポートすると、展開されたパッケージ内の `Prefab` が持つ `Material` の参照が切れることがある。

* サンプルモデルのMotionFadeが正しく機能せず、サンプルシーンでエラーが発生する

`Assets/Live2D/Cubism/Samples` 以下のシーンで一部のモーションのMotionFadeが正しく機能せず、エラーが発生する。
または、`.unitypackage`からインポートしたモデルのモーションのMotionFadeが正しく機能せず、利用時にエラーが発生する。


### 対応方法

#### .unitypackage 内にある Prefab の Material の参照が切れる

* 3D モードで作成した Unity プロジェクトを使用すると回避することが可能です。
* 現象が発生している場合、 `Prefab` を生成し直すことでも回避が可能です。

#### サンプルモデルのMotionFadeが正しく機能せず、サンプルシーンでエラーが発生する

* `Assets/Live2D/Cubism/Samples/Model`フォルダを再インポートすることで回避可能です。
* `.unitypackage`からインポートしたモデルを利用する場合、そのモデルのフォルダを再インポートすることで回避可能です。

---

©Live2D
