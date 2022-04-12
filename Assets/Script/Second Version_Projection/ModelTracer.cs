using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TensorFlowLite;

[RequireComponent(typeof(Animator))]
public class ModelTracer : MonoBehaviour
{
    public enum BodyPart
    {
        Head, Neck, Shoulder,
        Left_UpperArm, Left_LowerArm, Left_Hand, Right_UpperArm, Right_LowerArm, Right_Hand,
        Hip, Left_Knee, Left_Foot, Right_Knee, Right_Foot
    }

    public Predicter predicter;
    public RectTransform CanvasRect;
    public RectTransform CameraView;

    Animator animator;
    //모델 >> Rig >> 노출할 추가 트랜스폼에서 체크필요 , 안하면 위치값 가져오지 못함

    public float Z_Position = 1f;
    public float Z_Multiply = 1;
    public float Z_Limit = 0.4f;

    public Vector3 Offset = new Vector3(0, -1.25f, 0);

    [Space(5)]
    public Map<BodyPart, float> PartDistance = new();
    public List<GameObject> DebugPoints = new();
    public GameObject DebugObj;

    [Space(10)]
    public float FaceWideRate = 0.8f;
    public float HeadRollMultiply = 6.5f;
    public Vector3 HeadRotationRate = Vector3.zero; //Range : -1 ~ 1 / Vaule : -90 ~ 90

    [Space(10)]
    public LineRenderer LeftLine;
    public float LineZ_Offser = 0;

    [Space(10)]
    public Map<BodyPart, Vector3> TransPos = new();
    public Map<BodyPart, Vector3> SmoothPos = new();

    public Vector3 ArmDefaultPosRate = Vector2.right;
    public AnimationCurve HandZ_Limit_Mutiply = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
    public float HandLimitAreaOffset = 0.1f;
    public float HandZ_Limit = 0.1f;

    bool SetLeftElbow = false;
    bool SetRightElbow = false;

    //public Vector3 LocalHand = Vector3.zero;

    void Start()
    {
        animator = GetComponent<Animator>();

        ModelDefaultPartDistance();

        {
            TransPos.Add(BodyPart.Hip, Vector3.zero);

            TransPos.Add(BodyPart.Left_LowerArm, Vector3.zero);
            TransPos.Add(BodyPart.Left_Hand, Vector3.zero);
            TransPos.Add(BodyPart.Right_LowerArm, Vector3.zero);
            TransPos.Add(BodyPart.Right_Hand, Vector3.zero);
        }

        {
            SmoothPos.Add(BodyPart.Hip, Vector3.zero);

            SmoothPos.Add(BodyPart.Left_LowerArm, Vector3.zero);
            SmoothPos.Add(BodyPart.Left_Hand, Vector3.zero);
            SmoothPos.Add(BodyPart.Right_LowerArm, Vector3.zero);
            SmoothPos.Add(BodyPart.Right_Hand, Vector3.zero);
        }
    }

    
    void Update()
    {

    }
    private void OnAnimatorIK(int layerIndex)
    {
        //Debug.Log("Left : " + animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position + " / Right : " + animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position);
        //모델 양쪽 어깨 월드 좌표

        //Debug.Log(RateToUIWorldPos(CanvasRect, predicter.Result(5)));//UI기준의 왼쪽어깨 좌표를 월드좌표로 변환된 값 (인식된 월드좌표)

        //Debug.Log("Model : " + (GetBonePos(HumanBodyBones.LeftUpperArm) - GetBonePos(HumanBodyBones.RightUpperArm)) +
        //    " \n UI : " + (RateToUIWorldPos(CanvasRect, predicter.Result(5)) - RateToUIWorldPos(CanvasRect, predicter.Result(6))));
        //Debug.Log("Model Pos : " + (GetBonePos(HumanBodyBones.LeftUpperArm)) + " / UI Pos : " + RateToUIWorldPos(CanvasRect, predicter.Result(5)));

        float UIrate = CanvasRect.position.z / (RateToUIWorldPos(CanvasRect, predicter.Result(5)) - RateToUIWorldPos(CanvasRect, predicter.Result(6))).magnitude;// 캔버스.z / 양 어깨 거리

        Z_Position = UIrate * (GetBonePos(HumanBodyBones.LeftUpperArm) - GetBonePos(HumanBodyBones.RightUpperArm)).magnitude * Z_Multiply;

        gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, Mathf.Max(Z_Position, Z_Limit));

