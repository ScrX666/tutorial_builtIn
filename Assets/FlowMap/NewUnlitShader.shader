Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FlowMap ("FlowMap", 2D) = "white" {}
        _FlowSpeed ("FlowSpeed", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _FlowMap;
            float4 _FlowMap_ST;
            float _FlowSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float3 DoFlowMap(float2 uv)
            {
                float3 flowCol = tex2D(_FlowMap, uv);
                float2 flowDir = (flowCol.rg * 2.0 - 1.0);
                
                float phase0 = frac(_Time.y * 0.1 * _FlowSpeed + 0.5);
                float phase1 = frac(_Time.y * 0.1 * _FlowSpeed + 1.0);
                
                float2 tilingUV = uv * _MainTex_ST.xy + _MainTex_ST.zw;
                float3 tex0 = tex2D(_MainTex, tilingUV - flowDir * phase0);
                float3 tex1 = tex2D(_MainTex, tilingUV - flowDir * phase1);
                
                float flowLerp = abs((0.5 - phase0) / 0.5);
            
                return lerp(tex0, tex1, flowLerp);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                float3 flow = DoFlowMap(i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return float4(flow,1);
            }
            ENDCG
        }
    }
}
