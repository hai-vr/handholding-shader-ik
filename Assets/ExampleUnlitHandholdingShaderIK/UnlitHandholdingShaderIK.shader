Shader "Hai/HandholdingShaderIKExample/UnlitShaderHandholdingShaderIK"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_EnableFakeArm ("Enable Fake arm (for use with gesture animation)", Float) = 1
		_BoneLength ("Length of a bone", Float) = 2000
		_ExtraForearmLength ("Addition length of the forearm and hand", Float) = 500
		_ExtraGrabRatio ("Ratio of extra reach toward light", Float) = 0.5
		_ShaderIKTargetLightIntensity ("Shader IK Target light intensity", Float) = 0.1234
	}
	SubShader
	{
		Tags {
		    "RenderType"="Opaque"
		    "LightMode" = "ForwardBase" // <-- important
        }
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
			#include "../HaiHandholdingShaderIK/HaiHandholdingShaderIK.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR0; // <-- important
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

            float _EnableFakeArm;
			float _BoneLength;
            float _ExtraForearmLength;
            float _ExtraGrabRatio;
            float _ShaderIKTargetLightIntensity;

			v2f vert (appdata v)
			{
				v2f o;

				// scale up the base mesh so that players with shaders disabled will not see the fake arm
				float4 visibleVertex = float4(v.vertex.xyz, 1);

                float4 outputVertex = UnityObjectToClipPos(
                    transformArm(
                        visibleVertex, // input vertex position
                        v.color, // input vertex color: hand and forearm are red (or blue), upperarm is green, the rest must be white
                        _ShaderIKTargetLightIntensity, // when set to a negative value, any black light will be matched
                        true, // when set to true, target will be the closest distance to the shoulder instead of the first match
                        float4(0.001, -0.002, -0.003, 1) + float4(
                            sin(_Time.y * 0.3) * 0.00002,
                            sin(_Time.y * 0.43) * 0.000035,
                            sin(_Time.y * 1.24) * 0.00015, 0), // hand rest position when no light matches or when it is too far
                        _BoneLength / 1000000, // length of the upper arm
                        (_BoneLength + _ExtraForearmLength) / 1000000, // length of the forearm up to the palm of the hand
                        (_BoneLength * _ExtraGrabRatio + _ExtraForearmLength) / 1000000, // arm will point towards the target even when out of reach, up to this extra length limit
                        false
                    )
                );

				o.vertex = outputVertex;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			[maxvertexcount(3)]
            void geom(triangle v2f IN[3], inout TriangleStream <v2f> tristream)
            {
                if (_EnableFakeArm < 0.5) {
                    return;
                }

                tristream.Append(IN[0]);
                tristream.Append(IN[1]);
                tristream.Append(IN[2]);
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