        //보통 0.5 : 100 비율 , 예측결과를 비율에 맞춰 줄이고 >> 줄인값에 유사하게 본들을 배치

        var ResizedNeckPos = (ResizeToModel(5) + ResizeToModel(6)) * 0.5f;
        //Debug.Log("Resized Neck : " + ResizedNeckPos + " / RelativeResize LeftShoulder : " + (ResizeToModel(5) - ResizedNeckPos));

        gameObject.transform.position = ResizedNeckPos + Offset;//이건 아주 잘됨
        //================================================================================================================================================ Body Pitch

        {
            {
                Vector3 NosePos = RateToUIWorldPos(CanvasRect, predicter.Result(0));
                Vector3 ShoulderCenter = (RateToUIWorldPos(CanvasRect, predicter.Result(5)) + RateToUIWorldPos(CanvasRect, predicter.Result(6))) * 0.5f;
                float ShoulderLength = ((RateToUIWorldPos(CanvasRect, predicter.Result(5)) - RateToUIWorldPos(CanvasRect, predicter.Result(6))) * 0.5f).magnitude;

                //predicter.results 말고 RateToUIWorldPos의 좌표로 비율 구함
                HeadRotationRate.y = ((NosePos.x - ShoulderCenter.x) * FaceWideRate) / ShoulderLength * -1;

                //Debug.Log("Head Yaw / Offset : " + (NosePos.x - ShoulderCenter.x) * FaceWideRate + " / Shoulder Length : " + ShoulderLength);
            }// Head Yaw -> <보이는 월드위치에서> (코위치 - 어깨중심) / 어깨 길이 * 얼굴너비비율

            {
                if (predicter.results[3].confidence > predicter.threshold && predicter.results[4].confidence > predicter.threshold)//둘다 보일때
                {
                    HeadRotationRate.x = (predicter.results[3].y + predicter.results[4].y) * 0.5f - predicter.results[0].y;
                }
                else if (predicter.results[3].confidence > predicter.threshold)//왼쪽귀만 보일때
                {
                    HeadRotationRate.x = predicter.results[3].y - predicter.results[0].y;
                }
                else if (predicter.results[4].confidence > predicter.threshold)//오른쪽귀만 보일때
                {
                    HeadRotationRate.x = predicter.results[4].y - predicter.results[0].y;
                }
                else//둘다 안보일때 -> 정면
                {
                    HeadRotationRate.x = 0;
                }

                HeadRotationRate.x = (HeadRotationRate.x * HeadRollMultiply) / Mathf.Max(Z_Position, Z_Limit);
            }// Head Roll -> 양눈간의 높이차이 * 상수값 / 거리 

            {
                //이것도 RateToUIWorldPos로
                //양 눈간의 높이차이로 , 한눈이 가려지면 0

                if (predicter.results[1].confidence > predicter.threshold && predicter.results[2].confidence > predicter.threshold)
                {
                    float height = RateToUIWorldPos(CanvasRect, predicter.Result(1)).y - RateToUIWorldPos(CanvasRect, predicter.Result(2)).y;
                    float length = (RateToUIWorldPos(CanvasRect, predicter.Result(1)) - RateToUIWorldPos(CanvasRect, predicter.Result(2))).magnitude;

                    HeadRotationRate.z = (90 - Mathf.Acos(height / length) * Mathf.Rad2Deg) / 90;
                }
                else
                {
                    HeadRotationRate.z = 0;
                }

                // Angle = 90 - Acos (눈 높이차이 / 양 눈간의 길이)
            }// Head Pitch -> Angle = 90 - Acos (눈 높이차이 / 양 눈간의 길이)

            animator.SetBoneLocalRotation(HumanBodyBones.Neck, Quaternion.Euler(HeadRotationRate * -90));
        }//Head Rotation

