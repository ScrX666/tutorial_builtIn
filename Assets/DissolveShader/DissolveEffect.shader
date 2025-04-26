Shader "Custom/DissolveEffect"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _NoiseTex ("NoiseTex", 2D) = "white" {}
        _DissolveThreshold ("DissolveThreshold", Range(0, 1)) = 0.5
        _EdgeColor ("EdgeColor", Color) = (1, 0.5, 0, 1)
        _EdgeWidth ("EdgeWidth", Range(0, 0.2)) = 0.05
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
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NoiseTex;
            float _DissolveThreshold;
            float4 _EdgeColor;
            float _EdgeWidth;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 采样主纹理
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // 获取噪声纹理值
                float noiseValue = tex2D(_NoiseTex, i.uv).r;
                
                // 计算溶解效果
                float dissolveAmount = noiseValue - _DissolveThreshold;
                
                // 如果小于0，片段被丢弃（透明）
                clip(dissolveAmount);
                
                // 计算边缘效果
                if (dissolveAmount < _EdgeWidth)
                {
                    // 边缘区域显示发光效果
                    float edgeIntensity = dissolveAmount / _EdgeWidth;
                    col = lerp(_EdgeColor, col, edgeIntensity);
                }
                
                return col;
            }
            ENDCG
        }
    }
    
    Fallback "Diffuse"
} 