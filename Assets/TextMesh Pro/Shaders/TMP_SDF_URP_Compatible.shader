Shader "TextMeshPro/URP SDF" {
    Properties {
        [PerRendererData] _MainTex        ("Font Atlas", 2D) = "white" {}
        _FaceTex                          ("Face Texture", 2D) = "white" {}
        _FaceUVSpeedX                     ("Face UV Speed X", Range(-5, 5)) = 0.0
        _FaceUVSpeedY                     ("Face UV Speed Y", Range(-5, 5)) = 0.0
        [HDR]_FaceColor                   ("Face Color", Color) = (1,1,1,1)
        _FaceDilate                       ("Face Dilate", Range(-1,1)) = 0
        _FaceShininess                    ("Face Shininess", Range(0,1)) = 0
        [HDR]_OutlineColor                ("Outline Color", Color) = (0,0,0,1)
        _OutlineTex                       ("Outline Texture", 2D) = "white" {}
        _OutlineUVSpeedX                  ("Outline UV Speed X", Range(-5, 5)) = 0.0
        _OutlineUVSpeedY                  ("Outline UV Speed Y", Range(-5, 5)) = 0.0
        _OutlineWidth                     ("Outline Thickness", Range(0, 1)) = 0
        _OutlineSoftness                  ("Outline Softness", Range(0,1)) = 0
        _Outline2Color                    ("Outline 2 Color", Color) = (0,0,0,1)
        _Outline2Width                    ("Outline 2 Width", Range(0, 1)) = 0
        _OutlineOffset1                   ("Outline Offset 1", vector) = (0,0,0,0)
        _OutlineOffset2                   ("Outline Offset 2", vector) = (0,0,0,0)
        _OutlineOffset3                   ("Outline Offset 3", vector) = (0,0,0,0)
        _OutlineMode                      ("Outline Mode", Float) = 0
        _IsoPerimeter                     ("Iso Perimeter", Range(0,1)) = 0
        _Softness                         ("Softness", Range(0,1)) = 0
        _Bevel                            ("Bevel", Range(0,1)) = 0.5
        _BevelOffset                      ("Bevel Offset", Range(-0.5,0.5)) = 0
        _BevelWidth                       ("Bevel Width", Range(-.5,.5)) = 0
        _BevelClamp                       ("Bevel Clamp", Range(0,1)) = 0
        _BevelRoundness                   ("Bevel Roundness", Range(0,1)) = 0
        _BumpMap                          ("Normal map", 2D) = "bump" {}
        _BumpOutline                      ("Bump Outline", Range(0,1)) = 0
        _BumpFace                         ("Bump Face", Range(0,1)) = 0
        _ReflectFaceColor                 ("Reflection Color", Color) = (0,0,0,1)
        _ReflectOutlineColor              ("Reflection Color", Color) = (0,0,0,1)
        _Cube                             ("Reflection Cubemap", Cube) = "black" {}
        _EnvMatrixRotation                ("Texture Rotation", vector) = (0, 0, 0, 0)
        [HDR]_UnderlayColor               ("Border Color", Color) = (0,0,0, 0.5)
        _UnderlayOffsetX                  ("Border OffsetX", Range(-1,1)) = 0
        _UnderlayOffsetY                  ("Border OffsetY", Range(-1,1)) = 0
        _UnderlayDilate                   ("Border Dilate", Range(-1,1)) = 0
        _UnderlaySoftness                 ("Border Softness", Range(0,1)) = 0
        _UnderlayOffset                   ("Underlay Offset", vector) = (0,0,0,0)
        _UnderlayIsoPerimeter             ("Underlay Iso Perimeter", Range(0,1)) = 0
        [HDR]_GlowColor                   ("Color", Color) = (0, 1, 0, 0.5)
        _GlowOffset                       ("Offset", Range(-1,1)) = 0
        _GlowInner                        ("Inner", Range(0,1)) = 0.05
        _GlowOuter                        ("Outer", Range(0,1)) = 0.05
        _GlowPower                        ("Falloff", Range(1, 0)) = 0.75
        _WeightNormal                     ("Weight Normal", float) = 0
        _WeightBold                       ("Weight Bold", float) = 0.5
        _ShaderFlags                      ("Flags", float) = 0
        _ScaleRatioA                      ("Scale RatioA", float) = 1
        _ScaleRatioB                      ("Scale RatioB", float) = 1
        _ScaleRatioC                      ("Scale RatioC", float) = 1
        _TextureWidth                     ("Texture Width", float) = 512
        _TextureHeight                    ("Texture Height", float) = 512
        _GradientScale                    ("Gradient Scale", float) = 5.0
        _ScaleX                           ("Scale X", float) = 1.0
        _ScaleY                           ("Scale Y", float) = 1.0
        _PerspectiveFilter                ("Perspective Correction", Range(0, 1)) = 0.875
        _Sharpness                        ("Sharpness", Range(-1,1)) = 0
        _VertexOffsetX                    ("Vertex OffsetX", float) = 0
        _VertexOffsetY                    ("Vertex OffsetY", float) = 0
        _Padding                          ("Padding", float) = 1
        _MaskCoord                        ("Mask Coordinates", vector) = (0, 0, 32767, 32767)
        _ClipRect                         ("Clip Rect", vector) = (-32767, -32767, 32767, 32767)
        _UseClipRect                      ("Use Clip Rect", Float) = 0
        _MaskID                           ("Mask ID", Float) = 0
        _MaskSoftnessX                    ("Mask SoftnessX", float) = 0
        _MaskSoftnessY                    ("Mask SoftnessY", float) = 0
        _StencilComp                      ("Stencil Comparison", Float) = 8
        _Stencil                          ("Stencil ID", Float) = 0
        _StencilOp                        ("Stencil Operation", Float) = 0
        _StencilWriteMask                 ("Stencil Write Mask", Float) = 255
        _StencilReadMask                  ("Stencil Read Mask", Float) = 255
        _CullMode                         ("Cull Mode", Float) = 0
        _ColorMask                        ("Color Mask", Float) = 15
    }

    SubShader {
        Tags {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Stencil {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull [_CullMode]
        ZWrite Off
        ZTest LEqual
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass {
            Name "Default"
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature __ BEVEL_ON
            #pragma shader_feature __ UNDERLAY_ON UNDERLAY_INNER
            #pragma shader_feature __ GLOW_ON
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define fixed  half
            #define fixed2 half2
            #define fixed3 half3
            #define fixed4 half4

            #include "TMPro.cginc"

            TEXTURE2D(_MainTex);     SAMPLER(sampler_MainTex);
            TEXTURE2D(_FaceTex);     SAMPLER(sampler_FaceTex);
            TEXTURE2D(_OutlineTex);  SAMPLER(sampler_OutlineTex);
            TEXTURE2D(_BumpMap);     SAMPLER(sampler_BumpMap);
            TEXTURECUBE(_Cube);      SAMPLER(sampler_Cube);

            CBUFFER_START(UnityPerMaterial)
                float4 _FaceTex_ST;
                float4 _OutlineTex_ST;
                float4 _ClipRect;
                float4 _MaskCoord;
                float4 _FaceColor;
                float4 _OutlineColor;
                float4 _Outline2Color;
                float4 _UnderlayColor;
                float4 _GlowColor;
                float4 _ReflectFaceColor;
                float4 _ReflectOutlineColor;
                float4 _SpecularColor;
                float4 _OutlineOffset1;
                float4 _OutlineOffset2;
                float4 _OutlineOffset3;
                float4 _UnderlayOffset;
                float  _FaceDilate;
                float  _FaceShininess;
                float  _OutlineWidth;
                float  _OutlineSoftness;
                float  _Outline2Width;
                float  _OutlineMode;
                float  _IsoPerimeter;
                float  _Softness;
                float  _Bevel;
                float  _BevelOffset;
                float  _BevelWidth;
                float  _BevelClamp;
                float  _BevelRoundness;
                float  _BumpFace;
                float  _BumpOutline;
                float  _GlowOffset;
                float  _GlowInner;
                float  _GlowOuter;
                float  _GlowPower;
                float  _WeightNormal;
                float  _WeightBold;
                float  _ScaleRatioA;
                float  _ScaleRatioB;
                float  _ScaleRatioC;
                float  _TextureWidth;
                float  _TextureHeight;
                float  _GradientScale;
                float  _ScaleX;
                float  _ScaleY;
                float  _PerspectiveFilter;
                float  _Sharpness;
                float  _MaskSoftnessX;
                float  _MaskSoftnessY;
                float  _VertexOffsetX;
                float  _VertexOffsetY;
                float  _FaceUVSpeedX;
                float  _FaceUVSpeedY;
                float  _OutlineUVSpeedX;
                float  _OutlineUVSpeedY;
                float  _LightAngle;
                float  _Reflectivity;
                float  _SpecularPower;
                float  _Diffuse;
                float  _Ambient;
                float  _ShaderFlags;
                float  _Padding;
                float  _UseClipRect;
                float  _MaskID;
                float  _UnderlayDilate;
                float  _UnderlaySoftness;
                float  _UnderlayOffsetX;
                float  _UnderlayOffsetY;
                float  _UnderlayIsoPerimeter;
                float  _CullMode;
                float  _ColorMask;
                float  _StencilComp;
                float  _Stencil;
                float  _StencilOp;
                float  _StencilWriteMask;
                float  _StencilReadMask;
            CBUFFER_END

            float4x4 _EnvMatrix;
            float4   _EnvMatrixRotation;

            struct Attributes {
                float4 position : POSITION;
                float3 normal   : NORMAL;
                fixed4 color    : COLOR;
                float2 texcoord0: TEXCOORD0;
                float2 texcoord1: TEXCOORD1;
            };

            struct Varyings {
                float4 position    : SV_POSITION;
                fixed4 color       : COLOR;
                float2 atlas       : TEXCOORD0;
                float4 param       : TEXCOORD1;
                float4 mask        : TEXCOORD2;
                float3 viewDir     : TEXCOORD3;
            #if (UNDERLAY_ON || UNDERLAY_INNER)
                float4 texcoord2   : TEXCOORD4;
                fixed4 underlayColor: COLOR1;
            #endif
                float4 textures    : TEXCOORD5;
            };

            Varyings Vert(Attributes input) {
                Varyings output = (Varyings)0;

                float bold = step(input.texcoord1.y, 0);
                float4 vert = input.position;
                vert.x += _VertexOffsetX;
                vert.y += _VertexOffsetY;
                float4 worldPos = mul(UNITY_MATRIX_M, vert);
                float4 vPosition = mul(UNITY_MATRIX_VP, worldPos);

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(_ScaleX, _ScaleY) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float scale = rsqrt(dot(pixelSize, pixelSize));
                scale *= abs(input.texcoord1.y) * _GradientScale * (_Sharpness + 1);
                if (UNITY_MATRIX_P[3][3] == 0) {
                    float3 worldNormal = TransformObjectToWorldNormal(input.normal.xyz);
                    float3 worldView = normalize(_WorldSpaceCameraPos.xyz - worldPos.xyz);
                    scale = lerp(abs(scale) * (1 - _PerspectiveFilter), scale, abs(dot(worldNormal, worldView)));
                }

                float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
                weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;
                float bias = (.5 - weight) + (.5 / scale);
                float alphaClip = (1.0 - _OutlineWidth * _ScaleRatioA - _OutlineSoftness * _ScaleRatioA);

            #if GLOW_ON
                alphaClip = min(alphaClip, 1.0 - _GlowOffset * _ScaleRatioB - _GlowOuter * _ScaleRatioB);
            #endif
                alphaClip = alphaClip / 2.0 - (.5 / scale) - weight;

            #if (UNDERLAY_ON || UNDERLAY_INNER)
                float4 underlayColor = _UnderlayColor;
                underlayColor.rgb *= underlayColor.a;
                float bScale = scale;
                bScale /= 1 + ((_UnderlaySoftness * _ScaleRatioC) * bScale);
                float bBias = (0.5 - weight) * bScale - 0.5 - ((_UnderlayDilate * _ScaleRatioC) * 0.5 * bScale);
                float x = -(_UnderlayOffsetX * _ScaleRatioC) * _GradientScale / _TextureWidth;
                float y = -(_UnderlayOffsetY * _ScaleRatioC) * _GradientScale / _TextureHeight;
                float2 bOffset = float2(x, y);
            #endif

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (vert.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
                float2 textureUV = UnpackUV(input.texcoord1.x);
                float2 faceUV = textureUV * _FaceTex_ST.xy + _FaceTex_ST.zw;
                float2 outlineUV = textureUV * _OutlineTex_ST.xy + _OutlineTex_ST.zw;

                output.position = vPosition;
                output.color = input.color;
                output.atlas = input.texcoord0;
                output.param = float4(alphaClip, scale, bias, weight);
                output.mask = half4(vert.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_MaskSoftnessX, _MaskSoftnessY) + pixelSize.xy));
                output.viewDir = mul((float3x3)_EnvMatrix, _WorldSpaceCameraPos.xyz - worldPos.xyz);
            #if (UNDERLAY_ON || UNDERLAY_INNER)
                output.texcoord2 = float4(input.texcoord0 + bOffset, bScale, bBias);
                output.underlayColor = underlayColor;
            #endif
                output.textures = float4(faceUV, outlineUV);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target {
                float c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.atlas).a;
            #ifndef UNDERLAY_ON
                clip(c - input.param.x);
            #endif

                float scale  = input.param.y;
                float bias   = input.param.z;
                float weight = input.param.w;
                float sd = (bias - c) * scale;

                float outline  = (_OutlineWidth  * _ScaleRatioA) * scale;
                float softness = (_OutlineSoftness * _ScaleRatioA) * scale;

                half4 faceColor    = _FaceColor;
                half4 outlineColor = _OutlineColor;

                faceColor.rgb *= input.color.rgb;
                faceColor    *= SAMPLE_TEXTURE2D(_FaceTex,    sampler_FaceTex,    input.textures.xy + float2(_FaceUVSpeedX,    _FaceUVSpeedY)    * _Time.y);
                outlineColor *= SAMPLE_TEXTURE2D(_OutlineTex, sampler_OutlineTex, input.textures.zw + float2(_OutlineUVSpeedX, _OutlineUVSpeedY) * _Time.y);

                faceColor = GetColor(sd, faceColor, outlineColor, outline, softness);

            #if BEVEL_ON
                float3 dxy = float3(0.5 / _TextureWidth, 0.5 / _TextureHeight, 0);
                float3 n = GetSurfaceNormal(input.atlas, weight, dxy);
                float3 bump = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.textures.xy + float2(_FaceUVSpeedX, _FaceUVSpeedY) * _Time.y)).xyz;
                bump *= lerp(_BumpFace, _BumpOutline, saturate(sd + outline * 0.5));
                n = normalize(n - bump);
                float3 light = normalize(float3(sin(_LightAngle), cos(_LightAngle), -1.0));
                float3 col = GetSpecular(n, light);
                faceColor.rgb += col * faceColor.a;
                faceColor.rgb *= 1 - (dot(n, light) * _Diffuse);
                faceColor.rgb *= lerp(_Ambient, 1, n.z * n.z);
                float4 reflcol = SAMPLE_TEXTURECUBE(_Cube, sampler_Cube, reflect(input.viewDir, -n));
                faceColor.rgb += reflcol.rgb * lerp(_ReflectFaceColor.rgb, _ReflectOutlineColor.rgb, saturate(sd + outline * 0.5)) * faceColor.a;
            #endif

            #if UNDERLAY_ON
                float d = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord2.xy).a * input.texcoord2.z;
                faceColor += input.underlayColor * saturate(d - input.texcoord2.w) * (1 - faceColor.a);
            #endif
            #if UNDERLAY_INNER
                float d = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord2.xy).a * input.texcoord2.z;
                faceColor += input.underlayColor * (1 - saturate(d - input.texcoord2.w)) * saturate(1 - sd) * (1 - faceColor.a);
            #endif

            #if GLOW_ON
                float4 glowColor = GetGlowColor(sd, scale);
                faceColor.rgb += glowColor.rgb * glowColor.a;
            #endif

            #if UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
                faceColor *= m.x * m.y;
            #endif
            #if UNITY_UI_ALPHACLIP
                clip(faceColor.a - 0.001);
            #endif

                return half4(faceColor.rgb, faceColor.a * input.color.a);
            }
            ENDHLSL
        }
    }

    Fallback "TextMeshPro/Mobile/Distance Field"
    CustomEditor "TMPro.EditorUtilities.TMP_SDFShaderGUI"
}
