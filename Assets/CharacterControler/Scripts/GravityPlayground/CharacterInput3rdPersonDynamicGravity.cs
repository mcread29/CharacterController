using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-1)]
public class CharacterInput3rdPersonDynamicGravity : MonoBehaviour
{
    public CharacterControllerBase characterController;
    public CameraFollow cameraFollow;
    
    //public Transform characterTransform;
    //public Transform cameraTransform;
    
    private InputAction moveControl;
    private InputAction lookControl;
    private InputAction jumpAction;
    private InputCharacter inputCharacter;
    private InputAction mouseDelta;
    
    void Awake() {
        inputCharacter = new InputCharacter();
    }

    void OnEnable() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        inputCharacter.Enable();

        moveControl = inputCharacter.Character.Move;
        moveControl.Enable();
        
        lookControl = inputCharacter.Character.Look;
        lookControl.Enable();
        
        jumpAction = inputCharacter.Character.Jump;
        jumpAction.Enable();
        
        mouseDelta = inputCharacter.Character.MouseDelta;
        mouseDelta.Enable();
    }
    
    void OnDisable() {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        moveControl.Disable();
        lookControl.Disable();
        
        jumpAction.Disable();
        
        mouseDelta.Disable();

        inputCharacter.Disable();
    }

    void OnDestroy() {
        moveControl.Dispose();
        lookControl.Dispose();
        
        jumpAction.Dispose();
        
        mouseDelta.Dispose();
        
        inputCharacter.Dispose();
    }

    void Update() {
        UpdateCursorCapture();

        characterController.InputJump(jumpAction.WasPressedThisFrame(), jumpAction.IsPressed());
        characterController.InputMoveVector(GetWorldMoveVector());

        Vector2 lookVector = lookControl.ReadValue<Vector2>();
        lookVector += mouseDelta.ReadValue<Vector2>() * new Vector2(0.2f, 0.5f);
        
        cameraFollow.InputLookVector(lookVector);
    }
    
    private void UpdateCursorCapture() {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        } else if (Mouse.current?.leftButton.wasPressedThisFrame == true) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private Vector2 GetMoveVector() {
        return moveControl.ReadValue<Vector2>();
    }

    private Vector3 GetWorldMoveVector() {

        Vector2 localInput = GetMoveVector();
        Vector3 worldInput = Vector3.zero;

        Vector3 characterUp = characterController.transform.up;
        Vector3 dirToCharacter = (characterController.transform.position - cameraFollow.transform.position).normalized;
        
        //float forwardDot = Vector3.Dot(dirToCharacter, cameraFollow.transform.forward);
        //Vector3 camForward = Vector3.Lerp(cameraFollow.transform.forward, cameraFollow.transform.up, forwardDot).normalized;
        //Vector3 worldForward = (camForward - characterUp * Vector3.Dot(camForward, characterUp)).normalized;
        
        //float rightDot = Vector3.Dot(dirToCharacter, cameraFollow.transform.right);
        //Vector3 camRight = Vector3.Lerp(cameraFollow.transform.right, cameraFollow.transform.forward, rightDot).normalized;
        //Vector3 worldRight = (camRight - characterUp * Vector3.Dot(camRight, characterUp)).normalized;


        Vector3 camForward = cameraFollow.transform.forward;
        Vector3 worldForward = (camForward - characterUp * Vector3.Dot(camForward, characterUp)).normalized;
        
        Vector3 camRight = cameraFollow.transform.right;
        Vector3 worldRight = (camRight - characterUp * Vector3.Dot(camRight, characterUp)).normalized;
        
        worldInput += worldRight * localInput.x;
        worldInput += worldForward * localInput.y;
        
        return worldInput;
    }
    
    
}
