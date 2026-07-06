using System;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;


public interface ICharacterControllerInput
{
	void InputJump(bool jumpWasPressedThisFrame, bool jumpIsPressed);
	void InputMoveVector(Vector3 newMoveVector);
	Vector3 GetMoveVector();
}

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]

[DefaultExecutionOrder(1)]
public class CharacterControllerBase : MonoBehaviour, ICharacterControllerInput
{

	[Header("Visuals")]
	[SerializeField] protected GameObject visuals;
	[SerializeField] protected float visualsOffsetThreshold = 0.1f;
	[SerializeField] protected float maxVisualsOffset = 0.5f;
	[SerializeField] protected float visualsLerpFactor = 20f;
	
	[Header("Debug Ground")]
	[SerializeField] protected bool showDebugVisuals = true;
	[SerializeField] protected GameObject debugVisuals;
	
	[SerializeField] protected bool applyWorldRotation = true;
	[SerializeField] protected float velocityClipThreshold = 0.1f;

	protected enum GroundCollisionType {
		HighestPoint,
		AverageHeight,
		Capsule
	}
	
	[SerializeField] protected GroundCollisionType groundCollisionType = GroundCollisionType.AverageHeight;
	
	[Header("Character Size")]
	[SerializeField] protected float characterHeight = 2.0f;
	[SerializeField] protected float characterRadius = 0.5f;
	[SerializeField] protected float stepHeight = 0.5f;
	[SerializeField] protected float floorFudge = 0.1f;
	
	[Header("Slope Angles")]
	[SerializeField] protected float minSlope = 30.0f;
	[SerializeField] protected float maxSlope = 60.0f;

	[Header("Max Speeds")]
	[SerializeField] protected float maxWalkSpeed = 10.0f;
	[SerializeField] protected float maxRunSpeed = 10.0f;

	[Header("Drag for Air Movement and Falling")]
	[SerializeField] protected float airDrag = 1.0f;
	[SerializeField] protected float fallDrag = 0.25f;

	[Header("Acceleration for Ground and Air")]
	[SerializeField] protected float groundAcc = 7.0f;
	[SerializeField] protected float airAcc = 2.0f;

	[Header("Jumping")] 
	[SerializeField] protected bool useJumpCurve = false;
	[SerializeField] protected float jumpTime = 0.5f;
	[SerializeField] protected AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
	[SerializeField] protected float jumpCurveForce = 1.0f;
	[SerializeField] protected float jumpSpeed = 7.0f;
	[SerializeField] protected float jumpCoolDownTime = 0.1f;
	[SerializeField] protected float footstepDistance = 3.0f;
	
	[SerializeField] protected float coyoteTime = 0.1f;

	[Header("Change Gravity")] 
	[SerializeField] protected float gravity = 9.8f;
	[SerializeField] protected float gravityMultiply = 1.0f;
	[SerializeField] protected bool allowGravityDirectionChange = false;
	[SerializeField] protected Vector3 gravityDirection = Vector3.down;
	
	[SerializeField] protected LayerMask worldMask;
	
	[SerializeField] protected CharacterAudio characterAudio;

	
	// Cached variables
	protected float capsuleCenterLower = 0f;
	protected float capsuleCenterUpper = 0f;
	protected float capsuleHeightInner = 0f;
	protected float sphereCastRadius = 0f;
	protected float sphereCastThickness = 0f;
	
	protected Vector3[] groundSamplePoints;
	protected int[] groundSampleTris;
	protected int groundSampleTriCount = 0;

	protected bool[] groundSampleHits;
	protected Vector3[] groundSampleHitPoints;

	protected Transform parentHelper = null;
	protected Rigidbody thisRigidbody = null;
	protected CapsuleCollider thisCollider = null;
	
	// State variables
	protected Vector3 velocity = Vector3.zero;
	protected Vector3 groundNormal = Vector3.up;
	protected Vector3 localGroundNormal = Vector3.up;
	protected float groundHeight = 0f;
	
	protected bool inGround = false;
	protected bool inGroundPrev = false;
	
	protected bool touchingGround = false;
	protected bool touchingGroundPrev = false;
	protected bool grounded = false;
	protected float airTime = 0f;
	
	protected Vector3 moveVector = Vector3.zero;
	protected bool inputJumpIsPressed = false;
	
	protected bool inputJump = false;
	protected float lastInputJump = 0f;
	protected float jumpCoolDownTimer = 0.0f;
	protected bool canDoubleJump = false;
	protected bool jumping = false;
	protected float jumpTimer = 0.0f;
	
	protected float footstepDistanceTraveled = 0f;

