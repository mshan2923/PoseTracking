using System.Collections.Generic;
using UnityEngine;

public class BoneSphere : MonoBehaviour
{
    
    [Header("Bones")]
    public GameObject root = null;
    public GameObject x = null;
    public GameObject x2 = null;
    public GameObject y = null;
    public GameObject y2 = null;
    public GameObject Buttom = null;
    public GameObject Top = null;

    [Header("Spring Joint Settings"), Tooltip("Strength of spring")]
    public float Spring = 100f;
    [Tooltip("Higher the value the faster the spring oscillation stops")]
    public float Damper = 0.2f;
    public float PassVaule = 0.1f;

    [Header("Other Settings")]
    public float RootColliderSize = 0.005f;
    public float RootMass = 10f;
    public bool PositionLock = false;
    public Softbody.ColliderShape Shape = Softbody.ColliderShape.Box;
    public float ColliderSize = 0.002f;
    public float RigidbodyMass = 1f;
    public LineRenderer PrefabLine = null;
    public bool ViewLines = true;

    private void OnEnable()
    {
        Softbody.Init(Shape, ColliderSize, RigidbodyMass, Spring, Damper, RigidbodyConstraints.FreezeRotation, PrefabLine, ViewLines);

        Softbody.AddCollider(ref root, Softbody.ColliderShape.Sphere, RootColliderSize, RootMass);
        Softbody.AddCollider(ref x);
        Softbody.AddCollider(ref x2);
        Softbody.AddCollider(ref y);
        Softbody.AddCollider(ref y2);

        if (PositionLock)
        {
            Softbody.AddCollider(ref Buttom, Softbody.ColliderShape.Sphere, ColliderSize, 10000);
        }
        else
        {
            Softbody.AddCollider(ref Buttom);
        }
        Softbody.AddCollider(ref Top);

        Softbody.AddSpring(ref x, ref root, Spring, Damper, PassVaule);
        Softbody.AddSpring(ref x2, ref root, Spring, Damper, PassVaule);
        Softbody.AddSpring(ref y, ref root, Spring, Damper, PassVaule);
        Softbody.AddSpring(ref y2, ref root, Spring, Damper, PassVaule);
        Softbody.AddSpring(ref Buttom, ref root, Spring, Damper, PassVaule);
        Softbody.AddSpring(ref Top, ref root, Spring, Damper, PassVaule);
    }
    private void Start()
    {

    }
}
