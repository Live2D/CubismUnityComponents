## [制限事項] macOS 11.0 Big Surへの対応状況について (2021-01-12)
現在公開中のCubism SDKは、macOS 11.0 Big Surには対応しておりません。
正常に動作できない可能性がありますので、OSのアップグレードをお控えいただきご利用いただきますようお願いいたします。
現在対応検討中となりますが、対応バージョンや時期につきましては改めてお知らせいたします。

またApple Sillicon版のmacにつきましても、全てのCubism 製品において対応しておりませんのでこちらも合わせてご了承ください。


## [注意事項] macOS Catalina での `.bundle` と `.dylib` の利用について

macOS Catalina 上で `.bundle` と `.dylib` を利用する際、公証の確認のためオンライン環境に接続している必要があります。

詳しくは、Apple社 公式ドキュメントをご確認ください。

* [Apple社 公式ドキュメント](https://developer.apple.com/documentation/security/notarizing_your_app_before_distribution)


## [制限事項] Unity 2020 での動作について

Cubism 4 SDK for Unity は、 Unity 2020 上で以下の現象が確認されております。

* ファイルをインポートした際に、そのファイルの名前の頭文字が `_` に書き換わる
  * Unity 2020.1 は、 2020.1.17.1f1 以降で修正済み [Unity Issue Tracker](https://issuetracker.unity3d.com/issues/files-are-renamed-when-dragged-into-unity-using-file-explorer?_ga=2.32656061.240166293.1609898373-1093455778.1542267012)
* Unity 2020 で Linux(Ubuntu) 向けにビルドし、 Ubuntu 20.04 で実行すると `SIGFPE` が発生する
* モデル一式のインポート処理の中で、`.fadeMotionList.asset` や `.expressionList.asset` に要素が1つしか登録されていない

### 対応方法
* Cubism 4 SDK for Unity では、当面の間 Unity 2020 環境でのサポートが困難になる場合がありますので、Unity社の更新情報も合わせてご確認ください。
* Unity 2020 環境における Cubism SDKの利用が困難な場合は Unity 2019 等 LTS バージョンのご利用をご検討ください。


## [注意事項] Unity 2018 での動作について

* `.unitypackage` 内にある `Prefab` の `Material` の参照が切れる

macOS Catalina 上で Cubism の `Prefab` が同梱された `.unitypackage` を、
2Dモードで作成した Unity 2018 のプロジェクトにインポートすると、展開されたパッケージ内の `Prefab` が持つ `Material` の参照が切れることがある。

### 対応方法
* 3D モードで作成した Unity プロジェクトを使用すると回避することが可能です。
* 現象が発生している場合、 `Prefab` を生成し直すことでも回避が可能です。

---

©Live2D
