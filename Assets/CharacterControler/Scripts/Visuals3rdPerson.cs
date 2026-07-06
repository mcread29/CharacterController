using UnityEngine;

public class Visuals3rdPerson : MonoBehaviour
{
    
    public CharacterControllerBase characterController;
    
    // Update is called once per frame
    void Update() {
        
        if (characterController == null) return;
        
        float dTime = Time.deltaTime;
        
        // Get the current move vector from the character controller
        Vector3 moveVector = characterController.GetMoveVector();
        
        // Use the local move vector to support different orientations
        Vector3 localMoveVector = characterController.transform.InverseTransformVector(moveVector);

        // rotate the visuals to match the move vector
        if (localMoveVector.sqrMagnitude > 0.1f) {
            Vector3 lookVector = localMoveVector;
            lookVector.y = 0f;
            Quaternion lookRotation = Quaternion.LookRotation(lookVector, Vector3.up);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, lookRotation, dTime * 10f);
        }
        
    }
    
}
