using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationFollower : MonoBehaviour
{
    [SerializeField]
    public GameObject Object;

    public bool IsRotationFollow;

    public bool IsPositionFollow;

    public bool UseLookAt = false;

    public float RotationScaleX = 1f;

    public float RotationScaleY = 1f;

    public Vector3 InitialPosition;

    private void LateUpdate()
    {
        float Factor = RotationScaleX * 2 * Mathf.Rad2Deg;
        float Factor2 = RotationScaleY * 2 * Mathf.Rad2Deg;
        if (IsRotationFollow)
        {
            this.transform.localRotation = Quaternion.Euler(new Vector3(Object.transform.localRotation.x * Factor, Object.transform.localRotation.y * Factor2, this.transform.localRotation.z));
        }
        else if (UseLookAt)
        {
            //Vector3 dir = Object.transform.position - this.transform.position;
            //this.transform.LookAt(Object.transform, Vector3.up);
            this.transform.LookAt(transform.position + Object.transform.forward);
            //this.transform.rotation = Quaternion.Lerp(Quaternion.Euler(new Vector3(0f, 0f, 0f)), Quaternion.LookRotation(Object.transform.position - this.transform.position, Vector3.up), LookAtFactor);
            //this.transform.rotation = Quaternion.Lerp(Quaternion.Euler(new Vector3(0f, 0f, 0f)), Quaternion.LookRotation(new Vector3(0f, 0f, dir.z)), LookAtFactor);  failed
        }
        if (IsPositionFollow) 
        {
                this.transform.position = Object.transform.position + InitialPosition;
        }
        }
    }

