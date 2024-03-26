[English](NOTICE.md) / [日本語](NOTICE.ja.md)

---

# Notices

## [Restrictions] Support for Windows ARM64 (2024-01-18)

Cubism SDK for Unity currently does not support Windows ARM64 builds for Unity 2023.1 or later.
A supported version will be announced in a future release.


## [Caution] Support for Apple's Privacy Manifest Policy

This product does not use the APIs or third-party products specified in Apple's privacy manifest policy.
This will be addressed in future updates if this product requires such support.
Please check the documentation published by Apple for details.

[Privacy updates for App Store submissions](https://developer.apple.com/news/?id=3d8a9yyh)


### [Caution] About using `.bundle` and `.dylib` on macOS Catalina or later

To use `.bundle` and `.dylib` on macOS Catalina or later, you need to be connected to an online environment to verify your notarization.

For details, please check the official Apple documentation.

* [Apple Official Documentation](https://developer.apple.com/documentation/security/notarizing_your_app_before_distribution)


### [Restrictions] Manipulation of `RenderTextureCount` value for mask textures during execution (2024-03-26)

If `CubismMaskTexture.RenderTextureCount` is changed during scene execution to a value greater than that at the start of execution, the mask will not be regenerated correctly.
A supported version will be announced in a future release.
---

©Live2D
