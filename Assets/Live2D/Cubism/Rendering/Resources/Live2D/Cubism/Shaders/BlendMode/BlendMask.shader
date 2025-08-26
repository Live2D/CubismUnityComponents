/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


Shader "Unlit/BlendMode/BlendMask"
{
    Properties
    {
        [PerRendererData] _MainTex("Mask Texture", 2D) = "white" {}

        // Culling setting.
        _Cull("Culling", Int) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        LOD      100
        ZWrite   Off
        Lighting Off
        Cull     [_Cull]
        Blend    One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // setting vertex position and UV coordinates.
                OUT.vertex = IN.vertex;
                OUT.uv = IN.uv;

                // If reversed Z is enabled, flip the Y coordinate.
                #if UNITY_REVERSED_Z
                OUT.vertex.y = -OUT.vertex.y;
                #endif

                return OUT;
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                // Draw to the red channel because this is a high-precision mask method
                fixed4 col = float4(1.0, 0.0, 0.0, 1.0) * tex2D(_MainTex, IN.uv).a;

                return col;
            }
            ENDCG
        }
    }
}
