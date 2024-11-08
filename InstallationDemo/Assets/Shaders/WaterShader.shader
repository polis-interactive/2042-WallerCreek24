Shader "Unlit/WaterShader"
{
    Properties
    {
        _Resolution ("Resolution", Vector) = (512, 512, 0, 0)
        _Gamma ("Gamma", Float) = 1
        _Speed ("Speed", Float) = 1
        _Scale ("Scale", Float) = 1
        _Brightness ("Brightness", Float) = 0.5
        _Contrast ("Contrast", Float) = 1
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
            // #pragma multi_compile_fog

            #include "UnityCG.cginc"

            // Uniforms
            float2 _Resolution;
            float _Gamma;
            float _Speed;
            float _Scale;
            float _Brightness;
            float _Contrast;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 hsb2rgb(float3 c)
            {
                float3 rgb = saturate(abs(fmod(c.x * 6.0 + float3(0.0, 4.0, 2.0), 6.0) - 3.0) - 1.0);
                rgb = rgb * rgb * (3.0 - 2.0 * rgb);
                return c.z * lerp(float3(1.0, 1.0, 1.0), rgb, c.y);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t1 = _Time * _Speed;
                float2 chunkSize = 1.0 / _Resolution;
                float2 snappedUV = floor(i.uv / chunkSize) * chunkSize * _Scale;

                float2 p = fmod(snappedUV * 6.28318530718, 6.28318530718) - 250.0;
                float2 iter = p;
                float c = 1.0;
                float inten = 0.005;
                for (int n = 0; n < 5; n++)
                {
                    float t = t1 * (1.0 - (3.5 / (float)(n + 1)));
                    iter = p + float2(cos(t - iter.x) + sin(t + iter.y), sin(t - iter.y) + cos(t + iter.x));
                    c += 1.0 / length(float2(p.x / (sin(iter.x + t) / inten), p.y / (cos(iter.y + t) / inten)));
                }
                c /= 5.0;
                c = 1.17 - pow(c, 1.4);
                float value = pow(abs(c), 8.0);
                value += _Brightness;
                value = lerp(0.5, value, _Contrast);
                value = pow(value, _Gamma);
                return float4(value, value, value, 1.0);

            }
            ENDCG
        }
    }
    FallBack "Unlit/Texture"
}
