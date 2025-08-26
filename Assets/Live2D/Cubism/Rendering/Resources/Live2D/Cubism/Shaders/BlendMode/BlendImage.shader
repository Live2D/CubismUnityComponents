/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

Shader "Unlit/BlendMode/BlendImage"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture0", 2D) = "white" {}
        [PerRendererData] _RenderTexture ("Texture1", 2D) = "white" {}
        [PerRendererData] cubism_ModelOpacity("Model Opacity", Float) = 1

        // Extension Color settings.
        [PerRendererData] cubism_MultiplyColor("Multiply Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [PerRendererData] cubism_ScreenColor("Screen Color", Color) = (0.0, 0.0, 0.0, 1.0)

        // Culling setting.
        _Cull("Culling", Int) = 0

        [Toggle(CUBISM_MASK_ON)] cubism_MaskOn("Mask?", Int) = 0
        [Toggle(CUBISM_INVERT_ON)] cubism_InvertOn("Inverted?", Int) = 0

        [PerRendererData] cubism_MaskTexture("cubism_Internal", 2D) = "white" {}
    }
    SubShader
    {
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
            Blend One Zero, One Zero

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ CUBISM_INVERT_ON
            #pragma multi_compile _ CUBISM_MASK_ON

            #include "UnityCG.cginc"
            #include "../CubismCG.cginc"
            #include_with_pragmas "CubismVariants.cginc"
            #include "ColorBlendVariants.cginc"
            #include "AlphaBlendVariants.cginc"

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

            sampler2D _MainTex; // src (Drawable)
            sampler2D _RenderTexture; // dest (Background RenderTexture）
            float4 _MainTex_ST;
            float4 _RenderTexture_ST;
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
                OUT.vertex = IN.vertex;
                OUT.color = IN.color;

                OUT.texcoord = IN.texcoord;

                // Convert vertex position to UV coordinates.
                OUT.texcoord2 = IN.vertex.xy * 0.5 + float2(0.5,0.5);

                // If reversed Z is enabled, flip the Y coordinate.
                #if UNITY_REVERSED_Z
                OUT.vertex.y = -OUT.vertex.y;
                #endif

                return OUT;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Cd (Straight)
                fixed4 _tmp1 = tex2D(_RenderTexture, i.texcoord2);
                float3  Cd = _tmp1.rgb;
                float Ad = _tmp1.a;

                // Zero division check
                if (abs(Ad) < 0.00001)
                {
                    Cd = float3(0.0, 0.0, 0.0);
                }
                else
                {
                    // To Straight
                    Cd /= Ad;
                }

                // Cs (Straight)
                fixed4 _tmp2 = tex2D(_MainTex, i.texcoord);
                // Multiply
                _tmp2.rgb *= cubism_MultiplyColor.rgb;
                // Screen
                _tmp2.rgb = (_tmp2.rgb + cubism_ScreenColor.rgb) - (_tmp2.rgb * cubism_ScreenColor.rgb);

                float3 Cs = _tmp2.rgb;
                float As = _tmp2.a;

#if defined(CUBISM_MASK_ON) || defined(CUBISM_INVERT_ON)
                float clippingMask = 1.0;
                clippingMask = tex2D(cubism_MaskTexture , i.texcoord2);
                clippingMask = abs(cubism_InvertOn - clippingMask);
                As *= clippingMask;
#endif

                As *= i.color.a;


                float4 c = ALPHA_BLEND(COLOR_BLEND(Cs, Cd), Cs, As, Cd, Ad);

                // Apply Cubism alpha to color.
                c *= cubism_ModelOpacity;

               return c;
            }
            ENDCG
        }
    }
}
