/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Framework.Json;
using System;
using UnityEditor;
using UnityEngine;


namespace Live2D.Cubism.Editor.Importers
{
    /// <summary>
    /// Handles importing of Cubism motions.
    /// </summary>
    [Serializable]
    public sealed class CubismMotion3JsonImporter : CubismImporterBase
    {
        /// <summary>
        /// <see cref="Motion3Json"/> backing field.
        /// </summary>
        [NonSerialized]
        private CubismMotion3Json _motion3Json;

        /// <summary>
        ///<see cref="CubismMotion3Json"/> asset.
        /// </summary>
        public CubismMotion3Json Motion3Json
        {
            get
            {
                if (_motion3Json == null)
                {
                    _motion3Json = CubismMotion3Json.LoadFrom(AssetDatabase.LoadAssetAtPath<TextAsset>((AssetPath)));
                }


                return _motion3Json;
            }
        }


        /// <summary>
        /// GUID of generated clip.
        /// </summary>
        [SerializeField] private string _animationClipGuid;

        /// <summary>
        /// <see cref="AnimationClip"/> backing field.
        /// </summary>
        [NonSerialized] private AnimationClip _animationClip;

        /// <summary>
        /// Gets the moc3 importer.
        /// </summary>
        private AnimationClip AnimationClip
        {
            get
            {
                if (_animationClip == null)
                {
                    _animationClip = AssetGuid.LoadAsset<AnimationClip>(_animationClipGuid);
                }


                return _animationClip;
            }
            set
            {
                _animationClip = value;
                _animationClipGuid = AssetGuid.GetGuid(value);
            }
        }

        #region Unity Event Handling

        /// <summary>
        /// Registers importer.
        /// </summary>
        [InitializeOnLoadMethod]
        // ReSharper disable once UnusedMember.Local
        private static void RegisterImporter()
        {
            CubismImporter.RegisterImporter<CubismMotion3JsonImporter>(".motion3.json");
        }

        #endregion

        #region CubismImporterBase

        /// <summary>
        /// Imports the corresponding asset.
        /// </summary>
        public override void Import()
        {
            var isImporterDirty = false;
            

            // Convert motion.
            var animationClip = Motion3Json.ToAnimationClip();


            // Create animation clip.
            if (AnimationClip == null)
            {
                AssetDatabase.CreateAsset(animationClip, AssetPath.Replace(".motion3.json", ".anim"));


                AnimationClip = animationClip;


                isImporterDirty = true;
            }

            // Update animation clip.
            else
            {
                EditorUtility.CopySerialized(animationClip, AnimationClip);
                EditorUtility.SetDirty(AnimationClip);


                // Log event.
                CubismImporter.LogReimport(AssetPath, AssetDatabase.GUIDToAssetPath(_animationClipGuid));
            }


            // Trigger event.
            CubismImporter.SendMotionImportEvent(this, animationClip);


            // Apply changes.
            if (isImporterDirty)
            {
                Save();
            }
            else
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        #endregion
    }
}
