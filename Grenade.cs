using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grenade : MonoBehaviour {

    public ParticleSystem explosion;
    bool exploded;

    private void Start()
    {
        exploded = false;
    }

    //발사
    public void Launch(Vector3 Target)
    {
        Rigidbody LaunchingObject = this.GetComponent<Rigidbody>();
        Physics.gravity = Vector3.up * KinematicMovement.gravity;
        LaunchingObject.useGravity = true;
        LaunchingObject.velocity = KinematicMovement.CalculateLaunchData(LaunchingObject.transform.position, Target).initialVelocity;
    }

    //다른 오브젝트에 닿았을 때 실행
    public void GrenadeOnImpact()
    {
        GameManager.GetInstance().EffectInArea(this.transform.position);

        ParticleSystem effect = Instantiate(explosion, this.transform.position, this.transform.rotation);
        effect.Play();
        Destroy(effect, explosion.main.duration);

        Destroy(this.gameObject);

        exploded = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!exploded)
        {
            if (other.tag == "Obstacle" || other.tag == "Tile")
            {
                GrenadeOnImpact();
            }
        }
    }
}