        {
            // 7,8  , 9,10

            Vector3 Shoulder = GetBonePos(HumanBodyBones.LeftUpperArm);//ResizeToModel(5) + (Vector3.forward * Offset.z);
            var Lelbow = Arm_Local_2DTo3D(5, 7, 2);
            var Lhand = Arm_Local_2DTo3D(7, 9, 3);

            //Vector3 W_Elbow = Shoulder + Lelbow;
            SetTransPos(BodyPart.Left_LowerArm, Shoulder + Lelbow);
            //Vector3 W_Hand = Shoulder + Lelbow + Lhand;
            SetTransPos(BodyPart.Left_Hand, Shoulder + Lelbow + Lhand);

            //AllReturnPool();

            if (predicter.results[9].confidence < predicter.threshold)//왼쪽손 안보일때
            {
                //빠르게 움직이면 손을 인식못함 /

                if (predicter.results[7].confidence < predicter.threshold)
                {
                    //W_Hand = Shoulder + ArmDefaultPosRate * (PartDistance.GetVaule(2) + PartDistance.GetVaule(3));
                    SetTransPos(BodyPart.Left_Hand, Shoulder + ArmDefaultPosRate.normalized * (PartDistance.GetVaule(2) + PartDistance.GetVaule(3)));

                }//팔 전부 안보임
                else
                {
                    //W_Hand = Shoulder + Lelbow.normalized * (PartDistance.GetVaule(2) + PartDistance.GetVaule(3));
                    SetTransPos(BodyPart.Left_Hand, Shoulder + Lelbow.normalized * (PartDistance.GetVaule(2) + PartDistance.GetVaule(3)));// 이전값으로 덮어 쓰기

                }//손만 안보임

                SetLeftElbow = false;
            }
            else
            {
                /*
                float Xrate = GetTransPos(BodyPart.Left_Hand).x / (Shoulder.x + HandLimitAreaOffset);
                if (Mathf.Abs(GetTransPos(BodyPart.Left_Hand).z) < HandZ_Limit_Mutiply.Evaluate(Xrate) * (Mathf.Abs(Shoulder.z) + HandZ_Limit))
                {
                    Vector3 W_Hand = GetTransPos(BodyPart.Left_Hand);
                    W_Hand.z = Shoulder.z - (HandZ_Limit_Mutiply.Evaluate(Xrate) * HandZ_Limit);
                    W_Hand = (W_Hand - GetTransPos(BodyPart.Left_LowerArm)).normalized * PartDistance.GetVaule(3) + GetTransPos(BodyPart.Left_LowerArm);

                    SetTransPos(BodyPart.Left_Hand, W_Hand);

                }//Z Axis Limit
                */
                //Debug.Log("Rate : " + Xrate + " / Pos : " + GetTransPos(BodyPart.Left_Hand) + " / ");

                SetLeftElbow = true;
            }
        }//Left Hand

