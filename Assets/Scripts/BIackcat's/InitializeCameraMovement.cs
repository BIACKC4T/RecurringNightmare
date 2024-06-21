using UnityEngine;

public class InitializeCameraMovement : MonoBehaviour
{
    private CameraHeadBob cameraHeadBob;

    void Awake()
    {
        // CameraHeadBob 스크립트 찾기
        cameraHeadBob = GetComponent<CameraHeadBob>();
        if (cameraHeadBob != null)
        {
            // Awake 단계에서 CameraHeadBob 스크립트 비활성화
            cameraHeadBob.enabled = false;
        }
    }

    void Start()
    {
        // Start 단계에서 CameraHeadBob 스크립트 활성화
        if (cameraHeadBob != null)
        {
            cameraHeadBob.enabled = true;
        }
    }
}