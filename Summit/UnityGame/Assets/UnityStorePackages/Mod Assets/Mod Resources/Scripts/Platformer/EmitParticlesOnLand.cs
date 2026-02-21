using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

[RequireComponent(typeof(ParticleSystem))]
public class EmitParticlesOnLand : MonoBehaviour
{

    public bool emitOnLand = true;

#if UNITY_TEMPLATE_PLATFORMER

    ParticleSystem p;

    void Start()
    {
        p = GetComponent<ParticleSystem>();

        if (emitOnLand) {
            Platformer.Gameplay.PlayerLanded.OnExecute += PlayerLanded_OnExecute;
            void PlayerLanded_OnExecute(Platformer.Gameplay.PlayerLanded obj) {
                p.Play();
            }
        }


    }

#endif

}
