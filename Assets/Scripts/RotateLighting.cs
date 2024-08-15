using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public float rotationSpeed = 10.0f;
    void Update()
    {
        float currentY = transform.rotation.eulerAngles.y;
        float newY = currentY + rotationSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Euler(50f, newY, 0f);
    }
}
