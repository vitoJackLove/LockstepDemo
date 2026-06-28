// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X
Shader "Bohanshader/AlphaD2"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		_MainTex("MainTex", 2D) = "white" {}
		[HDR]_MainColor("MainColor", Color) = (0.5019608,0.5019608,0.5019608,0.5019608)
		_MaskTex("MaskTex", 2D) = "white" {}
		[Enum(R,0,A,1)]_MaskRASwitch1("MaskRASwitch1", Range( 0 , 1)) = 1
		_MaskTex2("MaskTex2", 2D) = "white" {}
		[Enum(R,0,A,1)]_MaskRASwitch2("MaskRASwitch2", Range( 0 , 1)) = 1
		_maskpower("maskpower", Range( 1 , 10)) = 1
		_MaskSpeedX("MaskSpeedX", Float) = 0
		_MaskSpeedY("MaskSpeedY", Float) = 0
		_Mask2SpeedX("Mask2SpeedX", Float) = 0
		_Mask2SpeedY("Mask2SpeedY", Float) = 0
		_DissolveTex("DissolveTex", 2D) = "white" {}
		_SpeedX("SpeedX", Float) = 0
		_SpeedY("SpeedY", Float) = 0
		_DissolveSpeedX("DissolveSpeedX", Float) = 0
		_DissolveSpeedY("DissolveSpeedY", Float) = 0
		[Enum(Off,0,On,1)]_DepthFade("DepthFade", Float) = 0
		_Distance("Distance", Range( 0 , 1)) = 1
		_FrontAndBehind("FrontAndBehind", Range( -20 , 20)) = 0

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

		Cull Off
		HLSLINCLUDE
		#pragma target 3.0

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

			Blend SrcAlpha OneMinusSrcAlpha , Zero Zero
			ZWrite Off
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA


			HLSLPROGRAM
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
				float4 ase_texcoord2 : TEXCOORD2;
				half4 ase_color : COLOR;
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
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_color : COLOR;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				float4 ase_texcoord6 : TEXCOORD6;
				float4 ase_texcoord7 : TEXCOORD7;
				float4 ase_texcoord8 : TEXCOORD8;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			half4 _MainColor;
			half4 _MainTex_ST;
			half4 _DissolveTex_ST;
			half4 _MaskTex2_ST;
			half4 _MaskTex_ST;
			half _FrontAndBehind;
			half _Mask2SpeedY;
			half _Mask2SpeedX;
			half _MaskRASwitch1;
			half _MaskSpeedY;
			half _MaskSpeedX;
			half _DepthFade;
			half _Distance;
			half _DissolveSpeedY;
			half _DissolveSpeedX;
			half _SpeedY;
			half _SpeedX;
			half _MaskRASwitch2;
			half _maskpower;
			float _TessPhongStrength;
			float _TessValue;
			float _TessMin;
			float _TessMax;
			float _TessEdgeLength;
			float _TessMaxDisp;
			float _GameSpeed;
			CBUFFER_END
			sampler2D _MainTex;
			sampler2D _DissolveTex;
			sampler2D _MaskTex;
			sampler2D _MaskTex2;



			VertexOutput VertexFunction ( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

			    const float customTime = _GameSpeed * _TimeParameters.x;        // 项目时停效果修改材质动画速率

				half3 normalizeResult101 = normalize( ( mul( GetWorldToObjectMatrix(), half4( _WorldSpaceCameraPos , 0.0 ) ).xyz - v.vertex.xyz ) );

				half2 uv0_MainTex = v.ase_texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				half2 appendResult82 = (half2(_SpeedX , _SpeedY));
				half2 vertexToFrag76 = ( uv0_MainTex + ( ( customTime ) * appendResult82 ) );
				o.ase_texcoord4.xy = vertexToFrag76;
				half2 uv0_DissolveTex = v.ase_texcoord.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
				half2 appendResult91 = (half2(_DissolveSpeedX , _DissolveSpeedY));
				half2 vertexToFrag94 = ( uv0_DissolveTex + ( ( customTime ) * appendResult91 ) );
				o.ase_texcoord4.zw = vertexToFrag94;
				half4 uv149 = v.ase_texcoord1;
				uv149.xy = v.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				half temp_output_67_0 = ( uv149.x * ( 1.0 + uv149.y ) );
				half hardness71 = uv149.z;
				half temp_output_56_0 = ( 1.0 - hardness71 );
				half vertexToFrag50 = ( temp_output_67_0 * ( 1.0 + temp_output_56_0 ) );
				o.ase_texcoord5.x = vertexToFrag50;
				half vertexToFrag70 = ( ( temp_output_67_0 - uv149.y ) * ( 1.0 + temp_output_56_0 ) );
				o.ase_texcoord5.y = vertexToFrag70;

				float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord7 = screenPos;
				half2 uv0_MaskTex = v.ase_texcoord.xy * _MaskTex_ST.xy + _MaskTex_ST.zw;
				half2 appendResult114 = (half2(_MaskSpeedX , _MaskSpeedY));
				half2 vertexToFrag119 = ( uv0_MaskTex + ( ( customTime ) * appendResult114 ) );
				o.ase_texcoord5.zw = vertexToFrag119;
				half2 uv0_MaskTex2 = v.ase_texcoord.xy * _MaskTex2_ST.xy + _MaskTex2_ST.zw;
				half2 appendResult122 = (half2(_Mask2SpeedX , _Mask2SpeedY));
				half2 vertexToFrag125 = ( uv0_MaskTex2 + ( ( customTime ) * appendResult122 ) );
				o.ase_texcoord8.xy = vertexToFrag125;

				o.ase_texcoord3 = v.ase_texcoord2;
				o.ase_color = v.ase_color;
				o.ase_texcoord6 = v.ase_texcoord1;

				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord8.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = ( normalizeResult101 * _FrontAndBehind );
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
				float4 ase_texcoord2 : TEXCOORD2;
				half4 ase_color : COLOR;
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
				o.ase_texcoord2 = v.ase_texcoord2;
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
				o.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
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
				half4 uv273 = IN.ase_texcoord3;
				uv273.xy = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				half2 vertexToFrag76 = IN.ase_texcoord4.xy;
				half4 tex2DNode5 = tex2D( _MainTex, vertexToFrag76 );
				half2 vertexToFrag94 = IN.ase_texcoord4.zw;
				half temp_output_52_0 = ( tex2D( _DissolveTex, vertexToFrag94 ).r + 1.0 );
				half vertexToFrag50 = IN.ase_texcoord5.x;
				half4 uv149 = IN.ase_texcoord6;
				uv149.xy = IN.ase_texcoord6.xy * float2( 1,1 ) + float2( 0,0 );
				half temp_output_7_0_g3 = uv149.z;
				half4 lerpResult69 = lerp( uv273 , ( IN.ase_color * _MainColor * tex2DNode5 ) , saturate( ( ( ( temp_output_52_0 - vertexToFrag50 ) - temp_output_7_0_g3 ) / ( 1.0 - temp_output_7_0_g3 ) ) ));
				half vertexToFrag70 = IN.ase_texcoord5.y;
				half temp_output_7_0_g2 = uv149.z;
				half temp_output_63_0 = saturate( ( ( ( temp_output_52_0 - vertexToFrag70 ) - temp_output_7_0_g2 ) / ( 1.0 - temp_output_7_0_g2 ) ) );
				half4 appendResult48 = (half4(lerpResult69.rgb , temp_output_63_0));

				float4 screenPos = IN.ase_texcoord7;
				half4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float screenDepth83 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				half distanceDepth83 = saturate( ( screenDepth83 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _Distance ) );
				half lerpResult84 = lerp( 1.0 , distanceDepth83 , _DepthFade);
				half2 vertexToFrag119 = IN.ase_texcoord5.zw;
				half4 tex2DNode41 = tex2D( _MaskTex, vertexToFrag119 );
				half lerpResult33 = lerp( tex2DNode41.r , tex2DNode41.a , _MaskRASwitch1);
				half2 vertexToFrag125 = IN.ase_texcoord8.xy;
				half4 tex2DNode104 = tex2D( _MaskTex2, vertexToFrag125 );
				half lerpResult108 = lerp( tex2DNode104.r , tex2DNode104.a , _MaskRASwitch2);

				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = appendResult48.xyz;
				float Alpha = ( temp_output_63_0 * _MainColor.a * tex2DNode5.a * IN.ase_color.a * lerpResult84 * saturate( ( lerpResult33 * lerpResult108 * _maskpower ) ) );
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


		Pass
		{

			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }

			ZWrite On
			ZTest LEqual

			HLSLPROGRAM
			#pragma multi_compile_instancing
			#define ASE_SRP_VERSION 999999
			#define REQUIRE_DEPTH_TEXTURE 1

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#define ASE_NEEDS_VERT_POSITION


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				half4 ase_color : COLOR;
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
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_color : COLOR;
				float4 ase_texcoord5 : TEXCOORD5;
				float4 ase_texcoord6 : TEXCOORD6;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			half4 _MainColor;
			half4 _MainTex_ST;
			half4 _DissolveTex_ST;
			half4 _MaskTex2_ST;
			half4 _MaskTex_ST;
			half _FrontAndBehind;
			half _Mask2SpeedY;
			half _Mask2SpeedX;
			half _MaskRASwitch1;
			half _MaskSpeedY;
			half _MaskSpeedX;
			half _DepthFade;
			half _Distance;
			half _DissolveSpeedY;
			half _DissolveSpeedX;
			half _SpeedY;
			half _SpeedX;
			half _MaskRASwitch2;
			half _maskpower;
			float _TessPhongStrength;
			float _TessValue;
			float _TessMin;
			float _TessMax;
			float _TessEdgeLength;
			float _TessMaxDisp;
			float _GameSpeed;
			CBUFFER_END
			sampler2D _DissolveTex;
			sampler2D _MainTex;
			sampler2D _MaskTex;
			sampler2D _MaskTex2;



			float3 _LightDirection;

			VertexOutput VertexFunction( VertexInput v )
			{
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

			    const float customTime = _GameSpeed * _TimeParameters.x;        // 项目时停效果修改材质动画速率

				half3 normalizeResult101 = normalize( ( mul( GetWorldToObjectMatrix(), half4( _WorldSpaceCameraPos , 0.0 ) ).xyz - v.vertex.xyz ) );

				half2 uv0_DissolveTex = v.ase_texcoord.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
				half2 appendResult91 = (half2(_DissolveSpeedX , _DissolveSpeedY));
				half2 vertexToFrag94 = ( uv0_DissolveTex + ( ( customTime ) * appendResult91 ) );
				o.ase_texcoord2.xy = vertexToFrag94;
				half4 uv149 = v.ase_texcoord1;
				uv149.xy = v.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				half temp_output_67_0 = ( uv149.x * ( 1.0 + uv149.y ) );
				half hardness71 = uv149.z;
				half temp_output_56_0 = ( 1.0 - hardness71 );
				half vertexToFrag70 = ( ( temp_output_67_0 - uv149.y ) * ( 1.0 + temp_output_56_0 ) );
				o.ase_texcoord2.z = vertexToFrag70;
				half2 uv0_MainTex = v.ase_texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				half2 appendResult82 = (half2(_SpeedX , _SpeedY));
				half2 vertexToFrag76 = ( uv0_MainTex + ( ( customTime ) * appendResult82 ) );
				o.ase_texcoord4.xy = vertexToFrag76;
				float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord5 = screenPos;
				half2 uv0_MaskTex = v.ase_texcoord.xy * _MaskTex_ST.xy + _MaskTex_ST.zw;
				half2 appendResult114 = (half2(_MaskSpeedX , _MaskSpeedY));
				half2 vertexToFrag119 = ( uv0_MaskTex + ( ( customTime ) * appendResult114 ) );
				o.ase_texcoord4.zw = vertexToFrag119;
				half2 uv0_MaskTex2 = v.ase_texcoord.xy * _MaskTex2_ST.xy + _MaskTex2_ST.zw;
				half2 appendResult122 = (half2(_Mask2SpeedX , _Mask2SpeedY));
				half2 vertexToFrag125 = ( uv0_MaskTex2 + ( ( customTime ) * appendResult122 ) );
				o.ase_texcoord6.xy = vertexToFrag125;

				o.ase_texcoord3 = v.ase_texcoord1;
				o.ase_color = v.ase_color;

				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.w = 0;
				o.ase_texcoord6.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = ( normalizeResult101 * _FrontAndBehind );
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif

				float3 normalWS = TransformObjectToWorldDir( v.ase_normal );

				float4 clipPos = TransformWorldToHClip( ApplyShadowBias( positionWS, normalWS, _LightDirection ) );

				#if UNITY_REVERSED_Z
					clipPos.z = min(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
				#else
					clipPos.z = max(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = clipPos;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				o.clipPos = clipPos;

				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				half4 ase_color : COLOR;

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
				o.ase_texcoord1 = v.ase_texcoord1;
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
				o.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
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

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

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

				half2 vertexToFrag94 = IN.ase_texcoord2.xy;
				half temp_output_52_0 = ( tex2D( _DissolveTex, vertexToFrag94 ).r + 1.0 );
				half vertexToFrag70 = IN.ase_texcoord2.z;
				half4 uv149 = IN.ase_texcoord3;
				uv149.xy = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				half temp_output_7_0_g2 = uv149.z;
				half temp_output_63_0 = saturate( ( ( ( temp_output_52_0 - vertexToFrag70 ) - temp_output_7_0_g2 ) / ( 1.0 - temp_output_7_0_g2 ) ) );
				half2 vertexToFrag76 = IN.ase_texcoord4.xy;
				half4 tex2DNode5 = tex2D( _MainTex, vertexToFrag76 );
				float4 screenPos = IN.ase_texcoord5;
				half4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float screenDepth83 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				half distanceDepth83 = saturate( ( screenDepth83 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _Distance ) );
				half lerpResult84 = lerp( 1.0 , distanceDepth83 , _DepthFade);
				half2 vertexToFrag119 = IN.ase_texcoord4.zw;
				half4 tex2DNode41 = tex2D( _MaskTex, vertexToFrag119 );
				half lerpResult33 = lerp( tex2DNode41.r , tex2DNode41.a , _MaskRASwitch1);
				half2 vertexToFrag125 = IN.ase_texcoord6.xy;
				half4 tex2DNode104 = tex2D( _MaskTex2, vertexToFrag125 );
				half lerpResult108 = lerp( tex2DNode104.r , tex2DNode104.a , _MaskRASwitch2);

				float Alpha = ( temp_output_63_0 * _MainColor.a * tex2DNode5.a * IN.ase_color.a * lerpResult84 * saturate( ( lerpResult33 * lerpResult108 * _maskpower ) ) );
				float AlphaClipThreshold = 0.5;

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif
				return 0;
			}

			ENDHLSL
		}


		Pass
		{

			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask 0

			HLSLPROGRAM
			#pragma multi_compile_instancing
			#define ASE_SRP_VERSION 999999
			#define REQUIRE_DEPTH_TEXTURE 1

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#define ASE_NEEDS_VERT_POSITION


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				half4 ase_color : COLOR;
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
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_color : COLOR;
				float4 ase_texcoord5 : TEXCOORD5;
				float4 ase_texcoord6 : TEXCOORD6;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			half4 _MainColor;
			half4 _MainTex_ST;
			half4 _DissolveTex_ST;
			half4 _MaskTex2_ST;
			half4 _MaskTex_ST;
			half _FrontAndBehind;
			half _Mask2SpeedY;
			half _Mask2SpeedX;
			half _MaskRASwitch1;
			half _MaskSpeedY;
			half _MaskSpeedX;
			half _DepthFade;
			half _Distance;
			half _DissolveSpeedY;
			half _DissolveSpeedX;
			half _SpeedY;
			half _SpeedX;
			half _MaskRASwitch2;
			half _maskpower;
			float _TessPhongStrength;
			float _TessValue;
			float _TessMin;
			float _TessMax;
			float _TessEdgeLength;
			float _TessMaxDisp;
			float _GameSpeed;
			CBUFFER_END
			sampler2D _DissolveTex;
			sampler2D _MainTex;
			sampler2D _MaskTex;
			sampler2D _MaskTex2;



			VertexOutput VertexFunction( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

			    const float customTime = _GameSpeed * _TimeParameters.x;        // 项目时停效果修改材质动画速率

				half3 normalizeResult101 = normalize( ( mul( GetWorldToObjectMatrix(), half4( _WorldSpaceCameraPos , 0.0 ) ).xyz - v.vertex.xyz ) );

				half2 uv0_DissolveTex = v.ase_texcoord.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
				half2 appendResult91 = (half2(_DissolveSpeedX , _DissolveSpeedY));
				half2 vertexToFrag94 = ( uv0_DissolveTex + ( ( customTime ) * appendResult91 ) );
				o.ase_texcoord2.xy = vertexToFrag94;
				half4 uv149 = v.ase_texcoord1;
				uv149.xy = v.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				half temp_output_67_0 = ( uv149.x * ( 1.0 + uv149.y ) );
				half hardness71 = uv149.z;
				half temp_output_56_0 = ( 1.0 - hardness71 );
				half vertexToFrag70 = ( ( temp_output_67_0 - uv149.y ) * ( 1.0 + temp_output_56_0 ) );
				o.ase_texcoord2.z = vertexToFrag70;
				half2 uv0_MainTex = v.ase_texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				half2 appendResult82 = (half2(_SpeedX , _SpeedY));
				half2 vertexToFrag76 = ( uv0_MainTex + ( ( customTime ) * appendResult82 ) );
				o.ase_texcoord4.xy = vertexToFrag76;
				float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord5 = screenPos;
				half2 uv0_MaskTex = v.ase_texcoord.xy * _MaskTex_ST.xy + _MaskTex_ST.zw;
				half2 appendResult114 = (half2(_MaskSpeedX , _MaskSpeedY));
				half2 vertexToFrag119 = ( uv0_MaskTex + ( ( customTime ) * appendResult114 ) );
				o.ase_texcoord4.zw = vertexToFrag119;
				half2 uv0_MaskTex2 = v.ase_texcoord.xy * _MaskTex2_ST.xy + _MaskTex2_ST.zw;
				half2 appendResult122 = (half2(_Mask2SpeedX , _Mask2SpeedY));
				half2 vertexToFrag125 = ( uv0_MaskTex2 + ( ( customTime ) * appendResult122 ) );
				o.ase_texcoord6.xy = vertexToFrag125;

				o.ase_texcoord3 = v.ase_texcoord1;
				o.ase_color = v.ase_color;

				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.w = 0;
				o.ase_texcoord6.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = ( normalizeResult101 * _FrontAndBehind );
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif

				o.clipPos = TransformWorldToHClip( positionWS );
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = clipPos;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				half4 ase_color : COLOR;

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
				o.ase_texcoord1 = v.ase_texcoord1;
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
				o.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
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

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

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

				half2 vertexToFrag94 = IN.ase_texcoord2.xy;
				half temp_output_52_0 = ( tex2D( _DissolveTex, vertexToFrag94 ).r + 1.0 );
				half vertexToFrag70 = IN.ase_texcoord2.z;
				half4 uv149 = IN.ase_texcoord3;
				uv149.xy = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				half temp_output_7_0_g2 = uv149.z;
				half temp_output_63_0 = saturate( ( ( ( temp_output_52_0 - vertexToFrag70 ) - temp_output_7_0_g2 ) / ( 1.0 - temp_output_7_0_g2 ) ) );
				half2 vertexToFrag76 = IN.ase_texcoord4.xy;
				half4 tex2DNode5 = tex2D( _MainTex, vertexToFrag76 );
				float4 screenPos = IN.ase_texcoord5;
				half4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float screenDepth83 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				half distanceDepth83 = saturate( ( screenDepth83 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _Distance ) );
				half lerpResult84 = lerp( 1.0 , distanceDepth83 , _DepthFade);
				half2 vertexToFrag119 = IN.ase_texcoord4.zw;
				half4 tex2DNode41 = tex2D( _MaskTex, vertexToFrag119 );
				half lerpResult33 = lerp( tex2DNode41.r , tex2DNode41.a , _MaskRASwitch1);
				half2 vertexToFrag125 = IN.ase_texcoord6.xy;
				half4 tex2DNode104 = tex2D( _MaskTex2, vertexToFrag125 );
				half lerpResult108 = lerp( tex2DNode104.r , tex2DNode104.a , _MaskRASwitch2);

				float Alpha = ( temp_output_63_0 * _MainColor.a * tex2DNode5.a * IN.ase_color.a * lerpResult84 * saturate( ( lerpResult33 * lerpResult108 * _maskpower ) ) );
				float AlphaClipThreshold = 0.5;

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif
				return 0;
			}
			ENDHLSL
		}


	}
	CustomEditor "UnityEditor.ShaderGraph.PBRMasterGUI"
	Fallback "False"

}
/*ASEBEGIN
Version=18100
529;144.5;1270;840.5;2300.941;841.1172;2.617568;True;False
Node;AmplifyShaderEditor.TextureCoordinatesNode;49;-2183.511,564.9836;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;112;-2615.637,1762.662;Half;False;Property;_MaskSpeedX;MaskSpeedX;7;0;Create;True;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;113;-2617.637,1836.663;Half;False;Property;_MaskSpeedY;MaskSpeedY;8;0;Create;True;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;127;-2584.424,2393.957;Half;False;Property;_Mask2SpeedX;Mask2SpeedX;9;0;Create;True;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;120;-2586.424,2467.958;Half;False;Property;_Mask2SpeedY;Mask2SpeedY;10;0;Create;True;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;88;-3385.994,538.8918;Half;False;Property;_DissolveSpeedX;DissolveSpeedX;16;0;Create;True;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;89;-3387.994,612.8919;Half;False;Property;_DissolveSpeedY;DissolveSpeedY;17;0;Create;True;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TimeNode;121;-2398.223,2152.758;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TimeNode;90;-3199.793,297.6918;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;91;-3078.894,529.0919;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;114;-2308.537,1752.863;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TimeNode;115;-2429.436,1521.463;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;71;-207.207,521.1256;Half;False;hardness;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;122;-2277.324,2384.158;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;126;-2254.361,1924.433;Inherit;False;0;104;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;72;-1518.146,770.2684;Inherit;False;71;hardness;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;95;-3055.931,69.36688;Inherit;False;0;42;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;116;-2064.136,1624.162;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;66;-1915.187,745.6259;Inherit;False;2;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;117;-2285.574,1293.138;Inherit;False;0;41;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;123;-2032.923,2255.457;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;92;-2834.493,400.3918;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;118;-1871.272,1501.738;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;80;-2463.497,64.60551;Half;False;Property;_SpeedX;SpeedX;14;0;Create;True;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;67;-1743.315,586.4586;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;81;-2465.497,138.6055;Half;False;Property;_SpeedY;SpeedY;15;0;Create;True;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;93;-2641.629,277.9669;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;124;-1840.058,2133.033;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;56;-1339.541,775.0358;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TimeNode;79;-2277.296,-176.5945;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexToFragmentNode;94;-2345.694,367.8918;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.VertexToFragmentNode;119;-1575.338,1591.662;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;58;-1160.506,1035.037;Inherit;False;2;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;82;-2156.397,54.80557;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;64;-1564.812,883.465;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexToFragmentNode;125;-1585.104,1899.678;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WorldSpaceCameraPos;96;-1232.979,1995.685;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SamplerNode;104;-1067.927,1436.148;Inherit;True;Property;_MaskTex2;MaskTex2;4;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;77;-1911.996,-73.89445;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;107;-1055.364,1634.3;Half;False;Property;_MaskRASwitch2;MaskRASwitch2;5;1;[Enum];Create;True;2;R;0;A;1;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;59;-831.3918,1017.517;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;42;-1063.175,262.0429;Inherit;True;Property;_DissolveTex;DissolveTex;11;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;15;-1031.542,1301.973;Half;False;Property;_MaskRASwitch1;MaskRASwitch1;3;1;[Enum];Create;True;2;R;0;A;1;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;41;-1045.568,1113.985;Inherit;True;Property;_MaskTex;MaskTex;2;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;74;-2133.434,-404.9194;Inherit;False;0;5;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldToObjectMatrix;97;-1173.325,2157.276;Inherit;False;0;1;FLOAT4x4;0
Node;AmplifyShaderEditor.LerpOp;108;-560.393,1424.417;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;87;-1303.037,95.44775;Half;False;Property;_Distance;Distance;22;0;Create;True;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;109;-651.6372,1597.17;Half;False;Property;_maskpower;maskpower;6;0;Create;True;0;0;False;0;False;1;1;1;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;75;-1719.132,-196.3194;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;98;-754.448,2057.854;Inherit;False;2;2;0;FLOAT4x4;0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.VertexToFragmentNode;70;-673.963,1019.563;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;33;-558.5723,1197.191;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;99;-954.9179,2183.195;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;52;-636.7393,290.6488;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;86;-469.8044,191.4613;Half;False;Property;_DepthFade;DepthFade;21;1;[Enum];Create;True;2;Off;0;On;1;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;106;-273.118,1283.498;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;83;-893.8044,58.4613;Inherit;False;True;True;False;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;100;-287.9185,2084.795;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.VertexToFragmentNode;76;-1423.197,-106.3944;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;62;-430.6586,985.6309;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;85;-496.8044,-81.5387;Half;False;Constant;_Float0;Float 0;12;0;Create;True;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;5;-1031.673,-140.4071;Inherit;True;Property;_MainTex;MainTex;0;0;Create;True;0;0;False;0;False;-1;None;53b60ab7ae7762144838d3c292ded649;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;63;-80.05846,810.7906;Inherit;False;smoothstep;-1;;2;3083023f861d54244a42071dfde96462;0;3;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;7;-1011.946,-324.9333;Inherit;False;Property;_MainColor;MainColor;1;1;[HDR];Create;True;0;0;False;0;False;0.5019608,0.5019608,0.5019608,0.5019608;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.NormalizeNode;101;-81.41866,2083.595;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;84;-245.8044,55.4613;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;102;-363.1795,2346.397;Half;False;Property;_FrontAndBehind;FrontAndBehind;23;0;Create;True;0;0;False;0;False;0;0;-20;20;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;9;-949.0684,-584.524;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;111;-44.80709,1280.654;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexToFragmentNode;50;-721.775,455.9667;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;54;-423.2387,369.0986;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;53;-965.5957,456.1364;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;57;-1131.878,626.0386;Inherit;False;2;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;55;-511.6581,548.6124;Half;False;Property;_Hardness;Hardness;18;0;Create;True;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;73;-216.2366,-703.4841;Inherit;False;2;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;69;151.053,-463.4582;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;46;-2254.654,1184.142;Half;False;Property;_DissolveSwitch;DissolveSwitch;13;1;[Enum];Create;True;2;Material;0;Particle;1;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;65;-2226.137,921.5905;Half;False;Property;_EdgeWidth;EdgeWidth;19;0;Create;True;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-169.7769,-327.4149;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;68;-206.3006,-531.2772;Float;False;Property;_EdgeColor;EdgeColor;20;1;[HDR];Create;True;0;0;False;0;False;0,0,0,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;51;-140.3291,228.3958;Inherit;False;smoothstep;-1;;3;3083023f861d54244a42071dfde96462;0;3;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;45;-1895.521,1038.315;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;48;489.7756,-285.7025;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;44;-2251.732,1061.345;Half;False;Property;_MaterialDissolve;MaterialDissolve;12;0;Create;True;0;0;False;0;False;0.5;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;40;240.3947,-107.5474;Inherit;False;6;6;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;103;202.321,2165.697;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;0;False;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;0,-212.3615;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;0;False;0;False;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;4;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;True;2;False;-1;False;False;False;False;False;True;1;LightMode=Meta;False;0;False;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;3;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;True;False;False;False;False;0;False;-1;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;0;False;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;822.1525,-212.3615;Half;False;True;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;Bohanshader/AlphaD2;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;7;False;False;False;True;2;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=50;True;2;0;True;1;5;False;-1;10;False;-1;1;0;False;-1;0;False;-1;False;False;False;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;2;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;0;False;0;0;Standard;21;Surface;1;  Blend;0;Two Sided;0;Cast Shadows;1;Receive Shadows;1;GPU Instancing;1;LOD CrossFade;0;Built-in Fog;0;Meta Pass;0;DOTS Instancing;0;Extra Pre Pass;0;Tessellation;0;  Phong;0;  Strength;0.5,False,-1;  Type;0;  Tess;16,False,-1;  Min;10,False,-1;  Max;25,False,-1;  Edge Length;16,False,-1;  Max Displacement;25,False,-1;Vertex Position,InvertActionOnDeselection;1;0;5;False;True;True;True;False;False;;0
WireConnection;91;0;88;0
WireConnection;91;1;89;0
WireConnection;114;0;112;0
WireConnection;114;1;113;0
WireConnection;71;0;49;3
WireConnection;122;0;127;0
WireConnection;122;1;120;0
WireConnection;116;0;115;2
WireConnection;116;1;114;0
WireConnection;66;1;49;2
WireConnection;123;0;121;2
WireConnection;123;1;122;0
WireConnection;92;0;90;2
WireConnection;92;1;91;0
WireConnection;118;0;117;0
WireConnection;118;1;116;0
WireConnection;67;0;49;1
WireConnection;67;1;66;0
WireConnection;93;0;95;0
WireConnection;93;1;92;0
WireConnection;124;0;126;0
WireConnection;124;1;123;0
WireConnection;56;0;72;0
WireConnection;94;0;93;0
WireConnection;119;0;118;0
WireConnection;58;1;56;0
WireConnection;82;0;80;0
WireConnection;82;1;81;0
WireConnection;64;0;67;0
WireConnection;64;1;49;2
WireConnection;125;0;124;0
WireConnection;104;1;125;0
WireConnection;77;0;79;2
WireConnection;77;1;82;0
WireConnection;59;0;64;0
WireConnection;59;1;58;0
WireConnection;42;1;94;0
WireConnection;41;1;119;0
WireConnection;108;0;104;1
WireConnection;108;1;104;4
WireConnection;108;2;107;0
WireConnection;75;0;74;0
WireConnection;75;1;77;0
WireConnection;98;0;97;0
WireConnection;98;1;96;0
WireConnection;70;0;59;0
WireConnection;33;0;41;1
WireConnection;33;1;41;4
WireConnection;33;2;15;0
WireConnection;52;0;42;1
WireConnection;106;0;33;0
WireConnection;106;1;108;0
WireConnection;106;2;109;0
WireConnection;83;0;87;0
WireConnection;100;0;98;0
WireConnection;100;1;99;0
WireConnection;76;0;75;0
WireConnection;62;0;52;0
WireConnection;62;1;70;0
WireConnection;5;1;76;0
WireConnection;63;6;62;0
WireConnection;63;7;49;3
WireConnection;101;0;100;0
WireConnection;84;0;85;0
WireConnection;84;1;83;0
WireConnection;84;2;86;0
WireConnection;111;0;106;0
WireConnection;50;0;53;0
WireConnection;54;0;52;0
WireConnection;54;1;50;0
WireConnection;53;0;67;0
WireConnection;53;1;57;0
WireConnection;57;1;56;0
WireConnection;69;0;73;0
WireConnection;69;1;8;0
WireConnection;69;2;51;0
WireConnection;8;0;9;0
WireConnection;8;1;7;0
WireConnection;8;2;5;0
WireConnection;51;6;54;0
WireConnection;51;7;49;3
WireConnection;48;0;69;0
WireConnection;48;3;63;0
WireConnection;40;0;63;0
WireConnection;40;1;7;4
WireConnection;40;2;5;4
WireConnection;40;3;9;4
WireConnection;40;4;84;0
WireConnection;40;5;111;0
WireConnection;103;0;101;0
WireConnection;103;1;102;0
WireConnection;1;2;48;0
WireConnection;1;3;40;0
WireConnection;1;5;103;0
ASEEND*/
//CHKSM=5418C1F8C173880E9DBE56EA2BC1AFFBD4792CFB
