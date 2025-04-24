using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MirrorScript : MonoBehaviour
{
    private Camera playerCamera;
    public Camera mirrorCamera;  // Assign the mirror's camera in the Inspector
    public float reflectionDepthFactor = 0.5f; // Adjust this to control how far the mirror camera is behind the mirror
    public Vector3 rotationOffset;  // Assign the rotation offset to simulate the mirror's angle (e.g., new Vector3(-10, 0, 0))

    void Start()
    {
        playerCamera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
    }

    void Update()
    {
        if (playerCamera is null || mirrorCamera is null) return;

        // Get the mirror's normal (assuming the mirror faces its forward direction)
        Vector3 mirrorNormal = transform.forward;
        Vector3 mirrorPosition = transform.position;

        // Reflect the camera position, but control the depth behind the mirror
        Vector3 playerToMirror = playerCamera.transform.position - mirrorPosition;
        Vector3 reflectedPosition = playerCamera.transform.position - 
            2 * Vector3.Dot(playerToMirror, mirrorNormal) * mirrorNormal;

        // Adjust depth behind the mirror for a convex effect
        reflectedPosition = Vector3.Lerp(playerCamera.transform.position, reflectedPosition, reflectionDepthFactor);
        mirrorCamera.transform.position = reflectedPosition;

        // Create a reflection matrix
        Matrix4x4 reflectionMatrix = Matrix4x4.identity;
        reflectionMatrix.m00 = 1 - 2 * mirrorNormal.x * mirrorNormal.x;
        reflectionMatrix.m01 = -2 * mirrorNormal.x * mirrorNormal.y;
        reflectionMatrix.m02 = -2 * mirrorNormal.x * mirrorNormal.z;
        reflectionMatrix.m10 = -2 * mirrorNormal.y * mirrorNormal.x;
        reflectionMatrix.m11 = 1 - 2 * mirrorNormal.y * mirrorNormal.y;
        reflectionMatrix.m12 = -2 * mirrorNormal.y * mirrorNormal.z;
        reflectionMatrix.m20 = -2 * mirrorNormal.z * mirrorNormal.x;
        reflectionMatrix.m21 = -2 * mirrorNormal.z * mirrorNormal.y;
        reflectionMatrix.m22 = 1 - 2 * mirrorNormal.z * mirrorNormal.z;

        // Reflect the forward and up vectors
        Vector3 reflectedForward = reflectionMatrix.MultiplyVector(playerCamera.transform.forward);
        Vector3 reflectedUp = reflectionMatrix.MultiplyVector(playerCamera.transform.up);

        // Apply the mirrored rotation
        mirrorCamera.transform.rotation = Quaternion.LookRotation(reflectedForward, reflectedUp);

        // Apply the rotational offset (you can change the offset to match your mirror's angle)
        mirrorCamera.transform.rotation *= Quaternion.Euler(rotationOffset);
    }
}
