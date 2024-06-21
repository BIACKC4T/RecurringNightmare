using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraHeadBob : MonoBehaviour
{
    public float bobbingSpeed = 0.18f;
    public float bobbingAmount = 0.05f;
    public float midpoint = 1.7f;

    private float timer = 0;

    void Update()
    {
        float waveslice = 0.0f;
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 cSharpPosition = transform.localPosition;

        if (Mathf.Abs(horizontal) == 0 && Mathf.Abs(vertical) == 0)
        {
            timer = 0;
        }
        else
        {
            waveslice = Mathf.Sin(timer);
            timer = timer + bobbingSpeed;
            if (timer > Mathf.PI * 2)
            {
                timer = timer - (Mathf.PI * 2);
            }
        }
        if (waveslice != 0)
        {
            float translateChange = waveslice * bobbingAmount;
            float totalAxes = Mathf.Abs(horizontal) + Mathf.Abs(vertical);
            totalAxes = Mathf.Clamp(totalAxes, 0.0f, 1.0f);
            translateChange = totalAxes * translateChange;

            cSharpPosition.y = midpoint + translateChange;
        }
        else
        {
            cSharpPosition.y = midpoint;
        }

        transform.localPosition = cSharpPosition;
    }
}