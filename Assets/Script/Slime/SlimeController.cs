using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SlimeController : MonoBehaviour
{
    [System.Serializable]
    public struct slot
    {
        public string Name;
        public List<GameObject> Obj;
    }

    public GameObject BoneRoot;
    public Map<GameObject, Quaternion> DefaultRotation;

    public Predictor predictor;//===================== 코위치 이동 방향 * 강도으로

    [NonReorderable]
    public List<slot> GroupObject = new();

    public float SwayArea = 0.75f;
    //충돌이 없을때 => Cos (Dot(움직일 방향, 본의 방향) * 0.5f) * 출렁거림 강도
    //충돌이 있을때 = > 움직일 방향과 인접한 본은 짧아지고 주변의 본이 벌어짐 
    //     바닥만 고정해서 할꺼라 / 일정각도 이내면 그냥 짧아지고 , Cos (Dot(움직일 방향, 본의 방향) * 확잔 범위[기본 : 0.5]) 으로 벌어짐
    //           확산범위 : 0 -> 전부 같이움직임 / 0.5 -> 180'에서 안움직임 / 1 -> 90'에서 안움직임

    public float ExpandRate = 1f;
    public float NormalDownPress = 0.75f;

    public float DefaultPress = 0.75f;

    public float BouncePower = 0.5f;//
    public float Speed = 10f;
    public float Sensitive = 5f;

    [Header(@"State")]
    public Vector3 ForceDirection = Vector3.forward;
    public float ForcePower = 1;
    public float StateRate = 0;
    public bool Reverse = false;

    public Vector2 PreNosePos = Vector2.zero;
    public Vector2 MoveOffset = Vector2.zero;

    void Start()
    {
        ApplyPress_Button(DefaultPress);
        PreNosePos = predictor.results[0].Position;
    }

    // Update is called once per frame
    void Update()
    {
        //predictor.results[0].Position
        MoveOffset = predictor.results[0].Position - PreNosePos;
        PreNosePos = predictor.results[0].Position;

        var offsetLength = MoveOffset.magnitude;
        if (offsetLength > 0.001f)
        {
            ForceDirection = MoveOffset.normalized;
            ForcePower += offsetLength * Sensitive;
        }

        if (ForcePower < 0.01f)
        {
            ForcePower = 0;
            StateRate = 0;
        }
        else
        {
            if (Reverse)
            {
                StateRate -= Time.deltaTime * Speed * ForcePower;
            }
            else
            {
                StateRate += Time.deltaTime * Speed * ForcePower;
            }

            if (Reverse && StateRate < 0.01f)
            {
                Reverse = false;
                ForceDirection *= -1;
                ForcePower *= BouncePower;
            }

            if (ForcePower > StateRate)
            {
                ApplyForce(ForceDirection * StateRate);
            }
            else
            {
                Reverse = true;
            }
        }
    }
    public void Reset()
    {
        for (int i = 0; i < BoneRoot.transform.childCount; i++)
        {
            BoneRoot.transform.GetChild(i).localPosition = Vector3.zero;
            BoneRoot.transform.GetChild(i).localScale = Vector3.one;
        }
        ResetRotation();

        gameObject.transform.localScale = Vector3.one;
    }
    public void ResetRotation()
    {
        for (int i = 0; i < DefaultRotation.Count; i++)
        {
            DefaultRotation.GetKey(i).transform.localRotation = DefaultRotation.GetVaule(i);
        }
    }
    //ApplyForce , ApplyPress_Button 를 자동으로 조합해서 설정
    public void ApplyForceWithPlane(Vector3 Force)
    {
        //ApplyForce >> ApplyPress_Button >> ApplyForce

        //처음에 ApplyForce하고 Down + 45' 본들의 자식이 평면보다 A만큼 떨어져야함

    }//=============== 그냥 하지 말자 (Later)

    public void SetDefaultRotation()
    {
        for (int i = 0; i < BoneRoot.transform.childCount; i++)
        {
            //BoneRoot.transform.GetChild(i)
            int Lindex = DefaultRotation.GetKey().FindIndex(t => t.Equals(BoneRoot.transform.GetChild(i)));
            if (Lindex >= 0)
            {
                DefaultRotation.SetVaule(Lindex, BoneRoot.transform.GetChild(i).localRotation);
            }
            else
            {
                DefaultRotation.Add(BoneRoot.transform.GetChild(i).gameObject, BoneRoot.transform.GetChild(i).localRotation);
            }
        }
    }
    public void SetRotation(int index, Quaternion rot)
    {
        for (int i = 0; i < GroupObject[index].Obj.Count; i++)
        {
            Quaternion origin = DefaultRotation.Get().Find(t => t.Key.Equals(GroupObject[index].Obj[i])).Vaule;

            GroupObject[index].Obj[i].transform.localRotation = origin * rot;
        }
    }
    [System.Obsolete()]
    public void AddRotation(int index, Quaternion rot)
    {
        for (int i = 0; i < GroupObject[index].Obj.Count; i++)
        {
            //GroupObject[index].Obj[i].transform.localRotation ;
            GroupObject[index].Obj[i].transform.localRotation *= rot;
        }
    }
    public void SetPosition(int index, Vector3 pos)
    {
        for (int i = 0; i < GroupObject[index].Obj.Count; i++)
        {
            GroupObject[index].Obj[i].transform.position = pos;
        }
    }
    public void SetBoneLength(int index, float Length)
    {
        for (int i = 0; i < GroupObject[index].Obj.Count; i++)
        {
            GroupObject[index].Obj[i].transform.localScale = new Vector3(1, Length, 1);
        }
    }


    public void ApplyForce(Vector3 Force)
    {
        //int index = 2;
        {
            //float dot = Vector3.Dot(Force.normalized, BoneRoot.transform.GetChild(index).rotation * Vector3.forward);
            //Debug.Log("Dot 1 : " + Mathf.Acos(dot) * Mathf.Rad2Deg);

            //Debug.Log(BoneRoot.transform.GetChild(0) + " : " + dot + "\n bone : " + (BoneRoot.transform.GetChild(0).rotation * Vector3.forward));

            //Debug.Log(Quaternion.Dot(Quaternion.LookRotation(Force), BoneRoot.transform.GetChild(12).rotation));

            //Debug.DrawLine(gameObject.transform.position, gameObject.transform.position + (Quaternion.LookRotation(Force) * Vector3.forward * 2), Color.green, 1);
            //Debug.DrawLine(gameObject.transform.position, gameObject.transform.position + (BoneRoot.transform.GetChild(12).rotation * Vector3.forward * 2), Color.red, 1);
        }//NotWork Correct

        //Vector3 BoneWorldDirection = (BoneRoot.transform.GetChild(index).GetChild(0).position - BoneRoot.transform.GetChild(index).position).normalized;

        //Debug.DrawLine(BoneRoot.transform.GetChild(index).position, BoneRoot.transform.GetChild(index).GetChild(0).position + BoneWorldDirection * 2, Color.red, 1.5f);
        //Debug.DrawLine(BoneRoot.transform.GetChild(index).position, BoneRoot.transform.GetChild(index).position + (Quaternion.LookRotation(Force.normalized) * Vector3.forward * 2), Color.green, 1.5f);

        //Debug.Log("Dot : " + Mathf.Acos(Vector3.Dot(Force.normalized, BoneWorldDirection)) * Mathf.Rad2Deg + " \n " + Force.normalized + " To " + BoneWorldDirection);

        for (int i = 0; i < BoneRoot.transform.childCount; i++)
        {
            Vector3 worldDirection = (BoneRoot.transform.GetChild(i).GetChild(0).position - BoneRoot.transform.GetChild(i).position).normalized;
            float rate = Mathf.Cos(Mathf.Acos(Vector3.Dot(Force.normalized, worldDirection)) * SwayArea);

            if (float.IsNaN(rate))
            {
                if (Vector3.Dot(Force.normalized, worldDirection) > 0)
                {
                    rate = 0;
                }else
                {
                    rate = Mathf.Cos(180 * SwayArea);
                }
            }

            BoneRoot.transform.GetChild(i).localPosition = Force * Mathf.Clamp01(rate);
        }
    }//<================= 
    public void ApplyPress_Button(float Distance)
    {
        float angle = Mathf.Acos(Mathf.Clamp01(Distance)) * Mathf.Rad2Deg;

        Vector3 scale = GroupObject[0].Obj[0].transform.localScale;
        scale.y = Mathf.Clamp(Distance, 0.01f, 1f);//===== ApplyForce() 에서 자손으로 방향을 구해야함 / 0 이면 위치가 (0,0,0) 으로
        GroupObject[0].Obj[0].transform.localScale = scale;

        SetRotation(1, Quaternion.Euler(0, 0, angle * 0.5f));
        SetRotation(2, Quaternion.Euler(0, 0, angle * 0.75f * 0.5f));
        SetRotation(3, Quaternion.Euler(0, 0, angle * 0.25f * 0.5f));

        //Debug.Log("Add Bone Length : " + (1 - Mathf.Clamp01(Mathf.Cos(angle * 2 * Mathf.Deg2Rad))));//추가 본 길이
        //float AddBoneLength = (1 - Distance);
        //SetBoneLength(1, 1 + AddBoneLength * ExpandRate);
        //SetBoneLength(2, 1 + AddBoneLength * 0.75f * ExpandRate);
        //SetBoneLength(3, 1 + AddBoneLength * 0.5f * ExpandRate);

        gameObject.transform.localScale = new Vector3(1 + ((1 - Distance) * ExpandRate), 1, 1 + ((1 - Distance) * ExpandRate));
    }//<========= 제대로 못쓰겠음 (0 , -1 , 2) 일때 Distance : 0.6
}

