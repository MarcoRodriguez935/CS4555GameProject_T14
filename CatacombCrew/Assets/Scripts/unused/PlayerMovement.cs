using UnityEngine;

public class Player1Movement : MonoBehaviour
{
    public CharacterController controller;

    float speed = 5.0f;
    float runSpeedMultiplier = 1.2f;
    float jogBackSpeedMultiplier = 1.02f;
    float speedRotate = 300.0f;
    float gravity = -9.81f;
    float verticalVelocity = 0.0f;

    void Update()
    {
        // Ground check
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -1f;
        }

        // Apply gravity
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = Vector3.zero;

        // FORWARD
        if (Input.GetKey(KeyCode.W))
        {
            Vector3 movement = new Vector3(0.0f, 0.0f, 1.0f * Time.deltaTime * speed);
            movement = transform.TransformDirection(movement);
            controller.Move(movement);
        }

        // BACKWARD
        if (Input.GetKey(KeyCode.S))
        {
            Vector3 movement = new Vector3(0.0f, 0.0f, -1.0f * Time.deltaTime * speed);
            movement = transform.TransformDirection(movement);
            controller.Move(movement);
        }

        // BACKWARD + SPRINT
        if (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.LeftShift))
        {
            Vector3 movement = new Vector3(0.0f, 0.0f, -1.0f * Time.deltaTime * (speed * jogBackSpeedMultiplier));
            movement = transform.TransformDirection(movement);
            controller.Move(movement);
        }

        // FORWARD + SPRINT
        if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.LeftShift))
        {
            Vector3 movement = new Vector3(0.0f, 0.0f, 1.0f * Time.deltaTime * (speed * runSpeedMultiplier));
            movement = transform.TransformDirection(movement);
            controller.Move(movement);
        }

        // FORWARD + SNEAK
        if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.C))
        {
            Vector3 movement = new Vector3(0.0f, 0.0f, 0.5f * Time.deltaTime * speed);
            movement = transform.TransformDirection(movement);
            controller.Move(movement);
        }

        // Gravity effect
        move.y = verticalVelocity * Time.deltaTime;
        controller.Move(move);

        // ROTATION
        if (Input.GetKey(KeyCode.A))
        {
            Vector3 rotation = new Vector3(0.0f, -1.0f * Time.deltaTime * speedRotate, 0.0f);
            transform.Rotate(rotation);
        }

        if (Input.GetKey(KeyCode.D))
        {
            Vector3 rotation = new Vector3(0.0f, 1.0f * Time.deltaTime * speedRotate, 0.0f);
            transform.Rotate(rotation);
        }
    }
}