Shader "Custom/HairStylized" // 自定义的头发风格化着色器
{
    Properties
    {
        // 基础材质属性
        _Color ("Color", Color) = (1,1,1,1) // 基础颜色
        _MainTex ("Albedo (RGB)", 2D) = "white" {} // 漫反射贴图
        _Glossiness ("Smoothness", Range(0,1)) = 0.5 // 光滑度，控制高光锐利程度
        _Metallic ("Metallic", Range(0,1)) = 0.0 // 金属度，控制反射特性
        
        [Header(Hair Light)] // 头发特有的光照参数
        _LightMap ("Light Map", 2D) = "white" {} // 光照贴图，控制头发各部分的光照分布
        _LightWidth ("Light Width", Range(0.1, 10)) = 1.0 // 光照宽度，控制高光宽度
        _LightLength ("Light Length", Range(0.1, 10)) = 1.0 // 光照长度，控制高光长度
        _LightFeather ("Light Feather", Range(0, 1)) = 0.5 // 光照羽化，控制高光边缘柔和度
        _LightThreshold ("Light Threshold", Range(0, 1)) = 0.5 // 光照阈值，控制次高光出现的阈值
        _LightColor_H ("Light Color High", Color) = (1,1,1,1) // 主高光颜色
        _LightColor_L ("Light Color Low", Color) = (0.5,0.5,0.5,1) // 次高光颜色
        _LightIntShadow ("Light Intensity in Shadow", Range(0, 1)) = 0.5 // 阴影区域的光照强度
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" } // 不透明渲染类型
        LOD 200 // 细节层次

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc" // Unity通用着色器函数库
            #include "Lighting.cginc" // Unity光照函数库

            // 顶点着色器输入结构体
            struct appdata
            {
                float4 vertex : POSITION; // 顶点位置
                float3 normal : NORMAL; // 顶点法线
                float2 uv : TEXCOORD0; // UV坐标
            };

            // 顶点着色器到片元着色器的数据传递结构体
            struct v2f
            {
                float2 uv : TEXCOORD0; // UV坐标
                float4 vertex : SV_POSITION; // 裁剪空间位置
                float3 positionWS : TEXCOORD1; // 世界空间位置
                float3 normalWS : TEXCOORD2; // 世界空间法线
            };

            // 声明着色器属性变量
            sampler2D _MainTex;
            sampler2D _LightMap;
            float4 _MainTex_ST;
            fixed4 _Color;
            half _Glossiness;
            half _Metallic;
            
            float _LightWidth;
            float _LightLength;
            float _LightFeather;
            float _LightThreshold;
            float4 _LightColor_H;
            float4 _LightColor_L;
            float _LightIntShadow;

            // 顶点着色器函数
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); // 转换到裁剪空间
                o.uv = TRANSFORM_TEX(v.uv, _MainTex); // 应用纹理缩放和偏移
                o.positionWS = mul(unity_ObjectToWorld, v.vertex).xyz; // 计算世界空间位置
                o.normalWS = UnityObjectToWorldNormal(v.normal); // 计算世界空间法线
                return o;
            }

            // 片元着色器函数
            fixed4 frag (v2f i) : SV_Target
            {
                // 基础纹理和颜色采样
                fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
                
                // 光照相关向量计算
                fixed3 worldNormal = normalize(i.normalWS); // 归一化世界空间法线
                fixed3 worldLightDir = normalize(_WorldSpaceLightPos0.xyz); // 归一化世界空间光源方向
                fixed3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.positionWS)); // 归一化世界空间视角方向
                fixed3 worldHalfDir = normalize(worldLightDir + worldViewDir); // 归一化半角向量
                
                // 基础漫反射光照计算
                fixed NdotL = max(0, dot(worldNormal, worldLightDir)); // 法线点乘光源方向
                fixed3 diffuse = _LightColor0.rgb * albedo.rgb * NdotL; // 漫反射颜色
                // 头发特殊高光计算
                float4 lightMap = tex2D(_LightMap, i.uv); // 采样光照贴图
                float shadowStep = 1.0; // 可以添加阴影贴图采样，用于控制明暗区域
                
                // 向量缩写，提高代码可读性
                float3 L = worldLightDir; // 光源方向
                float3 V = worldViewDir; // 视角方向
                float3 H = worldHalfDir; // 半角向量
                float3 N = worldNormal; // 法线向量
                
                // 将法线和半角向量转换到观察空间，便于进行各向异性高光计算
                float3 NV = mul(UNITY_MATRIX_V, N); // 观察空间法线
                float3 HV = mul(UNITY_MATRIX_V, H); // 观察空间半角向量
                
                float NdotH = dot(normalize(NV.xz), normalize(HV.xz));
                NdotH = pow(NdotH, 6) * _LightWidth;//6控制高光锐利程度，可以替换为属性
                NdotH = pow(NdotH, 1 / _LightLength);//_LightLength控制高光长度
                
                // 高光羽化和平滑过渡计算
                float lightFeather = _LightFeather * NdotH; // 根据高光强度动态调整羽化程度
                float lightStepMax = saturate(1 - NdotH + lightFeather); // 高光过渡上限
                float lightStepMin = saturate(1 - NdotH - lightFeather); // 高光过渡下限
                
                // 计算主高光和次高光颜色
                float3 lightColor_H = smoothstep(lightStepMin, lightStepMax, clamp(lightMap.b, 0, 0.99)) * _LightColor_H.rgb; // 主高光颜色
                float3 lightColor_L = smoothstep(_LightThreshold, 1, lightMap.b) * _LightColor_L.rgb; // 次高光颜色
                float3 specular = (lightColor_H + lightColor_L) * lerp(1, _LightIntShadow, shadowStep); // 合并高光并应用阴影衰减
                
                // 环境光计算
                fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz * albedo.rgb; // 环境光颜色
                
                // 最终颜色合成
                fixed3 finalColor = ambient + diffuse + specular; // 环境光 + 漫反射 + 高光
                
                return fixed4(finalColor, albedo.a); // 返回最终颜色
            }
            ENDCG
        }
    }
    FallBack "Diffuse" // 如果shader不支持，回退到标准漫反射着色器
}
