using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CameraController : MonoBehaviour
{
    Camera cam;
    GameManager.SceneState lastState;

    [Header("MainMenu")]
    public Transform mainMenuTf;
    bool mainMenu;

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
    bool inGame;

    Coroutine fovCoro, tiltCoro;

    private void Start()
    {
        cam = GetComponent<Camera>();
        cam.fieldOfView = fov;
    }

    private void Update()
    {
        if (GameManager.instance.sceneState != lastState)
            HandleStateChange();

        if (PlayerController.instance != null) playerObject = PlayerController.instance.gameObject;
        else playerObject = null;

        if (mainMenu)
        {
            transform.position = mainMenuTf.position;
            transform.rotation = mainMenuTf.rotation;
        } else if (inGame && playerObject != null)
        {
            transform.parent.position = playerObject.transform.position + (Vector3.up * verticalCameraOffset * playerObject.transform.localScale.y);
            transform.parent.localScale = playerObject.transform.localScale;
            transform.localScale = playerObject.transform.localScale;

            //mouse input
            float mouseX = Input.GetAxisRaw("Mouse X") * sensitivityX;
            float mouseY = Input.GetAxisRaw("Mouse Y") * sensitivityY;
            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90, 90);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
            }
        }
    }

    private void LateUpdate()
    {
        if (inGame && playerObject != null)
            //rotation
            transform.parent.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }

    private void FixedUpdate()
    {
        if (inGame && playerObject != null)
            //rotation
            playerObject.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    private void HandleStateChange()
    {
        mainMenu = GameManager.instance.sceneState == GameManager.SceneState.MainMenu;
        inGame = GameManager.instance.sceneState == GameManager.SceneState.InGame;

        if (mainMenu)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        } else if (inGame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        lastState = GameManager.instance.sceneState;
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
