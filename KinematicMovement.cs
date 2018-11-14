using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KinematicMovement : MonoBehaviour {
    
    public static float gravity = -18;
    
    //발사 경로 계산
    public static LaunchData CalculateLaunchData(Vector3 LaunchingObject, Vector3 Target)
    {
        float h = Target.y - LaunchingObject.y + (Vector3.Distance(LaunchingObject, Target) / 2f);

        float displacementY = Target.y - LaunchingObject.y;
        Vector3 displacementXZ = new Vector3(Target.x - LaunchingObject.x, 0, Target.z - LaunchingObject.z);
        float time = Mathf.Sqrt(-2 * h / gravity) + Mathf.Sqrt(2 * (displacementY - h) / gravity);
        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * h);
        Vector3 velocityXZ = displacementXZ / time;

        return new LaunchData(velocityXZ + velocityY * -Mathf.Sign(gravity), time);
    }

    //발사 경로 정보
    public struct LaunchData
    {
        public readonly Vector3 initialVelocity;
        public readonly float timeToTarget;

        public LaunchData(Vector3 initialVelocity, float timeToTarget)
        {
            this.initialVelocity = initialVelocity;
            this.timeToTarget = timeToTarget;
        }

    }
}
