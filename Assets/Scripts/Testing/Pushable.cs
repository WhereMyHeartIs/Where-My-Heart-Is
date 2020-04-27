using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pushable : MonoBehaviour
{
    public float pushDistance;
    public float verticalBreakpoint;
    public float verticalOffset;
    public float pushSpeed;
    public bool debug;

    private Vector3 spawn;

    private void Start()
    {
        spawn = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 directionToPlayer = Player.Instance.transform.position - transform.position;

        //Debug

        if(directionToPlayer.magnitude < pushDistance && 
            Mathf.Abs(transform.position.y + verticalOffset - Player.Instance.transform.position.y) < verticalBreakpoint)
        {
            GetComponent<Rigidbody>().velocity = GetComponent<Rigidbody>().velocity.y * Vector3.up + 
                (-directionToPlayer.normalized + (directionToPlayer.normalized.y * Vector3.up)) * pushSpeed;
        }
        else
        {
            GetComponent<Rigidbody>().velocity = GetComponent<Rigidbody>().velocity.y * Vector3.up;
        }

        if(transform.position.y < Player.Instance.deathPlane.position.y)
        {
            transform.position = spawn;
            GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
    }
}
