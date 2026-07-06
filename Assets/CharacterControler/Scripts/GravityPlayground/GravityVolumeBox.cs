using UnityEngine;

public class GravityVolumeBox : GravityVolume
{

    public Vector3 size = Vector3.one;
    public Vector3 falloff = Vector3.one;
    public Vector3 falloffOffset = Vector3.zero;
    
    
    public override float GetInfluenceAndGravityVector(Vector3 point, out Vector3 gravityVector) {
        
        Vector3 localPoint = transform.InverseTransformPoint(point);
        
        float falloffX = 0f;
        float falloffY = 0f;
        float falloffZ = 0f;
        
        Vector3 halfSize = size * 0.5f;
        
        if (localPoint.x > 0f) {
            float falloffPosX = 1.0f / Mathf.Max(falloff.x + falloffOffset.x, 1f);
            falloffX = 1.0f - Mathf.Clamp01((localPoint.x - halfSize.x) * falloffPosX);
        } else {
            float falloffNegX = 1.0f / Mathf.Max(falloff.x - falloffOffset.x, 1f);
            falloffX = 1.0f - Mathf.Clamp01((-localPoint.x - halfSize.x) * falloffNegX);
        }
        
        if (localPoint.y > 0f) {
            float falloffPosY = 1.0f / Mathf.Max(falloff.y + falloffOffset.y, 1f);
            falloffY = 1.0f - Mathf.Clamp01((localPoint.y - halfSize.y) * falloffPosY);
        } else {
   
            float falloffNegY = 1.0f / Mathf.Max(falloff.y - falloffOffset.y, 1f);
            falloffY = 1.0f - Mathf.Clamp01((-localPoint.y - halfSize.y) * falloffNegY);
        }
        
        if (localPoint.z > 0f) {
            float falloffPosZ = 1.0f / Mathf.Max(falloff.z + falloffOffset.z, 1f);
            falloffZ = 1.0f - Mathf.Clamp01( (localPoint.z - halfSize.z) * falloffPosZ);
        } else {
            float falloffNegZ = 1.0f / Mathf.Max(falloff.z - falloffOffset.z, 1f);
            falloffZ = 1.0f - Mathf.Clamp01((-localPoint.z - halfSize.z) * falloffNegZ);
        }

        float overContributionX = 1.0f - Mathf.Clamp01(Mathf.Abs(localPoint.x) / halfSize.x);
        float overContributionY = 1.0f - Mathf.Clamp01(Mathf.Abs(localPoint.y) / halfSize.y);
        float overContributionZ = 1.0f - Mathf.Clamp01(Mathf.Abs(localPoint.z) / halfSize.z);
        
        //float falloffX = 1.0f - Mathf.Clamp01( (Mathf.Abs(localPoint.x) - halfSize.x) * invFalloff);
        //float falloffY = 1.0f - Mathf.Clamp01( (Mathf.Abs(localPoint.y) - halfSize.y) * invFalloff);
        //float falloffZ = 1.0f - Mathf.Clamp01( (Mathf.Abs(localPoint.z) - halfSize.z) * invFalloff);

        float contribution = falloffX * falloffY * falloffZ;
        contribution += overContributionX * overContributionY * overContributionZ;
        
        //Debug.Log( contribution, gameObject );

        gravityVector = -transform.up;
        
        return contribution;
    }
    
    
    private void OnDrawGizmos() {
        Gizmos.color = wireColorStart;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, size);
        Gizmos.color = wireColorEnd;
        Vector3 falloffEnd = size + falloff * 2.0f;
        Gizmos.DrawWireCube(falloffOffset, falloffEnd);
        Gizmos.color = colorArrow;
        
        Gizmos.matrix = Matrix4x4.TRS(transform.position + Vector3.down, transform.rotation * Quaternion.Euler(-90,0,0), Vector3.one);
        Gizmos.DrawFrustum(Vector3.zero, 60f, 2f, 0.01f, 1f);
    }
    
    private void OnDrawGizmosSelected() {
        Gizmos.color = colorStart;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, size);
        Gizmos.color = colorEnd;
        Vector3 falloffEnd = size + falloff * 2.0f;
        Gizmos.DrawCube(falloffOffset, falloffEnd);
    }
}
