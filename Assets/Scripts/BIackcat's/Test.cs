using UnityEngine;

public class SimpleMovement : MonoBehaviour
{
    public float speed = 5.0f; // �̵� �ӵ�

    void Update()
    {
        float moveHorizontal = Input.GetAxis("Horizontal"); // ���� �Է��� �޽��ϴ�.
        float moveVertical = Input.GetAxis("Vertical"); // ���� �Է��� �޽��ϴ�.

        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical) * speed * Time.deltaTime; // �̵� ���͸� �����մϴ�.
        transform.Translate(movement, Space.World); // Transform.Translate�� ����Ͽ� ������Ʈ�� �̵���ŵ�ϴ�.
    }
}