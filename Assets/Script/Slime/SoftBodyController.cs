using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SoftBodyController : MonoBehaviour
{
    public Predictor Predictor;
    public BoneSphere softbody;

    public GameObject RootBone;
    public List<GameObject> Bones = new();
    public List<Rigidbody> RigidBones = new();

    public float SwayArea = 0.5f;
    //           확산범위 : 0 -> 전부 같이움직임 / 0.5 -> 180'에서 안움직임 / 1 -> 90'에서 안움직임
    public float Sensitive = 1;
    public float DecelerationRate = 0.5f;

    public float RandomForce = 1f;
    public float RandomForcePower = 0.1f;

    [Header("State")]
    public Vector2 PreNosePos = Vector2.zero;
    public Vector2 MoveOffset = Vector2.zero;
    public Vector2 Movement = Vector2.zero;

    [Header("Debug")]
    public Vector3 ForceDir = Vector3.forward;
    public float ForcePower = 0.5f;

    void Start()
    {
        for (int i = 0; i < Bones.Count; i++)
        {
            RigidBones.Add(Bones[i].GetComponent<Rigidbody>());
        }

        //StartCoroutine(Loop());
    }

    // Update is called once per frame
    void Update()
    {
        MoveOffset = ((Predictor.results[0].Position - (Vector2.one * 0.5f)) * 2).normalized - PreNosePos;
        PreNosePos = ((Predictor.results[0].Position - (Vector2.one * 0.5f)) * 2).normalized;
        Movement = MoveOffset * Sensitive;
        float dis = Movement.magnitude;

        {
            /*
if (dis < Sensitive)
{
    AddForce(Movement.magnitude * Time.deltaTime * MoveOffset.normalized);

    Movement -= (DecelerationRate) * Time.deltaTime * Movement;
    for (int i = 0; i < RigidBones.Count; i++)
    {
        if (RigidBones[i].velocity != null)
        {
            RigidBones[i].velocity -= DecelerationRate * Time.deltaTime * RigidBones[i].velocity;
        }
        else
        {
            RigidBones[i].velocity = Vector3.zero;
        }
    }
}
else
{
    Movement = Vector2.zero;
    for (int i = 0; i < RigidBones.Count; i++)
    {
        RigidBones[i].velocity = Vector3.zero;
    }
    RootBone.transform.localPosition = Vector3.zero;
}*/
        }

        //RootBone.GetComponent<Rigidbody>().AddForce(Movement.x, Movement.y, 0, ForceMode.Impulse);//Work wall
        if (dis < Sensitive)
        {
            for (int i = 0; i < RigidBones.Count; i++)
            {
                RigidBones[i].AddForce(new Vector3(Movement.x, Movement.y, 0) * Time.deltaTime, ForceMode.Impulse);
            }
            RootBone.transform.localPosition = Vector3.zero;
            //Movement -= (DecelerationRate) * Time.deltaTime * Movement;
        }
    }
    private void LateUpdate()
    {

    }
    IEnumerator Loop()
    {
        while(true)
        {
            //RandomForcePower
            //AddForce(new Vector3(Random.value, Random.value).normalized * RandomForcePower);

            Movement = new Vector3(Random.value, Random.value).normalized * RandomForcePower;

            yield return new WaitForSeconds(Random.Range(0.1f, RandomForce));
        }
    }

    public void AddForce(Vector3 force)
    {
        
        for (int i = 0; i < Bones.Count; i++)
        {
            Vector3 dir = (Bones[i].transform.position - RootBone.transform.position).normalized;
            float rate = Mathf.Cos(Mathf.Acos(Vector3.Dot(force.normalized, dir)) * SwayArea);

            if (softbody.PositionLock && softbody.Buttom.Equals(Bones[i]))
            {
                
            }else
            {
                Bones[i].transform.position += force * rate;
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SoftBodyController))]
public class SoftBodyControllerEditor : Editor
{
    SoftBodyController onwer;
    private void OnEnable()
    {
        onwer = target as SoftBodyController;
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();


        if (GUILayout.Button("Test Add Force"))
        {
            onwer.AddForce(onwer.ForceDir.normalized * onwer.ForcePower);
        }
    }
}
#endif
