using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire
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
    [SerializeField] private Vector3 currentOffsetAmount;

    [SerializeField] float mouseInterpolateDistance = 2f;
    [SerializeField] float cameraPanSpeed = 0.15f;

    [SerializeField] private float maxZoom = 50f;
    [SerializeField] private float minZoom = 30f;
    private float currentZoom;

    //Parallaxing-layers----------------------------------------------------------------------------

    [SerializeField] StarfieldParallaxLayer[] parallaxingLayers;
    [SerializeField] public List<CelestialParallaxLayer> starParallaxLayers;

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
        target = GameObject.Find("PlayerShip").transform;
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
        mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, Mathf.Clamp(currentZoom + (targetVelocityMagnitude * 0.1f), minZoom, maxZoom), 0.1f);
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        Vector3 targetVector = target.position + currentOffsetAmount + (mouseLerpPosition * mouseInterpolateDistance * (targetVelocityMagnitude * 0.03f));   

        targetVector.z = cameraTransform.position.z;

        Vector3 cameraLastPosition = cameraTransform.position;
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetVector, cameraPanSpeed * (Mathf.Clamp(targetVelocityMagnitude, 25, 50) * 0.09f));

        UpdateParllaxing(cameraLastPosition);
    }

    private void UpdateParllaxing(Vector2 cameraLastPosition)
    {
        //Starfields
        for (int i = 0; i < parallaxingLayers.Length; i++)
        {
            parallaxingLayers[i].Parallax(cameraLastPosition);
        }

        for (int i = 0; i < starParallaxLayers.Count; i++)
        {
            starParallaxLayers[i].Parallax(cameraLastPosition);
        }
    }

    public void ChangeFocus(Transform newFocus) => target = newFocus;

    public void Transport(Vector2 offset)
    {
        var currentOffset = cameraTransform.position - target.position;

        // the camera needs to be offset by the same amount as the player

        Vector2 newPosition = new Vector2(cameraTransform.position.x, cameraTransform.position.y) + offset;
        cameraTransform.position = new Vector3(newPosition.x, newPosition.y, cameraTransform.position.z);


    }

    public void SetOffset(Vector2 offsetVector) => currentOffsetAmount = offsetVector;

    public void ClearOffset() => currentOffsetAmount = Vector3.zero;
  }
}