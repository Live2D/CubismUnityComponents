/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Framework.EditorExtensions.Importers;
using UnityEditor;


namespace Live2D.Cubism.Framework.EditorExtensions
{
    /// <summary>
    /// Hooks into Unity's asset pipeline allowing custom processing of assets.
    /// </summary>
    public class CubismAssetProcessor : AssetPostprocessor
    {
        #region Unity Event Handling

        /// <summary>
        /// Called by Unity on asset import. Handles importing of Cubism related assets.
        /// </summary>
        /// <param name="importedAssetPaths">Paths of imported assets.</param>
        /// <param name="deletedAssetPaths">Paths of removed assets.</param>
        /// <param name="movedAssetPaths">Paths of moved assets</param>
        /// <param name="movedFromAssetPaths">Paths of moved assets before moving</param>
        private static void OnPostprocessAllAssets(
            string[] importedAssetPaths,
            string[] deletedAssetPaths,
            string[] movedAssetPaths,
            string[] movedFromAssetPaths)
        {
            foreach (var assetPath in importedAssetPaths)
            {
                var importer = CubismImporter.GetImporterAtPath(assetPath);


                if (importer == null)
                {
                    continue;
                }


                importer.Import();
            }
        }

        #endregion
    }
}
