using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private Animator anim;
    public string checkpointId;
    public bool activationStatus;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    [ContextMenu("Generate checkpoint id")]
    private void GenerateId()
    {
        checkpointId = System.Guid.NewGuid().ToString();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Player>() != null)
        {
            ActivateCheckpoint();
        }
    }

    public void ActivateCheckpoint()
    {
        activationStatus = true;
        anim.SetBool("active", true);
    }
}
