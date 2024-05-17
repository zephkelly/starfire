using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Cam
{
  public class CameraController : MonoBehaviour
  {
    public static CameraController Instance;
    private Camera mainCamera;
    private Transform cameraTransform;

    private Transform target;
    private Rigidbody2D targetRigidbody;
    private float targetVelocityMagnitude;

    private Vector3 mouseLerpPosition;
    private Vector3 currentOffsetAmount;

    [SerializeField] float mouseInterpolateDistance = 2f;
    [SerializeField] float cameraPanSpeed = 0.15f;

    [SerializeField] private float maxZoom = 50f;
    [SerializeField] private float minZoom = 30f;
    private float currentZoom;

    //Parallaxing-layers----------------------------------------------------------------------------

    [SerializeField] StarfieldLayer[] starfieldLayers;

    //----------------------------------------------------------------------------------------------

    private void Awake()
    {
        mainCamera = Camera.main;
        cameraTransform = mainCamera.transform;

        if (Instance == null)
        {
            Instance = this;
        } 
        else 
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        target = GameObject.Find("Player").transform;
        targetRigidbody = target.GetComponent<Rigidbody2D>();

        minZoom = mainCamera.orthographicSize;
        currentZoom = minZoom;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        mouseLerpPosition = (mainCamera.ScreenToWorldPoint(Input.mousePosition) - target.position).normalized;
        mouseLerpPosition.y = mouseLerpPosition.y * 1.4f;   //beacuse the camera is wider than it is tall

        targetVelocityMagnitude = targetRigidbody.velocity.magnitude;

        currentZoom += -Input.mouseScrollDelta.y;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, currentZoom + (targetVelocityMagnitude * 0.1f), 0.1f);
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        Vector3 targetVector = target.position + currentOffsetAmount + (mouseLerpPosition * mouseInterpolateDistance * (targetVelocityMagnitude * 0.03f));   

        //   Debug.DrawLine(target.position, targetVector, Color.red);  

        targetVector.z = cameraTransform.position.z;

        Vector3 cameraLastPosition = cameraTransform.position;
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetVector, cameraPanSpeed * (Mathf.Clamp(targetVelocityMagnitude, 25, 50) * 0.09f));

        //   Debug.DrawLine(target.position, Vector3.Lerp(cameraTransform.position, targetVector, cameraPanSpeed * (Mathf.Clamp(targetVelocityMagnitude, 25, 50) * 0.09f)), Color.green);
        
        UpdateParllaxing(cameraLastPosition);
    }

    private void UpdateParllaxing(Vector2 cameraLastPosition)
    {
        //Starfields
        for (int i = 0; i < starfieldLayers.Length; i++)
        {
            starfieldLayers[i].Parallax(cameraLastPosition);
        }
    }

    public void ChangeFocus(Transform newFocus) => target = newFocus;

    public void SetOffset(Vector2 offsetVector) => currentOffsetAmount = offsetVector;

    public void ClearOffset() => currentOffsetAmount = Vector3.zero;
  }
}