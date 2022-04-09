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

    public float Temp = 1;

    void Start()
    {
        animator = GetComponent<Animator>();

        ModelDefaultPartDistance();
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

            //Debug.Log("Visible L_Elbow: " + (predicter.results[7].confidence > predicter.threshold) + " / Visible L_Hand : " + (predicter.results[8].confidence > predicter.threshold) +
            //    "\n Visible R_Elbow: " + (predicter.results[9].confidence > predicter.threshold) + " / Visible R_Hand : " + (predicter.results[10].confidence > predicter.threshold));
            //문제점 : 한손만 보일때 왼손과 오른손 위치가 같아지는 문제 + 팔꿈치도
            //손만 인식은 되지않음 , 대신 손이 팔꿈치로 인식ㅋㅋㅋ

            //Debug.Log("Left => Shoulder - UpperArm : " + (ResizeToModel(5) - ResizeToModel(7)).normalized + " / UppderArm - LowerArm : " + (ResizeToModel(7) - ResizeToModel(9)).normalized);

            if (LeftLine != null && ((predicter.results[7].confidence > predicter.threshold) || (predicter.results[8].confidence > predicter.threshold)))
            {
                Vector3 Shoulder = ResizeToModel(5) + (Vector3.forward * Offset.z);

                var Lelbow = ((ResizeToModel(5) - ResizeToModel(7)).normalized * PartDistance.GetVaule(2));
                Lelbow = new Vector3(Lelbow.x * -1, Lelbow.y * -1, Lelbow.z);
                var Lhand = (ResizeToModel(7) - ResizeToModel(9)).normalized * PartDistance.GetVaule(3);
                Lhand = new Vector3(Lhand.x * -1, Lhand.y * -1, Lhand.z);

                LeftLine.gameObject.SetActive(true);
                LeftLine.SetPosition(0, Shoulder);
                LeftLine.SetPosition(1, Shoulder + Lelbow);
                LeftLine.SetPosition(2, (Shoulder + Lelbow + Lhand));

                //ResizeToModel으로 길이 측정시 PartDistance의 기본값 보다 짧게 나온경우 짧게 나온만큼 팔을 앞으로

                animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 1);
                animator.SetIKHintPosition(AvatarIKHint.LeftElbow, Shoulder + Lelbow);
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, (Shoulder + Lelbow + Lhand));

            }
            else
            {
                //손만 안보이는 경우 손을 팔꿈치 방향으로 

                LeftLine.gameObject.SetActive(false);
            }

            AllReturnPool();



            Vector3 debug_elbow = ResizeToModel(5) + Vector3.Scale((ResizeToModel(5) - ResizeToModel(7)), Vector3.one * -1) + (Vector3.forward * Offset.z);

            GetPool().transform.position = ResizeToModel(5) + (Vector3.forward * Offset.z);
            GetPool().transform.position = debug_elbow;
            GetPool().transform.position = debug_elbow + Vector3.Scale((ResizeToModel(7) - ResizeToModel(9)), Vector3.one * -1);//비율 맞는듯 , 이게 더 나을수도
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
    public GameObject GetPool()
    {
        for (int i = 0; i < DebugPoints.Count; i++)
        {
            if (! DebugPoints[i].activeSelf)
            {
                DebugPoints[i].SetActive(true);
                return DebugPoints[i];
            }
        }

        var obj = GameObject.Instantiate(DebugObj);
        DebugPoints.Add(obj);
        obj.transform.SetParent(gameObject.transform);
        return obj;
    }
    public void AllReturnPool()
    {
        for (int i = 0; i < DebugPoints.Count; i++)
        {
            DebugPoints[i].SetActive(false);
        }
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
}
