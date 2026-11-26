/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Rendering;
using Live2D.Cubism.Rendering.URP.RenderingInterceptor;
using UnityEngine;

/// <summary>
/// SetUp Models for the Rendering Interceptor Sample.
/// </summary>
public class RenderingInterceptorModelsSetting : MonoBehaviour
{
    /// <summary>
    /// The default sorting mode for both models.
    /// </summary>
    [SerializeField]
    public CubismSortingMode defaultSortingMode = CubismSortingMode.BackToFrontOrder;

    /// <summary>
    /// Model Prefab for the Normal Rendering.
    /// </summary>
    [SerializeField]
    private GameObject normalModelPrefab;

    /// <summary>
    /// Model Prefab for the Interceptor Rendering.
    /// </summary>
    [SerializeField]
    private GameObject interceptorModelPrefab;

    /// <summary>
    /// The Animator Controller for <see cref="interceptorModelPrefab"/>.
    /// </summary>
    [SerializeField]
    private RuntimeAnimatorController interceptorModelAnimatorController;

    /// <summary>
    /// The instance of the normal model prefab.
    /// </summary>
    private GameObject normalModelInstance;

    /// <summary>
    /// The instance of the interceptor model prefab.
    /// </summary>
    private GameObject interceptorModelInstance;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    private void Awake()
    {
        if (!normalModelPrefab)
        {
            Debug.LogWarning("normalModelPrefab is not set.");
            return;
        }

        if (!interceptorModelPrefab)
        {
            Debug.LogWarning("interceptorModelPrefab is not set.");
            return;
        }

        if (!interceptorModelAnimatorController)
        {
            Debug.LogWarning("interceptorModelAnimatorController is not set.");
            return;
        }

        normalModelInstance = Instantiate(normalModelPrefab);
        interceptorModelInstance = Instantiate(interceptorModelPrefab);

        var normalModelRenderController = normalModelInstance.GetComponent<CubismRenderController>();
        var interceptorModelRenderController = interceptorModelInstance.GetComponent<CubismRenderController>();

        if (!normalModelRenderController || !interceptorModelRenderController)
        {
            Debug.LogWarning("normalModelPrefab or interceptorModelPrefab does not have a CubismRenderController component.");
            return;
        }

        // Set the sorting mode for both models to BackToFrontOrder.
        normalModelRenderController.SortingMode = defaultSortingMode;
        interceptorModelRenderController.SortingMode = defaultSortingMode;

        // Set the GroupedSortingIndex for both models to 0.
        normalModelRenderController.GroupedSortingIndex = 0;
        interceptorModelRenderController.GroupedSortingIndex = 0;

        // Set the Animator Controller for the interceptor model prefab.
        var animator = interceptorModelInstance.GetComponent<Animator>();
        if (animator)
        {
            animator.runtimeAnimatorController = interceptorModelAnimatorController;
        }
        else
        {
            interceptorModelInstance.AddComponent<Animator>().runtimeAnimatorController = interceptorModelAnimatorController;
        }

        var interceptorController = interceptorModelInstance.GetComponent<CubismRenderingInterceptController>();

        if (!interceptorController)
        {
            interceptorController = interceptorModelInstance.AddComponent<CubismRenderingInterceptController>();
        }

        // Set the InvokeTimingSetting to PostRendering and the InterceptingMode to SortingOrder.
        interceptorController.InvokeTimingSetting = CubismRenderingInterceptController.InvokeTiming.PostRendering;
        interceptorController.Mode = interceptorModelRenderController.SortingMode.SortByDepth()
            ? CubismRenderingInterceptController.InterceptingMode.ZDepth
            : CubismRenderingInterceptController.InterceptingMode.SortingOrder;
        interceptorController.GroupSortingOrder = 0;

        interceptorModelInstance.transform.position = new Vector3(0.2f, 0.0f, 0.0f);
    }
}
