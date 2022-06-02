using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Project2DTo3D : MonoBehaviour
{
    public enum BodyPart
    {
        Head, Neck, Shoulder,
        Left_Shoulder, Left_Elbow, Left_Hand, Right_Shoulder, Right_Elbow, Right_Hand,
        Hip, Left_Knee, Left_Foot, Right_Knee, Right_Foot
    }//PosePart는 어깨랑 엉덩이가 양쪽에 있어서

    public Predictor predictor;
    public RectTransform CanvasRect;
    public RectTransform CameraView;

    Animator animator;
    //모델 >> Rig >> 노출할 추가 트랜스폼에서 체크필요 , 안하면 위치값 가져오지 못함

    [Space(10)]
    public Vector3 MovementMultiply = Vector3.one;
    public float Z_Position = 1f;
    float Z_Limit = 0.4f;//====필요가....

    [Space(5), Header(@"Offset")]
    public Vector3 ProjectOffset = new Vector3(0, 0.15f, 0);
    public float ProjectScaleOffset = 0.3f;
    public Vector3 DefaultPosition = new Vector3(0, -1.25f, 0);
    public Vector3 DefaultPosRate = new Vector3(0.3f, -1, 0);

    [Space(5), Header(@"Debug")]
    public Map<BodyPart, float> PartDistance = new();//시작할때 길이를 재서 IK에 주로 씀
    public List<GameObject> DebugPoints = new();
    public GameObject DebugObj;
    public float DebugPointsOffset = -0.3f;

    [Space(10)]
    //public float FaceWideRate = 0.8f;
    //public float HeadRollMultiply = 6.5f;
    public Vector3 HeadRotationMultiply = new Vector3(2, 0.05f, 2);
    public Vector3 HeadRotationRate = Vector3.zero; //Range : -1 ~ 1 / Vaule : -90 ~ 90

    [Space(5)]
    public Map<BodyPart, GameObject> IK_Target = new();

    public float Arm_ZLimitOffsetRate = 0.1f;
    public AnimationCurve Arm_ZLimitCurve = new(new Keyframe(0, 0), new Keyframe(1, 1));
    public float Arm_ZLimit = 0.125f;

    //[Space(10)]
    //public LineRenderer LeftLine;
    //public float LineZ_Offser = 0;

    [Space(10)]
    public Map<BodyPart, Vector3> TransPos = new();//이름 ProjectPos가 더 알맞음
    public Map<BodyPart, Vector3> SmoothPos = new();


    public bool ActiveLeftElbowIK = true;
    public bool ActiveRightElbowIK = true;

    public Vector3 LeftUpperTest;
    public Vector3 LeftLowerTest;

    public GameObject IKTarget;
    //
    public float MovementSpeed = 1;

    void Start()
    {
        animator = GetComponent<Animator>();

        ModelDefaultPartDistance();

        {
            TransPos.Add(BodyPart.Hip, Vector3.zero);

            TransPos.Add(BodyPart.Left_Elbow, Vector3.zero);
            TransPos.Add(BodyPart.Left_Hand, Vector3.zero);
            TransPos.Add(BodyPart.Right_Elbow, Vector3.zero);
            TransPos.Add(BodyPart.Right_Hand, Vector3.zero);
        }

        {
            SmoothPos.Add(BodyPart.Hip, Vector3.zero);

            SmoothPos.Add(BodyPart.Left_Elbow, Vector3.zero);
            SmoothPos.Add(BodyPart.Left_Hand, Vector3.zero);
            SmoothPos.Add(BodyPart.Right_Elbow, Vector3.zero);
            SmoothPos.Add(BodyPart.Right_Hand, Vector3.zero);
        }
    }

    void Update()
    {
        {
            if (SmoothPos.Get().Exists(t => t.Key == BodyPart.Left_Elbow))
            {
                SetSmoothPos(BodyPart.Left_Elbow, Vector3.Lerp(GetSmoothPos(BodyPart.Left_Elbow), GetTransPos(BodyPart.Left_Elbow), Time.deltaTime * MovementSpeed));
            }
            else
            {
                SetSmoothPos(BodyPart.Left_Elbow, GetTransPos(BodyPart.Left_Elbow));
            }

            if (SmoothPos.Get().Exists(t => t.Key == BodyPart.Left_Hand))
            {
                SetSmoothPos(BodyPart.Left_Hand, Vector3.Lerp(GetSmoothPos(BodyPart.Left_Hand), GetTransPos(BodyPart.Left_Hand), Time.deltaTime * MovementSpeed));
            }
            else
            {
                SetSmoothPos(BodyPart.Left_Hand, GetTransPos(BodyPart.Left_Hand));
            }

            if (SmoothPos.Get().Exists(t => t.Key == BodyPart.Right_Elbow))
            {
                SetSmoothPos(BodyPart.Right_Elbow, Vector3.Lerp(GetSmoothPos(BodyPart.Right_Elbow), GetTransPos(BodyPart.Right_Elbow), Time.deltaTime * MovementSpeed));
            }
            else
            {
                SetSmoothPos(BodyPart.Right_Elbow, GetTransPos(BodyPart.Right_Elbow));
            }

            if (SmoothPos.Get().Exists(t => t.Key == BodyPart.Right_Hand))
            {
                SetSmoothPos(BodyPart.Right_Hand, Vector3.Lerp(GetSmoothPos(BodyPart.Right_Hand), GetTransPos(BodyPart.Right_Hand), Time.deltaTime * MovementSpeed));
            }
            else
            {
                SetSmoothPos(BodyPart.Right_Hand, GetTransPos(BodyPart.Right_Hand));
            }
        }//Setup Smooth

        GetIKTarget(BodyPart.Left_Hand).transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).rotation;

        GetIKTarget(BodyPart.Left_Elbow).transform.position = GetSmoothPos(BodyPart.Left_Elbow);
        GetIKTarget(BodyPart.Left_Hand).transform.position = GetSmoothPos(BodyPart.Left_Hand);// OnAnimatorIK안에 있을때 안됨


        GetIKTarget(BodyPart.Right_Hand).transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightLowerArm).rotation;

        GetIKTarget(BodyPart.Right_Elbow).transform.position = GetSmoothPos(BodyPart.Right_Elbow);
        GetIKTarget(BodyPart.Right_Hand).transform.position = GetSmoothPos(BodyPart.Right_Hand);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        {
            //float UIrate = CanvasRect.position.z / (RateToUIWorldPos(CanvasRect, predicter.Result(5)) - RateToUIWorldPos(CanvasRect, predicter.Result(6))).magnitude;// 캔버스.z / 양 어깨 거리
            //Z_Position = UIrate * (GetBonePos(HumanBodyBones.LeftUpperArm) - GetBonePos(HumanBodyBones.RightUpperArm)).magnitude * MovementMultiply.z;
            //gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, Mathf.Max(Z_Position, Z_Limit));
        }//Disable - Previous Version Setup

        Z_Position = PartDistance.GetVaule(1) * (CanvasRect.position.z / (predictor.RateToWorldPos(5) - predictor.RateToWorldPos(6)).magnitude) * 0.5f * MovementMultiply.z;

        AllReturnPool();

        {
            /*
            //GetPool("Test Model Nose Pos").transform.position = predictor.RateToWorldPos(0, Z_Position);
            //GetPool("Test Model LeftShoulder Pos").transform.position = predictor.RateToWorldPos(5, Z_Position);
            var TempNose = GetPool("Test Model Nose Pos");
            TempNose.transform.position = ResizeToModle(0) + new Vector3(0, 0, DebugPointsOffset);
            TempNose.transform.localScale = Vector3.one * 0.1f;

            var TempShoulder = GetPool("Test Model LeftShoulder Pos");
            TempShoulder.transform.position = ResizeToModle(5) + new Vector3(0, 0, DebugPointsOffset);
            TempShoulder.transform.localScale = Vector3.one * 0.1f;

            if (predictor.results[7].Confidence > predictor.threshold)
            {
                var TempElbow = GetPool("Test Model Left Elbow Pos");
                TempElbow.transform.position = ResizeToModle(7) + new Vector3(0, 0, DebugPointsOffset);
                TempElbow.transform.localScale = Vector3.one * 0.1f;
            }

            if (predictor.results[9].Confidence > predictor.threshold)
            {
                var TempHand = GetPool("Test Model Left  Hand Pos");
                TempHand.transform.position = ResizeToModle(9) + new Vector3(0, 0, DebugPointsOffset);
                TempHand.transform.localScale = Vector3.one * 0.1f;
            }
            */
        }//Disable - Debug

        //Offset : (0, 0.15, 0) / Scale 0.3 / HeadRotationMulity : (0.5, 0.05, 2)
        //MovementMultiply : (5,5,1) / DefaultPosition : (0,-1.5,0)

        Vector3 AverageShoulder = (ResizeToModle(5) + ResizeToModle(6)) * 0.5f;

        gameObject.transform.position = Vector3.Scale(AverageShoulder, new Vector3(MovementMultiply.x, MovementMultiply.y, 1)) + DefaultPosition;

        //얼굴회전
        //팔 IK
        // 팔다리 방향Normal로 위치X

        {
            {
                Vector3 NosePos = ResizeToModle(0);
                //AverageShoulder
                //PartDistance[1]
                HeadRotationRate.y = ((NosePos.x - AverageShoulder.x) / PartDistance.GetVaule(1)) * -1 * HeadRotationMultiply.y;
                HeadRotationRate.y = Mathf.Clamp(HeadRotationRate.y, -1, 1);

            }//Head Yaw

            {
                if (predictor.results[3].Confidence > predictor.threshold && predictor.results[4].Confidence > predictor.threshold)
                {
                    //HeadRotationRate.x = (predictor.results[3].Position.y + predictor.results[4].Position.y) * 0.5f - predictor.results[0].Position.y;//4500
                    HeadRotationRate.x = (predictor.RateToWorldPos(3).y + predictor.RateToWorldPos(4).y) * 0.5f - predictor.RateToWorldPos(0).y;//300
                }
                else if (predictor.results[3].Confidence > predictor.threshold)//왼쪽귀만 보일때
                {
                    //HeadRotationRate.x = predictor.results[3].Position.y - predictor.results[0].Position.y;
                    HeadRotationRate.x = predictor.RateToWorldPos(3).y - predictor.RateToWorldPos(0).y;
                }
                else if (predictor.results[4].Confidence > predictor.threshold)//왼쪽귀만 보일때
                {
                    //HeadRotationRate.x = predictor.results[4].Position.y - predictor.results[0].Position.y;
                    HeadRotationRate.x = predictor.RateToWorldPos(4).y - predictor.RateToWorldPos(0).y;
                }
                else
                {
                    HeadRotationRate.x = 0;
                }

                ///predictor.results[3].Position.y / Multiply : 4500
                //predictor.RateToWorldPos() / Multiply : 300
                HeadRotationRate.x = (HeadRotationRate.x * HeadRotationMultiply.x) / Z_Position;
                HeadRotationRate.x = Mathf.Clamp(HeadRotationRate.x, -1, 1);
            }//Head Roll

            {
                //양눈 높이차이로
                if (predictor.results[1].Confidence > predictor.threshold && predictor.results[2].Confidence > predictor.threshold)
                {
                    float height = predictor.RateToWorldPos(1).y - predictor.RateToWorldPos(2).y;
                    float length = (predictor.RateToWorldPos(1) - predictor.RateToWorldPos(2)).magnitude;

                    HeadRotationRate.z = (90 - Mathf.Acos(height / length) * Mathf.Rad2Deg) / 90;
                }
                else
                {
                    HeadRotationRate.z = 0;
                }

                HeadRotationRate.z = HeadRotationRate.z * HeadRotationMultiply.z;
                HeadRotationRate.z = Mathf.Clamp(HeadRotationRate.z, -1, 1);
            }//Head Pitch

        }//Head (-1 ~ 1)

        {
            Vector3 ElbowOffset = gameObject.transform.forward * -0.01f;

            //GetPool("test");//========================================================================== 팔 부분 계산방식을 다시 , 상태에 따라서 결정하게
            //============  손위치 (모델 스케일기준), 방향 > 팔꿀치 예측

            //ResizeToModle 위치를 믿지 못하니까 방향 * 부위길이로 좌표 전달
            {

                Vector3 L_Hand;
                Vector3 L_Elbow = Arm_IK(true, ElbowOffset, DefaultPosRate, out L_Hand);

                {
                    //Debug.Log("Hand Offset : " + (predictor.results[7].Position - predictor.results[5].Position) +
                    //    "Rescale : " + (predictor.results[7].Position - predictor.results[5].Position).normalized * (PartDistance.GetVaule(2) + PartDistance.GetVaule(3)));
                    Vector3 LocalHand = (predictor.results[9].Position - predictor.results[5].Position);
                    Vector3 LocalElbow = (predictor.results[7].Position - predictor.results[5].Position);
                    if (predictor.Visible(7) && predictor.Visible(9))
                    {
                        //====Elbow는 LocalElbow.normalized 해서 모델의 UpperArm 길이 적용한거리 => 모델스케일 Elobw
                        //====Hand는 (LocalHand - LocalElbow).normalized * 모델의 LowerArm 길이 + 모델스케일 Elobw => 모델 스케일 Hand

                        L_Elbow = GetBonePos(HumanBodyBones.LeftUpperArm) + LocalElbow.normalized * PartDistance.GetVaule(2);
                        L_Hand = (LocalHand - LocalElbow).normalized * PartDistance.GetVaule(3) + L_Elbow;

                        GetPool("ReScaled Left Elbow").transform.position = L_Elbow;
                        GetPool("ReScaled Left Hand").transform.position = (LocalHand - LocalElbow).normalized * PartDistance.GetVaule(3)
                            + (GetBonePos(HumanBodyBones.LeftUpperArm) + LocalElbow.normalized * PartDistance.GetVaule(2));
                        //===========2D 기준 위치 , 보정작업하면 될듯?

                        L_Elbow = Arm_IK(GetBonePos(HumanBodyBones.LeftUpperArm), L_Elbow, L_Hand, ElbowOffset, PartDistance.GetVaule(2), PartDistance.GetVaule(3), out L_Hand);

                        GetPool("IkLeft Elbow").transform.position = L_Elbow;
                        GetPool("IkLeft Hand").transform.position = L_Hand;
                    }//Visible Elbow, Hand
                    else if ((predictor.Visible(7) == false) && predictor.Visible(9))
                    {
                        // 계산식을 좀 바꿔야함
                        //음... 팔꿈치 위치를 기본 위치로해서 보정하면?
                        
                        L_Elbow = GetBonePos(HumanBodyBones.LeftUpperArm) + DefaultPosRate.normalized * PartDistance.GetVaule(2);
                        //L_Hand = (L)// 2 방향... 1 임의의 좌표...
                        //    elbow는 기본위치에 고정 되어있고, 손만 입력을 받아 움ㅈ직임 , 움직임 비율을 어께너비와 비례해서

                        //float handRate = (predictor.results[9].Position - predictor.results[5].Position).magnitude / AverageShoulder.magnitude;

                    }//Visible Hand
                    else if (predictor.Visible(7) && ((predictor.Visible(9) == false)))
                    {
                        //손 위치만 방향에 맞춰서

                    }//Visible Elbow
                    else
                    {
                        // 기본위치로 / DefaultPosRate 방향에 맞춰서 

                    }//No Visible Elbow, Hand


                }//Test /=========== 전부 보일때 성공!! / 한부위라도 안보이면 ...안됨 , 팔꿈치 안보일땐 불안정 , 팔꿈치만 보이면  손위치 0,0,0 이 되서


                ActiveLeftElbowIK = predictor.results[7].Confidence > predictor.threshold;//=========== False 일때 문제발생, Elbow를 보정된 기본 위치로
                                          //!Mathf.Approximately(L_Elbow.sqrMagnitude, 0);

                //if (ActiveLeftElbowIK)
                    SetTransPos(BodyPart.Left_Elbow, L_Elbow);
                SetTransPos(BodyPart.Left_Hand, L_Hand);

            }//Left Hand, Elbow
            {
                Vector3 L_Hand;
                Vector3 L_Elbow = Arm_IK(false, ElbowOffset, DefaultPosRate, out L_Hand);

                ActiveRightElbowIK = false;// predictor.results[8].Confidence < predictor.threshold;
                // !Mathf.Approximately(L_Elbow.sqrMagnitude, 0);

                if (ActiveRightElbowIK)
                    SetTransPos(BodyPart.Right_Elbow, L_Elbow);
                SetTransPos(BodyPart.Right_Hand, L_Hand);
            }//Right Hand, Elbow
        }//팔 IK

        animator.SetBoneLocalRotation(HumanBodyBones.Head, Quaternion.Euler((HeadRotationRate * 90)));
        {

            {
                /*
if (ActiveLeftElbowIK)
{
    animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0);
    animator.SetIKHintPosition(AvatarIKHint.LeftElbow, GetTransPos(BodyPart.Left_LowerArm));
}
if (ActiveRightElbowIK)
{
    animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0);
    animator.SetIKHintPosition(AvatarIKHint.RightElbow, GetTransPos(BodyPart.Right_LowerArm));
}
animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
animator.SetIKPosition(AvatarIKGoal.LeftHand, GetTransPos(BodyPart.Left_Hand));
animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
animator.SetIKPosition(AvatarIKGoal.RightHand, GetTransPos(BodyPart.Right_Hand));
*///기존 방법 , 떨리면서 배치문제

                //gameObject.transform.TransformPoint (Local 2 Wolrd) / gameObject.transform.InverseTransformPoint (Wolrd 2 Local )
                //SetBoneLocalRotation이 팔,다리 부분엔 안되는듯, 내머리론 이해안됨 , 로컬 2 월드 변환해서 가져오는것도 안됨
                //SetIKPosition을 썻을때 떨리는 조건 찾기
            }//Legacy Set ArmPosition - Disabled / Prablem : Shake Arm

            {
                /*
                GetPool("Pretect Elbow").transform.position = GetTransPos(BodyPart.Left_Elbow);
                GetPool("Pretect Hand").transform.position = GetTransPos(BodyPart.Left_Hand);
                //팔꿈치와 손이 보일땐 잘됨

                var retargetElbow = GetBonePos(HumanBodyBones.LeftUpperArm) + LeftUpperTest.normalized * PartDistance.GetVaule(2);
                var retargetHand = retargetElbow + LeftUpperTest.normalized * PartDistance.GetVaule(3);

                {
                    //animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 1);
                    //animator.SetIKHintPosition(AvatarIKHint.LeftElbow, retargetElbow);

                    //animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                    //animator.SetIKPosition(AvatarIKGoal.LeftHand, GetBonePos(HumanBodyBones.LeftLowerArm) + LeftUpperTest.normalized * PartDistance.GetVaule(3));

                    //animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
                    //animator.SetIKRotation(AvatarIKGoal.LeftHand, Quaternion.LookRotation(LeftUpperTest));//이건 잘됨 , Elbow는 안됨
                }//Disable - SetIK(Hint)Position / Correct Position But ... Shaking Hand

                retargetElbow = Arm_IK(GetBonePos(HumanBodyBones.LeftUpperArm), retargetElbow, retargetHand, new Vector3(0, 0, 0.1f), PartDistance.GetVaule(2), PartDistance.GetVaule(3), out retargetHand);

                GetPool("Retarget Elbow").transform.position = retargetElbow;
                GetPool("World Elbow").transform.position = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position;//IK 미적용

                GetPool("Retarget Hand").transform.position = retargetHand;
                GetPool("Hand").transform.position = GetBonePos(HumanBodyBones.LeftLowerArm) + LeftUpperTest.normalized * PartDistance.GetVaule(3);

                {
                    //var L_ShoulderObj = GetPool("Left Shoulder");
                    //L_ShoulderObj.transform.position = GetBonePos(HumanBodyBones.LeftUpperArm) + new Vector3(0, 0, DebugPointsOffset);
                    //L_ShoulderObj.transform.rotation = Quaternion.LookRotation(gameObject.transform.right * -1) * Quaternion.LookRotation(transUpperArm);

                    //var L_Upper_Target = GetPool("Left Upper Taget");
                    //L_Upper_Target.transform.position = GetBonePos(HumanBodyBones.LeftUpperArm) + new Vector3(0, 0, DebugPointsOffset);
                    //L_Upper_Target.transform.rotation = Quaternion.LookRotation(LeftUpperTest.normalized);
                }//Disabled - UpperArm Direction
                */
            }//Disable - Test Trans Pos To Direction To Rotation


            //============================================================================================/ 리타겟팅 좌표는 맞지만 , 직접 값을 집어넣으면 작동안됨 / AnimationRigging으로 팔위치 지정
            //=======어깨도... 움직이게 해야겠지?, 
        }//Disable / Legacy ArmPosition
    }

    /// <summary>
    /// OnAnimatorIK() => Not Apply Animation? , Update() => Apply Animation
    /// </summary>
    /// <param name="bone"></param>
    /// <returns></returns>
    public Vector3 GetBonePos(HumanBodyBones bone)
    {
        return animator.GetBoneTransform(bone).position;
    }
    public void ModelDefaultPartDistance()
    {
        //모델 머리 ~ 어깨 평균 : 0.1 / 머리 ~ UpperArm 평균 : 0.15

        var ShoulderAverage = (GetBonePos(HumanBodyBones.LeftShoulder) + GetBonePos(HumanBodyBones.RightShoulder)) * 0.5f;

        PartDistance.Add(BodyPart.Head, (GetBonePos(HumanBodyBones.Head) - ShoulderAverage).magnitude);
        PartDistance.Add(BodyPart.Shoulder, (GetBonePos(HumanBodyBones.LeftUpperArm) - GetBonePos(HumanBodyBones.RightUpperArm)).magnitude);

        PartDistance.Add(BodyPart.Left_Shoulder, (GetBonePos(HumanBodyBones.LeftUpperArm) - GetBonePos(HumanBodyBones.LeftLowerArm)).magnitude);
        PartDistance.Add(BodyPart.Left_Elbow, (GetBonePos(HumanBodyBones.LeftLowerArm) - GetBonePos(HumanBodyBones.LeftHand)).magnitude);

        PartDistance.Add(BodyPart.Right_Shoulder, (GetBonePos(HumanBodyBones.RightUpperArm) - GetBonePos(HumanBodyBones.RightLowerArm)).magnitude);
        PartDistance.Add(BodyPart.Right_Elbow, (GetBonePos(HumanBodyBones.RightLowerArm) - GetBonePos(HumanBodyBones.RightHand)).magnitude);

        PartDistance.Add(BodyPart.Hip, (GetBonePos(HumanBodyBones.LeftUpperLeg) - GetBonePos(HumanBodyBones.RightUpperLeg)).magnitude);//=====

        PartDistance.Add(BodyPart.Left_Knee, (GetBonePos(HumanBodyBones.LeftUpperLeg) - GetBonePos(HumanBodyBones.LeftLowerLeg)).magnitude);
        PartDistance.Add(BodyPart.Left_Elbow, (GetBonePos(HumanBodyBones.LeftLowerLeg) - GetBonePos(HumanBodyBones.LeftFoot)).magnitude);

        PartDistance.Add(BodyPart.Right_Knee, (GetBonePos(HumanBodyBones.RightUpperLeg) - GetBonePos(HumanBodyBones.RightLowerLeg)).magnitude);
        PartDistance.Add(BodyPart.Right_Elbow, (GetBonePos(HumanBodyBones.RightLowerLeg) - GetBonePos(HumanBodyBones.RightFoot)).magnitude);
    }
    /// <summary>
    /// Not Accurate /정확하지 않음 / 스크린 기준
    /// </summary>
    /// <param name="resultIndex"></param>
    /// <returns></returns>
    public Vector3 ResizeToModle(int resultIndex)
    {
        Vector3 pos = predictor.RateToWorldPos(resultIndex, Z_Position);
        pos = new Vector3(pos.x * ProjectScaleOffset * (1 / Z_Position), pos.y * ProjectScaleOffset * (1 / Z_Position), Z_Position);
        pos += ProjectOffset;

        return pos;
    }

    /// <summary>
    /// Return : IKedElbow / ElbowOffset - 어느 방향으로 얼마나 굽힐껀지
    /// </summary>
    /// <param name="Shoulder"></param>
    /// <param name="Elbow"></param>
    /// <param name="Hand"></param>
    /// <param name="ElbowOffset"></param>
    /// <param name="UpperArm"></param>
    /// <param name="LowerArm"></param>
    /// <param name="IkedHand"></param>
    /// <returns></returns>
    public static Vector3 Arm_IK(Vector3 Shoulder, Vector3 Elbow, Vector3 Hand, Vector3 ElbowOffset, float UpperArm, float LowerArm, out Vector3 IkedHand)
    {
        //line_start + Vector3.Project(point - line_start, line_end - line_start); / ClosePointOnDirection


        Vector3 ClosePoint = Shoulder + Vector3.Project(Elbow + ElbowOffset - Shoulder, Hand - Shoulder);
        //Vector3 Center = Hand - (Hand - Shoulder).normalized * (LowerArm / (LowerArm + UpperArm)) * (Hand - Shoulder).magnitude;

        //UpperArm , LowerArm 의 길이가 맞는 ClosePoint ~ Elbow 방향의 거리
        // Acos (HC / LowerArm) = Angle  ==>  LowerArm * Sin (Angle) = EC
        // HC = Hand ~ ClosePoint / EC = Elbow ~ ClosePoint

        float Height = LowerArm * Mathf.Sin(Mathf.Acos((Hand - ClosePoint).magnitude / LowerArm));//Elbow는 옆으로 UpperArm + 약간의 Z축에 고정
        //float Height = UpperArm * Mathf.Sin(Mathf.Acos((Shoulder - ClosePoint).magnitude / UpperArm));//IkedElbow의 손방향으로의 거리로 팔굽힘결정

        //UpperArm 과 LowerArm 비율이 맞는부분 부터 Heghit 만큼

        Vector3 CorrectionElbow = Height > 0 ? (ClosePoint + (Elbow + ElbowOffset - ClosePoint).normalized * Height) : ClosePoint;

        Vector3 IKedElbow = Shoulder + (CorrectionElbow - Shoulder).normalized * UpperArm;
        IkedHand = IKedElbow + (Hand - IKedElbow).normalized * LowerArm;

        return IKedElbow;

        //Out 으로 팔위치 내보내기 , 항상 UpperArm 과 LowerArm 길이 고정


        //각도제한 있으면 좋은데 굳이?
    }
    /// <summary>
    /// Return : IKed Elbow
    /// </summary>
    /// <param name="LeftArm"></param>
    /// <param name="ElbowOffset">어느방향으로 얼마나 굽힐껀지</param>
    /// <param name="UpperArmLength">PartDistance.GetVaule(2)</param>
    /// <param name="LowerArmLength">PartDistance.GetVaule(3)</param>
    /// <param name="IKedHand"></param>
    /// <returns></returns>
    public Vector3 Arm_IK(bool LeftArm, Vector3 ElbowOffset, Vector3 DefaultPositionRate, out Vector3 IKedHand)
    {
        //방향은 예측좌표 변환값 / 기준위치는 모델좌표
        //팔꿈치만 보일때 , 팔이 안보일때 팔꿈치 IK 미적용 (떨림 문제)
        //====손이 몸 중앙으로 갈때 들어가지 않게 할려면 , IK를 두번해야...?
        //  ====Setup 단계에서 어깨 + Alpa 범위안에 있다면 Curve에 따라 Z축 추가
        Vector3 shoulder = Vector3.zero;
        Vector3 elbow = Vector3.zero;
        Vector3 hand = Vector3.zero;
        float UpperArmDistance = 0;
        float LowerArmDistance = 0;

        bool VisibleElbow = false;
        bool VisibleHand = false;

        Vector3 iked_Elbow = Vector3.zero;
        Vector3 iked_Hand = Vector3.zero;

        {
            if (LeftArm)
            {
                shoulder = GetBonePos(HumanBodyBones.LeftUpperArm);

                elbow = (predictor.results[7].Position - predictor.results[5].Position).normalized * PartDistance.GetVaule(2);
                //elbow += shoulder;

                hand = (predictor.results[9].Position - predictor.results[7].Position).normalized * PartDistance.GetVaule(3);
                //hand += elbow;

                UpperArmDistance = PartDistance.GetVaule(2);
                LowerArmDistance = PartDistance.GetVaule(3);

                VisibleElbow = predictor.results[7].Confidence > predictor.threshold;
                VisibleHand = predictor.results[9].Confidence > predictor.threshold;
            }
            else
            {
                shoulder = GetBonePos(HumanBodyBones.RightUpperArm);

                elbow = (predictor.results[8].Position - predictor.results[6].Position).normalized * PartDistance.GetVaule(4);
                //elbow += shoulder;

                hand = (predictor.results[10].Position - predictor.results[8].Position).normalized * PartDistance.GetVaule(5);
                //hand += elbow;

                UpperArmDistance = PartDistance.GetVaule(4);
                LowerArmDistance = PartDistance.GetVaule(5);

                VisibleElbow = predictor.results[8].Confidence > predictor.threshold;
                VisibleHand = predictor.results[10].Confidence > predictor.threshold;
            }
        }//Setup Parameter

        if (VisibleElbow && VisibleHand)
        {
            GetArmZPos(LeftArm, out float LelbowZ, out float LhandZ);
            elbow.z = LelbowZ;
            hand.z = LhandZ;

            iked_Elbow = Arm_IK(shoulder, shoulder + elbow, shoulder + elbow + hand, ElbowOffset, UpperArmDistance, LowerArmDistance, out iked_Hand);//Visible Elbow, Hand
        } else if (VisibleElbow)
        {
            iked_Elbow = Arm_IK(shoulder, shoulder + elbow, shoulder + elbow + (elbow.normalized * LowerArmDistance), ElbowOffset, UpperArmDistance, LowerArmDistance, out iked_Hand);//Visible Elbow, Hand

            IKedHand = iked_Hand;
            return Vector3.zero;
        }
        else if (VisibleHand)
        {

            float UIShoulderLength = (predictor.results[5].Position - predictor.results[6].Position).magnitude;
            float UIShoulderToHand = 0;
            Vector3 elbowAddOffset = Vector3.down;

            if (LeftArm)
            {
                UIShoulderToHand = (predictor.results[8].Position - predictor.results[5].Position).magnitude;

                hand = (predictor.results[9].Position - predictor.results[5].Position).normalized * PartDistance.GetVaule(2) * (UIShoulderToHand / UIShoulderLength);

                elbowAddOffset += gameObject.transform.right * -1;
            }
            else
            {
                UIShoulderToHand = (predictor.results[10].Position - predictor.results[6].Position).magnitude;

                hand = (predictor.results[10].Position - predictor.results[6].Position).normalized * PartDistance.GetVaule(4) * (UIShoulderToHand / UIShoulderLength);

                elbowAddOffset += gameObject.transform.right;
            }

            GetArmZPos(LeftArm, out float LelbowZ, out float LhandZ);
            elbow.z = LelbowZ;
            hand.z = LhandZ;

            iked_Elbow = Arm_IK(shoulder, shoulder + (hand * 0.5f), shoulder + hand, ElbowOffset + elbowAddOffset, UpperArmDistance, LowerArmDistance, out iked_Hand);

        }
        else
        {
            //elbow = DefaultPositionRate.normalized * UpperArmDistance;
            //hand = DefaultPositionRate.normalized * LowerArmDistance;
            DefaultPositionRate.x *= LeftArm ? gameObject.transform.right.x * -1 : gameObject.transform.right.x;

            iked_Elbow = shoulder + UpperArmDistance * DefaultPositionRate;
            iked_Hand = iked_Elbow + LowerArmDistance * DefaultPositionRate;

            //iked_Elbow = Arm_IK(shoulder, shoulder + elbow * 1.2f, shoulder + elbow + hand * 1.2f, ElbowOffset, UpperArmDistance, LowerArmDistance, out iked_Hand);

            IKedHand = iked_Hand;
            return Vector3.zero;
        }

        //GetPool("IK Elbow").transform.position = iked_Elbow;
        //GetPool("IK Hand").transform.position = iked_Hand;

        IKedHand = iked_Hand;
        return iked_Elbow;
    }
    public Quaternion Arm_IK_Direction(bool LeftArm, Vector3 ElbowOffset, Vector3 DefaultPositionRate, out Quaternion IKedHand)
    {
        //방향은 예측좌표 변환값 / 기준위치는 모델좌표
        //팔꿈치만 보일때 , 팔이 안보일때 팔꿈치 IK 미적용 (떨림 문제)
        //====손이 몸 중앙으로 갈때 들어가지 않게 할려면 , IK를 두번해야...?
        //  ====Setup 단계에서 어깨 + Alpa 범위안에 있다면 Curve에 따라 Z축 추가
        Vector3 shoulder = Vector3.zero;
        Vector3 elbow = Vector3.zero;
        Vector3 hand = Vector3.zero;
        float UpperArmDistance = 0;
        float LowerArmDistance = 0;

        bool VisibleElbow = false;
        bool VisibleHand = false;

        Vector3 iked_Elbow = Vector3.zero;
        Vector3 iked_Hand = Vector3.zero;

        {
            if (LeftArm)
            {
                shoulder = GetBonePos(HumanBodyBones.LeftUpperArm);

                elbow = (predictor.results[7].Position - predictor.results[5].Position).normalized * PartDistance.GetVaule(2);
                //elbow += shoulder;

                hand = (predictor.results[9].Position - predictor.results[7].Position).normalized * PartDistance.GetVaule(3);
                //hand += elbow;

                UpperArmDistance = PartDistance.GetVaule(2);
                LowerArmDistance = PartDistance.GetVaule(3);

                VisibleElbow = predictor.results[7].Confidence > predictor.threshold;
                VisibleHand = predictor.results[9].Confidence > predictor.threshold;
            }
            else
            {
                shoulder = GetBonePos(HumanBodyBones.RightUpperArm);

                elbow = (predictor.results[8].Position - predictor.results[6].Position).normalized * PartDistance.GetVaule(4);
                //elbow += shoulder;

                hand = (predictor.results[10].Position - predictor.results[8].Position).normalized * PartDistance.GetVaule(5);
                //hand += elbow;

                UpperArmDistance = PartDistance.GetVaule(4);
                LowerArmDistance = PartDistance.GetVaule(5);

                VisibleElbow = predictor.results[8].Confidence > predictor.threshold;
                VisibleHand = predictor.results[10].Confidence > predictor.threshold;
            }
        }//Setup Parameter

        if (VisibleElbow && VisibleHand)
        {
            if (LeftArm)
            {
                if (Mathf.Abs(predictor.results[5].Position.x) + Arm_ZLimitOffsetRate > Mathf.Abs(predictor.results[9].Position.x))
                {
                    elbow.z = (gameObject.transform.forward * Arm_ZLimit).z;
                    hand.z = (gameObject.transform.forward * Arm_ZLimit).z;
                }
            }
            else
            {
                if (Mathf.Abs(predictor.results[6].Position.x) + Arm_ZLimitOffsetRate > Mathf.Abs(predictor.results[10].Position.x))
                {
                    elbow.z = (gameObject.transform.forward * Arm_ZLimit).z;
                    hand.z = (gameObject.transform.forward * Arm_ZLimit).z;
                }
            }

            iked_Elbow = Arm_IK(shoulder, shoulder + elbow, shoulder + elbow + hand, ElbowOffset, UpperArmDistance, LowerArmDistance, out iked_Hand);//Visible Elbow, Hand
        }
        else if (VisibleElbow)
        {
            iked_Elbow = Arm_IK(shoulder, shoulder + elbow, shoulder + elbow + (elbow.normalized * LowerArmDistance), ElbowOffset, UpperArmDistance, LowerArmDistance, out iked_Hand);//Visible Elbow, Hand

        }
        else if (VisibleHand)
        {

            float UIShoulderLength = (predictor.results[5].Position - predictor.results[6].Position).magnitude;
            float UIShoulderToHand = 0;
            Vector3 elbowAddOffset = Vector3.down;

            if (LeftArm)
            {
                UIShoulderToHand = (predictor.results[8].Position - predictor.results[5].Position).magnitude;

                hand = (predictor.results[9].Position - predictor.results[5].Position).normalized * PartDistance.GetVaule(2) * (UIShoulderToHand / UIShoulderLength);

                elbowAddOffset += gameObject.transform.right * -1;
            }
            else
            {
                UIShoulderToHand = (predictor.results[10].Position - predictor.results[6].Position).magnitude;

                hand = (predictor.results[10].Position - predictor.results[6].Position).normalized * PartDistance.GetVaule(4) * (UIShoulderToHand / UIShoulderLength);

                elbowAddOffset += gameObject.transform.right;
            }

            if (LeftArm)
            {
                if (Mathf.Abs(predictor.results[5].Position.x) + Arm_ZLimitOffsetRate > Mathf.Abs(predictor.results[9].Position.x))
                {
                    elbow.z = (gameObject.transform.forward * Arm_ZLimit).z;
                    hand.z = (gameObject.transform.forward * Arm_ZLimit).z;
                }
            }
            else
            {
                if (Mathf.Abs(predictor.results[6].Position.x) + Arm_ZLimitOffsetRate > Mathf.Abs(predictor.results[10].Position.x))
                {
                    elbow.z = (gameObject.transform.forward * Arm_ZLimit).z;
                    hand.z = (gameObject.transform.forward * Arm_ZLimit).z;
                }
            }

            iked_Elbow = Arm_IK(shoulder, shoulder + (hand * 0.5f), shoulder + hand, ElbowOffset + elbowAddOffset, UpperArmDistance, LowerArmDistance, out iked_Hand);

        }
        else
        {
            //elbow = DefaultPositionRate.normalized * UpperArmDistance;
            //hand = DefaultPositionRate.normalized * LowerArmDistance;
            DefaultPositionRate.x *= LeftArm ? gameObject.transform.right.x * -1 : gameObject.transform.right.x;

            elbow = shoulder + UpperArmDistance * DefaultPositionRate;
            hand = iked_Elbow + LowerArmDistance * DefaultPositionRate;

            iked_Elbow = Arm_IK(shoulder, shoulder + elbow, shoulder + elbow + hand * 1.2f, ElbowOffset, UpperArmDistance, LowerArmDistance, out iked_Hand);
        }

        //GetPool("IK Elbow").transform.position = iked_Elbow;
        //GetPool("IK Hand").transform.position = iked_Hand;


        //Quaternion.LookRotation(iked_Elbow)
        //Quaternion.LookRotation(iked_Hand)


        IKedHand = Quaternion.LookRotation(iked_Hand - iked_Elbow);
        return Quaternion.LookRotation(iked_Elbow - shoulder);
    }
    public bool GetArmZPos(bool LeftArm, out float elbow, out float hand)
    {
        if (LeftArm)
        {
            //float offset = Mathf.Abs(predictor.results[5].Position.x) + Arm_ZLimitOffsetRate - Mathf.Abs(predictor.results[9].Position.x);//Offset 계산 방법 바꾸기 , ABS는 제거되야됨
            float offset = (predictor.results[5].Position.x + Arm_ZLimitOffsetRate) - predictor.results[9].Position.x;
            // ( 어깨 + Offset ) - Hand / 손을 몸 가운데 놓으면 => -(어깨너비 * 0.5f + Offset)

            if (offset > 0)
            {
                elbow = (gameObject.transform.forward * Arm_ZLimit * Arm_ZLimitCurve.Evaluate(offset / (PartDistance.GetVaule(1) * 0.5f))).z;
                hand = (gameObject.transform.forward * Arm_ZLimit * Arm_ZLimitCurve.Evaluate(offset / (PartDistance.GetVaule(1) * 0.5f))).z;
                return true;
            }
        }
        else
        {
            //float offset = (Mathf.Abs(predictor.results[6].Position.x) + Arm_ZLimitOffsetRate) - Mathf.Abs(predictor.results[10].Position.x);
            float offset = predictor.results[10].Position.x - (predictor.results[6].Position.x - Arm_ZLimitOffsetRate);
            //Shoulder: 0.3 / Hand : 0.5 ~ 0;

            if (offset > 0)
            {
                elbow = (gameObject.transform.forward * Arm_ZLimit * Arm_ZLimitCurve.Evaluate(offset / (PartDistance.GetVaule(1) * 0.5f))).z;
                hand = (gameObject.transform.forward * Arm_ZLimit * Arm_ZLimitCurve.Evaluate(offset / (PartDistance.GetVaule(1) * 0.5f))).z;
                return true;
            }
        }

        elbow = 0;
        hand = 0;
        return false;
    }

    public GameObject GetPool(string name = "")
    {
        for (int i = 0; i < DebugPoints.Count; i++)
        {
            if (!DebugPoints[i].activeSelf)
            {
                DebugPoints[i].SetActive(true);

                if (!string.IsNullOrEmpty(name))
                {
                    DebugPoints[i].name = name;
                }

                return DebugPoints[i];
            }
        }

        if (DebugObj != null)
        {
            var obj = GameObject.Instantiate(DebugObj);
            DebugPoints.Add(obj);
            //obj.transform.SetParent(gameObject.transform);

            if (!string.IsNullOrEmpty(name))
            {
                obj.name = name;
            }

            return obj;
        } else
        {
            return null;
        }
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
    public void SetTransPos(BodyPart part, Vector3 pos)
    {
        int index = TransPos.Get().FindIndex(t => t.Key == part);

        if (index >= 0)
            TransPos.SetVaule(index, pos);
        else
            TransPos.Add(part, pos);
    }
    public Vector3 GetSmoothPos(BodyPart part)
    {
        return SmoothPos.Get().Find(t => t.Key == part).Vaule;
    }
    public void SetSmoothPos(BodyPart part, Vector3 pos)
    {
        int index = SmoothPos.Get().FindIndex(t => t.Key == part);

        if (index >= 0)
            SmoothPos.SetVaule(index, pos);
        else
            SmoothPos.Add(part, pos);
    }
    public GameObject GetIKTarget(BodyPart part)
    {
        return IK_Target.Get().Find(t => t.Key == part).Vaule;
    }
}
