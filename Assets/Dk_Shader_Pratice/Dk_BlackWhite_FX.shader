Shader "Effect/Dk_Shader_Pratice/Dk_BlackWhite_FX"
{
    Properties
    {
        [HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
        [HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
        _MainTex("MainTex (Alpha Mask)", 2D) = "white" {}
        _Threshold("threshold", Range(0, 1)) = 0.149
        _Smooth("smooth", Range(0, 1)) = 0.2
        _Flip("Flip (0.5 as a threshold)", Range(0, 1)) = 0.473
        _WhiteColor("white_color", Color) = (1, 1, 1, 1)
        _BlackColor("black_color", Color) = (0, 0, 0, 1)
        [Toggle] _UseAlphaFlip("use alpha flip", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Int) = 8

        [HideInInspector] _QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector] _QueueControl("_QueueControl", Float) = -1
        [HideInInspector][ToggleOff] _ReceiveShadows("Receive Shadows", Float) = 1.0
    }

    SubShader
    {
        LOD 0

        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+100" "UniversalMaterialType"="Unlit" }

        Cull Off
        AlphaToMask Off

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UseColorTexture" }

            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            ZTest [_ZTest]
            Offset 0, 0
            ColorMask RGBA

            HLSLPROGRAM

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ASE_SRP_VERSION 140011

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            #define ASE_NEEDS_FRAG_SCREEN_POSITION
            #define ASE_NEEDS_FRAG_COLOR

            struct VertexInput
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 ase_color : COLOR;
                float4 ase_texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput
            {
                float4 positionCS : SV_POSITION;
                float4 clipPosV : TEXCOORD0;
                half4 ase_color : COLOR;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _WhiteColor;
            float4 _BlackColor;
            float _Threshold;
            float _Smooth;
            float _Flip;
            float _UseAlphaFlip;
            int _ZTest;
            CBUFFER_END

            // Global grabbed texture (set by Dk_GrabPassFeature)
            sampler2D _GrabbedTexture;
            sampler2D _MainTex;

            // Same function as FX_Distortion
            inline float4 ASE_ComputeGrabScreenPos( float4 pos )
            {
                #if UNITY_UV_STARTS_AT_TOP
                float scale = -1.0;
                #else
                float scale = 1.0;
                #endif
                float4 o = pos;
                o.y = pos.w * 0.5f;
                o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
                return o;
            }

            VertexOutput vert( VertexInput v )
            {
                VertexOutput o = (VertexOutput)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.ase_color = v.ase_color;
                o.uv = v.ase_texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;

                VertexPositionInputs vertexInput = GetVertexPositionInputs( v.positionOS.xyz );
                o.positionCS = vertexInput.positionCS;
                o.clipPosV = vertexInput.positionCS;
                return o;
            }

            half4 frag( VertexOutput IN ) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID( IN );
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

                // Screen position (same as FX_Distortion)
                float4 ScreenPos = ComputeScreenPos( IN.clipPosV );
                float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( ScreenPos );
                half4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;

                // Sample grabbed scene
                half3 sceneColor = tex2D( _GrabbedTexture, ase_grabScreenPosNorm.xy ).rgb;

                // Luminance
                half luminance = dot( sceneColor, half3(0.2126, 0.7152, 0.0722) );

                // Threshold + Smooth
                half bwMask = smoothstep(
                    _Threshold - _Smooth * 0.5,
                    _Threshold + _Smooth * 0.5,
                    luminance
                );

                // Flip
                bwMask = lerp( 1.0 - bwMask, bwMask, _Flip );

                // Color mapping
                half3 finalColor = lerp( _BlackColor.rgb, _WhiteColor.rgb, bwMask );

                // Alpha
                half4 texMask = tex2D( _MainTex, IN.uv );
                half alpha = texMask.a * IN.ase_color.a;
                alpha = lerp( alpha, 1.0 - alpha, _UseAlphaFlip );

                return half4( finalColor, alpha );
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Shader Graph/FallbackError"
}
