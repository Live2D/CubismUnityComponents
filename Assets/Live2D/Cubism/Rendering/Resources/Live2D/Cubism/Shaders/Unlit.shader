/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


Shader "Live2D Cubism/Unlit"
{
	Properties
	{
		// Texture and model opacity settings.
		[PerRendererData] _MainTex("Main Texture", 2D) = "white" {}
		[PerRendererData] cubism_ModelOpacity("Model Opacity", Float) = 1
	

		// Blend settings.
		_SrcColor("Source Color", Int)		= 0
		_DstColor("Destination Color", Int)	= 0
		_SrcAlpha("Source Alpha", Int)		= 0
		_DstAlpha("Destination Alpha", Int) = 0


		// Mask settings.
		[Toggle(CUBISM_MASK_ON)] cubism_MaskOn("Mask?", Int) = 0
		[PerRendererData] cubism_MaskTexture("cubism_Internal", 2D) = "white" {}
		[PerRendererData] cubism_MaskTile("cubism_Internal", Vector) = (0, 0, 0, 0)
		[PerRendererData] cubism_MaskTransform("cubism_Internal", Vector) = (0, 0, 0, 0)
	}
	SubShader
	{
		Tags
		{
			"Queue"				= "Transparent"
			"IgnoreProjector"	= "True"
			"RenderType"		= "Transparent"
			"PreviewType"		= "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Cull     Off
		Lighting Off
		ZWrite   Off
		Blend    [_SrcColor][_DstColor], [_SrcAlpha][_DstAlpha]

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile CUBISM_MASK_ON CUBISM_MASK_OFF
			

			#include "UnityCG.cginc"
			#include "CubismCG.cginc"


			struct appdata
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};


			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO

				// Add Cubism specific vertex output data.
				CUBISM_VERTEX_OUTPUT
			};


			sampler2D _MainTex;
			

			// Include Cubism specific shader variables.
			CUBISM_SHADER_VARIABLES


			v2f vert (appdata IN)
			{
				v2f OUT;


				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);


				OUT.vertex	 = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color	 = IN.color;


				// Initialize Cubism specific vertex output data.
				CUBISM_INITIALIZE_VERTEX_OUTPUT(IN, OUT);


				return OUT;
			}
			

			fixed4 frag (v2f IN) : SV_Target
			{
				fixed4 OUT = tex2D(_MainTex, IN.texcoord) * IN.color;
				

				// Apply Cubism alpha to color.
				CUBISM_APPLY_ALPHA(IN, OUT);


				return OUT;
			}
			ENDCG
		}
	}
}
