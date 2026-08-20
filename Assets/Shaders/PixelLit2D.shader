// Sprite-Lit-Default 克隆版：光照 UV 量化到光贴图像素网格，
// 双线性采样退化为点采样 → 光照/阴影边缘块状像素风
Shader "Echo/PixelLit2D"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _EdgeSoftness("Shadow Edge Softness", Range(0,1)) = 0.3

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex CombinedShapeLightVertex
            #pragma fragment CombinedShapeLightFragment

			// GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __
            #pragma multi_compile _ DEBUG_DISPLAY

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2  uv          : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                half4   color       : COLOR;
                float2  uv          : TEXCOORD0;
                half2   lightingUV  : TEXCOORD1;
                #if defined(DEBUG_DISPLAY)
                float3  positionWS  : TEXCOORD2;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            float4 _LightTexSize; // xy=光贴图像素数 zw=倒数，PixelLighting 脚本每帧设置

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);
            half4 _MainTex_ST;
            float4 _Color;
            half4 _RendererColor;

            #if USE_SHAPE_LIGHT_TYPE_0
            SHAPE_LIGHT(0)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_1
            SHAPE_LIGHT(1)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_2
            SHAPE_LIGHT(2)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_3
            SHAPE_LIGHT(3)
            #endif

            Varyings CombinedShapeLightVertex(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

#ifdef UNITY_INSTANCING_ENABLED
                v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteFlip);
#endif
                o.positionCS = TransformObjectToHClip(v.positionOS);
                #if defined(DEBUG_DISPLAY)
                o.positionWS = TransformObjectToWorld(v.positionOS);
                #endif
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);

                o.color = v.color * _Color * _RendererColor;
#ifdef UNITY_INSTANCING_ENABLED
                o.color *= unity_SpriteColor;
#endif
                return o;
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

            half _HDREmulationScale;
            half _UseSceneLighting;
            half _EdgeSoftness;

            // 量化点采样 + 四邻取 min：暗区各向膨胀 1 光素，盖住光素栅格相位缝隙
            #define SAMPLE_LIGHT_MIN(tex, samp, quv) \
                min(SAMPLE_TEXTURE2D(tex, samp, quv), \
                min(SAMPLE_TEXTURE2D(tex, samp, quv + half2(_LightTexSize.z, 0)), \
                min(SAMPLE_TEXTURE2D(tex, samp, quv - half2(_LightTexSize.z, 0)), \
                min(SAMPLE_TEXTURE2D(tex, samp, quv + half2(0, _LightTexSize.w)), \
                    SAMPLE_TEXTURE2D(tex, samp, quv - half2(0, _LightTexSize.w))))))

            // CombinedShapeLightShared 改版：量化点采样 + 按 _EdgeSoftness 混入双线性采样，
            // 阴影/光照边缘留 1 个光素宽的过渡带（0=纯块状，越大越柔）
            half4 PixelCombinedShapeLightShared(in SurfaceData2D surfaceData, in InputData2D inputData, in half2 rawLightingUV)
            {
                #if defined(DEBUG_DISPLAY)
                half4 debugColor = 0;
                if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
                {
                    return debugColor;
                }
                #endif

                half alpha = surfaceData.alpha;
                half4 color = half4(surfaceData.albedo, alpha);
                const half4 mask = surfaceData.mask;
                const half2 qUV = (floor(rawLightingUV * _LightTexSize.xy) + 0.5) * _LightTexSize.zw;

                if (alpha == 0.0)
                    discard;

#if USE_SHAPE_LIGHT_TYPE_0
                half4 shapeLight0 = lerp(
                    SAMPLE_LIGHT_MIN(_ShapeLightTexture0, sampler_ShapeLightTexture0, qUV),
                    SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, rawLightingUV),
                    _EdgeSoftness);

                if (any(_ShapeLightMaskFilter0))
                {
                    half4 processedMask = (1 - _ShapeLightInvertedFilter0) * mask + _ShapeLightInvertedFilter0 * (1 - mask);
                    shapeLight0 *= dot(processedMask, _ShapeLightMaskFilter0);
                }

                half4 shapeLight0Modulate = shapeLight0 * _ShapeLightBlendFactors0.x;
                half4 shapeLight0Additive = shapeLight0 * _ShapeLightBlendFactors0.y;
