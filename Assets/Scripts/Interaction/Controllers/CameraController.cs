using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.Netcode;

public class CameraController : MonoBehaviour
{
    Camera cam;
    GameManager.SceneState lastState;

    public Transform menuTf;
    bool menu;
    bool inGame;

    [Header("In Game")]
    public float sensitivityX;
    public float sensitivityY;
    public float fov;
    public float verticalCameraOffset;
    public float speed;
    float xRotation, yRotation;
    GameObject playerObject;
    bool lerpFov;
    bool lerpTilt;

    private void Start()
    {
        cam = GetComponent<Camera>();
        cam.fieldOfView = fov;
    }

    private void Update()
    {
        if (GameManager.Instance.sceneState != lastState)
            HandleStateChange();

        if (NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().controller != null) playerObject = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().character;
        else playerObject = null;

        if (menu)
        {
            //show level menu
            transform.parent.position = menuTf.position;
            transform.parent.rotation = menuTf.rotation;
        }
        else if (inGame && playerObject != null)
        {
            if (playerObject.GetComponent<HealthManager>().IsAlive)
            {
                transform.parent.position = playerObject.transform.position + (Vector3.up * verticalCameraOffset * playerObject.transform.localScale.y);
                transform.parent.localScale = playerObject.transform.localScale;
                transform.localScale = playerObject.transform.localScale;
            }

            //mouse input
            float mouseX = Input.GetAxisRaw("Mouse X") * sensitivityX;
            float mouseY = Input.GetAxisRaw("Mouse Y") * sensitivityY;
            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90, 90);
        }
    }

    private void LateUpdate()
    {
        if (inGame && playerObject != null)
            //rotation
            transform.parent.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }

    private void FixedUpdate()
    {
        if (inGame && playerObject != null && playerObject.GetComponent<HealthManager>().IsAlive)
            //rotation
            playerObject.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    private void HandleStateChange()
    {
        menu = GameManager.Instance.sceneState == GameManager.SceneState.LevelMenu;
        inGame = GameManager.Instance.sceneState == GameManager.SceneState.InGame;
        
        if (inGame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        lastState = GameManager.Instance.sceneState;
    }

    public void FOV(float delta, float tTime)
    {
        cam.DOFieldOfView(delta + fov, tTime);
    }

    public void ResetFOV(float tTime)
    {
        cam.DOFieldOfView(fov, tTime);
    }

    public void Tilt(float value, float tTime)
    {
        transform.DOLocalRotate(new Vector3(0f, 0f, value), tTime);
    }

    public void ResetTilt(float tTime)
    {
        transform.DOLocalRotate(Vector3.zero, tTime);
    }
}
