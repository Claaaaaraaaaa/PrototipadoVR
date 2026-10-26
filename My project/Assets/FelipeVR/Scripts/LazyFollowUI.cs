using UnityEngine;

public class LazyFollowUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform headCamera;

    [Header("Ajustes de Seguimiento")]
    [SerializeField] private float distance = 2.0f;
    [SerializeField] private float positionSmoothTime = 5.0f;
    [SerializeField] private float rotationSmoothTime = 5.0f;

    private void Start()
    {
        if (headCamera == null && Camera.main != null)
        {
            headCamera = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (headCamera == null) return;

        // Calcular posición objetivo al frente de la cámara
        Vector3 targetPosition = headCamera.position + (headCamera.forward * distance);

        // Interpolar posición de forma suave
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * positionSmoothTime);

        // Orientar el panel hacia la cámara
        Quaternion targetRotation = Quaternion.LookRotation(transform.position - headCamera.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothTime);
    }
}