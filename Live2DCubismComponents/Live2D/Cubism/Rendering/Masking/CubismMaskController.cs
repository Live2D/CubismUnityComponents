/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System.Collections.Generic;
using Live2D.Cubism.Core;
using UnityEngine;


namespace Live2D.Cubism.Rendering.Masking
{
    /// <summary>
    /// Controls rendering of Cubism masks.
    /// </summary>
    [ExecuteInEditMode]
    public sealed class CubismMaskController : MonoBehaviour, ICubismMaskSource
    {
        /// <summary>
        /// <see cref="MaskTexture"/> backing field.
        /// </summary>
        private CubismMaskTexture _maskTexture;

        /// <summary>
        /// Mask texture.
        /// </summary>
        public CubismMaskTexture MaskTexture
        {
            get
            {
                // Fall back to global mask texture.
                if (_maskTexture == null)
                {
                    _maskTexture = CubismMaskTexture.GlobalMaskTexture;
                }


                return _maskTexture;
            }
            set
            {
                // Return early if same value given.
                if (value == _maskTexture)
                {
                    return;
                }


                _maskTexture = value;


                // Try switch mask textures.
                OnDestroy();
                OnEnable();
            }
        }


        /// <summary>
        /// <see cref="CubismMaskRenderer"/>s.
        /// </summary>
        private CubismMaskRenderer[] MaskRenderers { get; set; }


        /// <summary>
        /// True if controller is revived.
        /// </summary>
        private bool IsRevived
        {
            get { return MaskRenderers != null; }
        }


        /// <summary>
        /// Makes sure controller is initialized once.
        /// </summary>
        private void TryRevive()
        {
            if (IsRevived)
            {
                return;
            }


            ForceRevive();
        }

        /// <summary>
        /// Initializes <see cref="MaskRenderers"/>.
        /// </summary>
        private void ForceRevive()
        {
            var drawables = this
                .FindCubismModel()
                .Drawables;


            // Find mask pairs.
            var pairs = new MasksMaskedsPairs();


            for (var i = 0; i < drawables.Length; i++)
            {
                if (!drawables[i].IsMasked)
                {
                    continue;
                }


                pairs.Add(drawables[i], drawables[i].Masks);
            }


            // Create renderers for pairs.
            MaskRenderers = new CubismMaskRenderer[pairs.Entries.Count];


            for (var i = 0; i < MaskRenderers.Length; ++i)
            {
                MaskRenderers[i] = new CubismMaskRenderer(pairs.Entries[i].Masks, pairs.Entries[i].Maskeds.ToArray())
                    .SetMaskTexture(MaskTexture);
            }
        }

        #region Unity Event Handling

        /// <summary>
        /// Initializes instance.
        /// </summary>
        private void OnEnable()
        {
            // Fail silently.
            if (MaskTexture == null)
            {
                return;
            }


            MaskTexture.AddSource(this);
        }

        /// <summary>
        /// Finalizes instance.
        /// </summary>
        private void OnDestroy()
        {
            if (MaskTexture == null)
            {
                return;
            }


            MaskTexture.RemoveSource(this);
        }

        #endregion

        #region ICubismMaskSource

        /// <summary>
        /// Queries the number of tiles needed by the source.
        /// </summary>
        /// <returns>The necessary number of tiles needed.</returns>
        int ICubismMaskSource.GetNecessaryTileCount()
        {
            TryRevive();


            return MaskRenderers.Length;
        }

        /// <summary>
        /// Assigns the tiles.
        /// </summary>
        /// <param name="value">Tiles to assign.</param>
        void ICubismMaskSource.SetTiles(CubismMaskTile[] value)
        {
            for (var i = 0; i < MaskRenderers.Length; ++i)
            {
                MaskRenderers[i].SetMaskTile(value[i]);
            }
        }

        /// <summary>
        /// Called when source should instantly draw.
        /// </summary>
        void ICubismMaskSource.DrawNow()
        {
            // Draw only if enabled.
            if (!isActiveAndEnabled)
            {
                return;
            }
            
            // Dispatch event.
            for (var i = 0; i < MaskRenderers.Length; ++i)
            {
                MaskRenderers[i].DrawMasksNow();
                
            }
            
        }

        #endregion

        #region Mask-Masked Pair

        /// <summary>
        /// Pair of masks and masked drawables.
        /// </summary>
        private struct MasksMaskedsPair
        {
            /// <summary>
            /// Mask drawables.
            /// </summary>
            public CubismRenderer[] Masks;

            /// <summary>
            /// Masked drawables.
            /// </summary>
            public List<CubismRenderer> Maskeds;
        }


        private class MasksMaskedsPairs
        {
            /// <summary>
            /// List of <see cref="MasksMaskedsPair"/>
            /// </summary>
            public List<MasksMaskedsPair> Entries = new List<MasksMaskedsPair>();


            /// <summary>
            /// Add <see cref="MasksMaskedsPair"/> to the list.
            /// </summary>
            public void Add(CubismDrawable masked, CubismDrawable[] masks)
            {
                // Try to add masked to existing mask compound.
                for (var i = 0; i < Entries.Count; ++i)
                {
                    var match = (Entries[i].Masks.Length == masks.Length);


                    if (!match)
                    {
                        continue;
                    }


                    for (var j = 0; j < Entries[i].Masks.Length; ++j)
                    {
                        if (Entries[i].Masks[j] != masks[j].GetComponent<CubismRenderer>())
                        {
                            match = false;


                            break;
                        }
                    }


                    if (!match)
                    {
                        continue;
                    }


                    Entries[i].Maskeds.Add(masked.GetComponent<CubismRenderer>());


                    return;
                }


                // Create a new pair.
                var renderers = new CubismRenderer[masks.Length];


                for (var i = 0; i < masks.Length; ++i)
                {
                    renderers[i] = masks[i].GetComponent<CubismRenderer>();
                }


                Entries.Add(new MasksMaskedsPair
                {
                    Masks = renderers,
                    Maskeds = new List<CubismRenderer>() {masked.GetComponent<CubismRenderer>() }
                });
            }
        }

        #endregion
    }
}
