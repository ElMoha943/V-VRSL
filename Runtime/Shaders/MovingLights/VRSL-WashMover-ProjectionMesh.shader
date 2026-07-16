Shader "VRSL/Wash Mover/Projection"
{

	Properties
	{
		 [HideInInspector]_DMXChannel ("Starting Channel", Int) = 0
		 [HideInInspector][Toggle] _NineUniverseMode ("Extended Universe Mode", Int) = 0
		 [HideInInspector][Toggle] _PanInvert ("Invert Mover Pan", Int) = 0
		 [HideInInspector][Toggle] _TiltInvert ("Invert Mover Tilt", Int) = 0

		 [Toggle] _EnableVerticalMode ("Enable Vertical Mode", Int) = 0
		 [Toggle] _EnableStrobe ("Enable Strobe", Int) = 0
		 [Toggle] _EnableDMX ("Enable Stream DMX/DMX Control", Int) = 0
		 [Toggle] _LegacyGoboRange ("Enable Legacy GOBO Range", Int) = 0
		 [HideInInspector]_FixtureBaseRotationY("Mover Pan Offset (Blue + Green)", Range(-540,540)) = 0
		 [HideInInspector]_FixtureRotationX("Mover Tilt Offset (Blue)", Range(-180,180)) = 0
		 [HideInInspector]_ProjectionSelection ("GOBO Selection", Range(0,8)) = 0
		 [HideInInspector]_StrobeFreq("Strobe Frequency", Range(0,25)) = 1
		 [HideInInspector][Toggle] _EnableSpin("Enable Auto Spinning", Float) = 0

		[HideInInspector]_FinalIntensity("Final Intensity", Range(0,1)) = 1
		_GlobalIntensity("Global Intensity", Range(0,1)) = 1
		_GlobalIntensityBlend("Global Intensity Blend", Range(0,1)) = 1
		_UniversalIntensity ("Universal Intensity", Range (0,1)) = 1
		[HDR]_Emission("Light Color Tint", Color) = (1,1,1,1)
		[Toggle] _EnableStaticEmissionColor("Enable Static Emission Color", Int) = 0
		_Saturation("Final Saturation", Range(0,1)) = 1
		_SaturationLength("Final Saturation Length", Range(0,0.2)) = 0.1
		_LensMaxBrightness("Lens Max Brightness", Range(0.01, 10)) = 5
		_ConeWidth("Cone Width", Range(0,5.5)) = 0
		_ConeLength("Cone Length", Range(1,10)) = 1
		_ConeSync ("Cone Scale Sync", Range(0,1)) = 0.2
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc ("Source Blend mode", Float) = 2
		[Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend Operation", Float) = 0
		_FixtureRotationOrigin("Fixture Pivot Origin", Float) = (0, 0.014709, -1.02868, 0)
		[Toggle] _UseRawGrid("Use Raw Grid For Light Intensity And Color", Int) = 0
		[Toggle] _EnableAudioReact ("Enable AudioLink Audio React", Int) = 0
		[NoScaleOffset] _AudioSpectrum("AudioSpectrum", 2D) = "black" {}
		_MaxMinPanAngle("Max/Min Pan Angle (-x, x)", Float) = 180
		_MaxMinTiltAngle("Max/Min Tilt Angle (-y, y)", Float) = 180

		_FixtureMaxIntensity ("Maximum Cone Intensity",Range (0,0.5)) = 0.5

		_ProjectionRotation("Static Projection UV Rotation", Range(-180, 180)) = 0
		_SpinSpeed ("Auto Spin Speed", Range(0, 10)) = 0
		_ProjectionIntensity ("Projection Intensity", Range(0,20)) = 0
		_ProjectionFade("Projection Edge Fade", Range(0,10)) = 0
		_ProjectionFadeCurve("Projection Edge Fade Harshness", Range(0, 10)) = 1
		_ProjectionDistanceFallOff("Projection Distance Fallof Strength", Range(0.005,0.5)) = 0.05
		_ProjectionRange ("Projection Drawing Range", Range(0,60)) = 0
		_ProjectionRangeOrigin ("Projection Drawing Range Scale Origin", Float) = (0, -0.07535, 0.12387, 0)
		_ProjectionShadowHarshness("Projection Shadow Harshness", Range(0,1)) = 0


		[NoScaleOffset] _ProjectionMainTex ("Projection Texture GOBO 1", 2D) = "white"{}
		_ProjectionUVMod ("Projection UV Scale Modifier 1", Range(0,2)) = 0
		_ProjectionUVMod2 ("Projection UV Scale Modifier 2", Range(0,2)) = 0
		_ProjectionUVMod3 ("Projection UV Scale Modifier 3", Range(0,2)) = 0
		_ProjectionUVMod4 ("Projection UV Scale Modifier 4", Range(0,2)) = 0
		_ProjectionUVMod5 ("Projection UV Scale Modifier 5", Range(0,2)) = 0
		_ProjectionUVMod6 ("Projection UV Scale Modifier 6", Range(0,2)) = 0	
		_ProjectionUVMod7 ("Projection UV Scale Modifier 7", Range(0,2)) = 0
		_ProjectionUVMod8 ("Projection UV Scale Modifier 8", Range(0,2)) = 0	

		_MinimumBeamRadius ("Minimum Beam Radius", Range(0.001,1)) = 1
		_RedMultiplier ("Red Channel Multiplier", Range(0, 5)) = 1
		_GreenMultiplier ("Green Channel Multiplier", Range(0, 5)) = 1
		_BlueMultiplier ("Blue Channel Multiplier", Range(0,5)) = 1

		_ProjectionCutoff("Projection Cutoff Point", Range(0,1)) = 0.25
		_ProjectionOriginCutoff("Projection Origin Cutoff Point", Range(0,3)) = 0.25
		
		[Enum(Transparent,1,AlphaToCoverage,2)] _RenderMode ("Render Mode", Int) = 1
        [Enum(Off,0,On,1)] _ZWrite ("Z Write", Int) = 0
		[Enum(Off,0,On,1)] _AlphaToCoverage ("Alpha To Coverage", Int) = 0
        [Enum(Off,0,One,1)] _BlendDst ("Destination Blend mode", Float) = 1
		[Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend Operation", Float) = 0
        _ClippingThreshold ("Clipping Threshold", Range (0,1)) = 0.5
	[Enum(Off,0,On,1)] _MultiSampleDepth ("Multi Sample Depth", Int) = 1



	}
    SubShader
	{
		
		Tags{ "Queue" = "Transparent+1" "IgnoreProjector"="True" "RenderType" = "Transparent" }

		Pass
         {
			AlphaToMask [_AlphaToCoverage]  
            Cull Front
            Ztest GEqual
            ZWrite Off
            Blend  DstColor [_BlendDst]
            BlendOp Add
			Offset -1, -1
			Lighting Off

			Stencil
			{
				Ref 142
				Comp NotEqual
				Pass Keep
			}
			Tags{ "LightMode" = "Always" }
            CGPROGRAM
			
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile_local _ _ALPHATEST_ON
			#pragma shader_feature_local _MULTISAMPLEDEPTH
			#pragma multi_compile_instancing
			#pragma instancing_options assumeuniformscaling
			#define PROJECTION_YES //To identify the pass in the vert/frag shaders
			#define PROJECTION_MOVER
			#define WASH
			#define VRSL_DMX

            #include "UnityCG.cginc"
			#include "../Shared/VRSL-Defines.cginc" //Property Defines are here
			

             struct appdata
             {
                 float4 vertex : POSITION;
                 float2 uv : TEXCOORD0;
                 float3 texcoord : TEXCOORD1;
				 float4 color : COLOR;
				 float3 normal : NORMAL;
				 float3 tangent : TANGENT;
				 float4 projectionorigin : TEXCOORD2;
				 UNITY_VERTEX_INPUT_INSTANCE_ID
             };
             struct v2f
             {
                 float4 pos : SV_POSITION;
                 float3 ray : TEXCOORD2;
                 float4 screenPos : TEXCOORD4;
				 float4 color : COLOR;
				 float4 worldDirection : TEXCOORD6;
				 float4 intensityStrobeWidth : TEXCOORD9;
				 half4 projectionRotationSinCos : TEXCOORD10;
				 half2 projectionUVRotationSinCos : TEXCOORD11;
				 float4 rgbColor : TEXCOORD12;
				 float4 emissionColor : TEXCOORD13;
				 UNITY_VERTEX_INPUT_INSTANCE_ID
				 UNITY_VERTEX_OUTPUT_STEREO 
             };
			#include "../Shared/VRSL-DMXFunctions.cginc" //Custom Functions
			#include "VRSL-StandardMover-ProjectionFrag.cginc" //Fragment Shader is here
			#include "VRSL-StandardMover-Vertex.cginc" //Vertex Shader is here

			 ENDCG

		 }


	}
	//CustomEditor "MoverProjectionLightCustomGUI"
	//CustomEditor "MoverProjectionLightCustomGUI"
	CustomEditor "VRSLInspector"
}
