Shader "Custom/FresnelEffect"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _FresnelColor ("FresnelColor", Color) = (0, 1, 1, 1)
        _FresnelPower ("FresnelPower", Range(0.5, 10)) = 2.0
        _FresnelScale ("FresnelScale", Range(0, 2)) = 1.0
        _RimThreshold ("RimThreshold", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _FresnelColor;
            float _FresnelPower;
            float _FresnelScale;
            float _RimThreshold;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                // 将法线转换到世界空间
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                
                // 获取顶点的世界空间位置
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 采样主纹理
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // 计算视角方向
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                
                // 计算法线和视角方向的点积
                float fresnel = dot(viewDir, normalize(i.worldNormal));
                // 将点积值反转并调整为菲涅尔效果
                fresnel = saturate(1.0 - fresnel);
                fresnel = pow(fresnel, _FresnelPower) * _FresnelScale;
                
                // 应用菲涅尔效果
                float4 finalColor = col;
                
                // 在边缘处应用菲涅尔颜色
                if (fresnel > _RimThreshold)
                {
                    // 计算边缘融合
                    float rimIntensity = smoothstep(_RimThreshold, 1.0, fresnel);
                    finalColor = lerp(col, _FresnelColor, rimIntensity);
                }
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    Fallback "Diffuse"
} 