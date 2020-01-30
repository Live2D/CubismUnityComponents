# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).


## [4-r.1] - 2020-01-30

### Changed

* Change registration of `CubismModel.OnRenderObject()` to `Player Loop` on Unity 2018.
* Change access modifier of `CubismFadeController.Refresh()` to `public`.
* Restructure scene maintaining Unity 2017-2019 compatibility.
* Change to manage .meta files, samples and models in the repository.
* Separate license file from `README.md`.
* Change to use gravity and wind settings from `physics3.json`.
* Reformat some code according to the coding standards.
* Change the shorten type name `Action` to full name `System.Action`.

### Fixed

* Fix occurring runtime error when importing the model exist with `.moc3.asset`.
* Fix only `EndTime` update with latest motion when motion fading.
* Fix the path of `.motion3.json` to save `.fade.asset`.
* Fix the `InstanceId`.
* Fix to reuse `InstanceId` recorded in generated `AnimationClip`.
* Fix condition to clear existing `AnimationClip` curve on reimporting.
* Fix the script of the `Original Workflow sample` scene.


## [4-beta.2] - 2019-11-14

### Fixed

* Fix sample scene sourcecode: `CubismSampleController.cs`
* Fix a bug when switching multiple motions in the same layer of `Motion` component.
* Fix priority not being set when playing idle motion.
* Fix importing process of `.pose3.json` on `Original Workflow`.


## [4-beta.1] - 2019-09-04

### Added

* Support new Inverted Masking features.
* Add `.editorconfig` and `.gitattributes` to manage file formats.
* Add `UWP/ARM64` Core binary file for experimental use.
* Add method to know playing motion or not [#35](https://github.com/Live2D/CubismUnityComponents/pull/35).
* Add sample model and sample scene.(`./Assets/Live2D/Cubism/Samples/OriginalWorkflow/DemoCubism4`)


### Changed

* Upgrade Core version to 04.00.0000 (67108864).
* Move to `Plugin/Experimental/UWP` from `Plugin/Experimental/uwp/Windows`.
* Convert all file formats according to `.editorconfig`.
* `LICENSE.txt` file has been integrated into `README.md`
* Remove changelog and regenerate `CHANGELOG.md`.
* What was `Package.json` is currently being changed to`cubism-info.yml`.
* Improve CubismUpdateController [#34](https://github.com/Live2D/CubismUnityComponents/pull/34).

### Fixed

* Fix issue of `Demo` and `Motion` sample in `OriginalWorkflow`.
* Fix issue that mesh remain when deleting model.
* Fix issue where Priority value was not reset after playing motion with CubismMotionController.


[4-r.1]: https://github.com/Live2D/CubismUnityComponents/compare/4-beta.2...4-r.1
[4-beta.2]: https://github.com/Live2D/CubismUnityComponents/compare/4-beta.1...4-beta.2
[4-beta.1]: https://github.com/Live2D/CubismUnityComponents/compare/86e5b07702f74d00b4ab52b7d6c15ba3464b8b85...4-beta.1
