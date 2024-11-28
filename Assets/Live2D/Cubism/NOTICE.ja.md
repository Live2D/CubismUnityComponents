[English](NOTICE.md) / [日本語](NOTICE.ja.md)

---

# お知らせ

## [制限事項] WebGL書き出し時のAudioClipからのリップシンク対応について (2024-11-28)

Cubism SDK for Unityの音声からのリップシンクは、波形情報の取得にAudioClipのAPIを利用しています。
しかし、AudioClipから動的に波形情報を取得するためのAPIはUnityからWebGLへの書き出しに非対応となっているため、Cubism SDK for UnityのリップシンクもWebGL書き出しには非対応となります。

詳しくは、Unity社 公式ドキュメントをご確認ください。

* [WebGL のオーディオ](https://docs.unity3d.com/ja/current/Manual/webgl-audio.html)


## [制限事項] 実行中のマスク用テクスチャの `RenderTextureCount` の値操作について (2024-03-26)

シーン実行中に `CubismMaskTexture.RenderTextureCount` を実行開始時よりも大きい値に変更すると、マスクが正常に再生成されない不具合を確認しています。
対応バージョンや時期につきましては今後のリリースをもってお知らせいたします。


## [制限事項] Windows ARM64向けの対応状況について (2024-01-18)

Unity 2023.1以降にて指定可能となったWindows ARM64向けビルドにつきまして、Cubim SDK for Unityは現在対応しておりません。
対応バージョンや時期につきましては今後のリリースをもってお知らせいたします。


## [注意事項] Apple社のPrivacy Manifest Policy対応について

Apple社が対応を必要としているPrivacy Manifest Policyについて、本製品では指定されているAPI及びサードパーティ製品を使用しておりません。
もし本製品で対応が必要と判断した場合、今後のアップデートにて順次対応する予定です。
詳しくはApple社が公開しているドキュメントをご確認ください。

[Privacy updates for App Store submissions](https://developer.apple.com/news/?id=3d8a9yyh)


## [注意事項] macOS Catalina 以降での `.bundle` と `.dylib` の利用について

macOS Catalina 以降で `.bundle` と `.dylib` を利用する際、公証の確認のためオンライン環境に接続している必要があります。

詳しくは、Apple社 公式ドキュメントをご確認ください。

* [Apple社 公式ドキュメント](https://developer.apple.com/documentation/security/notarizing_your_app_before_distribution)
---

©Live2D
