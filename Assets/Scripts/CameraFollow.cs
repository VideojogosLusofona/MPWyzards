using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    Wyzard localPlayer;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (localPlayer == null)
        {
            localPlayer = Wyzard.GetLocalPlayer();
            if ((localPlayer == null) || (localPlayer.isDead))
            {
                localPlayer = Wyzard.GetFirstPlayer();
            }
        }

        if (localPlayer != null)
        {
            if (localPlayer.isDead)
            {
                // Follow another player
                localPlayer = Wyzard.GetFirstPlayer();
            }
            if (localPlayer != null)
            {
                var target = localPlayer.transform.position;
                target.z = transform.position.z;

                transform.position = transform.position + (target - transform.position) * 0.05f;
            }
        }
    }
}
