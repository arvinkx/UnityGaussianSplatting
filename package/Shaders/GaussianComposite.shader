Shader "Hidden/Gaussian Splatting/Composite"
{
   SubShader
   {
      Tags { "RenderType" = "Opaque" }

      Pass
      {
         ZWrite Off
         ZTest Off
         Cull Off
         Blend SrcAlpha OneMinusSrcAlpha
    
         HLSLPROGRAM

         #pragma vertex vert
         #pragma fragment frag

         #pragma shader_feature UNITY_STEREO_INSTANCING_ENABLED

         #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
         #include "HLSLSupport.cginc"
         #include "UnityInstancing.cginc"

         TEXTURE2D_ARRAY(_GaussianSplatRTArray);
         TEXTURE2D(_GaussianSplatRT);

         struct appdata
         {
            uint vtxID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
         };

         struct v2f
         {
            float4 vertex : SV_POSITION;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID 
            UNITY_VERTEX_OUTPUT_STEREO
         };

         v2f vert (appdata v)
         {
            v2f o;

            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_OUTPUT(v2f, o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            uint vtxID = v.vtxID;
            float2 quadPos = float2(vtxID & 1, (vtxID >> 1) & 1) * 4.0 - 1.0;
            o.vertex = float4(quadPos, 1, 1);

            return o;
         }

         float4 frag (v2f i) : SV_Target
         {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
            #ifdef UNITY_STEREO_INSTANCING_ENABLED
            return _GaussianSplatRTArray.Load(int4(i.vertex.xy, unity_StereoEyeIndex, 0));
            #else
            return _GaussianSplatRT.Load(int3(i.vertex.xy, 0));
            #endif
         }
         ENDHLSL
      }
   }
}
