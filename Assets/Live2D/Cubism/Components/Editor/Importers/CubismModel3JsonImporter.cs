/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System;
using System.Collections.Generic;
using System.Linq;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Json;
using Live2D.Cubism.Framework.Physics;
using Live2D.Cubism.Rendering;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;


namespace Live2D.Cubism.Editor.Importers
{
    /// <summary>
    /// Handles importing of Cubism models.
    /// </summary>
    [Serializable]
    public sealed class CubismModel3JsonImporter : CubismImporterBase
    {
        /// <summary>
        /// <see cref="Model3Json"/> backing field.
        /// </summary>
        [NonSerialized] private CubismModel3Json _model3Json;

        /// <summary>
        ///<see cref="CubismModel3Json"/> asset.
        /// </summary>
        public CubismModel3Json Model3Json
        {
            get
            {
                if (_model3Json == null)
                {
                    _model3Json = CubismModel3Json.LoadAtPath(AssetPath);
                }


                return _model3Json;
            }
        }


        /// <summary>
        /// Guid of model prefab.
        /// </summary>
        [SerializeField] private string _modelPrefabGuid;

        /// <summary>
        /// <see cref="ModelPrefab"/> backing field.
        /// </summary>
        [NonSerialized] private GameObject _modelPrefab;

        /// <summary>
        /// Prefab of model.
        /// </summary>
        private GameObject ModelPrefab
        {
            get
            {
                if (_modelPrefab == null)
                {
                    _modelPrefab = AssetGuid.LoadAsset<GameObject>(_modelPrefabGuid);
                }


                return _modelPrefab;
            }
            set
            {
                _modelPrefab = value;
                _modelPrefabGuid = AssetGuid.GetGuid(value);
            }
        }


        /// <summary>
        /// Guid of moc.
        /// </summary>
        [SerializeField]
        private string _mocAssetGuid;

        /// <summary>
        /// <see cref="MocAsset"/> backing field.
        /// </summary>
        [NonSerialized]
        private CubismMoc _mocAsset;

