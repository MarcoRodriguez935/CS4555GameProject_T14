using System;
using Unity.Behavior;
using Unity.VisualScripting;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "SoundCaptured", story: "[Enemy] [hears] a [sound]", category: "Conditions", id: "aaedc840c72a85e6ba5c5f128671a2d1")]
public partial class SoundCapturedCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Enemy;
    [SerializeReference] public BlackboardVariable<UnityOnCollisionEnter2DMessageListener> Hears;
    [SerializeReference] public BlackboardVariable<Vector3> Sound;

    public override bool IsTrue()
    {
        return true;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
