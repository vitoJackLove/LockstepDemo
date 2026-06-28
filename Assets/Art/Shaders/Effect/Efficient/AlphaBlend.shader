// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Particles/AlphaBlended" 
{
    Properties
    {
        [HDR] _TintColor("色调", Color) = (1,1,1,1)
        _MainTex ("主贴图", 2D) = "white" {}
        _MainTexSpeed("主贴图流动速度xy", Vector) = (0,0,0,0)
        _MoveToCamera("移向摄像机", Range(-1, 1)) = 0
        _SoftParticle("软粒子", Range(0, 1)) = 0
        [Header(Mask)]
        [Space]
        [Toggle(_UseMask)] _UseMask("启用遮罩图", float) = 0
        _MaskTex("遮罩图", 2D) = "white" {}
        [Enum(R,0,A,1)] _MaskChannel("遮罩图通道选择", Float) = 0
        [Header(Seting)]
        [Space]
        [HideInInspector] _GameSpeed("GameSpeed", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode("Cull Mode", Int) = 2
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("Src Blend",Int) = 5
        [Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("Dst Blend",Int) = 10
        [Enum(UnityEngine.Rendering.CompareFunction)]_ZTest("Depth Test",Int) = 4
    }

    Category 
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline" 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+50" 
        }
        Blend [_SrcBlend][_DstBlend]
        ColorMask RGBA
        Cull [_CullMode]
        Lighting Off 
        ZWrite Off
        Ztest [_ZTest]

        SubShader 
        {
            Pass 
            {
                HLSLPROGRAM

                #pragma multi_compile_local _ _UseMask

                #pragma vertex vert
                #pragma fragment frag

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

                struct appdata_t 
                {
                    float4 vertex : POSITION;
                    half4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f 
                {
                    float4 vertex : SV_POSITION;
                    half4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                    half4 ScreenPosition : TEXCOORD1;
                    
				    UNITY_VERTEX_INPUT_INSTANCE_ID
				    UNITY_VERTEX_OUTPUT_STEREO
                };

                
                CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _TintColor;
                float4 _MainTexSpeed;
                half _MoveToCamera;
                
                float4 _MaskTex_ST;
                half _MaskChannel;
                
                float _GameSpeed;
                half _SoftParticle;
                CBUFFER_END

                sampler2D _MainTex;
                sampler2D _MaskTex;

                v2f vert (appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                    float3 cameraPositionOS = TransformWorldToObject(GetCameraPositionWS());
                    half3 vertexValue = normalize(cameraPositionOS - v.vertex.xyz) * _MoveToCamera;
                    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                    o.vertex = TransformWorldToHClip(worldPos + vertexValue);
                    o.color = v.color * _TintColor;
                    o.texcoord = v.texcoord;
                    // 屏幕坐标
                    o.ScreenPosition = ComputeScreenPos(o.vertex);
                    return o;
                }


                half4 frag (v2f i) : SV_Target
                {
                    const float customTime = _GameSpeed * _TimeParameters.x;        // 项目时停效果修改材质动画速率
                    half4 col = i.color * tex2D(_MainTex, TRANSFORM_TEX(i.texcoord, _MainTex) + _MainTexSpeed.xy * customTime);
                    col.a = saturate(col.a);

                #ifdef _UseMask
                    half4 mask = tex2D(_MaskTex, TRANSFORM_TEX(i.texcoord, _MaskTex));
                    col.a *= lerp(mask.r, mask.a, _MaskChannel);
                #endif

                    // half2 screenPosNorm = i.ScreenPosition.xy / i.ScreenPosition.w;
                    // half depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenPosNorm.xy));
                    float rawDepth = SampleSceneDepth(i.ScreenPosition.xy / i.ScreenPosition.w).r;
                    float depth = LinearEyeDepth(rawDepth, _ZBufferParams);
                    col.a *= saturate((depth - i.ScreenPosition.w) / _SoftParticle);

                    return col;
                }
                ENDHLSL
            }
        }
    }
}
