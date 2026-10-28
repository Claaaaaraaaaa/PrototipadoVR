using UnityEngine;

public class LazyFollowUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform headCamera;

    [Header("Ajustes de Seguimiento")]
    [SerializeField] private float distance = 1.5f;
    [SerializeField] private float positionSmoothTime = 5.0f;
    [SerializeField] private float rotationSmoothTime = 5.0f;

    private void Awake()
    {
        FindCamera();
    }

    private void OnEnable()
    {
        SnapToCameraFront();
    }

    private void LateUpdate()
    {
        if (headCamera == null) FindCamera();
        if (headCamera == null) return;

        Vector3 targetPosition = CalculateTargetPosition();

        // Seguir suavemente
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * positionSmoothTime);

        // Orientar hacia el usuario
        Quaternion targetRotation = Quaternion.LookRotation(transform.position - headCamera.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothTime);
    }

    /// <summary>
    /// Coloca el panel instantáneamente frente a la cámara sin animación suave.
    /// </summary>
    public void SnapToCameraFront()
    {
        if (headCamera == null) FindCamera();
        if (headCamera == null) return;

        transform.position = CalculateTargetPosition();
        transform.rotation = Quaternion.LookRotation(transform.position - headCamera.position);
    }

    private Vector3 CalculateTargetPosition()
    {
        Vector3 forward = headCamera.forward;
        forward.y = Mathf.Clamp(forward.y, -0.2f, 0.1f);
        forward.Normalize();

        return headCamera.position + (forward * distance);
    }

    private void FindCamera()
    {
        if (headCamera == null && Camera.main != null)
        {
            headCamera = Camera.main.transform;
        }
    }
}