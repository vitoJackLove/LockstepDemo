// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "KIIF/UI/UI Particle Default"
{
    Properties
    {
        [Header(Default)]
        [Space(30)]
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR]_Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 1
        
        [Space(30)]
        [Header(MainTexture)]
        [Space(5)]
        [Enum(UnityEngine.Rendering.CullMode)]_CullMode("开启双面", Float) = 2
        [Enum(Addtive,1,AlphaBlend,10)]_dst("叠加模式", Float) = 10
        _MainTexRotate("第一层贴图旋转", Range(0,1)) = 0
		_MainTexSpeedU("第一层贴图流动U", Float) = 0
		_MainTexSpeedV("第一层贴图流动V", Float) = 0
        [Toggle(_USE_RADIAL_ON)] _UseRadial("开启极坐标", Float) = 1
        
        [Space(30)]
        [Header(Mask1)]
        [Space(5)]
    	//[Toggle(_USE_MASK01_ON)] _UseMask01("开启第一层遮罩", Float) = 1
        _Mask01Tex("遮罩1", 2D) = "white" {}						
		_Mask01Rotate("第一层遮罩旋转", Range( 0,1)) = 0		
		_Mask01Power("第一层遮罩对比度", Float) = 1
		_Mask01SpeedU("第一层遮罩流动U", Float) = 0
		_Mask01SpeedV("第一层遮罩流动V", Float) = 0
        
        [Space(30)]
        [Header(Mask2)]
        [Space(5)]
        //[Toggle(_USE_MASK02_ON)] _UseMask02("开启第二层遮罩", Float) = 1
        _Mask02Tex("遮罩2", 2D) = "white" {}
		_Mask02Rotator("第二层遮罩旋转", Range( 0 , 1)) = 0
    	_Mask02Power("第二层遮罩对比度", Float) = 1
		_Mask02SpeedU("第二层遮罩流动U", Float) = 0
		_Mask02SpeedV("第二层遮罩流动V", Float) = 0
        
        [Space(30)]
        [Header(Distortion)]
        [Space(5)]
        [Toggle(_USE_DISTORTION_ON)] _UseDistortion("开启扰动效果", Float) = 0
		_DistortionTex("扰动图", 2D) = "black" {}
		_DistortionStrength("扰动强度", Range( 0 , 0.1)) = 0
		_DistortionSpeedU("扰动流动U", Float) = 0			
		_DistortionSpeedV("扰动流动V", Float) = 0
        
	    [Space(30)]
        [Header(Dissolve)]
        [Space(5)]
        [Toggle(_USE_DISSOLVE_ON)] _UseDissolve("开启溶解", Float) = 0
		_DissolveTex("溶解贴图", 2D) = "black" {}
		_DissolveSpeedU("溶解流动U", Float) = 0
		_DissolveSpeedV("溶解流动V", Float) = 0
		[Toggle(_USE_RADIALDISSOLVE_ON)] _UseRadialDissolve("开启极坐标", Float) = 0
		_DissolveStrength("溶解强度", Range( 0 , 1)) = 0
		_DissolveSoftStrength("软硬边强度", Range( 0 , 1)) = 0
		_DissolveWidth("溶解沟边宽度", Range( 0 , 1)) = 0
		[HDR]_DissolveColor("溶解沟边颜色", Color) = (0,0,0,0)
    	
    	[Space(30)]
        [Header(Shine)]
        [Space(5)]
    	[Toggle(_USE_SHINE_ON)] _UseShine("开启扫光", Float) = 0
    	_ShineTex("扫光贴图", 2D) = "black" {}
    	[HDR]_ShineColor("扫光颜色", Color) = (1,1,1,1)
        _ShineLightStrength("扫光贴图强度",Range(0,2)) = 0.1
        _ShineSpeed("扫光速度",Range(0,2)) = 1
        _ShineRotateAngel("旋转扫光贴图",Range(0,360)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull [_CullMode]
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha [_dst]
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

        

            // -------------------------------------
                // Material Keywords
            #pragma shader_feature_local _USERADIAL_ON
        // -------------------------------------
                // Material Keywords
            #pragma shader_feature_local _USE_RADIAL_ON			//开启主帖图极坐标
			// #pragma shader_feature_local _USE_MASK01_ON			//开启第一层遮罩
   //          #pragma shader_feature_local _USE_MASK02_ON			//开启第二层遮罩
            #pragma shader_feature_local _USE_DISTORTION_ON			//开启扰动图
			#pragma shader_feature_local _USE_DISSOLVE_ON		//开启溶解图
            #pragma shader_feature_local _USE_RADIALDISSOLVE_ON  //开启溶解图极坐标
			#pragma shader_feature_local _USE_SHINE_ON			//开启扫光效果

            struct appdata_t
            {
                float4 vertex        : POSITION;
                float4 color         : COLOR;
                float4 texcoord      : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 mask         : TEXCOORD2;
                float2 uv1          : TEXCOORD3;
            	half2 uv2           : TEXCOORD4;
            	half2 uv4          : TEXCOORD5;
            	float4 uv3			: TEXCOORD6;
                UNITY_VERTEX_OUTPUT_STEREO
            };

        //----------Default------------------
                sampler2D _MainTex;
                fixed4 _Color;
                fixed4 _TextureSampleAdd;
                float4 _ClipRect;
                float4 _MainTex_ST;
                float _UIMaskSoftnessX;
                float _UIMaskSoftnessY;
            //------------主图----------------------
                half _CullMode;
                half _dst;
                half _MainTexRotate;
                half _MainTexSpeedU;
                half _MainTexSpeedV;
            //--------------遮罩1--------------------
                half4 _Mask01Tex_ST;
                sampler2D _Mask01Tex;
                float _Mask01Rotate;
                float _Mask01Power;
                float _Mask01SpeedU;
                float _Mask01SpeedV;
            //--------------遮罩2--------------------
                half4 _Mask02Tex_ST;
                sampler2D _Mask02Tex;
                float _Mask02Rotator;
				float _Mask02Power;
                float _Mask02SpeedU;
                float _Mask02SpeedV;
            //--------------扰动图--------------------
                half4 _DistortionTex_ST;
                sampler2D _DistortionTex;
                float _DistortionStrength;
                float _DistortionSpeedU;
                float _DistortionSpeedV;
            //--------------溶解图--------------------
				half4 _DissolveTex_ST;
                sampler2D _DissolveTex;
				float _DissolveSpeedU;
				float _DissolveSpeedV;
				float _DissolveStrength;
				float _DissolveSoftStrength;
				float _DissolveWidth;
				float4 _DissolveColor;
        //--------------扫光图--------------------
				half4 _ShineTex_ST;
                sampler2D _ShineTex;
				float4 _ShineColor;
				float _ShineSpeed;
				float4 _LightColor;
				float _ShineLightStrength;
				float _ShineRotateAngel; 
            //------------------------------------
                sampler2D _SamplerNull;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (v.vertex.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
            	
                OUT.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                OUT.uv1 = TRANSFORM_TEX(v.texcoord.xy, _Mask01Tex);
            	OUT.uv2 = TRANSFORM_TEX(v.texcoord.xy, _Mask02Tex);
            	OUT.uv4 = TRANSFORM_TEX(v.texcoord.xy, _ShineTex);
            	OUT.uv3.xy = TRANSFORM_TEX(v.texcoord, _DissolveTex);
            	OUT.uv3.z = v.texcoord.z;
            	
                OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));
                OUT.color = v.color * _Color;
                return OUT;
            }
            
            fixed4 frag(v2f IN) : SV_Target
            {
                //Round up the alpha color coming from the interpolator (to 1.0/256.0 steps)
                //The incoming alpha could have numerical instability, which makes it very sensible to
                //HDR color transparency blend, when it blends with the world's texture.

                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0/alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision) * invAlphaPrecision;
                float time = fmod(_Time.y, 100);
                //-------------------------uv 流动--------------------
                float2 TexSpeedUV = float2(_MainTexSpeedU, _MainTexSpeedV);
				float2 SpeedUVTime = time * TexSpeedUV;
                //-------------------------极坐标---------------------
            	#ifdef _USE_RADIAL_ON
                float2 polar = ( IN.texcoord.xy * 2 - 1 );
                polar = float2(frac( atan2( polar.x , polar.y ) / UNITY_TWO_PI  ) , length( polar ));
                float2 Panner_TexSpeed = SpeedUVTime + polar;
                 //----------------------------------------------------
                float2 palarSpeeduv =  ( _MainTex_ST.xy * Panner_TexSpeed);
                #else
				 float2 palarSpeeduv = IN.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				 #endif
            	
               //----------------------uv旋转-------------------------
                float cos00 = cos(  _MainTexRotate * UNITY_TWO_PI );
				float sin00 = sin(  _MainTexRotate * UNITY_TWO_PI );
                float2 uv0 = mul( palarSpeeduv - float2( 0.5,0.5 ) , float2x2( cos00 , -sin00 , sin00 , cos00 )) + float2( 0.5,0.5 ) + SpeedUVTime;

                //----------------------uv扰动-------------------------
            	#ifdef _USE_DISTORTION_ON
            	float2 DistortionUV = (float2(_DistortionSpeedU , _DistortionSpeedV));
				float2 uv_DistortionTex = IN.texcoord.xy * _DistortionTex_ST.xy + _DistortionTex_ST.zw;
				float2 panner = time * DistortionUV + uv_DistortionTex;
				float4 DistortionTex = tex2D( _DistortionTex, panner );
				float FinalDistortion = ( DistortionTex.r * _DistortionStrength);
                uv0 = uv0 + FinalDistortion;
                #endif

                 //遮罩1
                //-------------------------uv 流动--------------------
            	//20240305 关键字无法打包
            	//#ifdef _USE_MASK01_ON
                float2 TexSpeedUV1 = float2(_Mask01SpeedU, _Mask01SpeedV);
				float2 SpeedUVTime1 = time * TexSpeedUV1;
                 //----------------------uv旋转-----------------------
                float cos01 = cos(  _Mask01Rotate * UNITY_TWO_PI );
				float sin01 = sin(  _Mask01Rotate * UNITY_TWO_PI );
                float2 uv1 = mul( IN.uv1.xy - float2( 0.5,0.5 ) , float2x2( cos01 , -sin01 , sin01 , cos01 )) + float2( 0.5,0.5 ) + SpeedUVTime1;
                float temp_mask1 = pow( tex2D( _Mask01Tex, uv1 ).r , _Mask01Power );
            	//#else
            	//float temp_mask1 = 1;
            	//#endif
                //return temp_mask1;

                 //遮罩2
                //-------------------------uv 流动--------------------
                 //#ifdef _USE_MASK02_ON
                half2 TexSpeedUV2 = half2(_Mask02SpeedU, _Mask02SpeedV);
				half2 SpeedUVTime2 = time * TexSpeedUV2;
                //----------------------uv旋转-----------------------
                half cos02 = cos(  _Mask02Rotator * UNITY_TWO_PI );
				half sin02 = sin(  _Mask02Rotator * UNITY_TWO_PI );
                half2 uv2 = mul( IN.uv2.xy - half2( 0.5,0.5 ) , half2x2( cos02 , -sin02 , sin02 , cos02 )) + half2( 0.5,0.5 ) + SpeedUVTime2;
				half temp_mask2 = pow(tex2D( _Mask02Tex, uv2 ).r,_Mask02Power);
                //#else
                // half temp_mask2 = 1;
                // #endif
            	
            	//-----------------mainTex----------------
                half4 mainTexColor = tex2D(_MainTex, uv0);

            	
            	//--------------------溶解------------------
            	//uv流动
            	half2 TexSpeedUV3 = (half2(_DissolveSpeedU , _DissolveSpeedV));
            	//极坐标
            	#ifdef _USE_RADIALDISSOLVE_ON
            	half2 polar1 = ( IN.uv3.xy * 2 - 1 );
                //polar1 = half2(frac( atan2( polar1.x , polar1.y ) / UNITY_TWO_PI  ) , length( polar1 ));
            	polar1 = half2(( atan2( polar1.x , polar1.y ) / UNITY_TWO_PI  ) , length( polar1 ));
            	half2 Panner_TexSpeed1 = half2(TexSpeedUV3 + polar1);
				half2 palarSpeeduv1 =  ( _DissolveTex_ST.xy * Panner_TexSpeed1 );
				#else
				half2 palarSpeeduv1 = IN.texcoord.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;;
				#endif
            	half2 palarSpeedUVST = ( palarSpeeduv1 + half2(_DissolveTex_ST.z , _DissolveTex_ST.w) );
                #ifdef _USE_DISTORTION_ON
				half2 uv3 = ( palarSpeedUVST + ( FinalDistortion.r * 1 ) );
				#else
				half2 uv3 = palarSpeedUVST;
				#endif
            	
            	half4 DissolvMap = tex2D( _DissolveTex, uv3 );
            	half percent = saturate((DissolvMap.r - (IN.uv3.z + _DissolveStrength)  + 0.001)/(max(0.0001,( _DissolveSoftStrength)  * (IN.uv3.z + _DissolveStrength) ) ));
				// half3 DissloveColor = lerp(mainTexColor.rgb,_DissolveColor1,step(percent,0.9) );
            	half PercentHard = saturate((DissolvMap.r - (IN.uv3.z + _DissolveStrength)  + 0.001)/(max(0.0001,( _DissolveWidth)  * (IN.uv3.z + _DissolveStrength) )));
            	half HardLineWidth = saturate(step(PercentHard,_DissolveWidth) - step(PercentHard,0));
            	half3 DissloveColorHard = lerp(mainTexColor.rgb,_DissolveColor,HardLineWidth); //硬边缘
            	
            	#ifdef _USE_DISSOLVE_ON
				half4 FinalColor = half4(DissloveColorHard, percent * mainTexColor.a );
				#else
				half4 FinalColor = mainTexColor;
				#endif

            	//--------------------扫光------------------
            	#ifdef _USE_SHINE_ON
				float angel = _ShineRotateAngel * 3.14159265359 / 180;//角度转弧度，因为正余弦采用弧度
                float2 lightUV = IN.uv4 - float2(0.5,0.5);//旋转前，改变轴心。不减的话，是以左下角(0,0)为轴心,减去后以(0.5,0.5)为轴心，这才是我们要的旋转效果
                lightUV = float2(lightUV.x * cos(angel) - lightUV.y * sin(angel),lightUV.y * cos(angel) + lightUV.x * sin(angel));//这里，应用旋转函数，把UV进行旋转
                lightUV = lightUV + float2(0.5,0.5);//加回偏移
                lightUV.x = lightUV.x + _ShineSpeed * time;//_Time的4个分量 float4 Time (t/20, t, t*2, t*3),//加上时间，让扫光动起来
            	float4 ShineMap = tex2D(_ShineTex, lightUV);
            	float3 FinalShine = lerp(FinalColor.rgb,  _ShineColor, saturate(ShineMap.r * _ShineLightStrength));
            	FinalColor.rgb = FinalShine;
            	#else
            	FinalColor = FinalColor;
            	#endif
            	
                half4  color =   IN.color * ( FinalColor + _TextureSampleAdd);
                half Alpha = (temp_mask1 * color.a * temp_mask2);
                //half4 color = IN.color * (_mainTexColor* _mainTexColor.a + _mainTex2Color * (1.0- _mainTexColor.a) + _TextureSampleAdd);
                //half4 color = IN.color * (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd);
                
                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
                #endif
                
                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif
                return half4(color.rgb,Alpha);
            }
        ENDCG
        }
    }
}
