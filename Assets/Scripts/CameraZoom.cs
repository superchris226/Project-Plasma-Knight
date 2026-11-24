using Unity.Cinemachine;
using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    public CinemachineCamera cam;
    void Update() { cam.Lens.OrthographicSize = 5f; }
}