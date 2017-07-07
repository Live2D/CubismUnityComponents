# The directory structure has been changed in preparation of the next release (R3) and isn't compatible with the current release (R2).

# Cubism SDK For Unity Components

Welcome to the open components of the Cubism SDK For Unity.

Go [here](http://www.live2d.com/products/cubism3) if you're looking for the official Live2D hoempage,
and [here](https://live2d.github.io) if you're looking for the download page of the SDK package.

## Structure

### Components

The components are grouped by their role,
and this grouping is reflected in both folder structure and namespaces.

#### Core Wrapper

Components and classes in this group are a shim layer for wrapping the unmanaged Cubism core library to C# and Unity and
are located in ``./Assets/Live2D/Cubism/Components/CoreWrapper``.

#### Framework

Components and classes in this group provide additional functionality like lip-syncing,
as well as integration of 'foreign' Cubism files with Unity.
Turning Cubism files into Prefabs and AnimationClips is done here.
All framework code is located in ``./Assets/Live2D/Cubism/Components/Framework``.

#### Rendering

Components and classes in this group provide the functionality for rendering Cubism models using Unity functionality and
are located in are located in ``./Assets/Live2D/Cubism/Components/Rendering``.

### Editor Extensions

Unity Editor extensions are located in ``./Assets/Live2D/Cubism/Components/Editor``.

### Resources

Resources like shaders and other assets are located in ``./Assets/Live2D/Cubism/Components/Resources``.

## Branches

If you're looking for the latest features and/or fixes, all development takes place in the ``develop`` branch.

The ``master`` branch is brought in sync with the ``develop`` branch once every official SDK release.

## Usage

Simply copy all files under ``./Assets`` into the folder the Live2D Cubism SDK is located in your Unity project.

### Unsafe Blocks

The Core wrapper requires unsafe code blocks to be allowed and the C# project Unity creates is patched accordingly.
If unsafe code isn't an option for you, currently the best way is to compile the components and drop that dll into your Unity project.

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