        /// <summary>
        /// Moc asset.
        /// </summary>
        private CubismMoc MocAsset
        {
            get
            {
                if (_mocAsset == null)
                {
                    _mocAsset = AssetGuid.LoadAsset<CubismMoc>(_mocAssetGuid);
                }


                return _mocAsset;
            }
            set
            {
                _mocAsset = value;
                _mocAssetGuid = AssetGuid.GetGuid(value);
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
            CubismImporter.RegisterImporter<CubismModel3JsonImporter>(".model3.json");
        }

#endregion

#region CubismImporterBase

        /// <summary>
        /// Imports the corresponding asset.
        /// </summary>
        public override void Import()
        {
            var isImporterDirty = false;


            // Instantiate model source and model.
            var model = Model3Json.ToModel(CubismImporter.OnPickMaterial, CubismImporter.OnPickTexture);
            var moc = model.Moc;


            // Create moc asset.
            if (MocAsset == null)
            {
                AssetDatabase.CreateAsset(moc, AssetPath.Replace(".model3.json", ".asset"));


                MocAsset = moc;


                isImporterDirty = true;
            }

            // Update moc asset.
            else
            {
                EditorUtility.CopySerialized(moc, MocAsset);
                EditorUtility.SetDirty(MocAsset);
            }


            // Create model prefab.
            if (ModelPrefab == null)
            {
                // Trigger event.
                CubismImporter.SendModelImportEvent(this, model);


                foreach (var texture in Model3Json.Textures)
                {
                    CubismImporter.SendModelTextureImportEvent(this, model, texture);
                }


                // Create prefab and trigger saving of changes.
                ModelPrefab = PrefabUtility.CreatePrefab(AssetPath.Replace(".model3.json", ".prefab"), model.gameObject);


                isImporterDirty = true;
            }


            // Update model prefab.
            else
            {
                // Sync proxies.
                // INV  Is syncing only proxies good enough?
                var destination = Object.Instantiate(ModelPrefab).FindCubismModel();


                SyncParameters(model, destination);
                SyncParts(model, destination);
                SyncDrawablesAndRenderers(model, destination);
                SyncPhysics(model, destination);
                

                // Trigger events.
                CubismImporter.SendModelImportEvent(this, destination);


                foreach (var texture in Model3Json.Textures)
                {
                    CubismImporter.SendModelTextureImportEvent(this, destination, texture);
                }


                // Replace prefab and clean up.
                PrefabUtility.ReplacePrefab(destination.gameObject, ModelPrefab);
                Object.DestroyImmediate(destination.gameObject, true);


                // Log event.
                CubismImporter.LogReimport(AssetPath, AssetDatabase.GUIDToAssetPath(_modelPrefabGuid));
            }


            // Clean up.
            Object.DestroyImmediate(model.gameObject, true);


            // Save state and assets.
            if (isImporterDirty)
            {
                Save();
            }
            else
            {
                AssetDatabase.SaveAssets();
            }
        }

#endregion

#region Set

        /// <summary>
        /// Set.
        /// </summary>
        /// <typeparam name="T">Type of elements.</typeparam>
        private struct Set<T> where T : Component
        {
#region Factory Methods

            /// <summary>
            /// Creates a set from to arrays.
            /// </summary>
            /// <param name="a">First items.</param>
            /// <param name="b">Second items.</param>
            /// <returns>Set.</returns>
            public static Set<T> From(T[] a, T[] b)
            {
                return new Set<T>
                {
                    ANotB = a.Where(i => b.All(j => j.name != i.name)).ToArray(),
                    BNotA = b.Where(i => a.All(j => j.name != i.name)).ToArray(),
                    AAndB = a
                        .Where(i => b.Any(j => j.name == i.name))
                        .Select(i => new KeyValuePair<T, T>(i, b.First(j => j.name == i.name)))
                        .ToArray()
                };
            }

#endregion

            /// <summary>
            /// Relative complement of B in A.
            /// </summary>
            private T[] ANotB { get; set; }

            /// <summary>
            /// Relative complement of A in B.
            /// </summary>
            private T[] BNotA { get; set; }

            /// <summary>
            /// Intersection of A and B.
            /// </summary>
            public KeyValuePair<T, T>[] AAndB { get; private set; }


            /// <summary>
            /// Makes B match A.
            /// </summary>
            public void Sync()
            {
                // 'Steal' new elements.
                if (BNotA.Length > 0)
                {
                    var bRoot = BNotA[0].transform.parent.gameObject;


                    foreach (var element in ANotB)
                    {
                        element.transform.SetParent(bRoot.transform);
                    }
                }


                // Remove 'lost' elements.
                foreach (var element in BNotA)
                {
                    Object.DestroyImmediate(element.gameObject);
                }


                // Update existing elements.
                foreach (var pair in AAndB)
                {
                    EditorUtility.CopySerialized(pair.Key, pair.Value);
                }
            }
        }


        /// <summary>
        /// Syncs model parameters.
        /// </summary>
        /// <param name="source">Source.</param>
        /// <param name="destination">Destination.</param>
        private static void SyncParameters(CubismModel source, CubismModel destination)
        {
            Set<CubismParameter>
                .From(source.Parameters, destination.Parameters)
                .Sync();
        }

        /// <summary>
        /// Syncs model parts.
        /// </summary>
        /// <param name="source">Source.</param>
        /// <param name="destination"></param>
        private static void SyncParts(CubismModel source, CubismModel destination)
        {
            Set<CubismPart>
                .From(source.Parts, destination.Parts)
                .Sync();
        }

        /// <summary>
        /// Syncs model drawables (including renderers).
        /// </summary>
        /// <param name="source">Source.</param>
        /// <param name="destination">Destination.</param>
        private static void SyncDrawablesAndRenderers(CubismModel source, CubismModel destination)
        {
            var set = Set<CubismDrawable>.From(source.Drawables, destination.Drawables);


            set.Sync();


            foreach (var pair in set.AAndB)
            {
                var sourceRenderer = pair.Key.GetComponent<CubismRenderer>();
                var destinationRenderer = pair.Value.GetComponent<CubismRenderer>();


                EditorUtility.CopySerialized(sourceRenderer, destinationRenderer);
                EditorUtility.CopySerialized(sourceRenderer.GetComponent<MeshFilter>(), destinationRenderer.GetComponent<MeshFilter>());
                EditorUtility.CopySerialized(sourceRenderer.GetComponent<MeshRenderer>(), destinationRenderer.GetComponent<MeshRenderer>());
            }
        }


        /// <summary>
        /// Syncs physics.
        /// </summary>
        /// <param name="source">Source.</param>
        /// <param name="destination">Destination.</param>
        private static void SyncPhysics(CubismModel source, CubismModel destination)
        {
            // Clean up destination if required.
            var rig = destination.transform.Find("PhysicsRig");


            if (rig != null)
            {
                Object.DestroyImmediate(destination.GetComponent<CubismPhysicsController>(), true);
                Object.DestroyImmediate(rig.gameObject, true);
            }


            // Steal source physics if possible.
            rig = source.transform.Find("PhysicsRig");


            if (rig != null)
            {
                rig.SetParent(destination.transform);


                // Copy controller state.
                var sourceController = source.GetComponent<CubismPhysicsController>();
                var destinationController = destination.gameObject.AddComponent<CubismPhysicsController>();


                EditorUtility.CopySerialized(sourceController, destinationController);
                EditorUtility.SetDirty(destinationController);
            }
        }

#endregion
    }
}