	protected Vector3 worldVelocity = Vector3.zero;
	protected Vector3 parentHelperLastPos = Vector3.zero;
	protected Vector3 parentHelperLastForward = Vector3.forward;
	
	
	void OnValidate() {
		thisCollider = transform.GetComponent<CapsuleCollider>();
		thisRigidbody = transform.GetComponent<Rigidbody>();
		
		thisRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
		
		thisCollider.radius = characterRadius;
		thisCollider.height = characterHeight - stepHeight;
		
		float verticalOffset = (characterHeight - stepHeight) * 0.5f + stepHeight;
		thisCollider.center = new Vector3(0, verticalOffset, 0);

		capsuleCenterLower = stepHeight + characterRadius;
		capsuleCenterUpper = characterHeight - characterRadius;
		capsuleHeightInner = capsuleCenterUpper - capsuleCenterLower;

		sphereCastRadius = characterRadius * 0.9f;

		sphereCastThickness = characterRadius - sphereCastRadius;
	}
	
	void Awake() {
		OnValidate();
		SetParent(null, false);
		
		groundSamplePoints = new Vector3[13];
		groundSamplePoints[0] = new Vector3(0.0f, 0f, 1.0f);
		groundSamplePoints[1] = new Vector3(0.7f, 0f, 0.7f);
		groundSamplePoints[2] = new Vector3(1.0f, 0f, 0.0f);
		groundSamplePoints[3] = new Vector3(0.7f, 0f, -0.7f);
		groundSamplePoints[4] = new Vector3(0.0f, 0f, -1.0f);
		groundSamplePoints[5] = new Vector3(-0.7f, 0f, -0.7f);
		groundSamplePoints[6] = new Vector3(-1.0f, 0f, 0.0f);
		groundSamplePoints[7] = new Vector3(-0.7f, 0f, 0.7f);
		groundSamplePoints[8] = new Vector3(0.0f, 0f, 0.5f);
		groundSamplePoints[9] = new Vector3(0.5f, 0f, 0.0f);
		groundSamplePoints[10] = new Vector3(0.0f, 0f, -0.5f);
		groundSamplePoints[11] = new Vector3(-0.5f, 0f, 0.0f);
		groundSamplePoints[12] = new Vector3(0.0f, 0f, 0.0f);
		
		groundSampleHits = new bool[13];
		groundSampleHitPoints = new Vector3[13];
		for (int i = 0; i < groundSamplePoints.Length; i++) {
			groundSamplePoints[i] = groundSamplePoints[i] * sphereCastRadius;
			groundSampleHits[i] = false;
			groundSampleHitPoints[i] = Vector3.zero;
		}

		groundSampleTriCount = 16;
		
		List<int> groundSampleTrisList = new List<int>();
		AddTriangle(groundSampleTrisList, 0, 1, 8);
		AddTriangle(groundSampleTrisList, 8, 1, 9);
		AddTriangle(groundSampleTrisList, 9, 1, 2);
		AddTriangle(groundSampleTrisList, 9, 2, 3);
		AddTriangle(groundSampleTrisList, 10, 9, 3);
		AddTriangle(groundSampleTrisList, 4, 10, 3);
		AddTriangle(groundSampleTrisList, 5, 10, 4);
		AddTriangle(groundSampleTrisList, 11, 10, 5);
		AddTriangle(groundSampleTrisList, 6, 11, 5);
		AddTriangle(groundSampleTrisList, 6, 7, 11);
		AddTriangle(groundSampleTrisList, 7, 8, 11);
		AddTriangle(groundSampleTrisList, 7, 0, 8);
		AddTriangle(groundSampleTrisList, 8, 9, 12);
		AddTriangle(groundSampleTrisList, 12, 9, 10);
		AddTriangle(groundSampleTrisList, 11, 12, 10);
		AddTriangle(groundSampleTrisList, 11, 8, 12);
		
		groundSampleTris = groundSampleTrisList.ToArray();
	}

	private void AddTriangle(List<int> tris, int a, int b, int c) {
		tris.Add(a);
		tris.Add(b);
		tris.Add(c);
	}
	

    // Update is called once per frame
    void Update() {
	    float dTime = Time.deltaTime;
	    UpdateRotationForGravity(dTime);
	    if (!PhysicsManager.s_characterUseFixedUpdate) {
		    thisRigidbody.interpolation = RigidbodyInterpolation.None;
		    WorldMovement(dTime, false);
		    CheckGround(dTime, false);
		    Movement(dTime, false);
	    }
	    FixVisualPosition(dTime);
    }

    void FixedUpdate() {
	    if (PhysicsManager.s_characterUseFixedUpdate) {
		    if (PhysicsManager.s_simulationMode == SimulationMode.FixedUpdate) {
			    thisRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		    } else {
			    thisRigidbody.interpolation = RigidbodyInterpolation.None;
		    }
		    float dTime = Time.fixedDeltaTime;
		    WorldMovement(dTime, true);
		    CheckGround(dTime, true);
		    Movement(dTime, true);
	    }
    }

