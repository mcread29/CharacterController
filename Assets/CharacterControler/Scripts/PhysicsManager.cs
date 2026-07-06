using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class PhysicsManager : MonoBehaviour
{

    [Tooltip("Set The simulation mode")]
    public SimulationMode simulationMode = SimulationMode.Script;
    public static SimulationMode s_simulationMode = SimulationMode.Script;
    
    [Tooltip("The Fixed Update Time that should be used when not using Script simulation mode")]
    public float fixedUpdateTime = 0.5f;
    
    [Tooltip("Should the platforms use fixed update")]
    public bool platformsUseFixedUpdate = false;
    public static bool s_platformsUseFixedUpdate = false;
    
    [Tooltip("Should the character controller use fixed update")]
    public bool characterUseFixedUpdate = false;
    public static bool s_characterUseFixedUpdate = false;
    
    // default max time step if 50 which is Unity's default as well
    [Tooltip("The maximum amount of time that can pass in a physics update")]
    public float maxTimeStep = 0.02f;

    // The more phicis updates that happen the longer the frame will take, then the next frame may have even more physics updates
    // It's a good ide to clamp this at a reasonable value.  If you need more than 1 or 2 slices you are already having a bad time so don't make it worse.
    [Tooltip("The maximum number of physics updates that can happen in a frame")]
    public int maxStepSlices = 2;
    
    // This will turn bad performance into a sort of slow-mo for physics instead of letting things become unstable
    [Tooltip("Allow physics time to slow down instead of becoming unstable")]
    public bool allowSlowDown = true;
    
    private float deltaTime = 0.0f;

    public float DeltaTime
    {
        get { return deltaTime; }
    }
    
    void Awake() {
        
        // We need the physics to update every frame so that the physics time matches the game time
        // We can't use FixedUpdate because this will lead to stuttering between the physical game objects and the regular game objects
        //Physics.simulationMode = SimulationMode.FixedUpdate;

        // Using the Update mode will make the physics update AFTER the rest of the scripts update
        // we need the physics update to happen BEFORE the rest of the updates but still every frame
        //Physics.simulationMode = SimulationMode.Update;
        
        // We want the physics update to happen every frame and BEFORE everything else so call it manually
        // We don't need to use any interpolation on the rigid bodies this way
        //Physics.simulationMode = SimulationMode.Script;
        
    }

    void Start() {
        UpdateInterpolation();
    }
    
    // If we are running the physics every frame we do not need to interpolate rigidbody motion
    void UpdateInterpolation() {
        RigidbodyInterpolation interpolation = RigidbodyInterpolation.None;
        if (Physics.simulationMode == SimulationMode.FixedUpdate) {
            interpolation = RigidbodyInterpolation.Interpolate;
        }
        
        Rigidbody[] rigidBodies = FindObjectsByType<Rigidbody>();
        foreach (Rigidbody rigidBody in rigidBodies) {
            rigidBody.interpolation = interpolation;
        }
    }

    // Update is called once per frame
    void Update() {
        
        deltaTime = Time.deltaTime;
        
        s_platformsUseFixedUpdate = platformsUseFixedUpdate;
        s_characterUseFixedUpdate = characterUseFixedUpdate;
        s_simulationMode = simulationMode;

        if (Physics.simulationMode != simulationMode) {
            Physics.simulationMode = simulationMode;
            UpdateInterpolation();
        }

        // If not using script update reset the delta time and return
        if (simulationMode != SimulationMode.Script) {
            Time.fixedDeltaTime = fixedUpdateTime;
            return;
        }
        
        // only need to do this if the game is paused AND we are not updating physics AND we want to still use the physics system for something like menu raycasts.
        // Sync transforms
        //Physics.SyncTransforms();
        
        // If we go over the max timeStep divide the time steps evenly
        // this will be a more stable simulation but can also lower FPS even more
        int timeStepSlices = Mathf.CeilToInt(deltaTime / maxTimeStep);
        
        // Cap the maximum number of time slices
        if (timeStepSlices > maxStepSlices) timeStepSlices = maxStepSlices;
        float dTime = deltaTime / timeStepSlices;
        
        // Slow down time if the max time step is exceeded
        if (allowSlowDown) dTime = Mathf.Min(dTime, maxTimeStep);
        
        // set the fixed delta time so fixed update scripts have access to it
        Time.fixedDeltaTime = dTime; 
        
        // Simulate physics with time steps
        for (int i = 0; i < timeStepSlices; i++) {
            Physics.Simulate(dTime);
        }
        
        // update the frame delta time to account for slowdown
        deltaTime = dTime * timeStepSlices;

    }
}
