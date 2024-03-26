/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System;
using UnityEngine;


namespace Live2D.Cubism.Rendering.Masking
{
    /// <summary>
    /// Virtual pool allocator for <see cref="CubismMaskTile"/>s.
    /// </summary>
    internal sealed class CubismMaskTilePool
    {
        private readonly int ClippingMaskMaxCountOnDefault = 36; // Maximum number of masks per normal frame buffer.
        private readonly int ClippingMaskMaxCountOnMultiRenderTexture = 32; // Maximum number of masks per frame buffer when there are two or more frame buffers.

        /// <summary>
        /// Level of subdivisions.
        /// </summary>
        private int Subdivisions { get; set; }

        /// <summary>
        /// Pool slots.
        /// </summary>
        /// <remarks>
        /// <see langword="true"/> slots are in use, <see langword="false"/> are available slots.
        /// </remarks>
        private bool[] Slots { get; set; }

        /// <summary>
        /// Number of <see cref="RenderTexture"/>.
        /// </summary>
        private int RenderTextureCount { get; set; }

        /// <summary>
        /// High precision mask flags.
        /// </summary>
        private bool IsUsingHighPrecisionMask { get; set; }

        /// <summary>
        /// Limit on number of clipping masks.
        /// </summary>
        private int UseClippingMaskMaxCount { get; set; }

        /// <summary>
        /// Channel count.
        /// </summary>
        private int ColorChannelCount { get; set; }

        /// <summary>
        /// Number of masks used.
        /// </summary>
        public int UsedMaskCount { get; set; }

        /// <summary>
        /// Array of structures holding information for each mask tile.
        /// </summary>
        private LayoutContext[] LayoutContexts { get; set; }

        /// <summary>
        /// <see cref="HeadOfChannels"/> backing field.
        /// </summary>
        private LayoutContext[] _headOfChannels;

        /// <summary>
        /// Top of channel.
        /// </summary>
        private LayoutContext[] HeadOfChannels
        {
            get { return _headOfChannels; }
            set { _headOfChannels = value; }
        }

        #region Ctors

        /// <summary>
        /// Initializes instance.
        /// </summary>
        /// <param name="subdivisions">Number of <see cref="CubismMaskTexture"/> subdivisions.</param>
        /// <param name="channels">Number of <see cref="CubismMaskTexture"/> color channels.</param>
        public CubismMaskTilePool(int subdivisions, int channels, int renderTextureCount = -1)
        {
            RenderTextureCount = renderTextureCount;
            ColorChannelCount = channels;

            if (RenderTextureCount < 1)
            {
#if UNITY_EDITOR
                Debug.Log("This MaskTexture use system: Subdivisions (Legacy)");
#endif

                Subdivisions = subdivisions;
                if (Subdivisions < 1)
                {
                    Subdivisions = 1;
                    Debug.LogError("`Subdivisions` must be at least 1. It will be automatically set to 1.");
                }

                Slots = new bool[(int)Mathf.Pow(4, Subdivisions) * ColorChannelCount];
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("This MaskTexture use system: Multiple RenderTexture");
#endif
                UseClippingMaskMaxCount = renderTextureCount > 1
                    ? ClippingMaskMaxCountOnMultiRenderTexture * renderTextureCount
                    : ClippingMaskMaxCountOnDefault;

                Slots = new bool[UseClippingMaskMaxCount * ColorChannelCount];
                UsedMaskCount = 0;

                LayoutContexts = new LayoutContext[UseClippingMaskMaxCount * ColorChannelCount];
                for (int layoutContextIndex = 0; layoutContextIndex < LayoutContexts.Length; layoutContextIndex++)
                {
                    LayoutContexts[layoutContextIndex] = new LayoutContext
                    {
                        RenderTextureIndex = 0,
                        Channel = 0,
                        LayoutCount = 0,
                        LayoutContextIndex = 0,
                    };
                }

                HeadOfChannels = new LayoutContext[0];
            }
        }

        #endregion