#if UNITY_EDITOR
[CustomEditor(typeof(SlimeController))]
public class SlimeControllerEditor : Editor
{
    SlimeController onwer;
    int index;
    Quaternion rot;

    float PressDis = 1;

    private void OnEnable()
    {
        onwer = target as SlimeController;
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        //EditorGUILayout.HelpBox("Unity Editor Error , List Element 0 Height is 0 \n Can't Fix It Fuuuck!", MessageType.Info);
        //But NonReorderable is Work

        if (GUILayout.Button("Reset"))
        {
            onwer.Reset();
        }
        if (GUILayout.Button("Set DefaultRotation"))
        {
            onwer.SetDefaultRotation();
        }

        {
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Force Test"))
            {
                onwer.ApplyForce(onwer.ForceDirection.normalized * onwer.ForcePower);
            }
            //if (GUILayout.Button("Force Test With Plane"))
            {
            //    onwer.ApplyForceWithPlane(ForceDir * ForceDis);
            }
        }

        EditorGUILayout.Space(10);
        PressDis = EditorGUILayout.FloatField("Press Distance", PressDis);
        if (GUILayout.Button("Press Down Test"))
        {
            onwer.ApplyPress_Button(PressDis);
        }

        {
            EditorGUILayout.Space(10);
            index = EditorGUILayout.IntField("Test Index", index);
            rot.eulerAngles = EditorGUILayout.Vector3Field("Test Angle", rot.eulerAngles);

            if (GUILayout.Button("Test Rotate"))
            {
                onwer.SetRotation(index, rot);
            }
        }//Test Rot
    }
}
#endif
