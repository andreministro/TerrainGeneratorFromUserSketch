Shader "Terrain/Biome Blend"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture Atlas", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        [HideInInspector] _BlendBiomes ("Blend Biomes", Float) = 0
        [HideInInspector] _BiomeWeights1 ("Biome Weights 1", 2D) = "black" {}
        [HideInInspector] _BiomeWeights2 ("Biome Weights 2", 2D) = "black" {}
        [HideInInspector] _PaintedMask ("Painted Sketch Area", 2D) = "white" {}
        [HideInInspector] _ClipUnpainted ("Clip Unpainted", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        CGPROGRAM
        #pragma surface surf Lambert alphatest:_Cutoff addshadow
        #pragma target 3.5

        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
        sampler2D _BiomeWeights1;
        sampler2D _BiomeWeights2;
        sampler2D _PaintedMask;
        float4 _BiomeMapSize;
        float4 _BiomeTiles[7];
        float4 _BiomeBottomTiles[7];
        float4x4 _WorldToBiome;
        half4 _Color;
        half _BlendBiomes;
        half _ClipUnpainted;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float4 color : COLOR;
        };

        void surf(Input input, inout SurfaceOutput output)
        {
            float2 grid = mul(_WorldToBiome, float4(input.worldPos, 1)).xz;
            float2 mapUV = (grid + _BiomeMapSize.zw) / _BiomeMapSize.xy;
            if (_ClipUnpainted > 0.5)
                clip(tex2D(_PaintedMask, mapUV).r - 0.5);

            half4 color = tex2D(_MainTex, input.uv_MainTex);
            if (_BlendBiomes > 0.5 && input.color.r < 0.5)
            {
                half4 first = tex2D(_BiomeWeights1, mapUV);
                half3 second = tex2D(_BiomeWeights2, mapUV).rgb;
                half total = dot(first, half4(1,1,1,1)) + dot(second, half3(1,1,1));
                if (total > 0.001)
                {
                    // Fade the blend in where one land texture dominates the neighborhood.
                    half largest = max(max(max(first.r, first.g), max(first.b, first.a)),
                        max(second.r, max(second.g, second.b)));
                    half mixAmount = saturate((total - largest) * (16 / total));
                    if (mixAmount > 0.001)
                    {
                        half weights[7] = { first.r, first.g, first.b, first.a, second.r, second.g, second.b };
                        float2 atlasInset = _MainTex_TexelSize.xy * 4;
                        float2 tileUV = clamp(frac(input.uv_MainTex * 8), atlasInset, 1 - atlasInset);
                        half4 blended = 0;
                        [unroll]
                        for (int biome = 0; biome < 7; biome++)
                        {
                            if (weights[biome] > 0)
                            {
                                float4 tile = _BiomeTiles[biome];
                                float2 tileOrigin = input.color.b > 0.5 ? _BiomeBottomTiles[biome].xy
                                    : input.color.g > 0.5 ? tile.zw : tile.xy;
                                blended += weights[biome] * tex2D(_MainTex, (tileOrigin + tileUV) / 8);
                            }
                        }
                        color = lerp(color, blended / total, mixAmount);
                    }
                }
            }
            output.Albedo = color.rgb * _Color.rgb;
            output.Alpha = color.a * _Color.a;
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/Diffuse"
}
