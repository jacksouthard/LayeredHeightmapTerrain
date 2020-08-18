Shader "Custom/HeightmapGradient"
{
    Properties
    {
		_Color("Top Color", Color) = (1, 1, 1, 1)
		_Color2("Bottom Color", Color) = (0, 0, 0, 1)
		_Height("Height", Range(0,100)) = 10
		_Alpha("Alpha", Range(0,1)) = 1
	}
    SubShader
    {
		Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
		Blend SrcAlpha OneMinusSrcAlpha
		Cull off
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float3 uv : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float4 local : TEXCOORD1;
				half3 uv : TEXCOORD0;
				//UNITY_FOG_COORDS(1)
			};

			half _Height;

			v2f vert(appdata_t v)
			{
				v2f o;
				o.local = v.vertex;
				v.vertex.y *= _Height;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				//UNITY_TRANSFER_FOG(o, o.vertex);
				return o;
			}

		   fixed4 _Color;
		   fixed4 _Color2;

		   float _Alpha;

			fixed4 frag(v2f i) : SV_Target
			{				
				//UNITY_APPLY_FOG(i.fogCoord, _Color.rgb);
				float p = i.local.y;
				fixed4 c = lerp(_Color, _Color2, p);
				c.a = _Alpha;
				return c;
			}
			ENDCG
		}
	}
		//Fallback "Diffuse"
}
