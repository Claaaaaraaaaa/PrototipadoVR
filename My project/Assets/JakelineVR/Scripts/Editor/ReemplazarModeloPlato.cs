using Oculus.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu JakelineVR > Reemplazar modelo del plato.
/// Cambia el modelo provisional del plato ("rondon en plato") por los modelos definitivos:
/// PLATOOOPORFIN (plato vacio) y plaatorondonbx (plato servido), sin perder nada de lo que hace funcionar
/// al plato: el script Plato, el collider del cucharon, el SnapInteractor y el tag "Plato" se pasan a un
/// objeto nuevo "Plato Logica" que tiene a los dos modelos de hijos.
/// Los modelos se escalan al diametro del plato viejo y se apoyan a la misma altura, para que los snaps,
/// la mesa y el cucharon sigan calzando. Se puede deshacer con Ctrl+Z.
/// </summary>
public static class ReemplazarModeloPlato
{
    private const string RutaVacio = "Assets/Claaaaaraaaaa/OBJETOS PINTADOS/PLATOOOPORFIN.fbx";
    private const string RutaServido = "Assets/JakelineVR/Picados_ingredientes/plaatorondonbx.fbx";
    private const string RutaMaterialVacio = "Assets/Claaaaaraaaaa/OBJETOS PINTADOS/color platooooo.mat";
    private const string RutaMaterialServido = "Assets/JakelineVR/Picados_ingredientes/color plato rondon.mat";
    private const string NombreLogica = "Plato Logica";
    private const float AlturaMinimaCaja = 0.06f;   // metros: margen para que el cucharon toque el collider

