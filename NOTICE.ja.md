[English](NOTICE.md) / [日本語](NOTICE.ja.md)

---

# お知らせ


## [制限事項] WebGL書き出し時のAudioClipからのリップシンク対応について (2024-11-28)

Cubism SDK for Unityの音声からのリップシンクは、波形情報の取得にAudioClipのAPIを利用しています。
しかし、AudioClipから動的に波形情報を取得するためのAPIはUnityからWebGLへの書き出しに非対応となっているため、Cubism SDK for UnityのリップシンクもWebGL書き出しには非対応となります。

詳しくは、Unity社 公式ドキュメントをご確認ください。

* [WebGL のオーディオ](https://docs.unity3d.com/ja/current/Manual/webgl-audio.html)


## [制限事項] Windows ARM64向けの対応状況について (2024-01-18)

Unity 2023.1以降にて指定可能となったWindows ARM64向けビルドにつきまして、Cubism SDK for Unityは現在対応しておりません。
対応バージョンや時期につきましては今後のリリースをもってお知らせいたします。


## [注意事項] macOS X86_64向け対応について (2026-10-29)

Cubism 5 SDK for Unity R6_1より、macOS X86_64向けは非推奨となります。
今後のリリースでサポートを終了し、macOS Apple Silicon (ARM64)向けのみ対応いたします。

終了時期について、詳しくは今後のリリースをもってお知らせいたします。


## [注意事項] Tuanjieでの.metaファイルの扱いについて

Cubism SDK for Unityは開発バージョンの都合により、 `.meta` ファイルの形式がTuanjieに対応しておりません。
インポート直後の `.meta` ファイルを残した状態でビルドを行うとエラーが発生する場合がございます。

Unity Packageをインポートしたのち、 `Reimport All` でファイルの再インポート処理を走らせ、 `.meta` ファイルの再生成を行うことで解決することができます。


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