    protected virtual void UpdateRotationForGravity(float dTime) {
	    if (!allowGravityDirectionChange) return;
	    Debug.DrawLine(transform.position, transform.position - gravityDirection * 10f, Color.red);
	    
	    Vector3 localForward = Vector3.Cross(transform.right, -gravityDirection);
	    Quaternion targetRotation = Quaternion.LookRotation(localForward, -gravityDirection);
		
	    transform.rotation = targetRotation;
    }
    
    protected virtual void FixVisualPosition(float dTime) {
	    Vector3 visualsPos = visuals.transform.localPosition;
	    visualsPos.y -= visualsPos.y * dTime * visualsLerpFactor;
	    visuals.transform.localPosition = visualsPos;
    }
    
    public void SetGravityDirection(Vector3 newGravityDirection) {
	    if (!allowGravityDirectionChange) return;
	    gravityDirection = newGravityDirection;
    }

    public void SetGravity(float newGravity) {
	    if (!allowGravityDirectionChange) return;
	    gravity = newGravity;
    }

    public virtual void InputMoveVector(Vector3 newMoveVector) {
	    moveVector = newMoveVector;
    }

    public virtual Vector3 GetMoveVector() {
	    return moveVector;
    }
    
    public void InputJump(bool jumpWasPressedThisFrame, bool jumpIsPressed) {
	    float dTime = Time.deltaTime;
	    
	    inputJump = false;
	    inputJumpIsPressed = jumpIsPressed;
	    
	    // keep track of the last time we pressed jump
	    lastInputJump += dTime;
	    if (jumpWasPressedThisFrame) {
		    lastInputJump = 0f;
	    }

	    // if we pressed jump a short enough time before we could honor that input when we can jump
	    if (jumpCoolDownTimer <= 0f) {
		    if (lastInputJump <= 0.1f) {
			    inputJump = true;
		    }
	    } else {
		    jumpCoolDownTimer -= dTime;
	    }
    }
    
    protected void SetParent(Transform parentTransform, bool fixedUpdate) {

	    if (parentHelper == null) {
		    parentHelper = new GameObject("CC_Parent_Helper").transform;
		    ResetParent(fixedUpdate);
	    }

	    parentHelper.SetParent(parentTransform);

	}

	protected void ResetParent(bool fixedUpdate) {

		if (fixedUpdate) {
			parentHelper.position = thisRigidbody.position;
		} else {
			parentHelper.position = transform.position;
		}

		parentHelper.localScale = Vector3.one;
		parentHelper.rotation = transform.rotation;
		parentHelperLastPos = parentHelper.position;
		parentHelperLastForward = parentHelper.forward;
	}
	
	//======================================//
	//	add the movement from the parent	//
	//======================================//
	
	protected virtual void WorldMovement(float dTime, bool fixedUpdate) {
	    
	    if (parentHelper == null) {
		    worldVelocity = Vector3.zero;
		    ResetParent(fixedUpdate);
		    return;
	    }
	    
    	if (parentHelper.parent == null) {
		    worldVelocity = Vector3.zero;
		    ResetParent(fixedUpdate);
    		return;
    	}
	    
	    // Apply world rotation to the visuals in local space 
	    if (applyWorldRotation) {
		    float deltaAngle = Vector3.SignedAngle(parentHelperLastForward, parentHelper.forward, parentHelper.up);
		    Vector3 baseRotation = visuals.transform.localEulerAngles;
		    baseRotation.y += deltaAngle;
		    visuals.transform.localEulerAngles = baseRotation;
	    }

	    Vector3 parentMovement = parentHelper.position - parentHelperLastPos;
	    worldVelocity = parentMovement / dTime;
	    
	    if (parentMovement.Equals(Vector3.zero)) {
		    ResetParent(fixedUpdate);
		    return;
	    }
	    
	    // subtract parent XZ movement from the character if we just landed, this puts the velocity in local space
	    // subtracting Y movement can lead to the character popping up if the ground is a rigidbody that the character just applied a landing impulse to
	    if (touchingGround && touchingGroundPrev == false) {
		    Vector3 rbVelocity = thisRigidbody.linearVelocity;
		    rbVelocity.x -= worldVelocity.x;
		    rbVelocity.z -= worldVelocity.z;
		    thisRigidbody.linearVelocity = rbVelocity;
		    
		    Debug.DrawRay(transform.position + Vector3.up, worldVelocity, Color.yellow, 1f);
		    Debug.DrawRay(parentHelper.position, Vector3.up * 5f, Color.yellow, 1f);
		    Debug.DrawRay(parentHelperLastPos, Vector3.up * 5f, Color.yellow, 1f);
	    }

	    Vector3 parentMovementDir = parentMovement.normalized;
    	float parentMovementLength = parentMovement.magnitude;
	    
    	// clip the movement against the world
	    // this keeps moving platforms from moving the player through a wall
	    // don't clip against non-kinematic rigid bodies
	    Vector3 capsuleStart = transform.position + transform.up * capsuleCenterLower;
	    Vector3 capsuleEnd = transform.position + transform.up * capsuleCenterUpper;
	    
	    RaycastHit[] hits = Physics.CapsuleCastAll(capsuleStart, capsuleEnd, sphereCastRadius, parentMovementDir, parentMovementLength + sphereCastThickness, worldMask, QueryTriggerInteraction.Ignore);
	    if (GetClosestHitKinematic(hits, out RaycastHit closestHitKinematic, parentMovementLength + sphereCastThickness)) {
		    parentMovementLength = closestHitKinematic.distance - sphereCastThickness;
	    }
	    
    	// apply the clipped movement
	    if (fixedUpdate) {
		    thisRigidbody.MovePosition(thisRigidbody.position + parentMovementDir * parentMovementLength);
	    } else {
		    transform.position += parentMovementDir * parentMovementLength;
	    }

	    ResetParent(fixedUpdate);

    }