        {
            // 7,8  , 9,10

            Vector3 Shoulder = GetBonePos(HumanBodyBones.RightUpperArm);//ResizeToModel(5) + (Vector3.forward * Offset.z);
            var Lelbow = Arm_Local_2DTo3D(6, 8, 4);
            var Lhand = Arm_Local_2DTo3D(8, 10, 5);

            //Vector3 W_Elbow = Shoulder + Lelbow;
            SetTransPos(BodyPart.Right_LowerArm, Shoulder + Lelbow);
            //Vector3 W_Hand = Shoulder + Lelbow + Lhand;
            SetTransPos(BodyPart.Right_Hand, Shoulder + Lelbow + Lhand);

            //AllReturnPool();

            if (predicter.results[10].confidence < predicter.threshold)//왼쪽손 안보일때
            {
                //빠르게 움직이면 손을 인식못함 /

                if (predicter.results[8].confidence < predicter.threshold)
                {
                    //W_Hand = Shoulder + ArmDefaultPosRate * (PartDistance.GetVaule(2) + PartDistance.GetVaule(3));
                    SetTransPos(BodyPart.Right_Hand, Shoulder + Vector3.Scale(ArmDefaultPosRate.normalized, new Vector3(-1, 1 ,1)) * (PartDistance.GetVaule(4) + PartDistance.GetVaule(5)));

                }//팔 전부 안보임
                else
                {
                    //W_Hand = Shoulder + Lelbow.normalized * (PartDistance.GetVaule(2) + PartDistance.GetVaule(3));
                    SetTransPos(BodyPart.Right_Hand, Shoulder + Lelbow.normalized * (PartDistance.GetVaule(4) + PartDistance.GetVaule(5)));// 이전값으로 덮어 쓰기

                }//손만 안보임

                SetRightElbow = false;
            }
            else
            {
                /*
                float Xrate = GetTransPos(BodyPart.Right_Hand).x / (Shoulder.x - HandLimitAreaOffset);
                if (Mathf.Abs(GetTransPos(BodyPart.Right_Hand).z) < HandZ_Limit_Mutiply.Evaluate(Xrate) * (Mathf.Abs(Shoulder.z) + HandZ_Limit))
                {
                    Vector3 W_Hand = GetTransPos(BodyPart.Right_Hand);
                    W_Hand.z = Shoulder.z - (HandZ_Limit_Mutiply.Evaluate(Xrate) * HandZ_Limit);
                    W_Hand = (W_Hand - GetTransPos(BodyPart.Right_LowerArm)).normalized * PartDistance.GetVaule(5) + GetTransPos(BodyPart.Right_LowerArm);

                    SetTransPos(BodyPart.Right_Hand, W_Hand);
                }//Z Axis Limit
                */
                SetRightElbow = true;
            }
        }//Right Hand