        /// <summary>
        /// Acquires tiles.
        /// </summary>
        /// <param name="count">Number of tiles to acquire.</param>
        /// <returns>Acquired tiles on success; <see langword="null"/> otherwise.</returns>
        public CubismMaskTile[] AcquireTiles(int count)
        {
            var result = new CubismMaskTile[count];


            // Populate container.
            for (var i = 0; i < count; ++i)
            {
                var allocationSuccessful = false;


                for (var j = 0; j < Slots.Length; ++j)
                {
                    // Skip occupied slots.
                    if (Slots[j])
                    {
                        continue;
                    }


                    // Generate tile.
                    result[i] = ToTile(j);


                    // Flag slot as occupied.
                    Slots[j] = true;


                    // Flag allocation as successful.
                    allocationSuccessful = true;


                    break;
                }


                // Return as soon as one allocation fails.
                if (!allocationSuccessful)
                {
                    Debug.LogError("The currently specified mask texture exceeds the number of masks that can be drawn.");
                    return null;
                }
            }


            // Return on success.
            return result;
        }

        /// <summary>
        /// Releases tiles.
        /// </summary>
        /// <param name="tiles">Tiles to release.</param>
        [Obsolete("ReturnTiles() is not used.", false)]
        public void ReturnTiles(CubismMaskTile[] tiles)
        {
            // Flag slots as available.
            for (var i = 0; i < tiles.Length; ++i)
            {
                Slots[ToIndex(tiles[i])] = false;
            }
        }


        /// <summary>
        /// Reset HeadOfChannels.
        /// </summary>
        public void ResetTiles()
        {
            HeadOfChannels = new LayoutContext[0];
            LayoutContexts = new LayoutContext[UseClippingMaskMaxCount * ColorChannelCount];
            for (var i = 0; i < Slots.Length; i++)
            {
                Slots[i] = false;
            }
        }


