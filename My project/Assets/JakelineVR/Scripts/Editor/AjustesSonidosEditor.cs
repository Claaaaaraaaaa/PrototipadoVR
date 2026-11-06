using UnityEditor;
using UnityEngine;

/// <summary>Panel de AjustesSonidos: un bloque por sonido, con boton para probarlo durante el Play.</summary>
[CustomEditor(typeof(AjustesSonidos))]
public class AjustesSonidosEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("mostrarDebug"));

        if (!EditorApplication.isPlaying)
            EditorGUILayout.HelpBox("Dale a Play para usar los botones de Probar. Los cambios hechos durante el Play se conservan.", MessageType.Info);

        SerializedProperty lista = serializedObject.FindProperty("ajustes");
        for (int i = 0; i < lista.arraySize; i++)
        {
            SerializedProperty ajuste = lista.GetArrayElementAtIndex(i);
            SerializedProperty sonido = ajuste.FindPropertyRelative("sonido");

            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(sonido.enumDisplayNames[sonido.enumValueIndex], EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(ajuste.FindPropertyRelative("inicio"));
                EditorGUILayout.PropertyField(ajuste.FindPropertyRelative("retraso"));
                EditorGUILayout.PropertyField(ajuste.FindPropertyRelative("volumen"));
                EditorGUILayout.PropertyField(ajuste.FindPropertyRelative("duracion"));

                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || SonidosCocina.Instancia == null))
                {
                    if (GUILayout.Button("Probar"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        SonidosCocina.Instancia.Probar((Sonido)sonido.enumValueIndex);
                    }
                }
            }
        }

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        if (GUILayout.Button("Guardar ajustes"))
            AssetDatabase.SaveAssets();
    }
}
