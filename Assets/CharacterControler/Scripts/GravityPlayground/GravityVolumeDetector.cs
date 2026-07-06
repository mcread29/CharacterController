using System.Collections.Generic;
using UnityEngine;

public class GravityVolumeDetector : MonoBehaviour
{
    
    [SerializeField] CharacterControllerBase characterController;
    [SerializeField] Vector3 defaultGravityDirection = Vector3.down;
    
    void Start() {
        characterController.SetGravityDirection(defaultGravityDirection);
    }
    
    void Update() {
        
        if (GravityVolume.gravityVolumes.Count <= 0) {
            characterController.SetGravityDirection(defaultGravityDirection);
            return;
        }
        
        Vector3 position = transform.position;
        
        List<GravityVolume> gravityVolumes = new List<GravityVolume>();
        gravityVolumes.AddRange(GravityVolume.gravityVolumes);

        // Sort from high priority to low
        gravityVolumes.Sort((GravityVolume x, GravityVolume y) => y.priority - x.priority);

        float influence = 0f;
        Vector3 gravityDirection = Vector3.zero;
        
        float priorityInfluence = 0f;
        Vector3 priorityGravityDirection = Vector3.zero;
        
        int currentPriority = gravityVolumes[0].priority;
        for (int i = 0; i < gravityVolumes.Count; i++) {
            GravityVolume thisGV = gravityVolumes[i];

            if (thisGV.priority < currentPriority) {

                if (priorityInfluence > 0f) {
                    priorityGravityDirection = priorityGravityDirection / priorityInfluence;
                    gravityDirection += priorityGravityDirection * (1.0f - influence);
                    influence += Mathf.Clamp01(priorityInfluence) * (1.0f - influence);
                }

                if (influence >= 1.0f) break;
                priorityInfluence = 0f;
                priorityGravityDirection = Vector3.zero;

                currentPriority = thisGV.priority;
            }

            float newInfluence = thisGV.GetInfluenceAndGravityVector(position, out Vector3 gravityVector);
            if (newInfluence > 0f) {
                priorityInfluence += newInfluence;
                priorityGravityDirection += gravityVector * newInfluence;
            }
  
        }
        
        if (priorityInfluence > 0f) {
            priorityGravityDirection = priorityGravityDirection / priorityInfluence;
            gravityDirection += priorityGravityDirection * (1.0f - influence);
            influence += Mathf.Clamp01(priorityInfluence) * (1.0f - influence);
        }

        gravityDirection *= influence;
        
        if (influence < 1f) {
            gravityDirection += defaultGravityDirection * (1.0f - influence);
        }
        
        gravityDirection.Normalize();
        
        characterController.SetGravityDirection(gravityDirection);

    }
}