	protected Vector3 AddGravity(Vector3 thisVelocity, float dTime) {
	    thisVelocity += gravityDirection * (gravity * gravityMultiply * dTime);
	    return thisVelocity;
    }
    
    //==================================================//
    //	check to see if we are touching ground			//
    //	set the characters position above the ground	//
    //==================================================//
    
    protected virtual void CheckGround(float dTime, bool fixedUpdate) {

	    // Get our current position
		Vector3 currentPos = transform.position;
		if (fixedUpdate) {
			currentPos = thisRigidbody.position;
		}
		
		touchingGroundPrev = touchingGround;
		touchingGround = false;
		
		inGroundPrev = inGround;
		inGround = false;
			
		Transform groundTransform = null;
		groundNormal = -gravityDirection;
		groundHeight = 0f;

		int groundPoints = 0;
		float groundHeightAvg = 0f;
		Vector3 groundNormalAverage = Vector3.zero;
		float highestPoint = -999999f;
		
		Ray ray = new Ray();
		Vector3 rayOrigin = currentPos - gravityDirection * (stepHeight + characterRadius);
		ray.direction = gravityDirection;
		float rayGroundLength = (stepHeight * 4f) + characterRadius + floorFudge;
		
		for (int i = 0; i < groundSamplePoints.Length; i++) {
			ray.origin = rayOrigin + transform.right * groundSamplePoints[i].x + transform.forward * groundSamplePoints[i].z;
			if( Physics.Raycast(ray, out RaycastHit hit, rayGroundLength, worldMask, QueryTriggerInteraction.Ignore) ) {
				
				// skip unwalkable slopes and let physics handle the slide
				float groundAngle = Vector3.Angle(-gravityDirection, hit.normal);
				if (groundAngle > maxSlope) {
					groundSampleHits[i] = false;
					groundSampleHitPoints[i] = Vector3.zero;
					continue;
				}
				
				// store the samples for later
				groundSampleHits[i] = true;
				groundSampleHitPoints[i] = hit.point;

				Vector3 localPoint = Vector3.zero;
				if (allowGravityDirectionChange) {
					localPoint = transform.InverseTransformPoint(hit.point);
				} else {
					localPoint = hit.point - transform.position;
				}

				// accumulate ground normal for average normal fallback
				// accumulate ground height for average height calculation
				groundNormalAverage += hit.normal;
				groundHeightAvg += localPoint.y;
				groundPoints++;
				
				Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);

				// set the touching ground flag if the ground is close enough
				if (localPoint.y + floorFudge > 0f) {
					touchingGround = true;
				}
				
				// get the highest point to use as the parent
				if (localPoint.y > highestPoint) {
					groundTransform = hit.transform;
					highestPoint = localPoint.y;
				}
			} else {
				// clear the samples if the ray didn't hit anything
				groundSampleHits[i] = false;
				groundSampleHitPoints[i] = Vector3.zero;
			}
		}
		
		// use the average ray y position as the ground height
		// average the normal as a fallback for the normal calculation
		if (groundPoints > 0) {
			groundHeight = groundHeightAvg / groundPoints;
			groundNormal = groundNormalAverage / groundPoints;
		}
		
		// get the velocity
		velocity = thisRigidbody.linearVelocity;
		Vector3 localVelocity = velocity;
		if (allowGravityDirectionChange) {
			localVelocity = transform.InverseTransformVector(velocity);
		}

