// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X
Shader "Particles/MeltSoft"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[Enum(UnityEngine.Rendering.CullMode)]_Cull("剔除方式", Float) = 0
		_MainTex("主贴图", 2D) = "white" {}
		[HDR]_TintColor("色调", Color) = (1,1,1,1)
		_MainSpeed("主贴图流动速度xy", Vector) = (0,0,0,0)
		_MaskTex0("遮罩图1", 2D) = "white" {}
		[Enum(R,0,A,1)]_Mask0Channel("遮罩图1通道选择", Float) = 0
		_Mask0Soft("遮罩图1软硬", Range( 1 , 10)) = 1
		_Mask0Range("遮罩图1范围", Range( 0.5 , 1)) = 0.5
		_MaskTex1("遮罩图2", 2D) = "white" {}
		[Enum(R,0,A,1)]_Mask1Channel("遮罩图2通道选择", Float) = 0
		_Mask1Soft("遮罩图2软硬", Range( 1 , 10)) = 1
		_Mask1Range("遮罩图2范围", Range( 0.5 , 1)) = 0.5
		_MaskStrenth("遮罩强度", Range( 1 , 10)) = 1
		_MaskSpeed("遮罩流动速度xy:图1 zw:图2", Vector) = (0,0,0,0)
		_MeltTex("溶解贴图", 2D) = "white" {}
		_MeltSpeed("溶解图流动速度xy", Vector) = (0,0,0,0)
		_Melt("溶解进度", Range( 0 , 1)) = 0
		_Hard("溶解硬度", Range( 1 , 30)) = 1
		_MeltBorder("溶解边框", Range( -1 , 1)) = -1
		[HDR]_MeltBorderColor("溶解边框颜色", Color) = (0,0,0,0)
		_MoveToCamera("移向摄像机", Range( -20 , 20)) = 0
		_SoftParticle("软粒子", Range( 0 , 1)) = 0

	    [HideInInspector] _GameSpeed("GameSpeed", Float) = 1
		[HideInInspector]_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		[HideInInspector]_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		[HideInInspector]_TessMin( "Tess Min Distance", Float ) = 10
		[HideInInspector]_TessMax( "Tess Max Distance", Float ) = 25
		[HideInInspector]_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		[HideInInspector]_TessMaxDisp( "Tess Max Displacement", Float ) = 25
	}

	SubShader
	{
		LOD 0


		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+50" }

		Cull [_Cull]
		HLSLINCLUDE
		#pragma target 2.0

		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}

		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		ENDHLSL


		Pass
		{

			Name "Forward"
			Tags { "LightMode"="UniversalForward" }

			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			ZTest LEqual
			Offset 0,0
			ColorMask RGBA


			HLSLPROGRAM
			#define _RECEIVE_SHADOWS_OFF 1
			#pragma multi_compile_instancing
			#define ASE_SRP_VERSION 999999
			#define REQUIRE_DEPTH_TEXTURE 1

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			#if ASE_SRP_VERSION <= 70108
			#define REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
			#endif

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_FRAG_COLOR


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				#ifdef ASE_FOG
				float fogFactor : TEXCOORD2;
				#endif
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_color : COLOR;
				float4 ase_texcoord4 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _MeltBorderColor;
			half4 _TintColor;
			float4 _MainTex_ST;
			float4 _MeltTex_ST;
			float4 _MaskTex1_ST;
			float4 _MaskSpeed;
			float4 _MaskTex0_ST;
			float2 _MainSpeed;
			float2 _MeltSpeed;
			float _Cull;
			float _Mask1Range;
			float _Mask1Soft;
			float _Mask1Channel;
			float _Mask0Range;
			half _Hard;
			float _Mask0Channel;
			float _MaskStrenth;
			half _Melt;
			half _MeltBorder;
			half _MoveToCamera;
			float _Mask0Soft;
			float _SoftParticle;
			float _TessPhongStrength;
			float _TessValue;
			float _TessMin;
			float _TessMax;
			float _TessEdgeLength;
			float _TessMaxDisp;
			float _GameSpeed;
			CBUFFER_END
			sampler2D _MainTex;
			sampler2D _MeltTex;
			sampler2D _MaskTex0;
			sampler2D _MaskTex1;
			//uniform float4 _CameraDepthTexture_TexelSize;


			float3 CustomExpression3_g9( float Scale , float3 Vertex )
			{
				return normalize(mul(unity_WorldToObject, _WorldSpaceCameraPos) - Vertex) * Scale;
			}


			VertexOutput VertexFunction ( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float Scale3_g9 = _MoveToCamera;
				float3 Vertex3_g9 = v.vertex.xyz;
				float3 localCustomExpression3_g9 = CustomExpression3_g9( Scale3_g9 , Vertex3_g9 );

				float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord4 = screenPos;

				o.ase_texcoord3 = v.ase_texcoord;
				o.ase_color = v.ase_color;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = localCustomExpression3_g9;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float4 positionCS = TransformWorldToHClip( positionWS );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				VertexPositionInputs vertexInput = (VertexPositionInputs)0;
				vertexInput.positionWS = positionWS;
				vertexInput.positionCS = positionCS;
				o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				#ifdef ASE_FOG
				o.fogFactor = ComputeFogFactor( positionCS.z );
				#endif
				o.clipPos = positionCS;
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_texcoord = v.ase_texcoord;
				o.ase_color = v.ase_color;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag ( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

			    const float customTime = _GameSpeed * _TimeParameters.x;        // 项目时停效果修改材质动画速率

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif
				float2 uv0_MainTex = IN.ase_texcoord3 * _MainTex_ST.xy + _MainTex_ST.zw;
				float4 tex2DNode6 = tex2D( _MainTex, ( ( _MainSpeed * customTime ) + uv0_MainTex ) );
				float4 uv0_MeltTex = IN.ase_texcoord3;
				uv0_MeltTex.xy = IN.ase_texcoord3.xy * _MeltTex_ST.xy + _MeltTex_ST.zw;
				float temp_output_14_0_g8 = ( saturate( ( tex2D( _MeltTex, ( float4( float2( 0,0 ), 0.0 , 0.0 ) + float4( ( customTime * _MeltSpeed ), 0.0 , 0.0 ) + uv0_MeltTex ).xy ).r - ( 1.0 - 1.0 ) ) ) + 1.0 );
				float temp_output_9_0_g8 = ( uv0_MeltTex.z + _Melt + 0.0 );
				float temp_output_26_0_g8 = saturate( ( ( ( ( temp_output_14_0_g8 - ( ( _MeltBorder + temp_output_9_0_g8 ) * 2.0 ) ) - 0.5 ) * _Hard ) + 0.5 ) );
				float4 lerpResult28_g8 = lerp( _MeltBorderColor , ( _TintColor * tex2DNode6 * IN.ase_color ) , temp_output_26_0_g8);

				float2 appendResult69 = (float2(_MaskSpeed.x , _MaskSpeed.y));
				float2 uv0_MaskTex0 = IN.ase_texcoord3.xy * _MaskTex0_ST.xy + _MaskTex0_ST.zw;
				float4 tex2DNode45 = tex2D( _MaskTex0, ( ( customTime * appendResult69 ) + uv0_MaskTex0 ) );
				float lerpResult88 = lerp( tex2DNode45.r , tex2DNode45.a , _Mask0Channel);
				float2 appendResult82 = (float2(_MaskSpeed.z , _MaskSpeed.w));
				float2 uv0_MaskTex1 = IN.ase_texcoord3.xy * _MaskTex1_ST.xy + _MaskTex1_ST.zw;
				float4 tex2DNode76 = tex2D( _MaskTex1, ( ( customTime * appendResult82 ) + uv0_MaskTex1 ) );
				float lerpResult93 = lerp( tex2DNode76.r , tex2DNode76.a , _Mask1Channel);
				float4 screenPos = IN.ase_texcoord4;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				// 20240222 修复特效被遮挡问题
				ase_screenPosNorm.z = lerp(ase_screenPosNorm.z * 0.5 + 0.5,ase_screenPosNorm.z,step(_MoveToCamera, 0));
				//ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				//ase_screenPosNorm.z = ase_screenPosNorm.z * 0.5 + 0.5;
				float screenDepth83 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float distanceDepth83 = saturate( ( screenDepth83 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _SoftParticle ) );

				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = lerpResult28_g8.rgb;
				float Alpha = ( saturate( ( ( ( ( temp_output_14_0_g8 - ( temp_output_9_0_g8 * 2.0 ) ) - 0.5 ) * _Hard ) + 0.5 ) ) * ( IN.ase_color.a * tex2DNode6.a * saturate( ( saturate( ( ( ( lerpResult88 - 0.5 ) * _Mask0Soft ) + _Mask0Range ) ) * saturate( ( ( ( lerpResult93 - 0.5 ) * _Mask1Soft ) + _Mask1Range ) ) * _MaskStrenth ) ) * _TintColor.a ) * distanceDepth83 );
				float AlphaClipThreshold = 0.5;

				#ifdef _ALPHATEST_ON
					clip( Alpha - AlphaClipThreshold );
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif

				#ifdef ASE_FOG
					Color = MixFog( Color, IN.fogFactor );
				#endif

				return half4( Color, Alpha );
			}

			ENDHLSL
		}


	}
	CustomEditor "UnityEditor.ShaderGraph.PBRMasterGUI"
	Fallback "Hidden/InternalErrorShader"

}
/*ASEBEGIN
Version=18100
529;144.5;1270;840.5;1247.611;809.6197;1.546862;True;False
Node;AmplifyShaderEditor.CommentaryNode;17;-2607.186,-968.5167;Inherit;False;2295.213;1086.75;颜色;41;32;114;115;113;97;103;107;1;12;6;11;28;64;100;106;63;99;10;111;112;105;98;80;104;94;93;88;76;45;89;92;70;79;71;47;77;78;82;69;65;81;;1,1,1,1;0;0
Node;AmplifyShaderEditor.Vector4Node;81;-2584.094,-413.6714;Float;False;Property;_MaskSpeed;遮罩流动速度xy:图1 zw:图2;13;0;Create;False;0;0;False;0;False;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;82;-2243.042,-225.7623;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;69;-2238.875,-518.4003;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;65;-2269.271,-607.1891;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;78;-2009.523,-250.7695;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;77;-2045.677,-152.3206;Inherit;False;0;76;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;71;-2006.578,-542.641;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;47;-2042.067,-447.2372;Inherit;False;0;45;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;70;-1807.578,-517.8411;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;79;-1810.522,-225.9698;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;89;-1407.015,-369.511;Inherit;False;Property;_Mask0Channel;遮罩图1通道选择;5;1;[Enum];Create;False;2;R;0;A;1;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;76;-1693.08,-255.1489;Inherit;True;Property;_MaskTex1;遮罩图2;8;0;Create;False;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;45;-1688.753,-546.6888;Inherit;True;Property;_MaskTex0;遮罩图1;4;0;Create;False;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;92;-1410.816,-86.31161;Inherit;False;Property;_Mask1Channel;遮罩图2通道选择;9;1;[Enum];Create;False;2;R;0;A;1;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;93;-1210.921,-198.586;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;88;-1207.072,-484.1954;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;94;-1196.525,-363.0084;Inherit;False;Property;_Mask0Soft;遮罩图1软硬;6;0;Create;False;0;0;False;0;False;1;0;1;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;103;-1196.785,-75.29636;Inherit;False;Property;_Mask1Soft;遮罩图2软硬;10;0;Create;False;0;0;False;0;False;1;1;1;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;104;-1047.515,-197.408;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;98;-1044.411,-483.9356;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;112;-1196.488,6.988897;Inherit;False;Property;_Mask1Range;遮罩图2范围;11;0;Create;False;0;0;False;0;False;0.5;0.5;0.5;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;105;-908.4875,-198.2966;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;111;-1196.8,-286.128;Inherit;False;Property;_Mask0Range;遮罩图1范围;7;0;Create;False;0;0;False;0;False;0.5;0;0.5;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;99;-905.3831,-484.8242;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;80;-2289.781,-780.2969;Float;False;Property;_MainSpeed;主贴图流动速度xy;3;0;Create;False;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleAddOpNode;100;-773.2447,-485.7129;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;106;-776.3492,-199.1852;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;63;-2007.156,-775.4628;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;10;-2042.506,-678.8584;Inherit;False;0;6;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;97;-653.1624,-485.971;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;64;-1807.156,-717.6627;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;107;-654.7838,-199.2965;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;114;-794.0463,-63.23966;Inherit;False;Property;_MaskStrenth;遮罩强度;12;0;Create;False;0;0;False;0;False;1;1;1;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;113;-484.6458,-223.1394;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;6;-1689.422,-746.9579;Inherit;True;Property;_MainTex;主贴图;1;0;Create;False;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;85;-910.1033,148.1842;Inherit;False;602.7114;154;软粒子;2;84;83;;1,1,1,1;0;0
Node;AmplifyShaderEditor.VertexColorNode;28;-1372.104,-768.5392;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;11;-1663.655,-921.6803;Half;False;Property;_TintColor;色调;2;1;[HDR];Create;False;0;0;False;0;False;1,1,1,1;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;115;-484.6457,-353.1398;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;12;-1211.17,-889.1789;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;84;-871.1033,216.9487;Inherit;False;Property;_SoftParticle;软粒子;23;0;Create;False;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;32;-482.3291,-571.7258;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;83;-575.392,198.1842;Inherit;False;True;True;False;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;123;-282.2162,-885.6613;Inherit;False;Melt;14;;8;33c725d25b56ec8409c8a24486e43648;0;4;42;FLOAT2;0,0;False;37;FLOAT;1;False;35;FLOAT;0;False;30;COLOR;0,0,0,0;False;3;COLOR;34;FLOAT;0;FLOAT;36
Node;AmplifyShaderEditor.RangedFloatNode;124;-216.1752,-1009.728;Inherit;False;Property;_Cull;剔除方式;0;1;[Enum];Create;False;0;1;UnityEngine.Rendering.CullMode;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;31.31154,-595.7899;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;121;-4.626152,-412.2121;Inherit;False;MoveToCamera;21;;9;eb715909966173f4fad7bab142f43236;0;0;1;FLOAT3;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;5;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;True;2;False;-1;False;False;False;False;False;True;1;LightMode=Meta;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;3;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2;224.558,-619.4042;Half;False;True;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;Particles/MeltSoft;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;7;False;False;False;True;2;True;124;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=50;True;0;0;True;2;5;False;-1;10;False;-1;0;1;False;-1;0;False;-1;False;False;False;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;2;False;-1;True;3;False;-1;True;False;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;0;Hidden/InternalErrorShader;0;0;Standard;21;Surface;1;  Blend;3;Two Sided;1;Cast Shadows;0;Receive Shadows;0;GPU Instancing;1;LOD CrossFade;0;Built-in Fog;0;Meta Pass;0;DOTS Instancing;0;Extra Pre Pass;0;Tessellation;0;  Phong;0;  Strength;0.5,False,-1;  Type;0;  Tess;16,False,-1;  Min;10,False,-1;  Max;25,False,-1;  Edge Length;16,False,-1;  Max Displacement;25,False,-1;Vertex Position,InvertActionOnDeselection;1;0;5;False;True;False;False;False;False;;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;-1485.744,-582.0396;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;0;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;4;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;True;False;False;False;False;0;False;-1;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
WireConnection;82;0;81;3
WireConnection;82;1;81;4
WireConnection;69;0;81;1
WireConnection;69;1;81;2
WireConnection;78;0;65;0
WireConnection;78;1;82;0
WireConnection;71;0;65;0
WireConnection;71;1;69;0
WireConnection;70;0;71;0
WireConnection;70;1;47;0
WireConnection;79;0;78;0
WireConnection;79;1;77;0
WireConnection;76;1;79;0
WireConnection;45;1;70;0
WireConnection;93;0;76;1
WireConnection;93;1;76;4
WireConnection;93;2;92;0
WireConnection;88;0;45;1
WireConnection;88;1;45;4
WireConnection;88;2;89;0
WireConnection;104;0;93;0
WireConnection;98;0;88;0
WireConnection;105;0;104;0
WireConnection;105;1;103;0
WireConnection;99;0;98;0
WireConnection;99;1;94;0
WireConnection;100;0;99;0
WireConnection;100;1;111;0
WireConnection;106;0;105;0
WireConnection;106;1;112;0
WireConnection;63;0;80;0
WireConnection;63;1;65;0
WireConnection;97;0;100;0
WireConnection;64;0;63;0
WireConnection;64;1;10;0
WireConnection;107;0;106;0
WireConnection;113;0;97;0
WireConnection;113;1;107;0
WireConnection;113;2;114;0
WireConnection;6;1;64;0
WireConnection;115;0;113;0
WireConnection;12;0;11;0
WireConnection;12;1;6;0
WireConnection;12;2;28;0
WireConnection;32;0;28;4
WireConnection;32;1;6;4
WireConnection;32;2;115;0
WireConnection;32;3;11;4
WireConnection;83;0;84;0
WireConnection;123;30;12;0
WireConnection;16;0;123;0
WireConnection;16;1;32;0
WireConnection;16;2;83;0
WireConnection;2;2;123;34
WireConnection;2;3;16;0
WireConnection;2;5;121;0
ASEEND*/
//CHKSM=F5C5D3A2F8475445AE4F7A5EB934E6A549C6A486
