// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Flowmap"
{
    Properties
    {
        // we have removed support for texture tiling/offset,
        // so make them not be displayed in material inspector
        _MainTex ("Texture", 2D) = "white" {}
        _FlowMap ("Flow Map", 2D) = "white" {}
        _FlowSpeed ("Flow Speed", Range(0, 10)) = 1
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            // use "vert" function as the vertex shader
            #pragma vertex vert
            // use "frag" function as the pixel (fragment) shader
            #pragma fragment frag
            #include "UnityCG.cginc"
            // vertex shader inputs
            struct appdata
            {
                float4 vertex : POSITION; // vertex position
                float2 uv : TEXCOORD0; // texture coordinate
            };

            // vertex shader outputs ("vertex to fragment")
            struct v2f
            {
                float2 uv : TEXCOORD0; // texture coordinate
                float4 vertex : SV_POSITION; // clip space position
            };
            
           
            
            // vertex shader
            v2f vert (appdata v)
            {
                v2f o;
                // transform position to clip space
                // (multiply with model*view*projection matrix)
                o.vertex = UnityObjectToClipPos(v.vertex);
                // just pass the texture coordinate
                o.uv = v.uv;
                return o;
            }
            
            // texture we will sample
            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _FlowMap;
            float4 _FlowMap_ST;
            float _FlowSpeed;


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

            // pixel shader; returns low precision ("fixed4" type)
            // color ("SV_Target" semantic)
            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float3 flow = DoFlowMap(uv);
                return float4(flow, 1.0);
            }
            ENDCG
        }
    }
}
