using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;

    float speed = 5.0f;
    float runSpeedMultiplier = 1.2f;
    float jogBackSpeedMultiplier = 1.02f;
    float speedRotate = 300.0f;
    float gravity = -9.81f;
    float verticalVelocity = 0.0f;

    //float jumpHeight = 2.0f;
    float originalHeight;

    void Start() 
    {
        originalHeight = controller.height;
    }

    void Update()
    {
        // Player stays grounded
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -1f;
        }

        // Always apply gravity
        verticalVelocity += gravity * Time.deltaTime;

        // Move forward & back
        Vector3 move = Vector3.zero;

        if (Input.GetKey("w")) // Move Forward
        {
            print("w");
            Vector3 movement = new Vector3(0.0f, 0.0f, 1.0f * Time.deltaTime * speed);
            movement = transform.TransformDirection(movement);
            controller.Move(movement);
        }

        if (Input.GetKey("s")) // Move Back
        {
            print("s");
            Vector3 movement = new Vector3(0.0f, 0.0f, -1.0f * Time.deltaTime * speed);
            movement = transform.TransformDirection(movement);
            controller.Move(movement);
        }

        if (Input.GetKey("s") && Input.GetKey("left shift"))
        {
            print("s");
            Vector3 movement = new Vector3(0.0f, 0.0f, -1.0f * Time.deltaTime * (speed * jogBackSpeedMultiplier));
            movement = transform.TransformDirection(movement);
            controller.Move(movement);
        }

        if (Input.GetKey("w") && Input.GetKey("left shift")) // Sprint
        {
            print("w + shift");
            Vector3 movement = new Vector3(0.0f, 0.0f, 1.0f * Time.deltaTime * (speed * runSpeedMultiplier));
            movement = transform.TransformDirection(movement);
            controller.Move(movement);
        }
        if (Input.GetKey("w") && Input.GetKey("c")) // Sneak (WIP)
        {
            print("w + c");
            Vector3 movement = new Vector3(0.0f, 0.0f, 0.5f * Time.deltaTime * speed);
            movement = transform.TransformDirection(movement);
            controller.Move(movement);
        }

        // Fall from gravity
        move.y = verticalVelocity * Time.deltaTime;

        controller.Move(move);

        // Rotate
        if (Input.GetKey("a"))
        {
            print("a");
            Vector3 rotation = new Vector3(0.0f, -1.0f * Time.deltaTime * speedRotate, 0.0f);
            transform.Rotate(rotation);
        }
        
        if (Input.GetKey("d"))
        {
            print("d");
            Vector3 rotation = new Vector3(0.0f, 1.0f * Time.deltaTime * speedRotate, 0.0f);
            transform.Rotate(rotation);
        }
    }
}
