// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "DiscreteLatticeGauge/Monitor" {
	Properties {
		_MainTex    ("Configuration (ARGB32)", 2D) = "white" {}
	}

	SubShader {
        Tags{ "RenderType" = "Transparent" }

        Pass{

        Blend SrcAlpha OneMinusSrcAlpha
        Tags{ "RenderType" = "Transparent" }
        Lighting Off
        Cull Off
        //Fog { Mode off }
        BindChannels{
            Bind "Vertex", vertex
            Bind "TexCoord", texcoord
        }

	    CGINCLUDE
	    //#pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
	    //#pragma exclude_renderers molehill    
        //#pragma fragmentoption ARB_precision_hint_nicest
	    #include "UnityCG.cginc"

	    float4 _MainTex_ST;
        uniform sampler2D _MainTex;

	    struct v2f {
		    float4 pos : SV_POSITION;
		    float2 uv : TEXCOORD0;
	    };

	
	    v2f vert (appdata_full v)
	    {
		    v2f o;
		    o.pos = UnityObjectToClipPos(v.vertex);
		    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
		
		    return o;
	    }

        float4 frag(v2f i) : COLOR
        {
            return tex2D(_MainTex, i.uv);
        }

	    ENDCG

	    CGPROGRAM

	    #pragma vertex vert
	    #pragma fragment frag
	    #pragma fragmentoption ARB_precision_hint_nicest		


	    ENDCG
	    }
	
	}
}