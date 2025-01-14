using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MirrorScript : MonoBehaviour
{
    private Transform playerTransform;
    private Vector3 offset;

    // Start is called before the first frame update
    void Start()
    {
        playerTransform = GameObject.FindWithTag("MainCamera").GetComponent<Transform>();
        offset = playerTransform.rotation.eulerAngles - transform.rotation.eulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        Quaternion newRotation = Quaternion.Euler(playerTransform.rotation.eulerAngles - offset * -1f);
        gameObject.transform.rotation = newRotation;
    }
}
