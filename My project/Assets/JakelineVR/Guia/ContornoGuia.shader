// Contorno pulsante para la guia de pasos: dibuja las caras traseras de la malla, infladas hacia afuera.
// Se mide en metros del mundo, asi que el grosor no depende de la escala de cada objeto.
Shader "Cocina/ContornoGuia"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.85, 0.2, 1)
        _Ancho ("Ancho (metros)", Range(0, 0.03)) = 0.005
        _Velocidad ("Velocidad del pulso", Range(0, 10)) = 3
        _AlfaMinimo ("Opacidad minima del pulso", Range(0, 1)) = 0.35
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Prueba de profundidad", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Contorno"
            Cull Front
            ZWrite Off
            ZTest [_ZTest]
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vertice
            #pragma fragment Pixel
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Ancho;
                float _Velocidad;
                float _AlfaMinimo;
            CBUFFER_END

            struct Entrada
            {
                float4 posicion : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Salida
            {
                float4 posicion : SV_POSITION;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Salida Vertice(Entrada entrada)
            {
                Salida salida;
                UNITY_SETUP_INSTANCE_ID(entrada);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(salida);

                float3 mundo = TransformObjectToWorld(entrada.posicion.xyz);
                mundo += TransformObjectToWorldNormal(entrada.normal) * _Ancho;
                salida.posicion = TransformWorldToHClip(mundo);

                // El pulso es igual en toda la malla: se calcula aqui para no repetirlo en cada pixel.
                float pulso = 0.5 + 0.5 * sin(_Time.y * _Velocidad);
                salida.color = half4(_Color.rgb, _Color.a * lerp(_AlfaMinimo, 1.0, pulso));
                return salida;
            }

            half4 Pixel(Salida salida) : SV_Target
            {
                return salida.color;
            }
            ENDHLSL
        }
    }
}
