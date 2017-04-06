# Cubism SDK For Unity Components

Welcome to the open components of the Cubism SDK For Unity.

Go [here](http://www.live2d.com/products/cubism3) if you're looking for the official Live2D hoempage,
and [here](https://live2d.github.io) if you're looking for the download page of the SDK package.

This repository is basically made up of 3 parts:

1. The runtime components found located in ``./Live2DCubismComponents``.
1. Unity Editor extensions for the runtime components located in ``./Live2DCubismComponents_EditorExtensions``.
1. Unity resources used by the runtime components located in ``./Live2DCubismComponents_Resources``.

The runtime components are grouped by their role,
and this grouping is reflected in both folder structure and namespaces.

## Components

### Core Wrapper

Components and classes in this group are a shim layer for wrapping the unmanaged Cubism core library to C# and Unity.

### Framework

Components and classes in this group provide additional functionality like lip-syncing,
as well as integration of 'foreign' Cubism files with Unity.
Turning Cubism files into Prefabs and AnimationClips is done here.

### Rendering

Components and classes in this group provide the functionality for rendering Cubism models using Unity functionality.

## Building

Building can be easily done on Windows using the included build scripts.
Run ``./BuildRelease.bat`` for release builds; ``./BuildDebug.bat`` for debug builds.

When build scripts are run from the repository root directory,
you'll find the build results output in the ``./Build`` directory.
From there you can simply take them into Unity.

### Pre-requisites

#### MSBuild

The build scripts rely on MSBuild, so make sure to run them from a
[developer command prompt](https://msdn.microsoft.com/en-us/library/ms229859(v=vs.110).aspx).

#### Unity Libraries

The projects rely on the ``UnityEngine.dll`` and ``UnityEditor.dll``, and
expect them to be located in ``./UnityDlls``.
Check [this post](https://forum.unity3d.com/threads/where-is-unityengine-dll-and-unityeditor-dll.103433/)
if you're unsure on where to find them.

### Why don't you compile all code?

We'd love to compile all code, but Unity's builtin JSON parser
[doesn't allow deserialization of classes inside of dlls](https://issuetracker.unity3d.com/issues/json-jsonutility-dot-tojson-does-not-return-data-of-class-inside-dll-in-json-format),
which framework features rely on.

Therefore, currently only the unsafe Core wrapper is getting compiled.


## Contributing

There are many ways to contribute to the project:
logging bugs, submitting pull requests, reporting issues, and creating suggestions.

### Forking And Pull Requests

We very appreciate your pull requests whether they bring fixes, improvements, or even new features.  
Note, however, that the wrapper is designed to be as lightweight and shallow as possible and
should therefore only be subject to bug fixes and memory/performance improvements.  
To keep the main repository as clean as possible, create a personal fork and feature branches there as needed.

### Bugs

All issues and feature requests are tracked using the GitHub issue tracker for this repository.
Before filing a bug report, please do a search in open issues to see if the issue or feature request has already been filed.
If you find your issue already exists, make relevant comments and add your reaction.

### Suggestions

We're also interested in your feedback for the future of the SDK.
You can submit a suggestion or feature request through the issue tracker.
To make this process more effective, we're asking that these include more information
to help define them more clearly.

### Discussion Etiquette

Please limit the discussion to English and keep it professional and things on topic.

## Coding Guidelines

### Naming

Try to stick to the [Microsoft guidelines](https://msdn.microsoft.com/en-us/library/ms229002(v=vs.110).aspx) whenever possible.
We name private fields in lower-camelcase starting with an underscore.

### Style

- In Unity Editor extension, try to write expressive code with LINQ and all the other fancy stuff.
- Stay away from LINQ and prefer ``for`` over ``foreach`` anywhere else.
- Try to be explicit. Prefer ``private void Update()`` over ``void Update()``.

## License

The license that applies to the source code in this project allows you modify all sources
without the need to submit any changes you made.
Whenever releasing a product using source code from this project,
you just have to make sure that you link your product with the Core distributed with the SDK package.
Refer to [this license](http://live2d.com/eula/live2d-open-software-license-agreement_en.html) for the gritty details.
