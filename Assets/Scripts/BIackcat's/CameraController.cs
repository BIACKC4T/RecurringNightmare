using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float mouseSensitivity = 100.0f;
    public float shakeMagnitude = 0.05f;
    public float breathingRate = 20.0f;

    private float breathTimer = 0;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleMouseMovement();
        HandleBreathingEffect();
    }

    void HandleMouseMovement()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // ���� ī�޶��� ���� ȸ���� ������
        Vector3 currentRotation = transform.localEulerAngles;
        currentRotation.x -= mouseY;
        currentRotation.y += mouseX;
        currentRotation.z = 0; // Z �� ȸ���� 0���� ����

        // ���ŵ� ȸ�� ���� ����
        transform.localEulerAngles = currentRotation;
    }

    void HandleBreathingEffect()
    {
        if (Input.GetAxis("Horizontal") == 0 && Input.GetAxis("Vertical") == 0) // no movement
        {
            breathTimer += Time.deltaTime * breathingRate;
            float breathingEffect = Mathf.Sin(breathTimer);
            transform.localPosition += new Vector3(0, breathingEffect * shakeMagnitude, 0);
        }
    }
}