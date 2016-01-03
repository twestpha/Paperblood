using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

	public float speed;
	public float turn_speed;
	public float jump_velocity;
	public float height_offset;

	private bool is_grounded;
	private float cosTheta;
	private float sinTheta;
	private Vector3 velocity;

	void Start(){
		// Precalculate the cos and sin of the world offset
		float theta_rads = 315.0f /* Degrees */ * Mathf.PI / 180.0f;
		cosTheta = Mathf.Cos(theta_rads);
		sinTheta = Mathf.Sin(theta_rads);

		// We start on the ground
		is_grounded = true;
	}

	void FixedUpdate(){

		if(is_grounded){
			bool shouldZeroVelocity = true;

			if(Input.GetButton("Fire2")){
				HandleMouseInput();
				shouldZeroVelocity = false;
			}

			if(Input.GetKeyDown("space")) {
				HandleSpacePressed();
				shouldZeroVelocity = false;
			}

			if(Input.GetButton("Fire1")){
				// Attacks always stop the player for a little while
				shouldZeroVelocity = true;
			}

			// All abilities and attacks go here

			if(shouldZeroVelocity){
				ZeroVelocity();
			}
		} else {
			FallDownward();
		}

		applyVelocityWithPhysics();
	}

	//#################################################
	// Physics functions
	//#################################################

	void applyVelocityWithPhysics(){
		// Tie the velocity to the timestep making it framerate independent
		CorrectVelocity();
		Vector3 next_position = PositionAfterVelocity(velocity);

		// Handle collisions sideways
		RaycastHit wall_hit;
		if(Physics.Raycast(transform.position, velocity, out wall_hit, (velocity.magnitude * Time.deltaTime) + 1.0f)){

			Vector3 projected = wall_hit.normal.normalized * Vector3.Dot(velocity, wall_hit.normal);
			velocity -= projected;
			next_position = PositionAfterVelocity(velocity);
		}

		// If (+z or -z) and (+x or -x) gets a hit - corners!
		if((Physics.Raycast(next_position, Vector3.forward, 1.0f) || Physics.Raycast(next_position, Vector3.back, 1.0f)) && (Physics.Raycast(next_position, Vector3.right, 1.0f) || Physics.Raycast(next_position, Vector3.left, 1.0f))){
			next_position.z = transform.position.z;
			next_position.x = transform.position.x;
		}

		// Apply everything!
		transform.position = next_position;
		correctHeight();
	}

	void FallDownward(){
		// Fall downward in addition to any of the other current velocities
		velocity = new Vector3(velocity.x, velocity.y - 0.3f, velocity.z);

		UpdateIsGrounded();
	}

	//#################################################
	// Correction functions
	//#################################################

	void CorrectVelocity(){
		// Fixes a VERY rare bug (seen once) that causes you to fly upward very fast after a collision
		velocity = new Vector3(velocity.x, Mathf.Min(velocity.y, jump_velocity), velocity.z);
	}

	void correctHeight(){
		// Fixes slight clipping bug player has after falling
		RaycastHit hit;

		if(Physics.Raycast(transform.position, Vector3.down, out hit, height_offset + 0.3f) && velocity.y < 0){
			transform.position = new Vector3(
				transform.position.x,
				hit.point.y + height_offset,
				transform.position.z
			);
		}
	}

	//#################################################
	// Helper functions
	//#################################################

	Vector3 PositionAfterVelocity(Vector3 v){
		return transform.position + (v * Time.deltaTime);
	}

	Vector2 MouseVectorToWorld(){
		// Convert mouseposition into vector originating from 1/2 width, 1/2 height
		// With bounds -1.0 < x < 1.0 and -1.0 < y < 1.0
		Vector2 screenMovement = new Vector2(
			((Input.mousePosition.x / Screen.width) - 0.5f) / 0.5f,
			((Input.mousePosition.y / Screen.height) - 0.5f) / 0.5f
		);

		// Transform this vector into world coordinates using the rotation from the orthographic projection
		Vector3 worldMovement = new Vector2(
			(screenMovement.x * cosTheta) + (screenMovement.y * sinTheta),
			(screenMovement.x * -1.0f * sinTheta) + (screenMovement.y * cosTheta)
		);

		// You can also
		// return worldMovement;
		// or for toggling movement
		// return worldMovement.normalized;

		// Make it so the maximum (1.0f * speed) is reached at 0.5 rather than at 1.0
		Vector2 NormalizedMovement = new Vector2(
			Mathf.Max(Mathf.Min(worldMovement.x * 2, 1.0f), -1.0f),
			Mathf.Max(Mathf.Min(worldMovement.y * 2, 1.0f), -1.0f)
		);

		return NormalizedMovement;
	}

	void ZeroVelocity(){
		velocity = new Vector3(0.0f, 0.0f, 0.0f);
	}

	void UpdateIsGrounded(){
		// Detect if we're going to hit the ground next step
		is_grounded = Physics.Raycast(transform.position, Vector3.down, (velocity.y * -1.0f * Time.deltaTime) + height_offset);
	}

	//#################################################
	// Input Handlers
	//#################################################

	void HandleMouseInput(){
		// Get the top-down (2D) velocity vector
		Vector2 velocity2D = MouseVectorToWorld() * speed;

		// Predict the point next step
		Vector3 position_next = PositionAfterVelocity(new Vector3(velocity2D.x, 0.0f, velocity2D.y));

		// Cast a ray downward from where we're going to be
		RaycastHit ground_hit;
		if(Physics.Raycast(position_next, Vector3.down, out ground_hit, height_offset + 0.3f)){
			// We will be hitting the ground, therefore we should clamp to it
			is_grounded = true;

			float y_velocity = (ground_hit.point.y - transform.position.y + height_offset)/Time.deltaTime;

			Vector2 x_forward = new Vector2(velocity2D.x, 0.0f);
			Vector2 x_vector = new Vector2(velocity2D.x, y_velocity);

			Vector2 z_forward = new Vector2(velocity2D.y, 0.0f);
			Vector2 z_vector = new Vector2(velocity2D.y, y_velocity);

			float theta_x = Vector2.Angle(x_forward, x_vector);
			float theta_z = Vector2.Angle(z_forward, z_vector);

			float x_velocity = velocity2D.x * Mathf.Cos(Mathf.Deg2Rad * theta_x);
			float z_velocity = velocity2D.y * Mathf.Cos(Mathf.Deg2Rad * theta_z);

			velocity = new Vector3(
				x_velocity,
				y_velocity,
				z_velocity
			);

			// Rotate the guy to face the direction of travel
			Vector3 target = new Vector3(x_velocity, 0.0f, z_velocity);
			Vector3 zero = Vector3.zero;

			Vector3 direction = Vector3.SmoothDamp(
				transform.forward,
				target,
				ref zero,
				turn_speed
			);

			transform.LookAt(transform.position + direction);
		} else {
			is_grounded = false;
		}
	}

	void HandleSpacePressed(){
		// Jump upward!
		velocity = new Vector3(velocity.x, jump_velocity, velocity.z);

		UpdateIsGrounded();
	}

}
