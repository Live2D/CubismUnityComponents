# Look At

This sample demonstrates the look functionality.
Looking must be set up manually, but setting it up isn't to cumbersome.
You simply have to add a ``CubismLookController`` component to the model game object,
and ``CubismLookParameter``s to any parameter you want to have participate in the look motion.

You're model will follow any ``ICubismLookTarget`` you assign to ``CubismLookContoller.Target``.
