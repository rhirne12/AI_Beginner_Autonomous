using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class Bot : MonoBehaviour
{
    NavMeshAgent agent;
    public GameObject target;
    Drive ds;
    bool coolDown = false;

    Vector3 wanderTarget = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        agent = this.GetComponent<NavMeshAgent>();
        ds = target.GetComponent<Drive>();
    }

    void Seek(Vector3 location)
    {
        agent.SetDestination(location);
    }

    void Flee(Vector3 location) 
    { 
        Vector3 fleeVector = location - this.transform.position;
        agent.SetDestination(this.transform.position - fleeVector);
    }

    void Pursue()
    {
        Vector3 targetDir = target.transform.position - this.transform.position;
        
        float relativeHeading = Vector3.Angle(this.transform.forward, this.transform.TransformVector(target.transform.forward));
        float toTarget = Vector3.Angle(this.transform.forward, this.transform.TransformVector(targetDir));


        if ((toTarget > 90 && relativeHeading < 20) || ds.currentSpeed < 0.01f)
        {
            Seek(target.transform.position);
            return;
        }

        float lookAhead = targetDir.magnitude / (agent.speed + ds.currentSpeed);
        Seek(target.transform.position + target.transform.forward * lookAhead);
    }

    void Evade()
    {
        Vector3 targetDir = target.transform.position - this.transform.position;

        float lookAhead = targetDir.magnitude / (agent.speed + ds.currentSpeed);
        Flee(target.transform.position + target.transform.forward * lookAhead);
    }

    void Wander()
    {
        float wanderRadius = 10;     // Size of the imaginary circle in front of navmeshagent
        float wanderDistance = 10;   // Distance from Agent to the middle of the imaginary circle in front of him
        float wanderJitter = 1;      // How the target location moves around from update to update

        wanderTarget += new Vector3(Random.Range(-1.0f, 1.0f) * wanderJitter, 0, Random.Range(-1.0f, 1.0f) * wanderJitter);  // Target is in the direction of the imaginary circle, but not ON the circle

        wanderTarget.Normalize();         // Brings it down to a length of 1
        wanderTarget *= wanderRadius;     // pushes it out to the correct length

        // Circle is now AROUND the agent, with a target on the circle.  Agent is at local position of 0,0,0
        // Need to create a vector3 postion in front of agent

        Vector3 targetLocal = wanderTarget + new Vector3(0, 0, wanderDistance);                     //  Moves local target out
        Vector3 targetWorld = this.gameObject.transform.InverseTransformVector(targetLocal);       //  changes local target to a world target that can be seeked  

        Seek(targetWorld);

    }

    void Hide()
    {
        // Find Closest hiding spot
        float dist = Mathf.Infinity;
        Vector3 chosenSpot = Vector3.zero;

        for (int i=0; i< World.Instance.GetHidingSpots().Length; i++)
        {
            Vector3 hideDir = World.Instance.GetHidingSpots()[i].transform.position - target.transform.position;  // Vector from the Cop to the Tree hiding spot
            Vector3 hidePos = World.Instance.GetHidingSpots()[i].transform.position + hideDir.normalized * 10;  // Vector to hiding spot behind tree hiding spot

            if (Vector3.Distance(this.transform.position, hidePos) < dist)
            {
                chosenSpot = hidePos;
                dist = Vector3.Distance(this.transform.position, hidePos);
            }
        }

        Seek(chosenSpot);
    }

    void CleverHide()
    {
        // Find Closest hiding spot
        float dist = Mathf.Infinity;
        Vector3 chosenSpot = Vector3.zero;
        Vector3 chosenDir = Vector3.zero;
        GameObject chosenGO = World.Instance.GetHidingSpots()[0];

        for (int i = 0; i < World.Instance.GetHidingSpots().Length; i++)
        {
            Vector3 hideDir = World.Instance.GetHidingSpots()[i].transform.position - target.transform.position;  // Vector from the Cop to the Tree hiding spot
            Vector3 hidePos = World.Instance.GetHidingSpots()[i].transform.position + hideDir.normalized * 10;  // Vector to hiding spot behind tree hiding spot

            if (Vector3.Distance(this.transform.position, hidePos) < dist)
            {
                chosenSpot = hidePos;
                chosenDir = hideDir;
                chosenGO = World.Instance.GetHidingSpots()[i];
                dist = Vector3.Distance(this.transform.position, hidePos);
            }
        }

        // Raycast to a spot behind the collider
        Collider hidCol = chosenGO.GetComponent<Collider>();
        Ray backRay = new Ray(chosenSpot, -chosenDir.normalized);  // casts ray back to the collider from the other side
        RaycastHit info;
        float distance = 100.0f;
        hidCol.Raycast(backRay, out info, distance);  // Hit point at the BACK of the collider stored inside info

        Seek(info.point + chosenDir.normalized * 3);
    }

    bool CanSeeTarget()
    {
        RaycastHit raycastInfo;
        Vector3 rayToTarget = target.transform.position - this.transform.position;
        float angle = Vector3.Angle(this.transform.forward, rayToTarget);

        if (angle < 60 && Physics.Raycast(this.transform.position, rayToTarget, out raycastInfo))
        { 
            if (raycastInfo.transform.gameObject.tag == "cop")
                return true;
        }
        return false;
    }

    bool CanSeeMe()
    {
        Vector3 rayToTarget = this.transform.position - target.transform.position;
        float angle = Vector3.Angle(target.transform.forward, rayToTarget);
        float distance = rayToTarget.magnitude;

        if (angle < 60)
            return true;
        return false;

    }

    void BehaviorCooldown()
    {
        coolDown = false;
    }

    bool TargetInRange()
    {
        if (Vector3.Distance(this.transform.position, target.transform.position) < 10)
            return true;
        return false;
    }

    // Update is called once per frame
    void Update()
    {
        // Flee(target.transform.position);
        if (!coolDown)
        {
            if (!TargetInRange())
            {
                Wander();
            }
            else if (CanSeeTarget() && CanSeeMe())
            {
                CleverHide();
                coolDown = true;
                Invoke("BehaviorCooldown", 5);
            }
            else
                Pursue();
        }
        Wander();
    }
}
