// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X
Shader "Particles/Distort"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[Enum(UnityEngine.Rendering.CullMode)]_Cull("剔除方式", Float) = 0
		[Enum(Open,0,Close,1)]_CustomData("自定义数据", Float) = 0
		_MainText("主贴图", 2D) = "white" {}
		_Sharpening("锐化", Range( 0 , 10)) = 1
		_SharpeningLight("锐化亮度", Range( 0 , 10)) = 1
		_SharpeningBase("锐化亮度过度", Range( 0 , 1)) = 0.5
		[HDR]_ColorTint("色调", Color) = (1,1,1,1)
		[Enum(Open,1,Close,0)]_MainTextClamp("主贴图Clamp", Float) = 0
		_ClampSoft("Clamp软边", Range( 0 , 1)) = 0
		_MainSpeed("主贴图图流动速度xy", Vector) = (0,0,0,0)
		_MaskTex("遮罩图", 2D) = "white" {}
		[Enum(R,0,A,1)]_MaskChannel("遮罩图通道选择", Float) = 0
		_MaskSoft("遮罩图软硬", Range( 1 , 10)) = 1
		_MaskRange("遮罩图范围", Range( 0.5 , 1)) = 0.5
		_MeltTex("溶解贴图", 2D) = "white" {}
		_MeltSpeed("溶解图流动速度xy", Vector) = (0,0,0,0)
		_Melt("溶解进度", Range( 0 , 1)) = 0
		_Hard("溶解硬度", Range( 1 , 30)) = 1
		_MeltBorder("溶解边框", Range( -1 , 1)) = -1
		[HDR]_MeltBorderColor("溶解边框颜色", Color) = (0,0,0,0)
		_MeltMaskTex("溶解遮罩图", 2D) = "white" {}
		[Enum(R,0,A,1)]_MeltMaskChannel("溶解遮罩图通道选择", Float) = 0
		_MeltMaskSoft("溶解遮罩图软硬", Range( 1 , 10)) = 1
		_MeltMaskRange("溶解遮罩图范围", Range( 0.5 , 1)) = 0.5
		_TwistTex("扭曲贴图", 2D) = "black" {}
		_TwistScale("扭曲强度", Float) = 0
		_TwistSpeed("抽曲流动速度xy", Vector) = (0,0,0,0)
		_SoftParticle("软粒子", Range( 0 , 1)) = 0
		_MoveToCamera("移向摄像机", Range( -20 , 20)) = 0

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
			Offset 0 , 0
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
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
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
				float4 ase_color : COLOR;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _MeltTex_ST;
			float4 _MeltMaskTex_ST;
			float4 _MaskTex_ST;
			float4 _MainText_ST;
			float4 _TwistTex_ST;
			float4 _ColorTint;
			float4 _MeltBorderColor;
			float2 _TwistSpeed;
			float2 _MeltSpeed;
			float2 _MainSpeed;
			float _MaskChannel;
			half _MeltBorder;
			float _MainTextClamp;
			float _ClampSoft;
			half _Hard;
			half _Melt;
			float _MaskSoft;
			float _MeltMaskRange;
			float _Cull;
			float _MeltMaskChannel;
			float _MaskRange;
			float _SharpeningBase;
			float _SharpeningLight;
			float _Sharpening;
			float _TwistScale;
			float _CustomData;
			half _MoveToCamera;
			float _MeltMaskSoft;
			float _SoftParticle;
			float _TessPhongStrength;
			float _TessValue;
			float _TessMin;
			float _TessMax;
			float _TessEdgeLength;
			float _TessMaxDisp;
			float _GameSpeed;
			CBUFFER_END
			sampler2D _MainText;
			sampler2D _TwistTex;
			sampler2D _MeltTex;
			sampler2D _MeltMaskTex;
			sampler2D _MaskTex;


			float3 CustomExpression3_g69( float Scale , float3 Vertex )
			{
				return normalize(mul(unity_WorldToObject, _WorldSpaceCameraPos) - Vertex) * Scale;
			}

			float UvToSoft10_g65( float2 uv , float border )
			{
				float Density = 1;
				Density *= smoothstep(0, border, uv.y);
				Density *= smoothstep(0, border, 1 - uv.y);
				Density *= smoothstep(0, border, uv.x);
				Density *= smoothstep(0, border, 1 - uv.x);
				return Density;
			}


			VertexOutput VertexFunction ( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float Scale3_g69 = _MoveToCamera;
				float3 Vertex3_g69 = v.vertex.xyz;
				float3 localCustomExpression3_g69 = CustomExpression3_g69( Scale3_g69 , Vertex3_g69 );

				float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord5 = screenPos;

				o.ase_color = v.ase_color;
				o.ase_texcoord3 = v.ase_texcoord;
				o.ase_texcoord4.xy = v.ase_texcoord1.xy;

				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord4.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = localCustomExpression3_g69;
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
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;

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
				o.ase_color = v.ase_color;
				o.ase_texcoord = v.ase_texcoord;
				o.ase_texcoord1 = v.ase_texcoord1;
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
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				o.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
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
				float2 uv0_MainText = IN.ase_texcoord3 * _MainText_ST.xy + _MainText_ST.zw;
				float CustomData89 = _CustomData;
				float2 lerpResult85 = lerp( IN.ase_texcoord4.xy , float2( 0,0 ) , CustomData89);
				float4 uv0_TwistTex = IN.ase_texcoord3;
				uv0_TwistTex.xy = IN.ase_texcoord3.xy * _TwistTex_ST.xy + _TwistTex_ST.zw;
				float lerpResult13_g15 = lerp( uv0_TwistTex.w , 0.0 , CustomData89);
				float4 temp_output_9_0 = ( float4( ( customTime * _MainSpeed ), 0.0 , 0.0 ) + float4( uv0_MainText, 0.0 , 0.0 ) + float4( lerpResult85, 0.0 , 0.0 ) + ( ( tex2D( _TwistTex, ( float4( ( _TwistSpeed * customTime ), 0.0 , 0.0 ) + uv0_TwistTex ).xy ) - float4( 0.5,0.5,0.5,0.5019608 ) ) * ( _TwistScale + lerpResult13_g15 ) ) );
				float4 temp_output_1_0_g60 = tex2D( _MainText, temp_output_9_0.rg );
				float4 temp_cast_7 = (_Sharpening).xxxx;
				float4 lerpResult13_g60 = lerp( pow( temp_output_1_0_g60 , temp_cast_7 ) , ( temp_output_1_0_g60 * _SharpeningLight ) , _SharpeningBase);
				float4 break6_g60 = lerpResult13_g60;
				float4 appendResult7_g60 = (float4(break6_g60.x , break6_g60.y , break6_g60.z , saturate( break6_g60.w )));
				float4 break55 = ( _ColorTint * appendResult7_g60 );
				float3 appendResult57 = (float3(break55.r , break55.g , break55.b));
				float lerpResult13_g66 = lerp( uv0_TwistTex.w , 0.0 , CustomData89);
				float4 uv0_MeltTex = IN.ase_texcoord3;
				uv0_MeltTex.xy = IN.ase_texcoord3.xy * _MeltTex_ST.xy + _MeltTex_ST.zw;
				float2 uv0_MeltMaskTex = IN.ase_texcoord3.xy * _MeltMaskTex_ST.xy + _MeltMaskTex_ST.zw;
				float lerpResult13_g14 = lerp( uv0_TwistTex.w , 0.0 , CustomData89);
				float4 tex2DNode23 = tex2D( _MeltMaskTex, ( float4( uv0_MeltMaskTex, 0.0 , 0.0 ) + ( ( tex2D( _TwistTex, ( float4( ( _TwistSpeed * customTime ), 0.0 , 0.0 ) + uv0_TwistTex ).xy ) - float4( 0.5,0.5,0.5,0.5019608 ) ) * ( _TwistScale + lerpResult13_g14 ) ) ).rg );
				float lerpResult14 = lerp( tex2DNode23.r , tex2DNode23.a , _MeltMaskChannel);
				float temp_output_14_0_g67 = ( saturate( ( tex2D( _MeltTex, ( float4( ( ( tex2D( _TwistTex, ( float4( ( _TwistSpeed * customTime ), 0.0 , 0.0 ) + uv0_TwistTex ).xy ) - float4( 0.5,0.5,0.5,0.5019608 ) ) * ( _TwistScale + lerpResult13_g66 ) ).rg, 0.0 , 0.0 ) + float4( ( customTime * _MeltSpeed ), 0.0 , 0.0 ) + uv0_MeltTex ).xy ).r - ( 1.0 - saturate( ( ( ( lerpResult14 - 0.5 ) * _MeltMaskSoft ) + _MeltMaskRange ) ) ) ) ) + 1.0 );
				float temp_output_9_0_g67 = ( uv0_MeltTex.z + _Melt + 0.0 );
				float temp_output_26_0_g67 = saturate( ( ( ( ( temp_output_14_0_g67 - ( ( _MeltBorder + temp_output_9_0_g67 ) * 2.0 ) ) - 0.5 ) * _Hard ) + 0.5 ) );
				float4 lerpResult28_g67 = lerp( _MeltBorderColor , float4( appendResult57 , 0.0 ) , temp_output_26_0_g67);

				float2 uv10_g65 = temp_output_9_0.rg;
				float border10_g65 = _ClampSoft;
				float localUvToSoft10_g65 = UvToSoft10_g65( uv10_g65 , border10_g65 );
				float lerpResult48 = lerp( break55.a , ( break55.a * localUvToSoft10_g65 ) , _MainTextClamp);
				float2 uv0_MaskTex = IN.ase_texcoord3.xy * _MaskTex_ST.xy + _MaskTex_ST.zw;
				float4 tex2DNode2_g68 = tex2D( _MaskTex, ( uv0_MaskTex + float2( 0,0 ) ) );
				float lerpResult4_g68 = lerp( tex2DNode2_g68.r , tex2DNode2_g68.a , _MaskChannel);
				float4 screenPos = IN.ase_texcoord5;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				//2024/2/27 特效遮挡bug修复
				ase_screenPosNorm.z = lerp(ase_screenPosNorm.z * 0.5 + 0.5,ase_screenPosNorm.z,step(_MoveToCamera, 0));
				//ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float screenDepth130 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float distanceDepth130 = saturate( ( screenDepth130 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _SoftParticle ) );

				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = ( IN.ase_color * lerpResult28_g67 ).rgb;
				float Alpha = ( IN.ase_color.a * saturate( ( ( ( ( temp_output_14_0_g67 - ( temp_output_9_0_g67 * 2.0 ) ) - 0.5 ) * _Hard ) + 0.5 ) ) * lerpResult48 * saturate( ( ( ( lerpResult4_g68 - 0.5 ) * _MaskSoft ) + _MaskRange ) ) * distanceDepth130 );
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
529;144.5;1270;840.5;1544.129;229.5826;1.729261;True;False
Node;AmplifyShaderEditor.RangedFloatNode;83;-79.31918,-385.417;Inherit;False;Property;_CustomData;自定义数据;1;1;[Enum];Create;False;2;Open;0;Close;1;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;89;99.20817,-385.6182;Inherit;False;CustomData;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;25;-1858.503,-571.6663;Inherit;False;1617.716;403.325;溶解遮罩;12;19;38;18;17;16;20;15;21;14;22;23;91;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;43;-2183.847,-148.0294;Inherit;False;1869.08;824.025;颜色;21;6;111;124;126;48;47;120;55;117;51;50;78;9;8;94;85;42;90;49;41;40;;1,1,1,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;91;-1810.266,-266.4967;Inherit;False;89;CustomData;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;49;-2137.045,270.2976;Inherit;False;1;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleTimeNode;40;-2131.009,-36.61322;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;41;-2149.367,43.11526;Inherit;False;Property;_MainSpeed;主贴图图流动速度xy;9;0;Create;False;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.GetLocalVarNode;90;-2154.138,396.3243;Inherit;False;89;CustomData;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;93;-1826.581,-360.3316;Inherit;False;Distort;26;;14;98fb1ef4e79710648ba382d4421049c7;0;1;12;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;19;-1828.323,-498.2824;Inherit;False;0;23;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;94;-1937.293,400.3937;Inherit;False;Distort;26;;15;98fb1ef4e79710648ba382d4421049c7;0;1;12;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;38;-1574.941,-492.9601;Inherit;False;2;2;0;FLOAT2;0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;42;-1924.644,-7.807285;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;85;-1918.448,270.3625;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;8;-1966.202,150.4651;Inherit;False;0;6;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;9;-1721.492,170.75;Inherit;False;4;4;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;23;-1441.381,-521.6663;Inherit;True;Property;_MeltMaskTex;溶解遮罩图;22;0;Create;False;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;22;-1158.639,-344.4886;Inherit;False;Property;_MeltMaskChannel;溶解遮罩图通道选择;23;1;[Enum];Create;False;2;R;0;A;1;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;78;-1571.428,284.113;Inherit;False;Property;_Sharpening;锐化;3;0;Create;False;0;0;False;0;False;1;0;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;124;-1572.792,356.5826;Inherit;False;Property;_SharpeningLight;锐化亮度;4;0;Create;False;0;0;False;0;False;1;0;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;111;-1573.275,426.532;Inherit;False;Property;_SharpeningBase;锐化亮度过度;5;0;Create;False;0;0;False;0;False;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;14;-959.6952,-459.1729;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;6;-1577.085,90.289;Inherit;True;Property;_MainText;主贴图;2;0;Create;False;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;21;-949.1482,-337.986;Inherit;False;Property;_MeltMaskSoft;溶解遮罩图软硬;24;0;Create;False;0;0;False;0;False;1;0;1;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;15;-797.0347,-458.913;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;126;-1237.968,154.2593;Inherit;False;Sharpening;-1;;60;9a3ce3dc9737eb24f88ce22fed761a61;0;4;1;FLOAT4;0,0,0,0;False;4;FLOAT;1;False;15;FLOAT;1;False;10;FLOAT;0.5;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ColorNode;50;-1263.321,-11.74784;Inherit;False;Property;_ColorTint;色调;6;1;[HDR];Create;False;0;0;False;0;False;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;20;-949.4233,-261.1056;Inherit;False;Property;_MeltMaskRange;溶解遮罩图范围;25;0;Create;False;0;0;False;0;False;0.5;0;0.5;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;-658.0068,-459.8017;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;51;-1039.496,32.42553;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;128;-267.6017,327.3893;Inherit;False;602.7114;154;软粒子;2;130;129;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;117;-975.2347,182.7668;Inherit;False;Property;_ClampSoft;Clamp软边;8;0;Create;False;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;55;-913.1902,31.33998;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.GetLocalVarNode;92;-321.2999,-63.64082;Inherit;False;89;CustomData;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;17;-525.8688,-460.6903;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;47;-684.7088,357.3518;Inherit;False;Property;_MainTextClamp;主贴图Clamp;7;1;[Enum];Create;False;2;Open;1;Close;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;129;-228.6017,396.1538;Inherit;False;Property;_SoftParticle;软粒子;30;0;Create;False;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;95;-137.6177,-59.48926;Inherit;False;Distort;26;;66;98fb1ef4e79710648ba382d4421049c7;0;1;12;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;120;-697.2032,187.215;Inherit;False;AlphaClamp;-1;;65;3b14c9f6ab32e184a8df65984df69006;0;3;11;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;18;-405.7869,-460.9484;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;57;-80.64863,12.97689;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode;39;-11.77045,219.1855;Inherit;False;TextureMask;10;;68;1938503a692980b499d8c34e23de4751;0;1;12;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;28;150.4585,-224.3196;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DepthFade;130;67.10947,377.3893;Inherit;False;True;True;False;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;48;-479.0877,142.5946;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;127;59.02084,-59.75711;Inherit;False;Melt;15;;67;33c725d25b56ec8409c8a24486e43648;0;4;42;FLOAT2;0,0;False;37;FLOAT;1;False;35;FLOAT;0;False;30;COLOR;0,0,0,0;False;3;COLOR;34;FLOAT;0;FLOAT;36
Node;AmplifyShaderEditor.RangedFloatNode;113;-76.42249,-501.912;Inherit;False;Property;_Cull;剔除方式;0;1;[Enum];Create;False;0;1;UnityEngine.Rendering.CullMode;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;29;538.4586,-78.31956;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;131;494.6693,244.235;Inherit;False;MoveToCamera;31;;69;eb715909966173f4fad7bab142f43236;0;0;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;541.7982,23.033;Inherit;False;5;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;4;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;True;False;False;False;False;0;False;-1;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;5;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;True;2;False;-1;False;False;False;False;False;True;1;LightMode=Meta;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;3;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2;690.2318,-43.42199;Float;False;True;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;Particles/Distort;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;7;False;False;False;True;2;True;113;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=50;True;0;0;True;1;5;False;-1;10;False;-1;0;1;False;-1;10;False;-1;False;False;False;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;2;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;0;Hidden/InternalErrorShader;0;0;Standard;21;Surface;1;  Blend;0;Two Sided;1;Cast Shadows;0;Receive Shadows;0;GPU Instancing;1;LOD CrossFade;0;Built-in Fog;0;Meta Pass;0;DOTS Instancing;0;Extra Pre Pass;0;Tessellation;0;  Phong;0;  Strength;0.5,False,-1;  Type;0;  Tess;16,False,-1;  Min;10,False,-1;  Max;25,False,-1;  Edge Length;16,False,-1;  Max Displacement;25,False,-1;Vertex Position,InvertActionOnDeselection;1;0;5;False;True;False;False;False;False;;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;282.8928,30.77495;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;0;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
WireConnection;89;0;83;0
WireConnection;93;12;91;0
WireConnection;94;12;90;0
WireConnection;38;0;19;0
WireConnection;38;1;93;0
WireConnection;42;0;40;0
WireConnection;42;1;41;0
WireConnection;85;0;49;0
WireConnection;85;2;90;0
WireConnection;9;0;42;0
WireConnection;9;1;8;0
WireConnection;9;2;85;0
WireConnection;9;3;94;0
WireConnection;23;1;38;0
WireConnection;14;0;23;1
WireConnection;14;1;23;4
WireConnection;14;2;22;0
WireConnection;6;1;9;0
WireConnection;15;0;14;0
WireConnection;126;1;6;0
WireConnection;126;4;78;0
WireConnection;126;15;124;0
WireConnection;126;10;111;0
WireConnection;16;0;15;0
WireConnection;16;1;21;0
WireConnection;51;0;50;0
WireConnection;51;1;126;0
WireConnection;55;0;51;0
WireConnection;17;0;16;0
WireConnection;17;1;20;0
WireConnection;95;12;92;0
WireConnection;120;11;117;0
WireConnection;120;1;55;3
WireConnection;120;2;9;0
WireConnection;18;0;17;0
WireConnection;57;0;55;0
WireConnection;57;1;55;1
WireConnection;57;2;55;2
WireConnection;130;0;129;0
WireConnection;48;0;55;3
WireConnection;48;1;120;0
WireConnection;48;2;47;0
WireConnection;127;42;95;0
WireConnection;127;37;18;0
WireConnection;127;30;57;0
WireConnection;29;0;28;0
WireConnection;29;1;127;34
WireConnection;11;0;28;4
WireConnection;11;1;127;0
WireConnection;11;2;48;0
WireConnection;11;3;39;0
WireConnection;11;4;130;0
WireConnection;2;2;29;0
WireConnection;2;3;11;0
WireConnection;2;5;131;0
ASEEND*/
//CHKSM=A1A5A2BDB6A5A8D005290DCF4C22D43F9411F54D
