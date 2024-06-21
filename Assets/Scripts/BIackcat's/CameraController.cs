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

        // 현재 카메라의 로컬 회전을 가져옴
        Vector3 currentRotation = transform.localEulerAngles;
        currentRotation.x -= mouseY;
        currentRotation.y += mouseX;
        currentRotation.z = 0; // Z 축 회전을 0으로 고정

        // 갱신된 회전 값을 적용
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