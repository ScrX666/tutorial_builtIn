Shader "Unlit/FXshader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        
        _EnableDissolve ("启用溶解效果", Float) = 0
        _DissolveMap ("Dissolve Map", 2D) = "white" {}
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _DissolveColor ("Dissolve Color", Color) = (1, 0, 0, 1)
        _DissolveWidth ("Dissolve Edge Width", Range(0, 0.1)) = 0.05
        
        _EnableFresnel ("启用菲涅尔效果", Float) = 0
        _FresnelColor ("菲涅尔颜色", Color) = (0, 1, 1, 1)
        _FresnelPower ("菲涅尔强度", Range(0.1, 10)) = 3
        _FresnelScale ("菲涅尔范围", Range(0, 1)) = 0.5
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
            #pragma shader_feature _ENABLEFRESNEL_ON
            #pragma shader_feature _ENABLEDISSOLVE_ON

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 dissolveUV : TEXCOORD2;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD3;
                float3 worldViewDir : TEXCOORD4;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _EnableDissolve;
            sampler2D _DissolveMap;
            float4 _DissolveMap_ST;
            float _DissolveAmount;
            float4 _DissolveColor;
            float _DissolveWidth;
            
            float _EnableFresnel;
            float4 _FresnelColor;
            float _FresnelPower;
            float _FresnelScale;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.dissolveUV = TRANSFORM_TEX(v.uv, _DissolveMap);
                
                // 计算世界空间法线和视角方向
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
                
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // --- 2 --- 添加溶解材质
                #ifdef _ENABLEDISSOLVE_ON
                    // 读取溶解贴图
                    fixed dissolveValue = tex2D(_DissolveMap, i.dissolveUV).r;
                    
                    // 计算溶解效果
                    clip(dissolveValue - _DissolveAmount);
                    
                    // 计算边缘效果
                    fixed edgeFactor = saturate((dissolveValue - _DissolveAmount) / _DissolveWidth);
                    
                    // 在边缘位置应用边缘颜色
                    if (edgeFactor < 1.0)
                    {
                        col = lerp(_DissolveColor, col, edgeFactor);
                    }
                #endif
                
                // --- 11 --- 添加菲涅尔效果
                #ifdef _ENABLEFRESNEL_ON
                    // 计算菲涅尔效果
                    float3 normalWS = normalize(i.worldNormal);
                    float3 viewDirWS = normalize(i.worldViewDir);
                    float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                    fresnel *= _FresnelScale;
                    col.rgb = lerp(col.rgb, _FresnelColor.rgb, fresnel);
                #endif
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    
    // --- 1 --- 指定 GUI
    CustomEditor "FXShaderGUI"
}
