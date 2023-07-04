Shader "LightMeterShader" {

	Properties{

		// BRDF Lookup texture, light direction on x and curvature on y.
		_BRDFTex("BRDF Lookup (RGB)", 2D) = "gray" {}

		// Curvature scale. Multiplier for the curvature - best to keep this very low - between 0.02 and 0.002.
		_CurvatureScale("Curvature Scale", Float) = 0.005

		// Controller for fresnel specular mask. For skin, 0.028 if in linear mode, 0.2 for gamma mode.
		_Fresnel("Fresnel Value", Float) = 0.2

		_Smoothness("Smoothness Value", Float) = 0.45

		_Specular("Specular Value", Float) = 0.5
	}

		SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM

		// Physically based Standard lighting model, and enable shadows on all light types
#pragma surface surf Sensor fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
#pragma target 3.0

		half _Specular;
		half _Smoothness;
		sampler2D _BRDFTex;
	
		float _CurvatureScale;

	struct Input {
		float2 uv_MarginMask;
		float3 worldPos;
		float3 worldNormal;
		INTERNAL_DATA
	};


	struct SurfaceOutputSensor {
		fixed3 Albedo;
		fixed3 Normal;
		fixed3 Emission;
		half Smoothness;
		half Occlusion;
		fixed Alpha;
		half3 Specular;

		fixed SSS;
		fixed3 NormalBlur;
		float Curvature;
		float2 uv;
	};


	
	void surf(Input IN, inout SurfaceOutputSensor o) {

		o.Albedo = float3(1,1,1);
		o.Alpha = 1;
		o.Specular = _Specular;
		o.Smoothness = _Smoothness;
		o.SSS = 1.0f;

		//	//////////////////////////////////////////////////////////
		// 	Skin shader specific functions

		//	Get the scale of the derivatives of the blurred world normal and the world position.
#if (SHADER_TARGET > 40) //SHADER_API_D3D11
		// In DX11, ddx_fine should give nicer results.
		float deltaWorldNormal = length(abs(ddx_fine(o.Normal)) + abs(ddy_fine(blurredWorldNormal)));
		float deltaWorldPosition = length(abs(ddx_fine(IN.worldPos)) + abs(ddy_fine(IN.worldPos)));
#else
		float deltaWorldNormal = length(fwidth(o.Normal));
		float deltaWorldPosition = length(fwidth(IN.worldPos));
#endif		
		o.Curvature = (deltaWorldNormal / deltaWorldPosition) * _CurvatureScale; // * combinedMap.b;
																				 //			o.Albedo = _Color*0.5;
	}

	inline fixed4 CookTorrenceLight(SurfaceOutputSensor s, half3 viewDir, UnityGI gi) {

		//s.Normal = normalize(s.Normal);			

		half oneMinusReflectivity;
		s.Albedo = EnergyConservationBetweenDiffuseAndSpecular(s.Albedo, s.Specular, /*out*/ oneMinusReflectivity);

#define oneMinusRoughness s.Smoothness

		half diff = dot(s.Normal, gi.light.dir);
		half dotNL = max(0, diff);

#define Pi 3.14159265358979323846
#define OneOnLN2_x6 8.656170

		half dotNV = max(0, dot(s.Normal, viewDir)); 			// UNITY BRDF does not normalize(viewDir) ) );
		half3 halfDir = normalize(gi.light.dir + viewDir);
		half dotNH = max(0, dot(s.Normal, halfDir));

		half dotLH = max(0, dot(gi.light.dir, halfDir));
		
		//	////////////////////////////////////////////////////////////
		//	Cook Torrrance
		//	from The Order 1886 // http://blog.selfshadow.com/publications/s2013-shading-course/rad/s2013_pbs_rad_notes.pdf
		half alpha = 1 - s.Smoothness; // alpha is roughness
		alpha *= alpha;
		half alpha2 = alpha * alpha;

		//	Specular Normal Distribution Function: GGX Trowbridge Reitz
		half denominator = (dotNH * dotNH) * (alpha2 - 1.f) + 1.f;
		half D = alpha2 / (Pi * denominator * denominator);
		//	Geometric Shadowing: Smith
		// B. Karis, http://graphicrants.blogspot.se/2013/08/specular-brdf-reference.html
		half G_L = dotNL + sqrt((dotNL - dotNL * alpha) * dotNL + alpha);
		half G_V = dotNV + sqrt((dotNV - dotNV * alpha) * dotNV + alpha);
		half G = 1.0 / (G_V * G_L);
		//	Fresnel: Schlick / fast fresnel approximation
		half F = 1 - oneMinusReflectivity + (oneMinusReflectivity)* exp2(-OneOnLN2_x6 * dotNH);

		//	Lazarov 2013, "Getting More Physical in Call of Duty: Black Ops II", changed by EPIC
		const half4 c0 = { -1, -0.0275, -0.572, 0.022 };
		const half4 c1 = { 1, 0.0425, 1.04, -0.04 };
		half4 r = (1 - oneMinusRoughness) * c0 + c1;
		half a004 = min(r.x * r.x, exp2(-9.28 * dotNV)) * r.x + r.y;
		half2 AB = half2(-1.04, 1.04) * a004 + r.zw;
		half3 F_L = s.Specular * AB.x + AB.y;

		//	Skin Lighting
		float2 brdfUV;
		// Half-Lambert lighting value based on blurred normals.
		brdfUV.x = dotNL * 0.5 + 0.5;
		brdfUV.y = s.Curvature *dot(gi.light.color, fixed3(0.22, 0.707, 0.071));

		half3 brdf = tex2D(_BRDFTex, brdfUV).rgb;


		//	Final composition
		half4 c = 0;
		c.rgb = s.Albedo *gi.light.color *diff;// lerp(dotNL.xxx, brdf, s.SSS); // diffuse
		+D * G * F * gi.light.color * dotNL; 					 // direct specular
		+gi.indirect.specular * F_L;						     // indirect specular

		//c.rgb = brdf;
		return c;
	}

	inline fixed4 LightingSensor(SurfaceOutputSensor s, half3 viewDir, UnityGI gi)
	{
		fixed4 c = fixed4(1, 1, 1, 1);
		c = CookTorrenceLight(s, viewDir, gi);

#if defined(DIRLIGHTMAP_SEPARATE)
#ifdef LIGHTMAP_ON
		c += UnityBlinnPhongLight(s, viewDir, gi.light2);
#endif
#ifdef DYNAMICLIGHTMAP_ON
		c += UnityBlinnPhongLight(s, viewDir, gi.light3);
#endif
#endif

#ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
		c.rgb += s.Albedo *gi.indirect.diffuse;
#endif
		return c;
	}

	inline void LightingSensor_GI(
		SurfaceOutputSensor s,
		UnityGIInput data,
		inout UnityGI gi)
	{
		gi = UnityGlobalIllumination(data, s.Occlusion, s.Smoothness, s.Normal, true); // reflections = true		
	}
	ENDCG
	}
		FallBack "Diffuse"
}
