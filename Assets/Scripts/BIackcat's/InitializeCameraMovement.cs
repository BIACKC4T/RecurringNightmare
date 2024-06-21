using UnityEngine;

public class InitializeCameraMovement : MonoBehaviour
{
    private CameraHeadBob cameraHeadBob;

    void Awake()
    {
        // CameraHeadBob ��ũ��Ʈ ã��
        cameraHeadBob = GetComponent<CameraHeadBob>();
        if (cameraHeadBob != null)
        {
            // Awake �ܰ迡�� CameraHeadBob ��ũ��Ʈ ��Ȱ��ȭ
            cameraHeadBob.enabled = false;
        }
    }

    void Start()
    {
        // Start �ܰ迡�� CameraHeadBob ��ũ��Ʈ Ȱ��ȭ
        if (cameraHeadBob != null)
        {
            cameraHeadBob.enabled = true;
        }
    }
}