using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

[ExecuteAlways]
[RequireComponent(typeof(CinemachineBrain))]
public class VirtuialCameraController : MonoBehaviour
{
    public float CameraDistance = 2f;

    public Vector3 ShoulderOffset = new Vector3(0.5f, -0.4f, 0f);

    public Vector3 TrackOffset;

    public float NoiseAmp = 0f;

    public float NoiseFraq = 0f;

    public float FOV = 50f;
    void Update()
    {
        CinemachineVirtualCamera[] childlist = GetComponentsInChildren<CinemachineVirtualCamera>();
        foreach (CinemachineVirtualCamera vcams in childlist)
        {
            Cinemachine3rdPersonFollow follow = vcams.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            CinemachineComposer composer = vcams.GetCinemachineComponent<CinemachineComposer>();
            CinemachineBasicMultiChannelPerlin noise = vcams.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (noise != null)
            {
                noise.m_AmplitudeGain = NoiseAmp;
                noise.m_FrequencyGain = NoiseFraq;
            }
            if(follow != null)
            {
                follow.CameraDistance = CameraDistance;
                follow.ShoulderOffset = ShoulderOffset;
            }
            if (composer != null)
            {
                composer.m_TrackedObjectOffset = TrackOffset;
            }
            vcams.m_Lens.FieldOfView = FOV;
        }
        Camera[] subcams = GetComponentsInChildren<Camera>();
        foreach(Camera cam in subcams)
        {
            cam.fieldOfView = FOV;
        }
    }
}
