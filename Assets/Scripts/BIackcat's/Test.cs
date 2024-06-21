using UnityEngine;

public class Test : MonoBehaviour
{
    public float speed = 5.0f; // 이동 속도

    void Update()
    {
        float moveHorizontal = Input.GetAxis("Horizontal"); // 수평 입력을 받습니다.
        float moveVertical = Input.GetAxis("Vertical"); // 수직 입력을 받습니다.

        // 카메라가 바라보는 방향으로 이동 벡터를 조정합니다.
        Vector3 movement = Camera.main.transform.forward * moveVertical + Camera.main.transform.right * moveHorizontal;
        movement.y = 0; // 수직 이동을 제거하여 카메라가 지면을 따라만 움직이도록 합니다.

        transform.Translate(movement * speed * Time.deltaTime, Space.World); // Transform.Translate를 사용하여 오브젝트를 이동시킵니다.

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            speed = 7.5f; // 달리기 속도
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            speed = 5.0f; // 걷기 속도
        }
    }
}