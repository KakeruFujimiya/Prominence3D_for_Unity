Shader "Prominence3D/Add"
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
		Blend SrcAlpha One
			ZWrite Off
			ZTest Always
			Cull Off

			CGPROGRAM

#pragma vertex VS
#pragma fragment FS

			float4 _TintColor;
		sampler2D _MainTex;
		uniform float4x4 _matTexture;

		struct VS_OUT
		{
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
		};

		VS_OUT VS(float4 pos : POSITION, float4 uv : TEXCOORD0)
		{
			VS_OUT o;

			o.pos = mul(UNITY_MATRIX_MVP, pos);
			o.uv = mul(_matTexture, uv).xy;
			//o.uv.x = 1.0f - o.uv.x;
			o.uv.y = 1.0f - o.uv.y;


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