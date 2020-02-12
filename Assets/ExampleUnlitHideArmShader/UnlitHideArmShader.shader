Shader "Hai/HandholdingShaderIKExample/UnlitHideArmShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_EnableFakeArm ("Enable Fake arm (for use with gesture animation)", Float) = 1
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
			#pragma geometry geom
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 color : TEXCOORD1;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float _EnableFakeArm;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			[maxvertexcount(3)]
            void geom(triangle v2f IN[3], inout TriangleStream <v2f> tristream)
            {
                for (int i = 0; i < 3; i ++)
                {
                    if (_EnableFakeArm > 0.5 && IN[i].color.g == 0 && IN[i].color.b == 0) {
                        return;
                    }
                    tristream.Append(IN[i]);
                }
                tristream.RestartStrip();
            }
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
