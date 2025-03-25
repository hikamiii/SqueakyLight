using UnityEngine;

public class SpotlightFollow : MonoBehaviour
{
    public Transform player; 
    public Transform cameraTransform; 
    public Vector3 offset = new Vector3(0, 2, 0); 

    void Update()
    {
        if (player == null || cameraTransform == null)
            return;

        transform.position = player.position + offset;

        transform.rotation = Quaternion.LookRotation(cameraTransform.forward);
    }
}