#else
                half4 shapeLight0Modulate = 0;
                half4 shapeLight0Additive = 0;
#endif

#if USE_SHAPE_LIGHT_TYPE_1
                half4 shapeLight1 = lerp(
                    SAMPLE_LIGHT_MIN(_ShapeLightTexture1, sampler_ShapeLightTexture1, qUV),
                    SAMPLE_TEXTURE2D(_ShapeLightTexture1, sampler_ShapeLightTexture1, rawLightingUV),
                    _EdgeSoftness);

                if (any(_ShapeLightMaskFilter1))
                {
                    half4 processedMask = (1 - _ShapeLightInvertedFilter1) * mask + _ShapeLightInvertedFilter1 * (1 - mask);
                    shapeLight1 *= dot(processedMask, _ShapeLightMaskFilter1);
                }

                half4 shapeLight1Modulate = shapeLight1 * _ShapeLightBlendFactors1.x;
                half4 shapeLight1Additive = shapeLight1 * _ShapeLightBlendFactors1.y;
#else
                half4 shapeLight1Modulate = 0;
                half4 shapeLight1Additive = 0;
#endif

#if USE_SHAPE_LIGHT_TYPE_2
                half4 shapeLight2 = lerp(
                    SAMPLE_LIGHT_MIN(_ShapeLightTexture2, sampler_ShapeLightTexture2, qUV),
                    SAMPLE_TEXTURE2D(_ShapeLightTexture2, sampler_ShapeLightTexture2, rawLightingUV),
                    _EdgeSoftness);

                if (any(_ShapeLightMaskFilter2))
                {
                    half4 processedMask = (1 - _ShapeLightInvertedFilter2) * mask + _ShapeLightInvertedFilter2 * (1 - mask);
                    shapeLight2 *= dot(processedMask, _ShapeLightMaskFilter2);
                }

                half4 shapeLight2Modulate = shapeLight2 * _ShapeLightBlendFactors2.x;
                half4 shapeLight2Additive = shapeLight2 * _ShapeLightBlendFactors2.y;
#else
                half4 shapeLight2Modulate = 0;
                half4 shapeLight2Additive = 0;
#endif

#if USE_SHAPE_LIGHT_TYPE_3
                half4 shapeLight3 = lerp(
                    SAMPLE_LIGHT_MIN(_ShapeLightTexture3, sampler_ShapeLightTexture3, qUV),
                    SAMPLE_TEXTURE2D(_ShapeLightTexture3, sampler_ShapeLightTexture3, rawLightingUV),
                    _EdgeSoftness);

                if (any(_ShapeLightMaskFilter3))
                {
                    half4 processedMask = (1 - _ShapeLightInvertedFilter3) * mask + _ShapeLightInvertedFilter3 * (1 - mask);
                    shapeLight3 *= dot(processedMask, _ShapeLightMaskFilter3);
                }

                half4 shapeLight3Modulate = shapeLight3 * _ShapeLightBlendFactors3.x;
                half4 shapeLight3Additive = shapeLight3 * _ShapeLightBlendFactors3.y;
#else
                half4 shapeLight3Modulate = 0;
                half4 shapeLight3Additive = 0;
#endif

                half4 finalOutput;
#if !USE_SHAPE_LIGHT_TYPE_0 && !USE_SHAPE_LIGHT_TYPE_1 && !USE_SHAPE_LIGHT_TYPE_2 && ! USE_SHAPE_LIGHT_TYPE_3
                finalOutput = color;
