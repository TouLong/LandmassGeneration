Shader "Custom/Terrain" {
	Properties {
		testTexture("Texture", 2D) = "white"{}
		testScale("Scale", Float) = 1

	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		const static int maxLayerCount = 16;
		const static float epsilon = 1E-4;

		int layerCount;
		float3 baseColors[maxLayerCount];
		float baseStartHeights[maxLayerCount];
		float baseBlends[maxLayerCount];

		float minHeight;
		float maxHeight;

		struct Input {
			float3 worldPos;
		};

		float inverseLerp(float a, float b, float value) {
			return saturate((value - a) / (b - a));
		}

		void surf(Input IN, inout SurfaceOutputStandard o) {
			float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
			//for (int i = 0; i < layerCount; i++) 
			//{
			//	float drawStrength = saturate(sign(heightPercent - baseStartHeights[i]));
			//	o.Albedo = o.Albedo * (1 - drawStrength) + baseColors[i] * drawStrength;
			//}
			for (int i = 0; i < layerCount; i++) 
			{
				float drawStrength = inverseLerp(-baseBlends[i] / 200 - epsilon, baseBlends[i] / 200, heightPercent - baseStartHeights[i]);
				o.Albedo = o.Albedo * (1 - drawStrength) + baseColors[i] * drawStrength;
			}
		}


		ENDCG
	}
	FallBack "Diffuse"
}
