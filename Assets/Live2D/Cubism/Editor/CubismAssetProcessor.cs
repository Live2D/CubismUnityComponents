/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Editor.Deleters;
using Live2D.Cubism.Editor.Importers;
using Live2D.Cubism.Rendering;
using Live2D.Cubism.Rendering.Masking;
using Live2D.Cubism.Rendering.Util;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


namespace Live2D.Cubism.Editor
{
    /// <summary>
    /// Hooks into Unity's asset pipeline allowing custom processing of assets.
    /// </summary>
    public class CubismAssetProcessor : AssetPostprocessor
    {
        #region Unity Event Handling

#if !UNITY_2017_3_OR_NEWER
        /// <summary>
        /// Called by Unity. Makes sure <see langword="unsafe"/> code is allowed.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static void OnGeneratedCSProjectFiles()
        {
            AllowUnsafeCode();
        }
#endif


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
            // Make sure builtin resources are available.
            GenerateBuiltinResources();

            var assetList = CubismCreatedAssetList.GetInstance();

            // Handle any imported Cubism assets.
            foreach (var assetPath in importedAssetPaths)
            {
                var importer = CubismImporter.GetImporterAtPath(assetPath);

                if (importer == null)
                {
                    continue;
                }
                
                if (assetPath.StartsWith("Assets/StreamingAssets/"))
                {
                    Debug.LogWarning("CubismAssetProcessor : Skipping import of " + assetPath);
                    continue;
                }
                
                try
                {
                    importer.Import();
                }
                catch(Exception e)
                {
                    Debug.LogError("CubismAssetProcessor : Following error occurred while importing " + assetPath);
                    Debug.LogError(e);
                }
            }

            assetList.OnPostImport();

            // Handle any deleted Cubism assets.
            foreach (var assetPath in deletedAssetPaths)
            {
                var deleter = CubismDeleter.GetDeleterAsPath(assetPath);

                if (deleter == null)
                {
                    continue;
                }

                deleter.Delete();
            }

        }

        #endregion

        #region C# Project Patching

