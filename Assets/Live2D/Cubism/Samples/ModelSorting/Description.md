[English](Description.md) / [日本語](Description.ja.md)

---

# Model Sorting

This sample demonstrates the drawing order settings for multiple models.  
The front-to-back relationships of the models are adjusted by setting the Grouped Sorting Index for each model.

From left to right on the screen, the settings are as follows:

- The Mao model is drawn in the background.
- The Mao model is drawn in the same group as the background Clipping model.
  - By changing the Local Order of the Clipping model's Drawables, `NoMask` and `Mask`, the model is rendered in a way that it appears between other models.
- The Mao model is drawn in the foreground.