    [MenuItem("JakelineVR/Reemplazar modelo del plato")]
    private static void Reemplazar()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Reemplazar modelo del plato", "Sal de Play primero.", "OK");
            return;
        }

        InspeccionPlato inspeccion = Object.FindFirstObjectByType<InspeccionPlato>(FindObjectsInactive.Include);
        if (inspeccion == null) { Error("No hay ningun objeto con InspeccionPlato en la escena."); return; }

        Plato viejo = inspeccion.GetComponentInChildren<Plato>(true);
        if (viejo == null) { Error("No encontre el script Plato dentro de " + inspeccion.name + "."); return; }
        if (viejo.name == NombreLogica) { Error("El plato ya tiene los modelos nuevos (existe '" + NombreLogica + "')."); return; }

        GameObject vacioAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RutaVacio);
        GameObject servidoAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RutaServido);
        if (vacioAsset == null) { Error("No encontre " + RutaVacio); return; }
        if (servidoAsset == null) { Error("No encontre " + RutaServido); return; }

        if (!EditorUtility.DisplayDialog("Reemplazar modelo del plato",
                "Se cambiara '" + viejo.name + "' por PLATOOOPORFIN (vacio) y plaatorondonbx (servido) dentro de '" +
                inspeccion.name + "'.\n\nSe puede deshacer con Ctrl+Z. ¿Continuar?", "Reemplazar", "Cancelar"))
            return;

        Undo.SetCurrentGroupName("Reemplazar modelo del plato");
        int grupo = Undo.GetCurrentGroup();

        // 1. Medidas del plato vacio viejo (la base "Cylinder"), para que el nuevo quede igual de grande y a la misma altura.
        Bounds medidaVieja = MedidaBaseVieja(viejo);

        // 2. Objeto logico en el mismo lugar que el modelo viejo, con sus componentes.
        GameObject logica = new GameObject(NombreLogica);
        Undo.RegisterCreatedObjectUndo(logica, "Crear " + NombreLogica);
        logica.layer = viejo.gameObject.layer;
        logica.transform.SetParent(viejo.transform.parent, false);
        logica.transform.SetPositionAndRotation(viejo.transform.position, viejo.transform.rotation);
        logica.transform.localScale = Vector3.one;
        logica.transform.SetSiblingIndex(viejo.transform.GetSiblingIndex());

        Plato plato = (Plato)Copiar(viejo, logica);
        BoxCollider caja = viejo.GetComponent<BoxCollider>() != null ? (BoxCollider)Copiar(viejo.GetComponent<BoxCollider>(), logica) : Undo.AddComponent<BoxCollider>(logica);
        int copiados = 2;
        foreach (SnapInteractor snap in viejo.GetComponents<SnapInteractor>()) { Copiar(snap, logica); copiados++; }
        foreach (TagSet tags in viejo.GetComponents<TagSet>()) { Copiar(tags, logica); copiados++; }

        // 3. Plato vacio: se usa el PLATOOOPORFIN que ya esta en la escena (con su material) o se crea uno.
        GameObject vacio = BuscarEnRaiz(RutaVacio);
        if (vacio != null)
            Undo.SetTransformParent(vacio.transform, logica.transform, "Mover PLATOOOPORFIN");
        else
        {
            vacio = (GameObject)PrefabUtility.InstantiatePrefab(vacioAsset, logica.transform);
            Undo.RegisterCreatedObjectUndo(vacio, "Crear PLATOOOPORFIN");
        }
        Undo.RecordObject(vacio.transform, "Ajustar PLATOOOPORFIN");
        vacio.transform.localRotation = vacioAsset.transform.localRotation;
        vacio.transform.localPosition = Vector3.zero;
        AsignarMaterialSiFalta(vacio, RutaVacio, RutaMaterialVacio);
        Encajar(vacio, medidaVieja);

        // 4. Plato servido: mismo diametro y misma base que el vacio, empieza oculto.
        GameObject servido = (GameObject)PrefabUtility.InstantiatePrefab(servidoAsset, logica.transform);
        Undo.RegisterCreatedObjectUndo(servido, "Crear plaatorondonbx");
        servido.transform.localRotation = servidoAsset.transform.localRotation;
        servido.transform.localPosition = Vector3.zero;
        AsignarMaterialSiFalta(servido, RutaServido, RutaMaterialServido);
        Encajar(servido, MedidaMundo(vacio));
        servido.SetActive(false);

        // 5. Collider ajustado al plato nuevo (con altura minima para que el cucharon lo toque).
        Undo.RecordObject(caja, "Ajustar collider");
        Bounds local = MedidaLocal(vacio, logica.transform);
        float alturaMinima = AlturaMinimaCaja / Mathf.Max(0.0001f, logica.transform.lossyScale.y);
        if (local.size.y < alturaMinima)
        {
            float fondo = local.min.y;
            local.size = new Vector3(local.size.x, alturaMinima, local.size.z);
            local.center = new Vector3(local.center.x, fondo + alturaMinima * 0.5f, local.center.z);
        }
        caja.center = local.center;
        caja.size = local.size;

        // 6. El script Plato cambia de un modelo al otro al servir.
        SerializedObject so = new SerializedObject(plato);
        so.FindProperty("platoVacio").objectReferenceValue = vacio;
        so.FindProperty("platoServido").objectReferenceValue = servido;
        so.FindProperty("nombreBasePlato").stringValue = vacio.name;
        so.ApplyModifiedProperties();

        // 7. Fuera el modelo provisional (y su SnapInteractor, para que no haya dos).
        string nombreViejo = viejo.name;
        Undo.DestroyObjectImmediate(viejo.gameObject);

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(logica.scene);
        Selection.activeGameObject = logica;
        EditorGUIUtility.PingObject(logica);

        Debug.Log("[ReemplazarModeloPlato] Listo: '" + nombreViejo + "' reemplazado. " + copiados +
                  " componentes pasados a '" + NombreLogica + "' (Plato, BoxCollider, SnapInteractor, TagSet). " +
                  "Revisa el tamano en la escena y guarda (Ctrl+S).", logica);
    }

    private static Component Copiar(Component origen, GameObject destino)
    {
        Component copia = Undo.AddComponent(destino, origen.GetType());
        EditorUtility.CopySerialized(origen, copia);
        return copia;
    }

    // Escala el modelo para que su diametro (x/z) sea el de la referencia y lo apoya sobre la misma base.
    private static void Encajar(GameObject modelo, Bounds referencia)
    {
        Bounds actual = MedidaMundo(modelo);
        float diametroActual = Mathf.Max(actual.size.x, actual.size.z);
        float diametroRef = Mathf.Max(referencia.size.x, referencia.size.z);
        if (diametroActual > 0.00001f && diametroRef > 0.00001f)
            modelo.transform.localScale *= diametroRef / diametroActual;

        actual = MedidaMundo(modelo);
        modelo.transform.position += new Vector3(referencia.center.x - actual.center.x,
                                                 referencia.min.y - actual.min.y,
                                                 referencia.center.z - actual.center.z);
    }

    private static Bounds MedidaBaseVieja(Plato viejo)
    {
        SerializedObject so = new SerializedObject(viejo);
        string nombreBase = so.FindProperty("nombreBasePlato").stringValue;
        foreach (Transform t in viejo.GetComponentsInChildren<Transform>(true))
            if (t.name == nombreBase && t.GetComponentInChildren<Renderer>(true) != null)
                return MedidaMundo(t.gameObject, true);
        return MedidaMundo(viejo.gameObject, true);
    }

    private static Bounds MedidaMundo(GameObject objeto, bool incluirInactivos = false)
    {
        Renderer[] renderers = objeto.GetComponentsInChildren<Renderer>(incluirInactivos);
        if (renderers.Length == 0) return new Bounds(objeto.transform.position, Vector3.zero);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    // Caja de las mallas del modelo en el espacio local de 'espacio' (para el BoxCollider).
    private static Bounds MedidaLocal(GameObject modelo, Transform espacio)
    {
        bool hay = false;
        Bounds r = new Bounds();
        foreach (MeshFilter mf in modelo.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            Bounds mb = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 esquina = mb.center + Vector3.Scale(mb.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = espacio.InverseTransformPoint(mf.transform.TransformPoint(esquina));
                if (!hay) { r = new Bounds(p, Vector3.zero); hay = true; }
                else r.Encapsulate(p);
            }
        }
        return r;
    }

    private static GameObject BuscarEnRaiz(string rutaAsset)
    {
        foreach (GameObject raiz in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            if (PrefabUtility.IsAnyPrefabInstanceRoot(raiz) &&
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(raiz) == rutaAsset)
                return raiz;
        return null;
    }

    // Si el modelo usa el material que viene dentro del FBX (o ninguno), le pone el material pintado.
    private static void AsignarMaterialSiFalta(GameObject modelo, string rutaFbx, string rutaMaterial)
    {
        Material pintado = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial);
        if (pintado == null) return;
        foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            bool cambio = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null || AssetDatabase.GetAssetPath(mats[i]) == rutaFbx)
                {
                    mats[i] = pintado;
                    cambio = true;
                }
            }
            if (!cambio) continue;
            Undo.RecordObject(r, "Material del plato");
            r.sharedMaterials = mats;
        }
    }

    private static void Error(string mensaje)
    {
        EditorUtility.DisplayDialog("Reemplazar modelo del plato", mensaje, "OK");
    }
}