        /// <summary>
        /// Makes sure <see langword="unsafe"/> code is allowed in the runtime project.
        /// </summary>
        private static void AllowUnsafeCode()
        {
            foreach (var csproj in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj"))
            {
                // Skip Editor assembly.
                if (csproj.EndsWith(".Editor.csproj"))
                {
                    continue;
                }


                var document = XDocument.Load(csproj);
                var project = document.Root;


                // Allow unsafe code.
                for (var propertyGroup = project.FirstNode as XElement; propertyGroup != null; propertyGroup = propertyGroup.NextNode as XElement)
                {
                    // Skip non-relevant groups.
                    if (!propertyGroup.ToString().Contains("PropertyGroup") || !propertyGroup.ToString().Contains("$(Configuration)|$(Platform)"))
                    {
                        continue;
                    }


                    // Add unsafe-block element if necessary.
                    if (!propertyGroup.ToString().Contains("AllowUnsafeBlocks"))
                    {
                        var nameSpace = propertyGroup.GetDefaultNamespace();


                        propertyGroup.Add(new XElement(nameSpace + "AllowUnsafeBlocks", "true"));
                    }


                    // Make sure unsafe-block element is always set to true.
                    for (var allowUnsafeBlocks = propertyGroup.FirstNode as XElement; allowUnsafeBlocks != null; allowUnsafeBlocks = allowUnsafeBlocks.NextNode as XElement)
                    {
                        if (!allowUnsafeBlocks.ToString().Contains("AllowUnsafeBlocks"))
                        {
                            continue;
                        }


                        allowUnsafeBlocks.SetValue("true");
                    }
                }


                // Store changes.
                document.Save(csproj);
            }
        }

        #endregion

        #region Resources Generation

        /// <summary>
        /// Sets Cubism-style normal blending for a material.
        /// </summary>
        /// <param name="material">Material to set up.</param>
        private static void EnableNormalBlending(Material material)
        {
            material.SetInt("_SrcColor", (int)BlendMode.One);
            material.SetInt("_DstColor", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_SrcAlpha", (int)BlendMode.One);
            material.SetInt("_DstAlpha", (int)BlendMode.OneMinusSrcAlpha);
        }

        /// <summary>
        /// Sets Cubism-style additive blending for a material.
        /// </summary>
        /// <param name="material">Material to set up.</param>
        private static void EnableAdditiveBlending(Material material)
        {
            material.SetInt("_SrcColor", (int)BlendMode.One);
            material.SetInt("_DstColor", (int)BlendMode.One);
            material.SetInt("_SrcAlpha", (int)BlendMode.Zero);
            material.SetInt("_DstAlpha", (int)BlendMode.One);
        }

        /// <summary>
        /// Sets Cubism-style multiplicative blending for a material.
        /// </summary>
        /// <param name="material">Material to set up.</param>
        private static void EnableMultiplicativeBlending(Material material)
        {
            material.SetInt("_SrcColor", (int)BlendMode.DstColor);
            material.SetInt("_DstColor", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_SrcAlpha", (int)BlendMode.Zero);
            material.SetInt("_DstAlpha", (int)BlendMode.One);
        }

        /// <summary>
        /// Sets Cubism-style culling for a mask material.
        /// </summary>
        /// <param name="material">Material to set up.</param>
        private static void EnableCulling(Material material)
        {
            material.SetInt("_Cull", (int)CullMode.Front);
        }

        /// <summary>
        /// Enables Cubism-style masking for a material.
        /// </summary>
        /// <param name="material">Material to set up.</param>
        private static void EnableMasking(Material material)
        {
            // Set toggle.
            material.SetInt("cubism_MaskOn", 1);


            // Enable keyword.
            var shaderKeywords = material.shaderKeywords.ToList();


            shaderKeywords.Clear();


            if (!shaderKeywords.Contains("CUBISM_MASK_ON"))
            {
                shaderKeywords.Add("CUBISM_MASK_ON");
            }


            material.shaderKeywords = shaderKeywords.ToArray();
        }

        /// <summary>
        /// Enables Cubism-style inverted mask for a material.
        /// </summary>
        /// <param name="material">Material to set up.</param>
        private static void EnableInvertedMask(Material material)
        {
            // Set toggle.
            material.SetInt("cubism_MaskOn", 1);
            material.SetInt("cubism_InvertOn", 1);


            // Enable keyword.
            var shaderKeywords = material.shaderKeywords.ToList();

            shaderKeywords.Clear();

            if (!shaderKeywords.Contains("CUBISM_INVERT_ON"))
            {
                shaderKeywords.Add("CUBISM_INVERT_ON");
            }


            material.shaderKeywords = shaderKeywords.ToArray();
        }

        /// <summary>
        /// Generates the builtin resources as necessary.
        /// </summary>
        private static void GenerateBuiltinResources()
        {
            var resourcesRoot = AssetDatabase
                .GetAssetPath(CubismBuiltinShaders.Unlit)
                .Replace("/Shaders/Unlit.shader", "");

            var materialsRoot = resourcesRoot + "/Materials";

            // Make sure materials folder exists.
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), materialsRoot)))
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), materialsRoot));
            }

            // Create materials.
            if (CubismBuiltinMaterials.Mask == null)
            {
                // Create mask material.
                var material = new Material (CubismBuiltinShaders.Mask)
                {
                    name = "Mask"
                };


                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                // Create mask material.
                material = new Material (CubismBuiltinShaders.Mask)
                {
                    name = "MaskCulling"
                };

                EnableCulling(material);
                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                // Create non-masked materials.
                material = new Material (CubismBuiltinShaders.Unlit)
                {
                    name = "Unlit"
                };

                EnableNormalBlending(material);
                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                material = new Material (CubismBuiltinShaders.Unlit)
                {
                    name = "UnlitAdditive"
                };

                EnableAdditiveBlending(material);
                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                material = new Material (CubismBuiltinShaders.Unlit)
                {
                    name = "UnlitMultiply"
                };

                EnableMultiplicativeBlending(material);
                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                // Create masked materials.
                material = new Material (CubismBuiltinShaders.Unlit)
                {
                    name = "UnlitMasked"
                };

                EnableNormalBlending(material);
                EnableMasking(material);
                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                material = new Material (CubismBuiltinShaders.Unlit)
                {
                    name = "UnlitAdditiveMasked"
                };

                EnableAdditiveBlending(material);
                EnableMasking(material);
                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                material = new Material (CubismBuiltinShaders.Unlit)
                {
                    name = "UnlitMultiplyMasked"
                };

                EnableMultiplicativeBlending(material);
                EnableMasking(material);
                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                // Create inverted mask materials.
                material = new Material (CubismBuiltinShaders.Unlit)
                {
                    name = "UnlitMaskedInverted"
                };

                EnableNormalBlending(material);
                EnableInvertedMask(material);
                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                material = new Material (CubismBuiltinShaders.Unlit)
                {
                    name = "UnlitAdditiveMaskedInverted"
                };

                EnableAdditiveBlending(material);
                EnableInvertedMask(material);
                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                material = new Material (CubismBuiltinShaders.Unlit)
                {
                    name = "UnlitMultiplyMaskedInverted"
                };

                EnableMultiplicativeBlending(material);
                EnableInvertedMask(material);
                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                // Create non-masked materials.
                material = new Material(CubismBuiltinShaders.Unlit)
                {
                    name = "UnlitCulling"
                };

                EnableNormalBlending(material);
                EnableCulling(material);
                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                material = new Material(CubismBuiltinShaders.Unlit)
                {
                    name = "UnlitAdditiveCulling"
                };

                EnableAdditiveBlending(material);
                EnableCulling(material);
                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                material = new Material(CubismBuiltinShaders.Unlit)
                {
                    name = "UnlitMultiplyCulling"
                };

                EnableMultiplicativeBlending(material);
                EnableCulling(material);
                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                // Create masked materials.
                material = new Material(CubismBuiltinShaders.Unlit)
                {
                    name = "UnlitMaskedCulling"
                };

                EnableNormalBlending(material);
                EnableMasking(material);
                EnableCulling(material);
                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                material = new Material(CubismBuiltinShaders.Unlit)
                {
                    name = "UnlitAdditiveMaskedCulling"
                };

                EnableAdditiveBlending(material);
                EnableMasking(material);
                EnableCulling(material);
                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                material = new Material(CubismBuiltinShaders.Unlit)
                {
                    name = "UnlitMultiplyMaskedCulling"
                };

                EnableMultiplicativeBlending(material);
                EnableMasking(material);
                EnableCulling(material);
                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                // Create inverted mask materials.
                material = new Material(CubismBuiltinShaders.Unlit)
                {
                    name = "UnlitMaskedInvertedCulling"
                };

                EnableNormalBlending(material);
                EnableInvertedMask(material);
                EnableCulling(material);
                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                material = new Material(CubismBuiltinShaders.Unlit)
                {
                    name = "UnlitAdditiveMaskedInvertedCulling"
                };

                EnableAdditiveBlending(material);
                EnableInvertedMask(material);
                EnableCulling(material);
                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                material = new Material(CubismBuiltinShaders.Unlit)
                {
                    name = "UnlitMultiplyMaskedInvertedCulling"
                };

                EnableMultiplicativeBlending(material);
                EnableInvertedMask(material);
                EnableCulling(material);
                AssetDatabase.CreateAsset(material, string.Format("{0}/{1}.mat", materialsRoot, material.name));


                EditorUtility.SetDirty(CubismBuiltinShaders.Unlit);
                AssetDatabase.SaveAssets();
            }

            #region Cubism 5.3

            var materialsDir = materialsRoot + "/BlendMode";

            // Make sure materials folder exists.
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), materialsDir)))
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), materialsDir));
            }

            GenerateBlendModeMaterials(materialsDir);

            #endregion

            // Create global mask texture.
            if (CubismMaskTexture.GlobalMaskTexture == null)
            {
                var globalMaskTexture = ScriptableObject.CreateInstance<CubismMaskTexture>();

                globalMaskTexture.name = "GlobalMaskTexture";


                AssetDatabase.CreateAsset(globalMaskTexture, string.Format("{0}/{1}.asset", resourcesRoot, globalMaskTexture.name));
            }
        }

        #region Cubism 5.3

        /// <summary>
        /// Enable compatible blend mode for Cubism 5.3.
        /// </summary>
        /// <param name="material">Material</param>
        /// <param name="colorBlend">Blend mode</param>
        private static void EnableCompatibleBlending(Material material, BlendTypes.ColorBlend colorBlend)
        {
            switch (colorBlend)
            {
                case BlendTypes.ColorBlend.Normal:
                    EnableNormalBlending(material);
                    break;
                case BlendTypes.ColorBlend.Add:
                    EnableAdditiveBlending(material);
                    break;
                case BlendTypes.ColorBlend.Multiply:
                    EnableMultiplicativeBlending(material);
                    break;
                default:
                    Debug.LogError("Unknown compatible blend type: " + colorBlend);
                    break;
            }
        }

        /// <summary>
        /// Enables the variables for a material based on the blend mode indices.
        /// </summary>
        /// <param name="material">Material</param>
        /// <param name="colorBlendIndex">ColorBlend index</param>
        /// <param name="alphaBlendIndex">AlphaBlend index</param>
        private static void EnableVariables(Material material, int colorBlendIndex, int alphaBlendIndex)
        {
            // Enable keyword.
            material.EnableKeyword("COLOR_BLEND_" + ((BlendTypes.ColorBlend)colorBlendIndex).ToString().ToUpper());
            material.EnableKeyword("ALPHA_BLEND_" + ((BlendTypes.AlphaBlend)alphaBlendIndex).ToString().ToUpper());
        }

        /// <summary>
        /// Loads a material from the given path.
        /// </summary>
        /// <param name="path">material's path.</param>
        /// <returns>Material</returns>
        private static Material LoadMaterialFromPath(string path)
        {
            return Resources.Load<Material>(path);
        }

        /// <summary>
        /// Generates material for ModelCanvas.
        /// </summary>
        /// <param name="materialsDir"></param>
        public static void GeneratePlaneMaterial(string materialsDir)
        {
            var materialName = "UnlitPlane";

            // Create non-masked materials.
            if (LoadMaterialFromPath("Live2D/Cubism/Materials/BlendMode/" + materialName) == null)
            {
                // Create non-masked materials.
                var material = new Material(CubismBuiltinShaders.Plane)
                {
                    name = materialName
                };

                AssetDatabase.CreateAsset(material, $"{materialsDir}/{material.name}.mat");
            }

            EditorUtility.SetDirty(CubismBuiltinShaders.Plane);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Generates a blend mask material.
        /// </summary>
        /// <param name="materialsDir"></param>
        private static void GenerateBlendMaskMaterial(string materialsDir)
        {
            var materialName = "UnlitBlendMask";

            // Create non-masked materials.
            if (LoadMaterialFromPath("Live2D/Cubism/Materials/BlendMode/" + materialName) == null)
            {
                // Create non-masked materials.
                var material = new Material(CubismBuiltinShaders.BlendMask)
                {
                    name = materialName
                };

                AssetDatabase.CreateAsset(material, $"{materialsDir}/{material.name}.mat");
            }

            // Create culling material.
            materialName += "Culling";
            if (LoadMaterialFromPath("Live2D/Cubism/Materials/BlendMode/" + materialName) == null)
            {
                // Create non-masked materials.
                var material = new Material(CubismBuiltinShaders.BlendMask)
                {
                    name = materialName
                };

                AssetDatabase.CreateAsset(material, $"{materialsDir}/{material.name}.mat");
            }

            EditorUtility.SetDirty(CubismBuiltinShaders.BlendMask);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Generates materials for blend mode.
        /// </summary>
        /// <param name="materialsDir"></param>
        private static void GenerateBlendModeMaterials(string materialsDir)
        {
            // Create plane material.
            GeneratePlaneMaterial(materialsDir);

            // マスク用シェーダーのマテリアルを作成
            GenerateBlendMaskMaterial(materialsDir);

            // Create blend mode materials.
            for (var colorBlendIndex = 0; colorBlendIndex < (int)BlendTypes.ColorBlend.End; colorBlendIndex++)
            {
                for (var alphaBlendIndex = 0; alphaBlendIndex < (int)BlendTypes.AlphaBlend.End; alphaBlendIndex++)
                {
                    // This case uses other shader.
                    if ((BlendTypes.ColorBlend)colorBlendIndex == BlendTypes.ColorBlend.Normal
                        && (BlendTypes.AlphaBlend)alphaBlendIndex == BlendTypes.AlphaBlend.Over)
                    {
                        GenerateBlendModeMaterial(CubismBuiltinShaders.CompatibleBlend, materialsDir, colorBlendIndex, alphaBlendIndex);
                        GenerateOffscreenMaterial(CubismBuiltinShaders.OffscreenCompatibleBlend, materialsDir, colorBlendIndex,
                            alphaBlendIndex);

                        continue;
                    }

                    // Combined blend modes.
                    if ((BlendTypes.ColorBlend)colorBlendIndex == BlendTypes.ColorBlend.Add
                        || (BlendTypes.ColorBlend)colorBlendIndex == BlendTypes.ColorBlend.Multiply)
                    {
                        GenerateCompatibleBlendModeMaterial(CubismBuiltinShaders.CompatibleBlend, materialsDir, colorBlendIndex);
                        GenerateOffscreenCompatibleMaterial(CubismBuiltinShaders.OffscreenCompatibleBlend, materialsDir, colorBlendIndex);
                        continue;
                    }

                    GenerateBlendModeMaterial(CubismBuiltinShaders.BlendMode, materialsDir, colorBlendIndex, alphaBlendIndex);
                    GenerateOffscreenMaterial(CubismBuiltinShaders.OffscreenBlend, materialsDir, colorBlendIndex,
                        alphaBlendIndex);
                }
            }

            EditorUtility.SetDirty(CubismBuiltinShaders.CompatibleBlend);
            EditorUtility.SetDirty(CubismBuiltinShaders.BlendMode);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Generates a blend mode material for compatible blend mode.
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="materialsDir"></param>
        /// <param name="colorBlendIndex"></param>
        private static void GenerateCompatibleBlendModeMaterial(Shader shader, string materialsDir, int colorBlendIndex)
        {
            var materialName =
                $"UnlitBlendMode{(BlendTypes.ColorBlend)colorBlendIndex}";
            var maskedMaterialName =
                $"UnlitBlendModeMasked{(BlendTypes.ColorBlend)colorBlendIndex}";
            var invertMaskedMaterialName =
                $"UnlitBlendModeInvertMasked{(BlendTypes.ColorBlend)colorBlendIndex}";

            // Generate drawable materials.
            GenerateCompatibleMaterial(materialName, maskedMaterialName, invertMaskedMaterialName,
                shader, materialsDir, colorBlendIndex);
        }

        /// <summary>
        /// Generates offscreen materials for compatible blend mode.
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="materialsDir"></param>
        /// <param name="colorBlendIndex"></param>
        private static void GenerateOffscreenCompatibleMaterial(Shader shader, string materialsDir, int colorBlendIndex)
        {
            var blendMode =
                $"{(BlendTypes.ColorBlend)colorBlendIndex}";
            var materialName =
                $"UnlitOffscreen{blendMode}";
            var maskedMaterialName =
                $"UnlitOffscreenMasked{blendMode}";
            var invertMaskedMaterialName =
                $"UnlitOffscreenInvertMasked{blendMode}";

            // Generate drawable materials.
            GenerateCompatibleMaterial(materialName, maskedMaterialName, invertMaskedMaterialName,
                shader, materialsDir, colorBlendIndex);
        }

        /// <summary>
        /// Generates a material for compatible blend mode.
        /// </summary>
        /// <param name="materialName"></param>
        /// <param name="maskedMaterialName"></param>
        /// <param name="invertMaskedMaterialName"></param>
        /// <param name="shader"></param>
        /// <param name="materialsDir"></param>
        /// <param name="colorBlendIndex"></param>
        private static void GenerateCompatibleMaterial(string materialName, string maskedMaterialName, string invertMaskedMaterialName,
            Shader shader, string materialsDir, int colorBlendIndex)
        {
            // 非マスクのシェーダーのマテリアルを作成
            if (LoadMaterialFromPath("Live2D/Cubism/Materials/BlendMode/" + materialName) == null)
            {
                // Create non-masked materials.
                var material = new Material(shader)
                {
                    name = materialName
                };

                EnableCompatibleBlending(material, (BlendTypes.ColorBlend)colorBlendIndex);
                AssetDatabase.CreateAsset(material, $"{materialsDir}/{material.name}.mat");
            }

            // Create non-masked materials culling.
            materialName += "Culling";
            if (LoadMaterialFromPath("Live2D/Cubism/Materials/BlendMode/" + materialName) == null)
            {
                // Create non-masked materials.
                var material = new Material(shader)
                {
                    name = materialName
                };

                EnableCompatibleBlending(material, (BlendTypes.ColorBlend)colorBlendIndex);
                EnableCulling(material);
                AssetDatabase.CreateAsset(material, $"{materialsDir}/{material.name}.mat");
            }

            // Create masked materials.
            if (LoadMaterialFromPath("Live2D/Cubism/Materials/BlendMode/" + maskedMaterialName) == null)
            {
                // Create masked materials.
                var material = new Material(shader)
                {
                    name = maskedMaterialName
                };

                EnableMasking(material);
                EnableCompatibleBlending(material, (BlendTypes.ColorBlend)colorBlendIndex);
                AssetDatabase.CreateAsset(material, $"{materialsDir}/{material.name}.mat");
            }

            // Create masked materials culling.
            maskedMaterialName += "Culling";
            if (LoadMaterialFromPath("Live2D/Cubism/Materials/BlendMode/" + maskedMaterialName) == null)
            {
                // Create masked materials.
                var material = new Material(shader)
                {
                    name = maskedMaterialName
                };

                EnableMasking(material);
                EnableCompatibleBlending(material, (BlendTypes.ColorBlend)colorBlendIndex);
                EnableCulling(material);
                AssetDatabase.CreateAsset(material, $"{materialsDir}/{material.name}.mat");
            }

            // Create invert masked materials.
            if (LoadMaterialFromPath("Live2D/Cubism/Materials/BlendMode/" + invertMaskedMaterialName) == null)
            {
                // Create invert masked materials.
                var material = new Material(shader)
                {
                    name = invertMaskedMaterialName
                };

                EnableInvertedMask(material);
                EnableCompatibleBlending(material, (BlendTypes.ColorBlend)colorBlendIndex);
                AssetDatabase.CreateAsset(material, $"{materialsDir}/{material.name}.mat");
            }

            // Create invert masked materials culling.
            invertMaskedMaterialName += "Culling";
            if (LoadMaterialFromPath("Live2D/Cubism/Materials/BlendMode/" + invertMaskedMaterialName) == null)
            {
                // Create invert masked materials.
                var material = new Material(shader)
                {
                    name = invertMaskedMaterialName
                };

                EnableInvertedMask(material);
                EnableCompatibleBlending(material, (BlendTypes.ColorBlend)colorBlendIndex);
                EnableCulling(material);
                AssetDatabase.CreateAsset(material, $"{materialsDir}/{material.name}.mat");
            }
        }

        /// <summary>
        /// Generates blend mode materials.
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="materialsDir"></param>
        /// <param name="colorBlendIndex"></param>
        /// <param name="alphaBlendIndex"></param>
        private static void GenerateBlendModeMaterial(Shader shader, string materialsDir, int colorBlendIndex, int alphaBlendIndex)
        {
            var blendMode =
                $"{(BlendTypes.ColorBlend)colorBlendIndex}{(BlendTypes.AlphaBlend)alphaBlendIndex}";

            var materialName =
                $"UnlitBlendMode{blendMode}";
            var maskedMaterialName =
                $"UnlitBlendModeMasked{blendMode}";
            var invertMaskedMaterialName =
                $"UnlitBlendModeInvertMasked{blendMode}";

            // Generate drawable materials.
            GenerateMaterialSection(materialName, maskedMaterialName, invertMaskedMaterialName,
                shader, materialsDir, colorBlendIndex, alphaBlendIndex);
        }

        /// <summary>
        /// Generates offscreen materials for rendering.
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="materialsDir"></param>
        /// <param name="colorBlendIndex"></param>
        /// <param name="alphaBlendIndex"></param>
        private static void GenerateOffscreenMaterial(Shader shader, string materialsDir, int colorBlendIndex, int alphaBlendIndex)
        {
            var blendMode =
                $"{(BlendTypes.ColorBlend)colorBlendIndex}{(BlendTypes.AlphaBlend)alphaBlendIndex}";
            var materialName =
                $"UnlitOffscreen{blendMode}";
            var maskedMaterialName =
                $"UnlitOffscreenMasked{blendMode}";
            var invertMaskedMaterialName =
                $"UnlitOffscreenInvertMasked{blendMode}";

            // Generate compatible materials.
            GenerateMaterialSection(materialName, maskedMaterialName, invertMaskedMaterialName,
                shader, materialsDir, colorBlendIndex, alphaBlendIndex);
        }

        /// <summary>
        /// Generates a material for blend mode materials.
        /// </summary>
        /// <param name="materialName"></param>
        /// <param name="maskedMaterialName"></param>
        /// <param name="invertMaskedMaterialName"></param>
        /// <param name="shader"></param>
        /// <param name="materialsDir"></param>
        /// <param name="colorBlendIndex"></param>
        /// <param name="alphaBlendIndex"></param>
        private static void GenerateMaterialSection(string materialName, string maskedMaterialName, string invertMaskedMaterialName,
            Shader shader, string materialsDir, int colorBlendIndex, int alphaBlendIndex)
        {
            // Create non-masked materials.
            if (LoadMaterialFromPath("Live2D/Cubism/Materials/BlendMode/" + materialName) == null)
            {
                // Create non-masked materials.
                var material = new Material(shader)
                {
                    name = materialName
                };

                EnableVariables(material, colorBlendIndex, alphaBlendIndex);
                AssetDatabase.CreateAsset(material, $"{materialsDir}/{material.name}.mat");
            }

            // Create non-masked materials culling.
            materialName += "Culling";
            if (LoadMaterialFromPath("Live2D/Cubism/Materials/BlendMode/" + materialName) == null)
            {
                // Create non-masked materials.
                var material = new Material(shader)
                {
                    name = materialName
                };

                EnableVariables(material, colorBlendIndex, alphaBlendIndex);
                EnableCulling(material);
                AssetDatabase.CreateAsset(material, $"{materialsDir}/{material.name}.mat");
            }

            // Create masked materials.
            if (LoadMaterialFromPath("Live2D/Cubism/Materials/BlendMode/" + maskedMaterialName) == null)
            {
                // Create masked materials.
                var material = new Material(shader)
                {
                    name = maskedMaterialName
                };

                EnableMasking(material);
                EnableVariables(material, colorBlendIndex, alphaBlendIndex);
                AssetDatabase.CreateAsset(material, $"{materialsDir}/{material.name}.mat");
            }

            // マスクドシェーダーのカリングマテリアルを作成
            maskedMaterialName += "Culling";
            if (LoadMaterialFromPath("Live2D/Cubism/Materials/BlendMode/" + maskedMaterialName) == null)
            {
                // Create masked materials.
                var material = new Material(shader)
                {
                    name = maskedMaterialName
                };

                EnableMasking(material);
                EnableVariables(material, colorBlendIndex, alphaBlendIndex);
                EnableCulling(material);
                AssetDatabase.CreateAsset(material, $"{materialsDir}/{material.name}.mat");
            }

            // Create invert masked materials.
            if (LoadMaterialFromPath("Live2D/Cubism/Materials/BlendMode/" + invertMaskedMaterialName) == null)
            {
                // Create invert masked materials.
                var material = new Material(shader)
                {
                    name = invertMaskedMaterialName
                };

                EnableInvertedMask(material);
                EnableVariables(material, colorBlendIndex, alphaBlendIndex);
                AssetDatabase.CreateAsset(material, $"{materialsDir}/{material.name}.mat");
            }

            // Create invert masked materials culling.
            invertMaskedMaterialName += "Culling";
            if (LoadMaterialFromPath("Live2D/Cubism/Materials/BlendMode/" + invertMaskedMaterialName) == null)
            {
                // Create invert masked materials.
                var material = new Material(shader)
                {
                    name = invertMaskedMaterialName
                };

                EnableInvertedMask(material);
                EnableVariables(material, colorBlendIndex, alphaBlendIndex);
                EnableCulling(material);
                AssetDatabase.CreateAsset(material, $"{materialsDir}/{material.name}.mat");
            }
        }

        #endregion

        #endregion
    }
}
