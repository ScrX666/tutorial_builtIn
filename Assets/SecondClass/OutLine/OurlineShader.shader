Shader "Unlit/OurlineShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Outline ("Outline", Range(0, 1)) = 0.01
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            // 正常物体渲染：渲染正面
            Cull Back            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 vert(float4 v : POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(v);
            }
            float4 frag() : SV_TARGET
            {
                return float4(1,1,1,1);
            }
            ENDCG
        }

        Pass
        {
            // 描边渲染：渲染背面
            Cull Front
            CGPROGRAM
            #include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag     

            float _Outline;
            fixed4 _OutlineColor;
            
            struct appdata
            {
                float4 pos : POSITION;
                float3 normal : NORMAL;
                // 3 - 使用切线空间法线外扩
                float3 tangent : TANGENT;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
            };
            // 1：基础使用模型空间法线外扩
            // v2f vert(appdata v)
            // {
            //     v2f o;
            //     //直接在模型空间外扩
            //     float3 pos = v.pos.xyz + v.tangent * _Outline; 
            //     o.pos = UnityObjectToClipPos(float4(pos,1));
            //     return o;
            // }

            // 2：使用NDC空间法线外扩 - 解决描边在近平面和远平面变形的问题
            v2f vert(appdata v)
            {
                v2f o;
                float4 pos = UnityObjectToClipPos(v.pos);
                float3 viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, v.tangent.xyz); // 4 - v.tangent.xyz
                // 将法线变换到裁切空间 乘以pos.w 是为了将法线变换到裁切空间
                float3 clipNormal = normalize(TransformViewToProjection(viewNormal.xyz)) * pos.w;
                pos.xy += 0.1 * _Outline * clipNormal .xy; 
                o.pos = pos;
                return o;
            }

            float4 frag(v2f i) : SV_TARGET
            {
                return float4(_OutlineColor.rgb, 1);
            }
            ENDCG
        }
    }
}
