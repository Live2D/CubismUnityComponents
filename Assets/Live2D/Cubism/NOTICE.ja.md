[English](NOTICE.md) / [日本語](NOTICE.ja.md)

---

# お知らせ

## [注意事項] SamplesにおけるInput System Packageへの対応について (2025-08-26)

Cubism SDK for UnityのSamplesでは従来の `Input Manager` を利用して入力を管理しております。
そのため、`Input System Package` のみを利用する方式が有効になっている場合、シーン再生時にエラーが発生する可能性があります。

必要に応じて以下の手順で `Input Manager` を利用するようにプロジェクト設定を変更してください。

1. `Project Settings` -> `Player` を開く。
1. `Active Input Handling` を `both` へ変更する。


## [制限事項] WebGL書き出し時のAudioClipからのリップシンク対応について (2024-11-28)

Cubism SDK for Unityの音声からのリップシンクは、波形情報の取得にAudioClipのAPIを利用しています。
しかし、AudioClipから動的に波形情報を取得するためのAPIはUnityからWebGLへの書き出しに非対応となっているため、Cubism SDK for UnityのリップシンクもWebGL書き出しには非対応となります。

詳しくは、Unity社 公式ドキュメントをご確認ください。

* [WebGL のオーディオ](https://docs.unity3d.com/ja/current/Manual/webgl-audio.html)


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
