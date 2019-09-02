# Perspective Camera

The default z offsetting of Cubism model parts won't work well with certain perspective camera settings.
If you want to use perspective cameras and don't get the expected results using the default z offsetting,
switch to another sorting mode using ``CubismRenderController.SortingMode``.

This sample shows how to have a model face a camera by overwriting the ``CubismRenderController.CameraToFace``.
