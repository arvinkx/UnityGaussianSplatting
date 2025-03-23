Shader "Gaussian Splatting/Render Splats"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            ZWrite On
            ZTest LEqual
            Blend OneMinusDstAlpha One
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma require compute
            #pragma multi_compile_instancing

            #pragma shader_feature UNITY_STEREO_INSTANCING_ENABLED

            #include "UnityCG.cginc"
            #include "GaussianSplatting.hlsl"

            StructuredBuffer<uint> _OrderBuffer;

            struct v2f
            {
                half4 col : COLOR0;
                float2 pos : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            StructuredBuffer<SplatViewData> _SplatViewData;
            ByteAddressBuffer _SplatSelectedBits;
            uint _SplatBitsValid;
            float4 _ScreenParam;

            struct appdata
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o = (v2f)0;

                // Hardcoded vertex position
    o.vertex = float4(0, 0, 0.5, 1); // Example: Center of the screen, near the camera

    // Hardcoded color
    o.col = half4(1, 0, 0, 1); // Red
                return o;
            }

            // v2f vert(appdata v)
            // {
            //     v2f o = (v2f)0;
            //
            //     UNITY_SETUP_INSTANCE_ID(v);
            //     UNITY_INITIALIZE_OUTPUT(v2f, o);
            //     UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            //     #ifdef UNITY_STEREO_INSTANCING_ENABLED
            //     uint instID = _OrderBuffer[v.instanceID];
            //     
            //     uint splatIndex = instID / 2;
            //     uint viewIndex = instID % 2;
            //     //o.stereoTargetEyeIndex = viewIndex;
            //     // #else
            //     // uint instID = _OrderBuffer[v.instanceID];
            //     // uint splatIndex = instID / 2;
            //     // uint viewIndex = instID % 2;
            //     
            //
            //     SplatViewData view = _SplatViewData[instID];
            //     float4 centerClipPos = view.pos;
            //     bool behindCam = centerClipPos.w <= 0;
            //
            //     if (behindCam)
            //     {
            //         o.vertex = asfloat(0x7fc00000); // NaN discards the primitive
            //     }
            //     else
            //     {
            //         o.col.r = f16tof32(view.color.x >> 16);
            //         o.col.g = f16tof32(view.color.x);
            //         o.col.b = f16tof32(view.color.y >> 16);
            //         o.col.a = f16tof32(view.color.y);
            //
            //         uint idx = v.vertexID;
            //         float2 quadPos = float2(idx & 1, (idx >> 1) & 1) * 2.0 - 1.0;
            //         quadPos *= 2;
            //
            //         o.pos = quadPos;
            //
            //         float2 deltaScreenPos = (quadPos.x * view.axis1 + quadPos.y * view.axis2) * 2 / _ScreenParam.xy;
            //         o.vertex = centerClipPos;
            //         o.vertex.xy += deltaScreenPos * centerClipPos.w;
            //         
            //         // is this splat selected?
            //         if (_SplatBitsValid)
            //         {
            //             uint wordIdx = splatIndex / 32;
            //             uint bitIdx = splatIndex & 31;
            //             uint selVal = _SplatSelectedBits.Load(wordIdx * 4);
            //             if (selVal & (1 << bitIdx))
            //             {
            //                 o.col.a = -1;
            //             }
            //         }
            //     }
            //
            //     // FlipProjectionIfBackbuffer replacement (if needed)
            //     // This is often not needed on modern platforms
            //     // If you are seeing inverted images, you can add this back in.
            //     // if (_ProjectionParams.x < 0)
            //     // {
            //     //     o.vertex.y = -o.vertex.y;
            //     // }
            //     #endif
            //     
            //     return o;
            // }


            half4 frag(v2f i) : SV_Target
            {
                return i.col;
                
                float power = -dot(i.pos, i.pos);
                half alpha = exp(power);
                
                if (i.col.a >= 0)
                {
                    alpha = saturate(alpha * i.col.a);
                }
                else
                {
                    // "selected" splat: magenta outline, increase opacity, magenta tint
                    half3 selectedColor = half3(1, 0, 1);
                    if (alpha > 7.0 / 255.0)
                    {
                        if (alpha < 10.0 / 255.0)
                        {
                            alpha = 1;
                            i.col.rgb = selectedColor;
                        }
                        alpha = saturate(alpha + 0.3);
                    }
                    i.col.rgb = lerp(i.col.rgb, selectedColor, 0.5);
                }
                
                if (alpha < 1.0 / 255.0)
                    discard;
                
                half4 res = half4(i.col.rgb * alpha, alpha);
                return res;
            }
            ENDCG
        }
    }
}