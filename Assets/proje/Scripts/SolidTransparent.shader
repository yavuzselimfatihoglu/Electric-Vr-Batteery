Shader "Custom/SolidTransparent"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,0.5)
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType"="Transparent"}
        LOD 100

        // İŞTE SİHİRLİ KISIM BURASI:
        // ZWrite On: Unity'ye "Ben şeffafım ama katı bir cisim gibi derinliğim var" der.
        // Bu sayede nesnenin arka yüzleri ön yüzünden görünmez.
        ZWrite On
        
        // Standart şeffaflık karışımı
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sadece belirlediğimiz rengi döndür
                return _Color;
            }
            ENDCG
        }
    }
}