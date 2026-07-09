using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class KeyboardSceneTester : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float turnSpeed = 120f;
    [SerializeField] private Transform cameraPivot;

    private CharacterController controller;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        float turn = Input.GetAxisRaw("Mouse X");
        if (Input.GetKey(KeyCode.Q))
            turn -= 1f;
        if (Input.GetKey(KeyCode.E))
            turn += 1f;
        transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f);

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        input = Vector3.ClampMagnitude(input, 1f);

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -1f;

        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        Vector3 motion = transform.TransformDirection(input) * moveSpeed;
        motion.y = verticalVelocity;
        controller.Move(motion * Time.deltaTime);

        if (cameraPivot != null)
            cameraPivot.position = transform.position;
    }
}
