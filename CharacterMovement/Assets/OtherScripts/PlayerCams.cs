using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem; 
using UnityEngine;

public class PlayerCams : MonoBehaviour
{
    public GameObject player;
    public Camera cam;
    private PlayerControl control;

    public InputActionReference camZoom;
    private bool defaultZoom = true;

    private float navFOV = 60f;
    private float detFOV = 45f;

    /* camvals: 
        nav cam: pos: 0, 15, -15 ; rotation 65 ; fov 60 --- provides good view of the area 
        det cam: pos: 0, 3, -5 ; rotation 25 ; fov 45 --- provides a shoulder view for details/interactions
            *need to design levels so stuff is on a specific angle 
            or we change cam rotation to mouse/stick control instead of hardcoding an offset and leaving it 
    */

    private Vector3 navOffset = new Vector3(0, 15, -7); //player starts at -8, want cam at -15
    private Vector3 detOffset = new Vector3(0, 5, -7);
    private Vector3 currentOffset;

    //camera easing stuff
    private Quaternion targetRotation;
    private Vector3 targetOffset;

    void Start() {
        camZoom.action.performed += ctx => Zoom();
        control = player.GetComponent<PlayerControl>();

        cam.fieldOfView = navFOV;
        currentOffset = navOffset;
        targetOffset = navOffset;
        targetRotation = Quaternion.Euler(65, 0, 0);

        control.SetZoomedIn(false);
    }

    void LateUpdate() {

        if(!defaultZoom){
            float playerYaw = player.transform.eulerAngles.y;
            Quaternion yawRotation = Quaternion.Euler(0f, playerYaw, 0f);
            targetRotation = Quaternion.Euler(25, playerYaw, 0);
            targetOffset = yawRotation * detOffset;
        }

        //lerps are the camera easing
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, 5 * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 5 * Time.deltaTime);
        transform.position = player.transform.position + currentOffset;
    }

    void Zoom(){ //Camera should zoom in / out on player input (navigating vs detail)
        if(defaultZoom){ //zoom in; lower cam, behind player
            cam.fieldOfView = detFOV;
            defaultZoom = false;
            control.SetZoomedIn(true);
        }
        else{ //zoom out; higher cam, more topdown/isometric
            targetRotation = Quaternion.Euler(65, 0, 0);
            targetOffset = navOffset;
            cam.fieldOfView = navFOV;
            defaultZoom = true;
            control.SetZoomedIn(false);
        }
    }

}
