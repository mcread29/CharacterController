using UnityEngine;


public static class GizmoExtensions
{
     public static void DrawWireCapsule(Vector3 position, Quaternion rotation, float radius, float height)
    {
        if (radius <= 0)
            return;

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(position, rotation, Vector3.one);

        float cylinderHeight = height * 0.5f;// - radius * 2f;
        Vector3 top = Vector3.forward * cylinderHeight;
        Vector3 bottom = -Vector3.forward * cylinderHeight;

        // Draw the top and bottom spheres
        Gizmos.DrawWireSphere(top, radius);
        Gizmos.DrawWireSphere(bottom, radius);

        // Draw the cylinder lines
        Gizmos.DrawLine(top + Vector3.up * radius, bottom + Vector3.up * radius);
        Gizmos.DrawLine(top - Vector3.up * radius, bottom - Vector3.up * radius);
        Gizmos.DrawLine(top + Vector3.right * radius, bottom + Vector3.right * radius);
        Gizmos.DrawLine(top - Vector3.right * radius, bottom - Vector3.right * radius);

        Gizmos.matrix = oldMatrix;
    }
}