        /// <summary>
        /// Converts from index to <see cref="CubismMaskTile"/>.
        /// </summary>
        /// <param name="index">Index to convert.</param>
        /// <returns>Mask tile matching index.</returns>
        private CubismMaskTile ToTile(int index)
        {
            if (RenderTextureCount > 0)
            {
                if (UsedMaskCount > UseClippingMaskMaxCount)
                {
                    var overCount = UsedMaskCount - UseClippingMaskMaxCount;
                    Debug.LogError($"Not supported mask count : {overCount}\n[Details] render texture count : {RenderTextureCount}, mask count : {UsedMaskCount}");
                    return new CubismMaskTile
                    {
                        Channel = 0,
                        Column = 0,
                        Row = 0,
                        Size = 1,
                        RenderTextureIndex = 0,
                        HeadOfChannelsIndex = -1
                    };
                }

                // If there is one render texture, divide it into 9 pieces (maximum 36 pieces).
                var layoutCountMaxValue = RenderTextureCount <= 1 ? 9 : 8;

                // Lay out masks using one RenderTexture as much as possible.
                // If the number of mask groups is less than 4, place one mask for each RGBA channel; if the number is between 5 and 6, place RGBA as 2,2,1,1.

                // how many pieces to allocate per render texture (rounded up)
                var countPerSheetDiv = (UsedMaskCount + RenderTextureCount - 1) / RenderTextureCount;

                // Number of render textures that reduce the number of layouts by one (for this number of render textures)
                var reduceLayoutTextureCount = UsedMaskCount % RenderTextureCount;

                // Use RGBA in sequence.
                var divCount = countPerSheetDiv / ColorChannelCount; // Number of masks to be placed in one channel.
                var modCount = countPerSheetDiv % ColorChannelCount; // Excess. Allocate one by one to this numbered channel.

                // Start the calculation as the first index of that channel.
                if (LayoutContexts[index].LayoutCount < 1)
                {
                    var headOfChannelsIndex = (HeadOfChannels.Length == 0)
                        ? 0
                        : HeadOfChannels.Length - 1; ;
                    if (HeadOfChannels.Length < 1)
                    {
                        LayoutContexts[index].Channel = 0;
                        LayoutContexts[index].RenderTextureIndex = 0;
                    }
                    else
                    {
                        var previousUseChannel = HeadOfChannels[HeadOfChannels.Length - 1].Channel;

                        // Channel
                        LayoutContexts[index].Channel = previousUseChannel < (ColorChannelCount - 1)
                            ? (previousUseChannel + 1) : 0;

                        // RenderTextureIndex
                        LayoutContexts[index].RenderTextureIndex = previousUseChannel < (ColorChannelCount - 1)
                            ? HeadOfChannels[headOfChannelsIndex].RenderTextureIndex : HeadOfChannels[headOfChannelsIndex].RenderTextureIndex + 1;
                    }

                    // Number of layouts in this channel.
                    // NOTE: Number of layouts = basic masks to place on one channel + one additional channel to place extra masks.
                    LayoutContexts[index].LayoutCount = divCount + (LayoutContexts[index].Channel < modCount
                        ? 1
                        : 0);

                    // Determine the channel that does it when reducing the number of layouts by one.
                    // Adjust to be within the normal index range when div is 0.
                    var checkChannelIndex = modCount + (divCount < 1 ? -1 : 0);

                    // If this is the target channel and there is a render texture that reduces the number of layouts by one.
                    if (LayoutContexts[index].Channel == checkChannelIndex && reduceLayoutTextureCount > 0)
                    {
                        // If the current render texture is the target render texture, reduce the number of layouts by one.
                        LayoutContexts[index].LayoutCount -= !(LayoutContexts[index].RenderTextureIndex < reduceLayoutTextureCount)
                            ? 1
                            : 0;
                    }

                    LayoutContexts[index].LayoutContextIndex = 0;

                    // Set the required number of information.
                    for (var count = 1; count < LayoutContexts[index].LayoutCount; count++)
                    {
                        LayoutContexts[index + count].RenderTextureIndex = LayoutContexts[index].RenderTextureIndex;
                        LayoutContexts[index + count].Channel = LayoutContexts[index].Channel;
                        LayoutContexts[index + count].LayoutCount = LayoutContexts[index].LayoutCount;
                        LayoutContexts[index + count].LayoutContextIndex = count;
                    }

                    Array.Resize(ref _headOfChannels, HeadOfChannels.Length + 1);
                    HeadOfChannels[HeadOfChannels.Length - 1] = LayoutContexts[index];
                }

                var layoutCount = LayoutContexts[index].LayoutCount;

                // Set tile layout.
                if (layoutCount <= 1)
                {
                    return new CubismMaskTile
                    {
                        Channel = LayoutContexts[index].Channel,
                        Column = 0,
                        Row = 0,
                        Size = 1,
                        RenderTextureIndex = LayoutContexts[index].RenderTextureIndex,
                        Index = index
                    };
                }
                else if (layoutCount <= 4)
                {
                    var tilesPerRow = 2; // Rows per tile
                    var currentTilePosition = LayoutContexts[index].LayoutContextIndex;
                    var tileSize = 1f / (float)tilesPerRow;
                    var column = currentTilePosition / tilesPerRow;
                    var rowId = currentTilePosition % tilesPerRow;

                    return new CubismMaskTile
                    {
                        Channel = LayoutContexts[index].Channel,
                        Column = column,
                        Row = rowId,
                        Size = tileSize,
                        RenderTextureIndex = LayoutContexts[index].RenderTextureIndex,
                        Index = index
                    };
                }
                else if (layoutCount <= layoutCountMaxValue)
                {
                    var tilesPerRow = 3; // Rows per tile
                    var currentTilePosition = LayoutContexts[index].LayoutContextIndex;
                    var tileSize = 1f / (float)tilesPerRow;
                    var column = currentTilePosition / tilesPerRow;
                    var rowId = currentTilePosition % tilesPerRow;

                    return new CubismMaskTile
                    {
                        Channel = LayoutContexts[index].Channel,
                        Column = column,
                        Row = rowId,
                        Size = tileSize,
                        RenderTextureIndex = LayoutContexts[index].RenderTextureIndex,
                        Index = index
                    };
                }
                else
                {
                    var overCount = UsedMaskCount - UseClippingMaskMaxCount;
                    Debug.LogError($"Not supported mask count : {overCount}\n[Details] render texture count : {RenderTextureCount}, mask count : {UsedMaskCount}");
                    return new CubismMaskTile
                    {
                        Channel = 0,
                        Column = 0,
                        Row = 0,
                        Size = 1f,
                        RenderTextureIndex = 0,
                        HeadOfChannelsIndex = 0,
                        Index = index
                    };
                }
            }
            else
            {
                var tileCounts = (int)Mathf.Pow(4, Subdivisions - 1);
                var tilesPerRow = (int)Mathf.Pow(2, Subdivisions - 1);
                var tileSize = 1f / (float)tilesPerRow;
                var channel = index / tileCounts;
                var currentTilePosition = index - (channel * tileCounts);
                var column = currentTilePosition / tilesPerRow;
                var rowId = currentTilePosition % tilesPerRow;

                return new CubismMaskTile
                {
                    Channel = channel,
                    Column = column,
                    Row = rowId,
                    Size = tileSize,
                    RenderTextureIndex = -1,
                    HeadOfChannelsIndex = -1,
                    Index = index
                };
            }
        }

