/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


#ifndef CUBISM_CG_INCLUDED
#define CUBISM_CG_INCLUDED


// Applies the affine (scale, offset) mask transform to a model-space vertex.
inline float2 CubismMaskUv(float2 vertex, float4 transform)
{
    return vertex * transform.xy + transform.zw;
}

// Sums the selected channel(s) of the mask texture at the given UV.
// The mask buffer uses the Original Workflow subtractive convention (cleared to white, drawn with
// dst *= 1 - src), so the sampled value is inverted here: covered -> 1, uncovered -> 0.
inline float CubismSampleMask(sampler2D tex, float2 uv, float4 channelFlag)
{
    float4 texel = (1.0 - tex2D(tex, uv)) * channelFlag;
    float maskVal = texel.r + texel.g + texel.b + texel.a;


    // Clip masked draws that overflow the mask rect: a sample outside the mask UV [0,1] range contributes
    // no coverage. Needed by the high-precision path, whose rect is sized to the mask drawables and can be
    // smaller than the masked draw. A no-op for the atlas path, where each drawable maps inside its layout tile.
    maskVal *= step(0.0, uv.x) * step(0.0, uv.y) * step(uv.x, 1.0) * step(uv.y, 1.0);


    return maskVal;
}

// Region test for mask generation: 1 inside the layout tile, 0 outside.
// region is (left, bottom, right, top) in clip space.
inline float CubismMaskRegionCoverage(float2 clip, float4 region)
{
    return step(region.x, clip.x)
        * step(region.y, clip.y)
        * step(clip.x, region.z)
        * step(clip.y, region.w);
}


// Usage: CUBISM_MATERIAL_TEXTURES_BEGIN, the texture declarations, CUBISM_MATERIAL_VARIABLES_BEGIN,
// the remaining material variables, CBUFFER_END.
#if defined(SHADER_API_PSSL)
#define CUBISM_MATERIAL_TEXTURES_BEGIN
#define CUBISM_MATERIAL_VARIABLES_BEGIN CBUFFER_START(UnityPerMaterial)
#else
#define CUBISM_MATERIAL_TEXTURES_BEGIN CBUFFER_START(UnityPerMaterial)
#define CUBISM_MATERIAL_VARIABLES_BEGIN
#endif


#if defined (CUBISM_MASK_ON) || defined(CUBISM_INVERT_ON)
// Textures must be declared outside of constant buffers (unsupported on PSSL),
// so they are separated from the other uniforms.
#define CUBISM_MASK_SHADER_TEXTURES     \
    sampler2D cubism_MaskTexture;

#define CUBISM_MASK_SHADER_UNIFORMS     \
    float4 cubism_MaskUvTransform;      \
    float4 cubism_ChannelFlag;          \
    float4 cubism_MaskRegion;

#define CUBISM_MASK_SHADER_VARIABLES    \
    CUBISM_MASK_SHADER_TEXTURES         \
    CUBISM_MASK_SHADER_UNIFORMS


// Mask generation. cubism_MaskUv holds the unflipped clip position for the region test;
// OUT.vertex additionally applies the render-texture Y orientation (_ProjectionParams.x).
#define CUBISM_TO_MASK_CLIP_POS(IN, OUT)                                            \
    OUT.cubism_MaskUv = CubismMaskUv(IN.vertex.xy, cubism_MaskUvTransform);         \
    OUT.vertex = float4(OUT.cubism_MaskUv, 0.0, 1.0);                               \
    OUT.vertex.y *= _ProjectionParams.x;
#define CUBISM_MASK_CHANNEL(IN) (cubism_ChannelFlag * CubismMaskRegionCoverage(IN.cubism_MaskUv, cubism_MaskRegion))


// Masked drawable. cubism_MaskUv maps the vertex into the mask texture's [0,1] UV space.
#define CUBISM_VERTEX_OUTPUT float2 cubism_MaskUv : TEXCOORD3;
#define CUBISM_INITIALIZE_VERTEX_OUTPUT(IN, OUT) OUT.cubism_MaskUv = CubismMaskUv(IN.vertex.xy, cubism_MaskUvTransform);


#if defined(CUBISM_INVERT_ON)
#define CUBISM_APPLY_MASK(IN, COLOR)                                                            \
    float cubism_maskVal = CubismSampleMask(cubism_MaskTexture, IN.cubism_MaskUv, cubism_ChannelFlag); \
    COLOR *= 1.0 - cubism_maskVal;
#else
#define CUBISM_APPLY_MASK(IN, COLOR)                                                            \
    float cubism_maskVal = CubismSampleMask(cubism_MaskTexture, IN.cubism_MaskUv, cubism_ChannelFlag); \
    COLOR *= cubism_maskVal;
#endif
#else
#define CUBISM_MASK_SHADER_TEXTURES
#define CUBISM_MASK_SHADER_UNIFORMS
#define CUBISM_MASK_SHADER_VARIABLES


#define CUBISM_TO_MASK_CLIP_POS(IN, OUT)
#define CUBISM_MASK_CHANNEL(IN) float4(1, 1, 1, 1)


#define CUBISM_VERTEX_OUTPUT
#define CUBISM_INITIALIZE_VERTEX_OUTPUT(IN, OUT)


#define CUBISM_APPLY_MASK(IN, COLOR)
#endif


// Texture declarations only. Use outside of CBUFFER_START/CBUFFER_END.
#define CUBISM_SHADER_TEXTURES      \
    CUBISM_MASK_SHADER_TEXTURES

// Non-texture uniform declarations only. Safe to use inside CBUFFER_START/CBUFFER_END.
#define CUBISM_SHADER_UNIFORMS      \
    float cubism_ModelOpacity;      \
    CUBISM_MASK_SHADER_UNIFORMS

// All shader variables. Use outside of CBUFFER_START/CBUFFER_END.
#define CUBISM_SHADER_VARIABLES     \
    CUBISM_SHADER_TEXTURES          \
    CUBISM_SHADER_UNIFORMS

#define CUBISM_APPLY_ALPHA(IN, COLOR)   \
    COLOR.rgb *= COLOR.a;               \
    CUBISM_APPLY_MASK(IN, COLOR);       \
    COLOR *= cubism_ModelOpacity;


#endif
