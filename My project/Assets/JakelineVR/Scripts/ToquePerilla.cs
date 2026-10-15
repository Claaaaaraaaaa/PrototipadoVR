
using UnityEngine;

public class ToquePerilla : MonoBehaviour
{
    [Header("Mano")]
    [SerializeField] private OVRHand manoIzquierda;
    [SerializeField] private OVRHand manoDerecha;

    [Header("Fogón")]
    [SerializeField] private ControlFogon controlFogon;

    [Header("Detección")]
    [SerializeField] private float distanciaToque = 0.025f;

    private bool estabaTocando;

    private void Update()
    {
        bool tocando = DedoCerca(manoIzquierda) ||
                       DedoCerca(manoDerecha);

        if (tocando && !estabaTocando && controlFogon != null)
        {
            controlFogon.AlternarFogon();
        }

        estabaTocando = tocando;
    }

    private bool DedoCerca(OVRHand mano)
    {
        if (mano == null || !mano.IsTracked)
            return false;

        OVRSkeleton esqueleto = mano.GetComponent<OVRSkeleton>();

        if (esqueleto == null || esqueleto.Bones == null)
            return false;

        foreach (var hueso in esqueleto.Bones)
        {
            if (hueso.Id == OVRSkeleton.BoneId.Hand_IndexTip ||
                hueso.Id == OVRSkeleton.BoneId.Hand_ThumbTip)
            {
                if (hueso.Transform != null &&
                    Vector3.Distance(
                        hueso.Transform.position,
                        transform.position
                    ) < distanciaToque)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
