using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour {

	public GameObject player;
	public float speed;
	public float turn_speed;

	void FixedUpdate () {

		Vector3 target = new Vector3(player.transform.position.x, 0.0f, player.transform.position.z);
		Vector3 zero = Vector3.zero;

		Vector3 direction = Vector3.SmoothDamp(
			transform.forward,
			target,
			ref zero,
			turn_speed
		);

		transform.LookAt(transform.position + direction);
		transform.position += new Vector3(0.0f, 0.0f, speed * Time.deltaTime);
	}
}