		if (touchingGround) {

			float desiredMovement = 0f;
			
			if (groundCollisionType == GroundCollisionType.HighestPoint) {
				desiredMovement = MathF.Max(highestPoint, 0f);
			}
			
			if (groundCollisionType == GroundCollisionType.AverageHeight) {
				desiredMovement = MathF.Max(groundHeight, 0f);
			}

			if (groundCollisionType == GroundCollisionType.Capsule) {
				ray.origin = transform.position + transform.up * capsuleCenterLower;
				ray.direction = gravityDirection;
				float rayRadius = sphereCastRadius;
				float rayLength = stepHeight + sphereCastThickness;
			
				if (Physics.SphereCast(ray, rayRadius, out RaycastHit hit, rayLength, worldMask, QueryTriggerInteraction.Ignore)) {
					Debug.DrawLine(ray.origin, ray.origin + ray.direction * hit.distance, Color.red);
					desiredMovement = Mathf.Max(0.0f, rayLength - hit.distance);
				} else {
					Debug.DrawLine(ray.origin, ray.origin + ray.direction * rayLength, Color.green);
				}

			}

			// calculate the ground normal based on raycasts
			CalculateGroundNormal(groundNormal);
			
			// show the debug ground visuals
			DebugGroundVisuals(true, currentPos);
			
			// move rigid body if needed
			if (desiredMovement > 0f) {

				inGround = true;

				ray.origin = transform.TransformPoint(new Vector3(0f, capsuleCenterLower, 0f));
				ray.direction = -gravityDirection;
				float rayRadius = sphereCastRadius;
				float rayLength = capsuleHeightInner + desiredMovement + sphereCastThickness;
				
				// make sure we are not obstructed by a kinematic collider and clip the movement
				RaycastHit[] hits = Physics.SphereCastAll(ray, rayRadius, rayLength, worldMask, QueryTriggerInteraction.Ignore);
				if (GetClosestHitKinematic(hits, out RaycastHit closestHitKinematic, rayLength)) {
					Debug.DrawLine(ray.origin, ray.origin + ray.direction * closestHitKinematic.distance, Color.red);
					float hitDif = rayLength - closestHitKinematic.distance;
					desiredMovement = Mathf.Max(0.0f, desiredMovement - hitDif);
				} else {
					Debug.DrawLine(ray.origin, ray.origin + ray.direction * rayLength, Color.green);
				}
				
				// move the character out of the ground
				if (fixedUpdate) {
					thisRigidbody.MovePosition(thisRigidbody.position + (-gravityDirection * desiredMovement));
				} else {
					transform.position = transform.position + (-gravityDirection * desiredMovement);
				}
				
				// move the visuals down so it does not pop up, only do this if the distance exceeds the threshold
				if (desiredMovement > visualsOffsetThreshold) {
					Vector3 visualsPos = visuals.transform.localPosition;
					visualsPos.y -= desiredMovement;
					// don't let the visuals get too far down
					if (visualsPos.y < -maxVisualsOffset) {
						visualsPos.y = -maxVisualsOffset;
					}
					visuals.transform.localPosition = visualsPos;
				}

				// get the ridged body of what we are standing on
				Rigidbody groundRigidbody = groundTransform?.GetComponent<Rigidbody>();
				if (groundRigidbody != null) {

					if (!groundRigidbody.isKinematic) {
						// push rigid bodies down when we walk on them
						groundRigidbody.AddForceAtPosition(gravityDirection * (thisRigidbody.mass * 9.8f * gravityMultiply * dTime), currentPos, ForceMode.Impulse);

						// add impact force if we just landed
						if (!inGroundPrev && localVelocity.y < 0f) {
							
							//Vector3 impactForce = gravityDirection * (thisRigidbody.mass * -localVelocity.y);
							
							// Account for rigid body's velocity
							Vector3 pointVelocity = thisRigidbody.GetPointVelocity(currentPos);
							float gravityDot = Mathf.Clamp01(Vector3.Dot(pointVelocity, gravityDirection));
							Vector3 impactForce = gravityDirection * (thisRigidbody.mass * -localVelocity.y * gravityDot);
							groundRigidbody.AddForceAtPosition(impactForce, currentPos, ForceMode.Impulse);
						}
					}
				}
				
				// stop vertical velocity but let it slide up or down a slope if we are moving.
				if (localVelocity.y < 0f && (localVelocity.x > 0.01f || localVelocity.x < -0.01f || localVelocity.z > 0.01f || localVelocity.z < -0.01f)) {
					Vector3 groundCross = Vector3.Cross(localGroundNormal, localVelocity);
					Vector3 groundMove = Vector3.Cross(groundCross, localGroundNormal);
					localVelocity.y = Mathf.Max(groundMove.y, localVelocity.y);
				} else {
					localVelocity.y = Mathf.Max(0f, localVelocity.y);
				}
				
				if (allowGravityDirectionChange) {
					// transform back to world velocity
					velocity = transform.TransformVector(localVelocity);
				} else {
					velocity = localVelocity;
				}
			}
			
			// set the current parent
			SetParent(groundTransform, fixedUpdate);
			
		} else {
			// If we just left the ground add the worlds velocity to the character velocity
			if (touchingGroundPrev) {
				velocity += worldVelocity;
			}
			
			// clear the parent
			SetParent(null, fixedUpdate);
			
			// hide the debug ground
			DebugGroundVisuals(false, currentPos);
		}
		
