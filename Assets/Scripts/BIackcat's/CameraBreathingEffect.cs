using UnityEngine;

public class CameraBreathingEffect : MonoBehaviour
{
    public float breathingRate = 10.0f;

    private float breathTimer = 0;

    void Update()
    {
        breathTimer += Time.deltaTime * breathingRate;
        float breathingEffect = Mathf.Sin(breathTimer);
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y + breathingEffect * 0.3f, transform.localPosition.z);
    }
}