// SPDX-License-Identifier: MIT
Shader "Gaussian Splatting/Debug/Render Points"
{
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent" "Queue"="Transparent"
        }
        Pass
        {
            ZWrite On
            ZTest LEqual

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
            
            struct v2f
            {
                half3 color : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _SplatSize;
            bool _DisplayIndex;
            int _SplatCount;

            struct appdata
            {
                uint vertexID : SV_VertexID;
                #ifdef UNITY_STEREO_INSTANCING_ENABLED
                UNITY_VERTEX_INPUT_INSTANCE_ID
                #else
                uint instanceID : SV_InstanceID;
                #endif
            };

            v2f vert(appdata v)
            {
               v2f o;
                #ifdef UNITY_STEREO_INSTANCING_ENABLED
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                #endif
                
                uint splatIndex = v.instanceID;
                SplatData splat = LoadSplatData(splatIndex);
                
                o.vertex = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4(splat.pos, 1.0)));
                
                uint idx = v.vertexID;
                float2 quadPos = float2(idx & 1, (idx >> 1) & 1) * 2.0 - 1.0;
                o.vertex.xy += (quadPos * _SplatSize / _ScreenParams.xy * o.vertex.w);
                
                o.color.rgb = saturate(splat.sh.col);
                
                if (_DisplayIndex)
                {
                    o.color.r = frac((float)splatIndex / (float)_SplatCount * 100);
                    o.color.g = frac((float)splatIndex / (float)_SplatCount * 10);
                    o.color.b = (float)splatIndex / (float)_SplatCount;
                }
                
                FlipProjectionIfBackbuffer(o.vertex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return half4(i.color.rgb, 1);
            }
            ENDHLSL
        }
    }
}