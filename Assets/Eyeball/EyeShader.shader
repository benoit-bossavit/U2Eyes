// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "EyeShader" {

	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_BumpTex ("Bump", 2D) = "bump" {}
		_GlossTex ("Glossiness", 2D) = "white" {}
		_RefractiveIdx ("Refractive index", Range(1,2)) = 1.3
		_PupilSize ("Pupil size change", Range(-1,1)) = 0
		_LookAt("Look at", Vector) = (0,0,1)
		[MaterialToggle] _ApplySymetry("Apply symetry", Float) = 0
		_IrisRotationAngle("Iris rotation", Range(-180,180)) = 0
		_ScleraRotationAngle("Sclera rotation", Range(-15,15)) = 0
	}
	
	SubShader {
		Tags { "RenderType" = "Opaque" }
		CGPROGRAM
		
		// Physically based Standard lighting model, and enable shadows on all light types
		#include "UnityPBSLighting.cginc"
		#pragma surface surf Standard fullforwardshadows keepalpha 
//		vertex:vert
//		#include "Tessellation.cginc"
		
		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		
		struct Input {			
			float2 uv_MainTex;
			float3 worldPos;	
			INTERNAL_DATA
		};
		
		sampler2D _MainTex;
		sampler2D _BumpTex;
		sampler2D _GlossTex;
		float _RefractiveIdx, _PupilSize;
		float3 _LookAt;
		bool _ApplySymetry;
		float _IrisRotationAngle;
		float _ScleraRotationAngle;
		
		float2 RotateTextureInDegrees(float2 uvs, float degrees)
		{
			float sina, cosa;
			sincos(degrees * UNITY_PI / 180.0f, sina, cosa);
			float2x2 m = float2x2(cosa, -sina, sina, cosa);
			return mul(m, uvs);
		}
		
		
		float2 ApplyEyeTransformation(float2 uvs)
		{
			//move the centre of the eye to the position (0,0)
			float2 uv = uvs - float2(0.5, 0.5);

			//apply symetry
			if (_ApplySymetry == 1)
				uv.x *= -1;

			//rotate iris and sclera
			float irisLimit = 0.1414;
			float scleraLimit = 0.385;
			float size = length(uv);			
			if (size < irisLimit)
				uv = RotateTextureInDegrees(uv, _IrisRotationAngle);			
			else if (size <= scleraLimit)
				uv = RotateTextureInDegrees(uv, _ScleraRotationAngle);

			//move back the centre of the eye to the position (0.5,0.5)
			return uv + float2(0.5, 0.5);
		}


		void surf (Input IN, inout SurfaceOutputStandard o) {
						
			o.Normal = UnpackNormal (tex2D (_BumpTex, IN.uv_MainTex));
			
			float3 worldNormal = normalize(WorldNormalVector(IN, float3(0.0,0.0,1)));
			float3 viewDir =  _LookAt - IN.worldPos;
		
			float3 frontNormalW = normalize(
				mul(unity_ObjectToWorld, float4(0.0,0.0,1.0,0)));
			
			float heightW = saturate(dot(
				IN.worldPos - mul(unity_ObjectToWorld, float4(IN.worldPos.x,0.0,0.0109,1)),
				frontNormalW));
			
			float3 refractedW = refract(
				normalize(viewDir)*-1,
				normalize(worldNormal),
				1.0/_RefractiveIdx);
		
			float cosAlpha = dot(frontNormalW, -refractedW);
			float dist = heightW / cosAlpha;
			float3 offsetW = dist * refractedW;
			float3 offsetL = mul(unity_WorldToObject, float4(offsetW,0));
			
			// clamp offset to 12mm in total to avoid over-refraction
			offsetL = clamp(offsetL, float3(-0.006,-0.006,-0.006), float3(0.006,0.006,0.006));
						
			float2 uv = ApplyEyeTransformation(IN.uv_MainTex);

			float2 offsetL2 = float2(offsetL.x, offsetL.y);
			uv += float2(-1.0, 1.0)*offsetL2 * float2(24,24);
			
			float2 offset_from_centre = (float2(0.5, 0.5) - uv) * heightW;
			uv += offset_from_centre * _PupilSize * 1; //there was a factor 3 before!					

			o.Albedo = tex2D(_MainTex, uv).rgb * 0.85;
			o.Smoothness = saturate(tex2D(_GlossTex, uv).rgb * 1.2);
			o.Alpha = 0.5f;
		}
		
		ENDCG
	} 
	Fallback "Diffuse"
}
