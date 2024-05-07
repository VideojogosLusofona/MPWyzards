using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointTowardsMouseCursor : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        Vector3 worldSpaceMouse = mainCamera.ScreenPointToRay(Input.mousePosition).origin;
        Vector3 targetVector = (worldSpaceMouse - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, targetVector);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Time.deltaTime * 360.0f);
    }
}