        /// <summary>
        /// Converts from <see cref="CubismMaskTile"/> to index.
        /// </summary>
        /// <param name="tile">Tile to convert.</param>
        /// <returns>Tile index.</returns>
        [Obsolete("ToIndex() is not used.", false)]
        private int ToIndex(CubismMaskTile tile)
        {
            var tilesPerRow = 0;
            var tileCounts = 0;

            if (RenderTextureCount > 0)
            {
                var countPerSheetDiv = UsedMaskCount / RenderTextureCount;
                var countPerSheetMod = UsedMaskCount % RenderTextureCount;

                // Use RGBA in sequence.
                var div = countPerSheetDiv / ColorChannelCount; // Number of masks to be placed in one channel.
                var mod = countPerSheetDiv % ColorChannelCount; // Excess. Allocate one by one to this numbered channel.

                tileCounts = div + (tile.Channel < mod ? 1 : 0);

                var checkChannelNo = mod + 1 >= ColorChannelCount ? 0 : mod + 1;
                if (tile.Channel == checkChannelNo)
                {
                    tileCounts += tile.RenderTextureIndex < countPerSheetMod ? 1 : 0;
                }

                if (tileCounts <= 1)
                {
                    tilesPerRow = 0;
                }
                else if (tileCounts <= 4)
                {
                    tilesPerRow = 2;
                }
                else if (tileCounts <= 9)
                {
                    tilesPerRow = 3;
                }

                return (int)((tile.Channel * tileCounts) + (tile.Column * tilesPerRow) + tile.Channel * (UseClippingMaskMaxCount / ColorChannelCount));
            }
            else
            {
                tileCounts = (int)Mathf.Pow(4, Subdivisions - 1);
                tilesPerRow = (int)Mathf.Pow(2, Subdivisions - 1);
                return (int)((tile.Channel * tileCounts) + (tile.Column * tilesPerRow) + tile.Row);
            }
        }

        /// <summary>
        /// The structure that holds the information for each mask tile.
        /// </summary>
        private struct LayoutContext
        {
            /// <summary>
            /// Index of the <see cref="RenderTexture"/> to which this mask is assigned.
            /// </summary>
            public int RenderTextureIndex;

            /// <summary>
            /// Index of the <see cref="ColorChannelCount"/> to which this mask is assigned.
            /// </summary>
            public int Channel;

            /// <summary>
            /// Number of channel divisions to which this mask is assigned.
            /// </summary>
            public int LayoutCount;

            /// <summary>
            /// Index within a division.
            /// </summary>
            public int LayoutContextIndex;
        }
    }
}
