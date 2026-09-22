Shader "LevelLayouts/ForestSilhouette"
{
    Properties
    {
        _MainTex ("Forest sprite atlas", 2D) = "white" {}
        _Color ("Distance color", Color) = (0.32, 0.47, 0.46, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 _Color;
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            Output vert(Input input)
            {
                Output output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }
            fixed4 frag(Output input) : SV_Target
            {
                return fixed4(_Color.rgb, tex2D(_MainTex, input.uv).a * _Color.a);
            }
            ENDCG
        }
    }
}