        //손인식 완성하면 Smooth하게
        {
            {
                if (SetLeftElbow)
                {
                    //GetPool("Elbow Point").transform.position = W_Elbow;

                    animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 1);
                    animator.SetIKHintPosition(AvatarIKHint.LeftElbow, GetTransPos(BodyPart.Left_LowerArm));
                }
                //GetPool("Shoulder Point").transform.position = Shoulder;
                //GetPool("Hand Point").transform.position = W_Hand;

                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, GetTransPos(BodyPart.Left_Hand));
            }//Left

            {
                if (SetRightElbow)
                {
                    //GetPool("Elbow Point").transform.position = W_Elbow;

                    animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 1);
                    animator.SetIKHintPosition(AvatarIKHint.RightElbow, GetTransPos(BodyPart.Right_LowerArm));
                }
                //GetPool("Shoulder Point").transform.position = Shoulder;
                //GetPool("Hand Point").transform.position = W_Hand;

                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                animator.SetIKPosition(AvatarIKGoal.RightHand, GetTransPos(BodyPart.Right_Hand));
            }//Right
        }
    }

    public void ModelDefaultPartDistance()
    {
        //모델 머리 ~ 어깨 평균 : 0.1 / 머리 ~ UpperArm 평균 : 0.15

        var ShoulderAverage = (GetBonePos(HumanBodyBones.LeftShoulder) + GetBonePos(HumanBodyBones.RightShoulder)) * 0.5f;

        PartDistance.Add(BodyPart.Head, (GetBonePos(HumanBodyBones.Head) - ShoulderAverage).magnitude );
        PartDistance.Add(BodyPart.Shoulder, (GetBonePos(HumanBodyBones.LeftUpperArm) - GetBonePos(HumanBodyBones.RightUpperArm)).magnitude);

        PartDistance.Add(BodyPart.Left_UpperArm, (GetBonePos(HumanBodyBones.LeftUpperArm) - GetBonePos(HumanBodyBones.LeftLowerArm)).magnitude);
        PartDistance.Add(BodyPart.Left_LowerArm, (GetBonePos(HumanBodyBones.LeftLowerArm) - GetBonePos(HumanBodyBones.LeftHand)).magnitude);

        PartDistance.Add(BodyPart.Right_UpperArm, (GetBonePos(HumanBodyBones.RightUpperArm) - GetBonePos(HumanBodyBones.RightLowerArm)).magnitude);
        PartDistance.Add(BodyPart.Right_LowerArm, (GetBonePos(HumanBodyBones.RightLowerArm) - GetBonePos(HumanBodyBones.RightHand)).magnitude);

        PartDistance.Add(BodyPart.Hip, (GetBonePos(HumanBodyBones.LeftUpperLeg) - GetBonePos(HumanBodyBones.RightUpperLeg)).magnitude);//=====

        PartDistance.Add(BodyPart.Left_Knee, (GetBonePos(HumanBodyBones.LeftUpperLeg) - GetBonePos(HumanBodyBones.LeftLowerLeg)).magnitude);
        PartDistance.Add(BodyPart.Left_LowerArm, (GetBonePos(HumanBodyBones.LeftLowerLeg) - GetBonePos(HumanBodyBones.LeftFoot)).magnitude);

        PartDistance.Add(BodyPart.Right_Knee, (GetBonePos(HumanBodyBones.RightUpperLeg) - GetBonePos(HumanBodyBones.RightLowerLeg)).magnitude);
        PartDistance.Add(BodyPart.Right_LowerArm, (GetBonePos(HumanBodyBones.RightLowerLeg) - GetBonePos(HumanBodyBones.RightFoot)).magnitude);
    }
    public GameObject GetPool(string Lname = "")
    {
        for (int i = 0; i < DebugPoints.Count; i++)
        {
            if (! DebugPoints[i].activeSelf)
            {
                DebugPoints[i].SetActive(true);

                if (!string.IsNullOrEmpty(Lname))
                {
                    DebugPoints[i].name = Lname;
                }

                return DebugPoints[i];
            }
        }

        var obj = GameObject.Instantiate(DebugObj);
        DebugPoints.Add(obj);
        obj.transform.SetParent(gameObject.transform);

        if (! string.IsNullOrEmpty(Lname))
        {
            obj.name = Lname;
        }

        return obj;
    }
    public void AllReturnPool()
    {
        for (int i = 0; i < DebugPoints.Count; i++)
        {
            DebugPoints[i].SetActive(false);
        }
    }


    public Vector3 GetTransPos(BodyPart part)
    {
        return TransPos.Get().Find(t => t.Key == part).Vaule;
    }
    public bool SetTransPos(BodyPart part, Vector3 pos)
    {
        int index = TransPos.Get().FindIndex(t => t.Key == part);

        TransPos.SetVaule(index, pos);
        return index >= 0;
    }
    public Vector3 GetSmoothPos(BodyPart part)
    {
        return SmoothPos.Get().Find(t => t.Key == part).Vaule;
    }
    public bool SetSmoothPos(BodyPart part, Vector3 pos)
    {
        int index = SmoothPos.Get().FindIndex(t => t.Key == part);

        SmoothPos.SetVaule(index, pos);
        return index >= 0;
    }


    public Vector2 RateToScreenPos(RectTransform canvasRect, Vector2 rate)
    {
        //return (pointerSize * 0.5f + ((Canvas.sizeDelta.y * Vector2.one) - pointerSize) * rate) * new Vector2(1, -1);//피벗 : 좌상단
        return (rate - Vector2.one * 0.5f) * canvasRect.sizeDelta.y * new Vector2(canvasRect.lossyScale.x, canvasRect.lossyScale.y * -1);
    }
    public Vector3 RateToUIWorldPos(RectTransform canvasRect, Vector2 rate)
    {
        Vector3 pos = RateToScreenPos(canvasRect, rate);
        return pos + (canvasRect.position.z * Vector3.forward);
    }//UI기준
    public Vector3 GetBonePos(HumanBodyBones bone)
    {
        return animator.GetBoneTransform(bone).position;
    }
    public Vector3 ResizeToModel(int PartIndex)
    {
        //        Debug.Log("RatioToUI : " + (Z_Position / CanvasRect.position.z) + " / Ratio Position : " + (RateToUIWorldPos(CanvasRect, predicter.Result(5)) * (Z_Position / CanvasRect.position.z)));
        return (RateToUIWorldPos(CanvasRect, predicter.Result(PartIndex)) * (Z_Position / CanvasRect.position.z));
    }//시야기준


    public Vector3 Arm_Local_2DTo3D(Vector3 Parent, Vector3 Target, int PartDistanceIndex)
    {
        {
            /*
Vector3 Shoulder = ResizeToModel(5) + (Vector3.forward * Offset.z);
var Lelbow = ((ResizeToModel(5) - ResizeToModel(7)).normalized * PartDistance.GetVaule(2));
Lelbow = new Vector3(Lelbow.x * -1, Lelbow.y * -1, Lelbow.z);
var Lhand = (ResizeToModel(7) - ResizeToModel(9)).normalized * PartDistance.GetVaule(3);
Lhand = new Vector3(Lhand.x * -1, Lhand.y * -1, Lhand.z);
*/
        }//Legacy - Notuse

        Vector3 Elbow_2D = Vector3.Scale((Parent - Target), Vector3.one * -1);
        float Elbow_2D_Dis = Elbow_2D.magnitude;
        float Elbow_2D_DisRate = 0;


        if (Elbow_2D_Dis > PartDistance.GetVaule(PartDistanceIndex))
        {
            Elbow_2D = Elbow_2D.normalized * PartDistance.GetVaule(PartDistanceIndex);
            Elbow_2D_DisRate = 1;
        }
        else
        {
            Elbow_2D_DisRate = Elbow_2D_Dis / PartDistance.GetVaule(PartDistanceIndex);
        }

        float z = Mathf.Sin(Mathf.Acos(Elbow_2D_DisRate)) * PartDistance.GetVaule(PartDistanceIndex);

        // 팔길이 * Sin ( ACos (2D 길이 / 팔길이)) = Z축

        return new Vector3(Elbow_2D.x, Elbow_2D.y, z * -1);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ParentPartIndex">
    /// LeftUpper : 5 / LeftHand : 7</param>
    /// <param name="TargetPartIndex">
    /// LeftUpper : 7/ LeftHand : 9</param>
    /// <param name="PartDistanceIndex">
    /// LeftUpper : 2/ LeftHand : 3</param>
    /// <returns></returns>
    public Vector3 Arm_Local_2DTo3D(int ParentPartIndex, int TargetPartIndex, int PartDistanceIndex)
    {
        /*
        Vector3 Elbow_2D = Vector3.Scale((ResizeToModel(ParentPartIndex) - ResizeToModel(TargetPartIndex)), Vector3.one * -1) * Z_Position;
        float Elbow_2D_Dis = Elbow_2D.magnitude;
        float Elbow_2D_DisRate = 0;


        if (Elbow_2D_Dis > PartDistance.GetVaule(PartDistanceIndex))
        {
            Elbow_2D = Elbow_2D.normalized * PartDistance.GetVaule(PartDistanceIndex);
            Elbow_2D_DisRate = 1;
        }
        else
        {
            Elbow_2D_DisRate = Elbow_2D_Dis / PartDistance.GetVaule(PartDistanceIndex);
        }

        float z = Mathf.Sin(Mathf.Acos(Elbow_2D_DisRate)) * PartDistance.GetVaule(PartDistanceIndex);

        // 팔길이 * Sin ( ACos (2D 길이 / 팔길이)) = Z축

        return new Vector3(Elbow_2D.x, Elbow_2D.y, z * -1);
        */
        return Arm_Local_2DTo3D(ResizeToModel(ParentPartIndex), ResizeToModel(TargetPartIndex), PartDistanceIndex);
    }
}
