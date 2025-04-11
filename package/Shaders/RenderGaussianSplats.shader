Shader "Gaussian Splatting/Render Splats"
{
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent" "Queue"="Transparent"
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            Blend OneMinusDstAlpha One
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma require compute
            #pragma multi_compile_instancing

            #pragma shader_feature UNITY_STEREO_INSTANCING_ENABLED

            #include "GaussianSplatting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "HLSLSupport.cginc"

            StructuredBuffer<uint> _OrderBufferL;
            StructuredBuffer<uint> _OrderBufferR;
            uint _ViewCount;

            struct appdata
            {
                uint vertexID : SV_VertexID;
                #ifdef UNITY_STEREO_INSTANCING_ENABLED
                UNITY_VERTEX_INPUT_INSTANCE_ID
                #else
                uint instanceID : SV_InstanceID;
                #endif
            };

            struct v2f
            {
                half4 col : COLOR0;
                float2 pos : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            StructuredBuffer<SplatViewData> _SplatViewDataL;
            StructuredBuffer<SplatViewData> _SplatViewDataR;
            ByteAddressBuffer _SplatSelectedBits;
            uint _SplatBitsValid;

            v2f vert(appdata v)
            {
                v2f o = (v2f)0;

                #ifdef UNITY_STEREO_INSTANCING_ENABLED
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_OUTPUT(v2f, o);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                #endif
                uint splatIndex;
                SplatViewData view;
                if (unity_StereoEyeIndex == 0)
                {
                    splatIndex = _OrderBufferL[v.instanceID];
                    view = _SplatViewDataL[splatIndex];
                }
                else
                {
                    splatIndex = _OrderBufferR[v.instanceID];
                    view = _SplatViewDataR[splatIndex];
                }

                float4 centerClipPos = view.pos;
                bool behindCam = centerClipPos.w <= 0;
                if (behindCam)
                {
                    o.vertex = asfloat(0x7fc00000); // NaN discards the primitive
                }
                else
                {
                    o.col.r = f16tof32(view.color.x >> 16);
                    o.col.g = f16tof32(view.color.x);
                    o.col.b = f16tof32(view.color.y >> 16);
                    o.col.a = f16tof32(view.color.y);

                    uint idx = v.vertexID;
                    float2 quadPos = float2(idx & 1, (idx >> 1) & 1) * 2.0 - 1.0;
                    quadPos *= 2;

                    o.pos = quadPos;

                    float2 deltaScreenPos = (quadPos.x * view.axis1 + quadPos.y * view.axis2) * 2 / _ScreenParams.xy;
                    o.vertex = centerClipPos;
                    o.vertex.xy += deltaScreenPos * centerClipPos.w;

                    // is this splat selected?
                    if (_SplatBitsValid)
                    {
                        uint wordIdx = v.instanceID / 32;
                        uint bitIdx = v.instanceID & 31;
                        uint selVal = _SplatSelectedBits.Load(wordIdx * 4);
                        if (selVal & (1 << bitIdx))
                        {
                            o.col.a = -1;
                        }
                    }
                }
                FlipProjectionIfBackbuffer(o.vertex);
                return o;
            }


            half4 frag(v2f i) : SV_Target
            {
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

                return half4(i.col.rgb * alpha, alpha);
            }
            ENDHLSL
        }
    }
}