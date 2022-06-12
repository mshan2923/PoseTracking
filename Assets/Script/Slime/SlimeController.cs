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

    public Predictor predictor;//===================== ����ġ �̵� ���� * ��������

    [NonReorderable]
    public List<slot> GroupObject = new();

    public float SwayArea = 0.75f;
    //�浹�� ������ => Cos (Dot(������ ����, ���� ����) * 0.5f) * �ⷷ�Ÿ� ����
    //�浹�� ������ = > ������ ����� ������ ���� ª������ �ֺ��� ���� ������ 
    //     �ٴڸ� �����ؼ� �Ҳ��� / �������� �̳��� �׳� ª������ , Cos (Dot(������ ����, ���� ����) * Ȯ�� ����[�⺻ : 0.5]) ���� ������
    //           Ȯ����� : 0 -> ���� ���̿����� / 0.5 -> 180'���� �ȿ����� / 1 -> 90'���� �ȿ�����

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
    //ApplyForce , ApplyPress_Button �� �ڵ����� �����ؼ� ����
    public void ApplyForceWithPlane(Vector3 Force)
    {
        //ApplyForce >> ApplyPress_Button >> ApplyForce

        //ó���� ApplyForce�ϰ� Down + 45' ������ �ڽ��� ��麸�� A��ŭ ����������

    }//=============== �׳� ���� ���� (Later)

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
        scale.y = Mathf.Clamp(Distance, 0.01f, 1f);//===== ApplyForce() ���� �ڼ����� ������ ���ؾ��� / 0 �̸� ��ġ�� (0,0,0) ����
        GroupObject[0].Obj[0].transform.localScale = scale;

        SetRotation(1, Quaternion.Euler(0, 0, angle * 0.5f));
        SetRotation(2, Quaternion.Euler(0, 0, angle * 0.75f * 0.5f));
        SetRotation(3, Quaternion.Euler(0, 0, angle * 0.25f * 0.5f));

        //Debug.Log("Add Bone Length : " + (1 - Mathf.Clamp01(Mathf.Cos(angle * 2 * Mathf.Deg2Rad))));//�߰� �� ����
        //float AddBoneLength = (1 - Distance);
        //SetBoneLength(1, 1 + AddBoneLength * ExpandRate);
        //SetBoneLength(2, 1 + AddBoneLength * 0.75f * ExpandRate);
        //SetBoneLength(3, 1 + AddBoneLength * 0.5f * ExpandRate);

        gameObject.transform.localScale = new Vector3(1 + ((1 - Distance) * ExpandRate), 1, 1 + ((1 - Distance) * ExpandRate));
    }//<========= ����� �������� (0 , -1 , 2) �϶� Distance : 0.6
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
