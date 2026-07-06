using System;
using System.Collections.Generic;
using UnityEngine;

public class GravityVolume : MonoBehaviour
{
    
    public static List<GravityVolume> gravityVolumes = new List<GravityVolume>();

    public int priority = 0;
    
    protected static Color colorStart = new Color(0.2f, 1.0f, 0.1f, 0.2f);
    protected static Color wireColorStart = new Color(0.2f, 1.0f, 0.1f, 1.0f);
    
    protected static Color colorEnd = new Color(0.1f, 0.2f, 1.0f, 0.2f);
    protected static Color wireColorEnd = new Color(0.1f, 0.2f, 1.0f, 1.0f);
    
    protected static Color colorArrow = new Color(1.0f, 0.7f, 0.2f, 0.5f);

    void OnEnable() {
        if (gravityVolumes.Contains(this)) return;
        gravityVolumes.Add(this);
    }

    void OnDisable() {
        gravityVolumes.Remove(this);
    }

    void OnValidate() {
        transform.localScale = Vector3.one;
    }

    public virtual float GetInfluenceAndGravityVector(Vector3 point, out Vector3 gravityVector) {
        gravityVector = Vector3.zero;
        return 0f;
    }
    
    public virtual float GetContribution(Vector3 point) {
        return 0f;
    }

    public virtual Vector3 GetGravityVector(Vector3 point) {
        return Vector3.down;
    }
    
    public static Vector3 ClosestPointOnSegment(Vector3 start, Vector3 end, Vector3 point)
    {
        Vector3 segment = end - start;
        float segmentLengthSq = segment.sqrMagnitude;

        // Handle degenerate segment
        if (segmentLengthSq < Mathf.Epsilon)
            return start;

        // Project point onto the segment
        float t = Vector3.Dot(point - start, segment) / segmentLengthSq;

        // Clamp to the segment bounds
        t = Mathf.Clamp01(t);

        return start + segment * t;
    }
    
}