#else
                half4 finalModulate = shapeLight0Modulate + shapeLight1Modulate + shapeLight2Modulate + shapeLight3Modulate;
                half4 finalAdditve = shapeLight0Additive + shapeLight1Additive + shapeLight2Additive + shapeLight3Additive;
                finalOutput = _HDREmulationScale * (color * finalModulate + finalAdditve);
#endif

                finalOutput.a = alpha;
                finalOutput = lerp(color, finalOutput, _UseSceneLighting);

                return max(0, finalOutput);
            }

            half4 CombinedShapeLightFragment(Varyings i) : SV_Target
            {
                const half4 main = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                const half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);
                SurfaceData2D surfaceData;
                InputData2D inputData;

                InitializeSurfaceData(main.rgb, main.a, mask, surfaceData);
                InitializeInputData(i.uv, i.lightingUV, inputData);

                return PixelCombinedShapeLightShared(surfaceData, inputData, i.lightingUV);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            // GPU Instancing
            #pragma multi_compile_instancing

            struct Attributes
            {
                float3 positionOS   : POSITION;
                half4 color         : COLOR;
                float2 uv           : TEXCOORD0;
                float4 tangent      : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS      : SV_POSITION;
                half4   color           : COLOR;
                float2  uv              : TEXCOORD0;
                half3   normalWS        : TEXCOORD1;
                half3   tangentWS       : TEXCOORD2;
                half3   bitangentWS     : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            half4 _NormalMap_ST;

            Varyings NormalsRenderingVertex(Attributes attributes)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(attributes);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

#ifdef UNITY_INSTANCING_ENABLED
                attributes.positionOS = UnityFlipSprite(attributes.positionOS, unity_SpriteFlip);
#endif
                o.positionCS = TransformObjectToHClip(attributes.positionOS);
                o.uv = TRANSFORM_TEX(attributes.uv, _NormalMap);
                o.color = attributes.color;
                o.normalWS = -GetViewForwardDir();
                o.tangentWS = TransformObjectToWorldDir(attributes.tangent.xyz);
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * attributes.tangent.w;
#ifdef UNITY_INSTANCING_ENABLED
                o.color *= unity_SpriteColor;
#endif
                return o;
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"

            half4 NormalsRenderingFragment(Varyings i) : SV_Target
            {
                const half4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                const half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv));

                return NormalsRenderingShared(mainTex, normalTS, i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            // GPU Instancing
            #pragma multi_compile_instancing

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS      : SV_POSITION;
                float4  color           : COLOR;
                float2  uv              : TEXCOORD0;
                #if defined(DEBUG_DISPLAY)
                float3  positionWS  : TEXCOORD2;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Color;
            half4 _RendererColor;

            Varyings UnlitVertex(Attributes attributes)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(attributes);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

#ifdef UNITY_INSTANCING_ENABLED
                attributes.positionOS = UnityFlipSprite(attributes.positionOS, unity_SpriteFlip);
#endif
                o.positionCS = TransformObjectToHClip(attributes.positionOS);
                #if defined(DEBUG_DISPLAY)
                o.positionWS = TransformObjectToWorld(attributes.positionOS);
                #endif
                o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
                o.color = attributes.color * _Color * _RendererColor;
#ifdef UNITY_INSTANCING_ENABLED
                o.color *= unity_SpriteColor;
#endif
                return o;
            }

            float4 UnlitFragment(Varyings i) : SV_Target
            {
                float4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                #if defined(DEBUG_DISPLAY)
                SurfaceData2D surfaceData;
                InputData2D inputData;
                half4 debugColor = 0;

                InitializeSurfaceData(mainTex.rgb, mainTex.a, surfaceData);
                InitializeInputData(i.uv, inputData);
                SETUP_DEBUG_DATA_2D(inputData, i.positionWS);

                if(CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
                {
                    return debugColor;
                }
                #endif

                return mainTex;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
