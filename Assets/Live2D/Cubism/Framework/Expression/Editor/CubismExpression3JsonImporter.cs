/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Framework.Expression;
using Live2D.Cubism.Framework.Json;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Live2D.Cubism.Editor.Importers
{
    public sealed class CubismExpression3JsonImporter : CubismImporterBase
    {
        /// <summary>
        /// <see cref="CubismPose3Json"/> backing field.
        /// </summary>
        [NonSerialized]
        private CubismExp3Json _expressionJson;

        private CubismExp3Json ExpressionJson
        {
            get 
            {
                if(_expressionJson != null)
                {
                    return _expressionJson;
                }

                if(string.IsNullOrEmpty(AssetPath))
                {
                    return null;
                }

                var expressionJson = AssetDatabase.LoadAssetAtPath<TextAsset>((AssetPath));
                _expressionJson = CubismExp3Json.LoadFrom(expressionJson);

                return _expressionJson;
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
            CubismImporter.RegisterImporter<CubismExpression3JsonImporter>(".exp3.json");
        }

        #endregion

        #region Cubism Import Event Handling

        /// <summary>
        /// Imports the corresponding asset.
        /// </summary>
        public override void Import()
        {
            var oldExpressionData = AssetDatabase.LoadAssetAtPath<CubismExpressionData>(AssetPath.Replace(".exp3.json", ".exp3.asset"));

            // Create expression data.
            CubismExpressionData expressionData;

            if(oldExpressionData == null)
            {
                expressionData = CubismExpressionData.CreateInstance(ExpressionJson);
                AssetDatabase.CreateAsset(expressionData, AssetPath.Replace(".exp3.json", ".exp3.asset"));
            }
            else
            {
                expressionData = CubismExpressionData.CreateInstance(oldExpressionData, ExpressionJson);
                EditorUtility.CopySerialized(expressionData, oldExpressionData);
                expressionData = oldExpressionData;
            }

            EditorUtility.SetDirty(expressionData);

            // Add expression data to expression list.
            var directoryName = Path.GetDirectoryName(AssetPath).ToString();
            var modelDir = Path.GetDirectoryName(directoryName).ToString();
            var modelName = Path.GetFileName(modelDir).ToString();
            var expressionListPath = Path.GetDirectoryName(directoryName).ToString() + "/" + modelName + ".expressionList.asset";
            var expressionList = AssetDatabase.LoadAssetAtPath<CubismExpressionList>(expressionListPath);

            // Create expression list.
            if(expressionList == null)
            {
                expressionList = ScriptableObject.CreateInstance<CubismExpressionList>();
                expressionList.CubismExpressionObjects = new CubismExpressionData[0];
                AssetDatabase.CreateAsset(expressionList, expressionListPath);
            }

            // Finde index.
            var expressionName = Path.GetFileName(AssetPath).Replace(".json", "");
            var expressionIndex = -1;
            for (var i = 0; i < expressionList.CubismExpressionObjects.Length; ++i)
            {
                var expression = expressionList.CubismExpressionObjects[i];

                if (expression == null || expression.name != expressionName)
                {
                    continue;
                }

                expressionIndex = i;
                break;
            }

            // Set expression data.
            if (expressionIndex != -1)
            {
                expressionList.CubismExpressionObjects[expressionIndex] = expressionData;
            }
            else
            {
                expressionIndex = expressionList.CubismExpressionObjects.Length;
                Array.Resize(ref expressionList.CubismExpressionObjects, expressionIndex + 1);
                expressionList.CubismExpressionObjects[expressionIndex] = expressionData;
            }

            EditorUtility.SetDirty(expressionList);

        }

        #endregion
    }
}