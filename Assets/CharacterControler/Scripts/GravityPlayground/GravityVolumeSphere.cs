using UnityEngine;

public class GravityVolumeSphere : GravityVolume
{

    public float radius = 1f;
    public float falloff = 2f;
    
    public override float GetInfluenceAndGravityVector(Vector3 point, out Vector3 gravityVector) {
        Vector3 position = transform.position;
        float distance = Vector3.Distance(position, point);
        float contribution = (distance - radius) / (falloff);
        contribution = 1.0f - Mathf.Clamp01(contribution);
        
        if (contribution > 0f) {
            gravityVector = Vector3.Normalize(transform.position - point);
        } else {
            gravityVector = Vector3.zero;
        }

        return contribution;
    }
    
    /*
    public override float GetContribution(Vector3 point) {
        Vector3 position = transform.position;
        float distance = Vector3.Distance(position, point);
        float contribution = (distance - radiusStart) / (radiusEnd - radiusStart);
        contribution = 1.0f - Mathf.Clamp01(contribution);
        return contribution;
    }
    
    public override Vector3 GetGravityVector(Vector3 point) {
        return Vector3.Normalize(transform.position - point);
    }
    */
    private void OnDrawGizmos() {
        Gizmos.color = wireColorStart;
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.color = wireColorEnd;
        Gizmos.DrawWireSphere(transform.position, radius + falloff);
    }
    
    private void OnDrawGizmosSelected() {
        Gizmos.color = colorStart;
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = colorEnd;
        Gizmos.DrawSphere(transform.position, radius + falloff);
    }
}
