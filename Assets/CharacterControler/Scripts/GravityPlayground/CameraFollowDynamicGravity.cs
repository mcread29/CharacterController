using System;
using Unity.VisualScripting;
using UnityEngine;

public class CameraFollowDynamicGravity : CameraFollow
{
    Vector3 dynamicUp = Vector3.up;

    Transform targetTransformHelper = null;


    Vector3 FlattenNormalize(Vector3 vector, Vector3 flattenedVector) {
        float dot = Vector3.Dot(vector, flattenedVector);
        vector -= flattenedVector * dot;
        return vector.normalized;
    }
    
    void Start() {
        
        targetTransformHelper = new GameObject().transform;
        targetTransformHelper.name = "TargetTransformHelper";
        
        targetVelocity = Vector3.zero;
        lastTargetPosition = target.position;
        lookTarget = target.position + targetOffsetHigh;

        camPos = transform.position;
        
        dynamicUp = target.up;
        targetTransformHelper.transform.position = target.position;
        targetTransformHelper.transform.rotation = target.rotation;
    }

    // Update is called once per frame
    protected override void LateUpdate() {

        float dTime = Time.deltaTime;

        dynamicUp = Vector3.Lerp(dynamicUp, target.transform.up, dTime * lerpSpeed);
        
        Debug.DrawLine(transform.position, transform.position + dynamicUp * 2f, Color.red);
        targetTransformHelper.transform.position = target.position;
        targetTransformHelper.transform.rotation = target.rotation;
        
        // look the camera up and down
        cameraYOffset = Mathf.Clamp(cameraYOffset - lookVector.y * dTime * 0.5f, 0f, 1f);
        
        Vector3 currentTargetOffset = Vector3.Lerp(targetOffsetLow, targetOffsetHigh, cameraYOffset);
        currentTargetOffset = targetTransformHelper.TransformVector(currentTargetOffset);
   
        
        Debug.DrawLine(targetTransformHelper.position, targetTransformHelper.position + currentTargetOffset, Color.green);
        
        Vector3 targetPos = target.position;
        Vector3 newTargetVelocity = (targetPos - lastTargetPosition) / dTime;
        targetVelocity = Vector3.Lerp(targetVelocity, newTargetVelocity, dTime * 5.0f);
        lastTargetPosition = targetPos;

        Vector3 newLookTarget = targetPos + (targetVelocity * 0.25f) + currentTargetOffset;
        lookTarget = Vector3.Lerp(lookTarget, newLookTarget, dTime * lerpSpeed);
        
        Vector3 targetCamPosLocal = targetTransformHelper.InverseTransformPoint(camPos);
        
        // look the camera left and right
        Vector3 cameraRight = targetTransformHelper.InverseTransformDirection(transform.right);
        targetCamPosLocal -= cameraRight * (lookVector.x * 2.0f);

        targetCamPosLocal.y = 0;
        targetCamPosLocal = targetCamPosLocal.normalized * cameraXYOffset;
        targetCamPosLocal.y = cameraYOffset * 10f;
        
        Vector3 targetCamPos = targetTransformHelper.TransformPoint(targetCamPosLocal);

        camPos = Vector3.Lerp(camPos, targetCamPos, dTime * lerpSpeed);
        
        transform.position = camPos;
        transform.LookAt(lookTarget, dynamicUp);
    }

   void OnDrawGizmos() {
       if (targetTransformHelper == null) return;
       Gizmos.color = Color.green;
       Gizmos.matrix = targetTransformHelper.localToWorldMatrix;
       Gizmos.DrawWireCube(Vector3.zero, new Vector3(2,1,2));
        
   }
}
