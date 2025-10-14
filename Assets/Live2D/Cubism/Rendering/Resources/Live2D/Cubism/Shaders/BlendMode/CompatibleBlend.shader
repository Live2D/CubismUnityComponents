/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


Shader "Unlit/BlendMode/CompatibleBlend"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        [PerRendererData] cubism_ModelOpacity("Model Opacity", Float) = 1

        // Extension Color settings.
        [PerRendererData] cubism_MultiplyColor("Multiply Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [PerRendererData] cubism_ScreenColor("Screen Color", Color) = (0.0, 0.0, 0.0, 1.0)

        // Blend settings.
        _SrcColor("Source Color", Int) = 1
        _DstColor("Destination Color", Int) = 10
        _SrcAlpha("Source Alpha", Int) = 1
        _DstAlpha("Destination Alpha", Int) = 10

        // Culling setting.
        _Cull("Culling", Int) = 0

        [Toggle(CUBISM_MASK_ON)] cubism_MaskOn("Mask?", Int) = 0
        [Toggle(CUBISM_INVERT_ON)] cubism_InvertOn("Inverted?", Int) = 0

        [PerRendererData] cubism_MaskTexture("cubism_Internal", 2D) = "white" {}
    }
    SubShader
    {
        Blend    [_SrcColor][_DstColor], [_SrcAlpha][_DstAlpha]

        Tags {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull     [_Cull]
        Lighting Off
        ZWrite   Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ CUBISM_INVERT_ON
            #pragma multi_compile _ CUBISM_MASK_ON

            #include "UnityCG.cginc"
            #include "../CubismCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 texcoord : TEXCOORD0;
                float2 texcoord2 : TEXCOORD1;
                float4 color    : COLOR;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 cubism_MultiplyColor;
            fixed4 cubism_ScreenColor;

            #if defined(CUBISM_MASK_ON) || defined(CUBISM_INVERT_ON)
            int cubism_InvertOn;
            #endif

            // Include Cubism specific shader variables.
            CUBISM_SHADER_VARIABLES

            v2f vert (appdata IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // Setting vertex position.
                OUT.vertex = UnityObjectToClipPos(IN.vertex);

                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color;
                OUT.texcoord2 = OUT.vertex.xy * 0.5 + float2(0.5,0.5);

                #if UNITY_REVERSED_Z
                OUT.vertex.y = -OUT.vertex.y;
                #endif

                return OUT;
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                // Sample the texture
                fixed4 textureColor = tex2D(_MainTex, IN.texcoord);

                // Multiply
                textureColor.rgb *= cubism_MultiplyColor.rgb;
                // Screen
                textureColor.rgb = (textureColor.rgb + cubism_ScreenColor.rgb) - (textureColor.rgb * cubism_ScreenColor.rgb);

                fixed4 OUT = textureColor * IN.color;

#if defined(CUBISM_MASK_ON) || defined(CUBISM_INVERT_ON)
                float clippingMask = 1.0;
                clippingMask = tex2D(cubism_MaskTexture , IN.texcoord2).r;
                clippingMask = abs(cubism_InvertOn - clippingMask);
                OUT *= clippingMask;
#endif

                // Apply Cubism alpha to color.
                OUT.rgb *= OUT.a;
                OUT *= cubism_ModelOpacity;

                return OUT;
            }
            ENDCG
        }
    }
}
