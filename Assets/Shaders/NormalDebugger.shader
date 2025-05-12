Shader "Custom/NormalDebugger"
{  
    Properties
    {
        _MainTex("Texture", 2D) = "White" {}
    }

    SubShader
    {
        Tags {"Render Type" = "Opaque"}
        LOD 600

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 4.0

        // UNITY_DECLARE_TEX2D(_MainTex);

        struct Input {
            float3 worldPos : POSITION;
            float3 worldNormal : NORMAL;
            float2 uv_MainTex : TEXCOORD0;
        };

        sampler2D _MainTex;
        #define TARGET_ANGLE 3.0
        void surf(Input IN, inout SurfaceOutputStandard o) {
            float angle = degrees(acos(dot(float3(0,1,0),IN.worldNormal))); // [-90,90]
            float err = abs(TARGET_ANGLE-angle); // [0, 87]
            float color = pow(1.0-(90-TARGET_ANGLE-err)/(90-TARGET_ANGLE),3);
            // o.Albedo = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo += float3(1.0-color, color, 0);
        }

        ENDCG
    }

    Fallback "Diffuse"
}
