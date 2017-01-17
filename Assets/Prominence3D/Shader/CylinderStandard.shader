Shader "Prominence3D/CylinderStanderd"
{
	Properties
	{
		_TintColor("Color", Color) = (1, 1, 1, 1)
		_MainTex("Texture", 2D) = "white" {}
	}

	SubShader
	{
		Pass
		{
			Tags
			{
				"Queue" = "9999999"
				"RenderType" = "Transparent"
			}
			Blend SrcAlpha OneMinusSrcAlpha 
			ZWrite Off
			ZTest Always
			Cull Off

			CGPROGRAM

#pragma vertex VS
#pragma fragment FS

			float4 _TintColor;
			sampler2D _MainTex;
			uniform float4x4 _matTop;
			uniform float4x4 _matBottom;

			struct VS_OUT
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			VS_OUT VS(float4 pos : POSITION, float2 uv : TEXCOORD0)
			{
				VS_OUT o;

				if (pos.z >-0.5)
				{
					//底
					o.pos = mul(pos, _matBottom);
				}
				else
				{
					//先
					o.pos = mul(pos, _matTop);
				}

				o.pos = mul(UNITY_MATRIX_MVP, o.pos);
				o.uv.x = 1.0f - uv.x;
				o.uv.y = uv.y;

				return o;
			}

			float4 FS(VS_OUT i) : COLOR
			{
				return _TintColor * tex2D(_MainTex, i.uv);
			}

			ENDCG
		}
	}
}