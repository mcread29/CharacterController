using UnityEngine;

public class GravityVolumeCapsule : GravityVolume
{

    public float radius = 1f;
    public float falloff = 2f;
    public float height = 5f;
    public bool reverseDirection = false;
    
    public override float GetInfluenceAndGravityVector(Vector3 point, out Vector3 gravityVector) {
        
        Vector3 worldStart = transform.position + transform.forward * (height * 0.5f);
        Vector3 worldEnd = transform.position - transform.forward * (height * 0.5f);
        Vector3 worldClosest = ClosestPointOnSegment(worldStart, worldEnd, point);
        Vector3 offset = worldClosest - point;
        float distance = offset.magnitude;
        
        float contribution = (distance - radius) / (falloff);
        contribution = 1.0f - Mathf.Clamp01(contribution);

        if (contribution > 0f) {
            gravityVector = offset.normalized;
        } else {
            gravityVector = Vector3.zero;
        }
        
        return contribution;
    }
    
    public override Vector3 GetGravityVector(Vector3 point) {
        return Vector3.Normalize(transform.position - point);
    }

    private void OnDrawGizmos() {
        Gizmos.color = wireColorStart;
        GizmoExtensions.DrawWireCapsule(transform.position, transform.rotation, radius, height);
        Gizmos.color = wireColorEnd;
        GizmoExtensions.DrawWireCapsule(transform.position, transform.rotation, radius + falloff, height);
    }
    
    /*
    private void OnDrawGizmosSelected() {
        Gizmos.color = colorStart;
        Gizmos.DrawSphere(transform.position, radiusStart);
        Gizmos.color = colorEnd;
        Gizmos.DrawSphere(transform.position, radiusEnd);
    }
    */
}
