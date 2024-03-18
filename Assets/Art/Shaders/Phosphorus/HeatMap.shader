// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Zavala/Heat Map"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Overlay"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="False"
        }

        Cull Off
        Lighting Off
        ZWrite Off
		ZTest Always
        Blend One OneMinusSrcAlpha
		BlendOp Add

		Pass {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
			#pragma multi_compile_instancing

			#include "UnityCG.cginc"

			fixed4 _Color;
			
			sampler2D _HeatMapTex;

            struct input_t {
                float3 vertex : POSITION;
                float color: TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct output_t {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
				UNITY_VERTEX_OUTPUT_STEREO
            };

            output_t vert (input_t v)
            {
                output_t o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float4 lutUV = float4(v.color, 0.5, 0, 0);
				float4 lut = tex2Dlod(_HeatMapTex, lutUV);

				o.vertex = float4(v.vertex, 1.0);
                o.vertex = UnityObjectToClipPos(o.vertex);
                o.color = _Color * lut;
                return o;
            }

            fixed4 frag (output_t i) : SV_Target
            {
                fixed4 c = i.color;
				c.rgb *= c.a;
				return c;
            }
        ENDCG
		}
	}
}