		// always add gravity
		velocity = AddGravity(velocity, dTime); 
		
		thisRigidbody.linearVelocity = velocity;

	}
    
	protected bool GetClosestHitKinematic(RaycastHit[] hits, out RaycastHit closestHit, float maxDistance) {
		closestHit = new RaycastHit { distance = maxDistance };
		
		bool hitSomething = false;
		for (int j = 0; j < hits.Length; j++) {

			RaycastHit hit = hits[j];
		    
			// don't count dynamic rigid bodies
			Rigidbody hitRb = hit.transform.GetComponent<Rigidbody>();
			if (hitRb != null) {
				if (!hitRb.isKinematic) continue;
			}
		    
			if (hit.distance < closestHit.distance) {
				hitSomething = true;
				closestHit = hit;
			}

		}

		return hitSomething;
	}
	
	protected bool GetClosestHitDynamic(RaycastHit[] hits, out RaycastHit closestHit, out Rigidbody closestRB, float maxDistance) {
		closestHit = new RaycastHit { distance = maxDistance };
		closestRB = null;
		
		bool hitSomething = false;
		for (int j = 0; j < hits.Length; j++) {

			RaycastHit hit = hits[j];
		    
			// only count dynamic rigid bodies
			Rigidbody hitRb = hit.transform.GetComponent<Rigidbody>();
			if (hitRb == null) continue;
			if (hitRb.isKinematic) continue;
			
			if (hit.distance < closestHit.distance) {
				hitSomething = true;
				closestHit = hit;
				closestRB = hitRb;
			}

		}

		return hitSomething;
	}

    // Clips the velocity against the world.  This is helpful when using concave colliders or when your time step gets high.
    protected virtual Vector3 ClipVelocity(Vector3 thisVelocity, float dTime) {
	    
	    // the vector we will be traveling this time step
	    Vector3 thisVelocityStep = thisVelocity * dTime;

	    // get the distance we are traveling this frame
	    float velocityDist = thisVelocityStep.magnitude;

	    // if the distance we are traveling is low just return it.
	    if (velocityDist < velocityClipThreshold) return thisVelocity;

	    Vector3 capsuleStart = transform.position + transform.up * capsuleCenterLower;
	    Vector3 capsuleEnd = transform.position + transform.up * capsuleCenterUpper;
	    
	    RaycastHit[] hits = Physics.CapsuleCastAll(capsuleStart, capsuleEnd, sphereCastRadius, thisVelocity, velocityDist + sphereCastThickness, worldMask, QueryTriggerInteraction.Ignore);
	    
	    if (GetClosestHitKinematic(hits, out RaycastHit closestHit, velocityDist - sphereCastThickness)) {
		    float distFrac = (closestHit.distance - sphereCastThickness) / velocityDist;
		    Vector3 vStep = thisVelocityStep * (1.0f - distFrac);
		    vStep -= Vector3.Dot( vStep, closestHit.normal) * closestHit.normal; 
					
		    thisVelocityStep *= distFrac;
		    thisVelocityStep += vStep;
	    }
	    
	    return thisVelocityStep / dTime;
	    
	}
    
	// a helpful math function like smooth step but linear
	protected float LinearStep( float min, float max, float val ){
		return Mathf.Clamp01( ( val - min ) * ( 1.0f / ( max - min ) ) );
	}

	// lowers the movement vector based on the slope
	protected Vector3 InhibitMovementAgainstSlope(Vector3 thisMoveVector, Vector3 thisGroundNormal) {
		float slopeMoveAngle = 90f - Vector3.Angle(-thisMoveVector.normalized, thisGroundNormal);
		float slopeMoveAngleMapped = LinearStep(minSlope, maxSlope, slopeMoveAngle);
		thisMoveVector *= 1.0f - slopeMoveAngleMapped * slopeMoveAngleMapped;
		return thisMoveVector;
	}

	// conforms the movement vector to the slope
	// this keeps the character from hopping down slopes and stairs
	protected Vector3 ConformMovementAgainstSlope(Vector3 thisMoveVector, Vector3 thisGroundNormal) {
		Vector3 groundCross = Vector3.Cross(thisGroundNormal, thisMoveVector);
		if (groundCross.sqrMagnitude < 0.001f) return thisMoveVector;  // the ground is flat
		Vector3 groundMove = Vector3.Cross(groundCross, thisGroundNormal);
		thisMoveVector = groundMove;
				
		// normalize the XY speed
		Vector3 thisMoveVectorXY = new Vector3(thisMoveVector.x, 0f, thisMoveVector.z);
		thisMoveVector *= thisMoveVector.magnitude / thisMoveVectorXY.magnitude;
		
		return thisMoveVector;
	}

	protected void SetGrounded() {
		// play land sound
		if (!grounded) MovementLand();
			
		// set grounded
		grounded = true;
				
		// allow double jump
		canDoubleJump = true;

		// no longer Jumping
		jumping = false;

		// reset air time
		airTime = 0f;
	}
	
	//==========================================//
	//	all things player controlled movement.	//
	//==========================================//
	
	protected virtual void Movement(float dTime, bool fixedUpdate) {
		
		// Get the current velocity of the rigid body and move vector
		velocity = thisRigidbody.linearVelocity;
		Vector3 localVelocity = velocity;
		Vector3 localMoveVector = moveVector;

		// Transform to local space if needed
		if (allowGravityDirectionChange) {
			localVelocity = transform.InverseTransformVector(velocity);
			localMoveVector = transform.InverseTransformVector(moveVector);
		}
		
		if (touchingGround) {
			// if we are jumping wait until we collide with the ground and the jump timer is done
			if (jumping) {
				if (inGround && jumpCoolDownTimer <= 0f) {
					SetGrounded();
				}
			} else {
				SetGrounded();
			}

		} else {

			// add to airTime
			airTime += dTime;
			if (airTime > coyoteTime) {
				grounded = false;
			}

		}
		
		//==========================================//
		//	build acceleration based on difference	//
		//	between current speed and top speed		//
		//==========================================//
		
		// velocityXZ is the game movement
		Vector3 localVelocityXZ = localVelocity;
		localVelocityXZ.y = 0f;

		// set current max speed to walk speed
		float currentMaxSpeed = maxRunSpeed;
		
		// apply movement input
		if (touchingGround) {
			
			if (localMoveVector.sqrMagnitude > 0.01f) {
				localMoveVector = InhibitMovementAgainstSlope(localMoveVector, localGroundNormal);
				localMoveVector = ConformMovementAgainstSlope(localMoveVector, localGroundNormal);
				Debug.DrawRay(transform.position, localMoveVector, Color.red);
			} else {
				localMoveVector = Vector3.zero;
			}

			// use target velocity instead of drag and acceleration
			Vector3 targetVelocity = localMoveVector * currentMaxSpeed;
			targetVelocity.y = Mathf.Min(targetVelocity.y, 0f); // stop from hopping up ramps
			targetVelocity.y += localVelocity.y; // add current velocity to stop hopping down ramps
			localVelocity += (targetVelocity - localVelocity) * (groundAcc * dTime);

			// do footstep stuff
			footstepDistanceTraveled += localVelocityXZ.magnitude * dTime;
			if (footstepDistanceTraveled >= footstepDistance) {
				characterAudio?.PlayStep();
				footstepDistanceTraveled = 0f;
			}
			
			//if (float.IsNaN(localVelocity.x) || float.IsNaN(localVelocity.y) || float.IsNaN(localVelocity.z)) {
			//	Debug.LogError(localVelocity);
			//}

		} else {

			// acceleration in respect to current velocity
			float vDot = Vector3.Dot(localMoveVector, localVelocityXZ);
			float accDif = (localMoveVector.magnitude * currentMaxSpeed) - vDot;

			// add air drag
			localVelocity -= localVelocityXZ * (airDrag * dTime);
			// add fall drag
			localVelocity.y -= localVelocity.y * fallDrag * dTime;
			// add movement
			localVelocity += localMoveVector * (airAcc * accDif * dTime);
			
			// no footsteps in the air
			footstepDistanceTraveled = 0f;
			
		}

		if (allowGravityDirectionChange) {
			// transform back to world velocity
			velocity = transform.TransformVector(localVelocity);
		} else {
			velocity = localVelocity;
		}
		
		// jumping
		MovementJump(dTime); 
		
		// clip velocity against world if needed
		velocity = ClipVelocity(velocity, dTime);
		
		// Update the Rigid Body velocity
		thisRigidbody.linearVelocity = velocity;
		
	}

	protected void MovementJump(float dTime) {
		if (useJumpCurve) {
			MovementJumpCurve(dTime);
		} else {
			MovementJumpInstant(dTime);
		}
	}

	protected void MovementJumpInstant(float dTime) {
		if (!inputJump) return;
		
		if (grounded) {
			jumpCoolDownTimer = 0.1f;
			grounded = false;
			jumping = true;
			velocity -= Vector3.Dot(velocity, gravityDirection) * gravityDirection;
			velocity -= gravityDirection * jumpSpeed;
			characterAudio?.PlayJump();
			return;
		}

		if (canDoubleJump) {
			jumpCoolDownTimer = 0.1f;
			grounded = false;
			jumping = true;
			canDoubleJump = false;
			velocity -= Vector3.Dot(velocity, gravityDirection) * gravityDirection;
			velocity -= gravityDirection * jumpSpeed;
			characterAudio?.PlayJump();
			return;
		}
	}

	protected void MovementJumpCurve(float dTime) {
		
		if (jumping) {
			if (!inputJumpIsPressed) {
				jumping = false;
				jumpTimer = 0f;
				return;
			}

			jumpTimer += dTime;
			if (jumpTimer > jumpTime) {
				jumping = false;
				jumpTimer = 0f;
				return;
			}
			
			float timeFraction = jumpTimer / jumpTime;
			velocity -= gravityDirection * (jumpCurve.Evaluate(timeFraction) * jumpCurveForce * dTime);
			return;
		}
		
		if (!inputJump) return;
		
		if (grounded) {
			jumpCoolDownTimer = 0.1f;
			jumpTimer = 0f;
			grounded = false;
			jumping = true;

			velocity -= Vector3.Dot(velocity, gravityDirection) * gravityDirection;
			velocity -= gravityDirection * jumpSpeed;
			characterAudio.PlayJump();
			return;
		}

		if (canDoubleJump) {
			jumpCoolDownTimer = 0.1f;
			grounded = false;
			jumping = true;
			canDoubleJump = false;
			velocity -= Vector3.Dot(velocity, gravityDirection) * gravityDirection;
			velocity -= gravityDirection * jumpSpeed;
			characterAudio?.PlayJump();
			return;
		}
	}
	

	protected void MovementLand() {
		// If we were in the air for more time play a land sound
		// if we were in the air for less time play a step sound
		// if we were not in the air for very long at all do not play a sound so sounds don't get spammed
		if (airTime > 0.3f) {
			characterAudio?.PlayLand();
		} else if( airTime > 0.1f){
			characterAudio?.PlayStep();
		}
	}

	protected virtual void CalculateGroundNormal(Vector3 defaultGroundNormal) {

		int normalCount = 0;
		Vector3 normals = Vector3.zero;
		
		for (int i = 0; i < groundSampleTriCount; i++) {
			int tri = i * 3;
			
			if( !groundSampleHits[groundSampleTris[tri]]) continue;
			if( !groundSampleHits[groundSampleTris[tri+1]]) continue;
			if( !groundSampleHits[groundSampleTris[tri+2]]) continue;
			
			Vector3 point0 = groundSampleHitPoints[groundSampleTris[tri]];
			Vector3 point1 = groundSampleHitPoints[groundSampleTris[tri+1]];
			Vector3 point2 = groundSampleHitPoints[groundSampleTris[tri+2]];
			
			Debug.DrawLine(point0, point1, Color.green);
			Debug.DrawLine(point1, point2, Color.green);
			Debug.DrawLine(point2, point0, Color.green);
			
			Vector3 newNormal = Vector3.Cross(point1 - point0, point2 - point0);
			//if( newNormal.sqrMagnitude == 0f ) continue;
			//if( Vector3.Dot(newNormal, gravityDirection) > 0f ) newNormal = -newNormal;
			newNormal.Normalize();
			normals += newNormal;
			normalCount++;
		}

		if (normalCount == 0) {
			groundNormal = defaultGroundNormal;
		} else {
			groundNormal = (normals / normalCount).normalized;
		}

		if (allowGravityDirectionChange) {
			localGroundNormal = transform.InverseTransformDirection(groundNormal);
		} else {
			localGroundNormal = groundNormal;
		}
	}

	protected virtual void DebugGroundVisuals(bool isActive, Vector3 debugPos) {
		if (!showDebugVisuals) {
			if (debugVisuals.activeSelf) debugVisuals.SetActive(false);
			return;
		}
		
		debugVisuals.SetActive(isActive);
		
		debugPos -= gravityDirection * groundHeight;
		debugVisuals.transform.position = debugPos;
			
		Quaternion debugRotation = Quaternion.LookRotation(groundNormal, transform.right);
		debugVisuals.transform.rotation = debugRotation;
	}


}
