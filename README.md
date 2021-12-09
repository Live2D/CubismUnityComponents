# Cubism Unity Components

Welcome to the open components of the Cubism SDK for Unity.

Go [here](https://www.live2d.com/download/cubism-sdk/download-unity/)
if you're looking for the download page of the SDK package.


## License

Please read the [license](LICENSE.md) before use.


## Notice

Please read the [notices](NOTICE.md) before use.


## Structure

### Components

The components are grouped by their role,
and this grouping is reflected in both folder structure and namespaces.

#### Core Wrapper

Components and classes in this group are a shim layer for wrapping the unmanaged Cubism core library to
C# and Unity and are located in `./Assets/Live2D/Cubism/Core`.

#### Framework

Components and classes in this group provide additional functionality like lip-syncing,
as well as integration of 'foreign' Cubism files with Unity.
Turning Cubism files into Prefabs and AnimationClips is done here.
All framework code is located in `./Assets/Live2D/Cubism/Framework`.

#### Rendering

Components and classes in this group provide the functionality for rendering Cubism models using Unity functionality
and are located in `./Assets/Live2D/Cubism/Rendering`.

### Editor Extensions

Unity Editor extensions are located in `./Assets/Live2D/Cubism/Editor`.

### Resources

Resources like shaders and other assets are located in `./Assets/Live2D/Cubism/Rendering/Resources`.


## Development environment

| Unity | Version |
| --- | --- |
| Latest | 2021.1.24f1 |
| LTS | 2020.3.19f1 |
| LTS | 2019.4.29f1 |

| Library / Tool | Version |
| --- | --- |
| Android SDK / NDK | *1 |
| Visual Studio 2019 | 16.11.7 |
| Windows SDK | 10.0.19041.0 |
| Xcode | 13.1 |

*1 Use libraries embedded with Unity or recommended.

### C# compiler

Build using Roslyn or mcs compiler supported by Unity 2018.4 and above.

Note: The mcs compiler is deprecated and we only check the build.

Please refer to the following official documentation for the versions of C# you can use.

https://docs.unity3d.com/ja/2018.4/Manual/CSharpCompiler.html


## Tested environment

| Platform | Version |
| --- | --- |
| Android | 11 |
| iOS | 15.1 |
| iPadOS | 15.1 |
| Ubuntu | 20.04.3 |
| macOS | 11.6 |
| Windows 10 | 21H2 |
| Google Chrome | 94.0.4606.81 |
| Chrome OS | 94.0.4606.124 |


## Branches

If you're looking for the latest features and/or fixes, all development takes place in the `develop` branch.

The `master` branch is brought in sync with the `develop` branch once every official SDK release.


## Usage

Simply copy all files under `./Assets` into the folder the Live2D Cubism SDK is located in your Unity project.

### Unsafe Blocks

The Core wrapper requires unsafe code blocks to be allowed and the C# project Unity creates is patched accordingly.
If unsafe code isn't an option for you, currently the best way is to compile the components and drop that dll into your Unity project.


## Contributing

There are many ways to contribute to the project:
logging bugs, submitting pull requests on this GitHub, and reporting issues and making suggestions at Live2D Community.

### Forking And Pull Requests

We very appreciate your pull requests whether they bring fixes, improvements, or even new features.
Note, however, that the wrapper is designed to be as lightweight and shallow as possible and
should therefore only be subject to bug fixes and memory/performance improvements.
To keep the main repository as clean as possible, create a personal fork and feature branches there as needed.

### Bugs

We are regularly checking issue-reports and feature requests at Live2D Community.
Before filing a bug report, please do a search in Live2D Community to see if the issue-report or feature request has already been posted.
If you find your issue already exists, make relevant comments and add your reaction.

### Suggestions

We're also interested in your feedback for the future of the SDK.
You can submit a suggestion or feature request at Live2D Community.
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
- Stay away from LINQ and prefer `for` over `foreach` anywhere else.
- Try to be explicit. Prefer `private void Update()` over `void Update()`.


## Community

If you have any questions, please join the official Live2D community and discuss with other users.
- [Live2D 公式コミュニティ(Japanese)](https://creatorsforum.live2d.com/)
- [Live2D community](http://community.live2d.com/)
