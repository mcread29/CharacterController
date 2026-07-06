using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GravityBySurfaceProximity : MonoBehaviour
{
    
    [SerializeField] CharacterControllerBase characterController;
    [SerializeField] Vector3 defaultGravityDirection = Vector3.down;
    [SerializeField] int samples = 16;
    [SerializeField] float startOffset = 1.0f;
    [SerializeField] float minDistance = 1.0f;
    [SerializeField] float maxDistance = 5.0f;
    [SerializeField] float dirBias = 0.3f;
    [SerializeField] float lerpSpeed = 10.0f;
    [SerializeField] LayerMask layerMask;
    
    private Vector3[] directions;
    private Vector3 gravityDirection;
    private float oneOverDistDiff = 1f;
    
    
    public static Vector3[] GenerateHemispherePoints(int count, float dirBias) {
        Vector3[] points = new Vector3[count];

        float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));

        for (int i = 0; i < count; i++) {
            float z = (i + 0.5f) / count;
            float radius = Mathf.Sqrt(1f - z * z);
            float theta = i * goldenAngle;
            Vector3 p = new Vector3( radius * Mathf.Cos(theta), z, radius * Mathf.Sin(theta));
            points[i] = new Vector3(p.x, p.y + dirBias, p.z);
        }

        return points;
    }
    
    
    
    void Start() {
        gravityDirection = defaultGravityDirection;
        characterController.SetGravityDirection(defaultGravityDirection);
        directions = GenerateHemispherePoints(samples, dirBias);
        oneOverDistDiff = 1.0f / (maxDistance - minDistance);
    }

    // Update is called once per frame
    void Update() {

        Vector3 localX = characterController.transform.right;
        Vector3 localY = -characterController.transform.up;
        Vector3 localZ = characterController.transform.forward;
        
        Vector3 startPos = characterController.transform.position + characterController.transform.up * startOffset;

        float accumulation = 0f;
        Vector3 surfaceNormal = Vector3.zero;

        for (int i = 0; i < directions.Length; i++) {
            Vector3 localDir = directions[i];
            Vector3 worldDir = localX * localDir.x + localY * localDir.y + localZ * localDir.z;
            Vector3 worldVector = worldDir * maxDistance;
            
            if (Physics.Raycast(startPos, worldVector, out RaycastHit hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore)) {
                Debug.DrawRay(startPos, worldDir * hit.distance, Color.green);

                float newAccumulation = 1.0f - (hit.distance - minDistance) * oneOverDistDiff;
                
                accumulation += newAccumulation;
                surfaceNormal += hit.normal * newAccumulation;
            } else {
                Debug.DrawRay(startPos, worldVector, Color.red);
            }
            
        }

        Vector3 newGravityDirection = defaultGravityDirection;

        if (accumulation > 0f) {
            surfaceNormal *= 1.0f / accumulation;
            newGravityDirection = Vector3.Lerp(defaultGravityDirection, -surfaceNormal, Mathf.Clamp01(accumulation)).normalized;
        }
        
        gravityDirection = Vector3.Lerp( gravityDirection, newGravityDirection, Time.deltaTime * lerpSpeed ).normalized;
        
        characterController.SetGravityDirection(gravityDirection);
        
    }
}
