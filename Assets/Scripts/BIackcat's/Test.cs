using UnityEngine;

public class SimpleMovement : MonoBehaviour
{
    public float speed = 5.0f; // 이동 속도

    void Update()
    {
        float moveHorizontal = Input.GetAxis("Horizontal"); // 수평 입력을 받습니다.
        float moveVertical = Input.GetAxis("Vertical"); // 수직 입력을 받습니다.

        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical) * speed * Time.deltaTime; // 이동 벡터를 생성합니다.
        transform.Translate(movement, Space.World); // Transform.Translate를 사용하여 오브젝트를 이동시킵니다.
    }
}