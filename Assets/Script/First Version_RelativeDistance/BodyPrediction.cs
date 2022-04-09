using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using TensorFlowLite;

public class BodyPrediction : MonoBehaviour
{
    [System.Serializable]
    public class IKInfo
    {
        public Quaternion FaceRotation = Quaternion.identity;
        public Vector3 BodyPosition = Vector3.zero;
        public Vector3 BodyRotation;//y - object Yaw , z - UpperChest Pitch

        public Vector3 L_ElbowPosition;
        public Vector3 L_WristPosition;

        public Vector3 R_ElbowPosition;
        public Vector3 R_WristPosition;

        public Vector3 HipPosition;
    }

    //BodyParts Position (Eye, Nose, Ears) To Body Transform (Head) 
    //얼굴 표정 인식은 다른걸로 (AR Foundation 으로)

    [SerializeField, FilePopup("*.tflite")] string fileName = "posenet_mobilenet_v1_100_257x257_multi_kpt_stripped.tflite";
    [SerializeField] RawImage cameraView = null;
    [SerializeField] GLDrawer glDrawer = null;
    [SerializeField, Range(0f, 1f)] public float threshold = 0.5f;

    Coroutine PredictionCoroutine;
    bool EnableCoroutine = false;

    public float PredictionDelay = 0.1f;
    public bool DebugMode = true;
    private int currentIndex = 0;
    WebCamTexture webcamTexture;
    PoseNet poseNet;
    Vector3[] corners = new Vector3[4];

    public PoseNet.Result[] results;
    public IKInfo PredictedIKs = new();
    public IKInfo SmoothIKs = new();

    [Space(10)]
    public float NeckLengthRate = 1.3f;// ((ShoulderCenter - Nose).Distance / (LeftShoulder - RightShoulder).Distance * 0.5f)
    public AnimationCurve NeckLengthMultiCurve = new AnimationCurve(new Keyframe(0.35f, 1f), new Keyframe(0.5f, 0.75f));// 후면일때 씀

    [Space(5)]
    public float NoseOffset = 0;
    public Vector3 FaceSensitive = new Vector3(0.5f, -6.7f, 12);
    public float RotationSpeed = 5;

    [Space(5)]
    public Vector3 OriginPosition = Vector3.zero;//양어깨의 중심이 기준
    public Vector3 ArmPositionToInvisible = Vector3.down;
    public float MovementSpeed = 5;
    public Vector3 MovementScaleOffset = Vector3.one;

    [Space(5)]
    public bool DebugHiddenFace = false;
    public bool FocusInputBlock = false;

    public enum BodySlot { Head, UpperBody , LowerBody}
    [System.Serializable]
    public class BodyInfo
    {
        public BodySlot Body;
        public float HeighestConfidence = 0;
        public Vector2 Position;
        public bool IsVisible = false;

        public Vector3 RotationOffset;

        public BodyInfo(BodySlot body , float Threshold, PoseNet.Result Origin, PoseNet.Result Left, PoseNet.Result Right, float Z_Multiply = 2)
        {
            Body = body;
            HeighestConfidence = Mathf.Max(Left.confidence, Right.confidence);
            IsVisible = HeighestConfidence >= Threshold;
            Position = new Vector2(Origin.x, Origin.y);
            RotationOffset.z = (Right.y - Left.y) * Z_Multiply * (0.13f / Vector2.Distance(new Vector2(Right.x, Right.y), new Vector2(Left.x, Left.y)));//Pitch (정면 축 회전 - Z)
        }

        public BodyInfo(BodySlot body, float Threshold, PoseNet.Result Left, PoseNet.Result Right, float OffsetMultiply = 1)
        {
            Body = body;
            HeighestConfidence = Mathf.Max(Left.confidence, Right.confidence);
            IsVisible = HeighestConfidence >= Threshold;
            Position = new Vector2((Left.x + Right.x), (Left.y + Right.y)) * 0.5f;
            RotationOffset.z = (Right.y - Left.y) * OffsetMultiply;//Pitch (정면 축 회전 - Z)//거리반비례해서 값 증가 
            //상체는 기울기가 필요 X
        }
    }
    public BodyInfo[] bodyInfos = new BodyInfo[3];

    void Start()
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        poseNet = new PoseNet(path);

        // Init camera
        //string cameraName = WebCamUtil.FindName();
        string cameraName = WebCamTexture.devices[currentIndex].name;
        Debug.Log("Start " + cameraName);
        //webcamTexture = new WebCamTexture(cameraName, 640, 480, 30);
        webcamTexture = new WebCamTexture(cameraName)
        {
            requestedFPS = 30
        };

        webcamTexture.Play();

        if (DebugMode)
        {
            cameraView.gameObject.SetActive(true);
            cameraView.texture = webcamTexture;

            var rect = cameraView.GetComponent<RectTransform>();
            rect.GetWorldCorners(corners);
        }

        //glDrawer.OnDraw += OnGLDraw;

        poseNet.Invoke(webcamTexture);
        results = poseNet.GetResults();

        EnableCoroutine = true;
        PredictionCoroutine = StartCoroutine(PredictionLoop());
    }

    void OnDestroy()
    {
        webcamTexture?.Stop();
        poseNet?.Dispose();

        EnableCoroutine = false;
        StopCoroutine(PredictionCoroutine);
        PredictionCoroutine = null;
        //glDrawer.OnDraw -= OnGLDraw;
    }

    void Update()
    {
        //poseNet.Invoke(webcamTexture);
        //results = poseNet.GetResults();

        // set uv
        //cameraView.material = poseNet.transformMat;
        // cameraView.uvRect = TextureToTensor.GetUVRect(
        //     (float)webcamTexture.width / webcamTexture.height,
        //     1,
        //     TextureToTensor.AspectMode.Fill);


        SmoothIKs.FaceRotation = Quaternion.LerpUnclamped(SmoothIKs.FaceRotation, PredictedIKs.FaceRotation, Time.deltaTime * RotationSpeed);

        SmoothIKs.BodyPosition = Vector3.LerpUnclamped(SmoothIKs.BodyPosition, PredictedIKs.BodyPosition, (Time.deltaTime * MovementSpeed));

        SmoothIKs.BodyRotation = Vector3.LerpUnclamped(SmoothIKs.BodyRotation, PredictedIKs.BodyRotation, (Time.deltaTime * RotationSpeed));

        SmoothIKs.L_ElbowPosition = Vector3.LerpUnclamped(SmoothIKs.L_ElbowPosition, PredictedIKs.L_ElbowPosition, (Time.deltaTime * MovementSpeed));
        SmoothIKs.L_WristPosition = Vector3.LerpUnclamped(SmoothIKs.L_WristPosition, PredictedIKs.L_WristPosition, (Time.deltaTime * MovementSpeed));

        SmoothIKs.R_ElbowPosition = Vector3.LerpUnclamped(SmoothIKs.R_ElbowPosition, PredictedIKs.R_ElbowPosition, (Time.deltaTime * MovementSpeed));
        SmoothIKs.R_WristPosition = Vector3.LerpUnclamped(SmoothIKs.R_WristPosition, PredictedIKs.R_WristPosition, (Time.deltaTime * MovementSpeed));

        SmoothIKs.HipPosition = Vector3.LerpUnclamped(SmoothIKs.HipPosition, PredictedIKs.HipPosition, (Time.deltaTime * MovementSpeed));
    }

    #region Disable
    /*
    void OnGLDraw()
    {
        if (DebugMode)
        {
            
            //var rect = cameraView.GetComponent<RectTransform>();
            //rect.GetWorldCorners(corners);
            Vector3 min = corners[0];
            Vector3 max = corners[2];

            GL.Begin(GL.LINES);

            GL.Color(Color.green);
            var connections = PoseNet.Connections;
            int len = connections.GetLength(0);
            for (int i = 0; i < len; i++)
            {
                var a = results[(int)connections[i, 0]];
                var b = results[(int)connections[i, 1]];
                if (a.confidence >= threshold && b.confidence >= threshold)
                {
                    GL.Vertex(Leap3(min, max, new Vector3(a.x, 1f - a.y, 0)));
                    GL.Vertex(Leap3(min, max, new Vector3(b.x, 1f - b.y, 0)));
                }
            }

            GL.End();
            
        }//Sample Code - Draw Line

        for (int i = 0; i < results.Length; i++)//순서가 항상 같으니 
        {
            //머리 기준 : 코 / 상체 기준 : 양쪽 어깨 중심 / 하체 기준 : 양쪽 엉덩이 중심
            //기준 좌표 와 TillingOffset(왼 > 기준 > 오른쪽 과의 높이차이 평균) 을 저장
            //코와 어깨중심간 Y축거리로 Roll(X축회전) , TillingOffset으로 Pitch(정면축회전 - Z) , 어깨중심 기준 좌표계로 코와 어깨중심간 X축거리 Yaw(위쪽축회전 - Y)

            bodyInfos[0] = new BodyInfo(BodySlot.Head, threshold, results[0], results[1], results[2], FaceSensitive.z);//중심 : 코 , 좌우 : 귀
            bodyInfos[1] = new BodyInfo(BodySlot.UpperBody, threshold, results[5], results[6], 3.8f);
            bodyInfos[2] = new BodyInfo(BodySlot.LowerBody, threshold, results[11], results[12]);
        }

        if (FocusInputBlock)
        {
            PredictedIKs.FaceRotation = Quaternion.identity;
            PredictedIKs.BodyRotation = Vector3.zero;
            PredictedIKs.BodyPosition = OriginPosition;

            PredictedIKs.L_ElbowPosition = ArmPositionToInvisible * 0.5f;
            PredictedIKs.L_WristPosition = ArmPositionToInvisible;

            PredictedIKs.R_ElbowPosition = ArmPositionToInvisible * 0.5f;
            PredictedIKs.R_WristPosition = ArmPositionToInvisible;

            return;
        }

        {
            float NoseLength = Vector2.Distance(bodyInfos[0].Position, ToVector2(1));

            if (bodyInfos[0].IsVisible && !DebugHiddenFace)
            {
                //bodyInfos[0].RotationOffset.x = (NeckLength) - Vector2.Distance(bodyInfos[0].Position, bodyInfos[1].Position);//코와 어깨중심간 Y축거리로 Roll(X축회전)
                bodyInfos[0].RotationOffset.x = ((bodyInfos[0].Position.y - VisibleEarsAverage().y + NoseOffset) / NoseLength) * FaceSensitive.x;
                //보이는 귀 좌표 평균내서  코.y - 귀평균.y 으로 바꾸기

                {
                    //어깨 중심 - 오른어깨 의 수직방향 * NeckLength => Yaw가 0 일때 코 위치  ==> 어깨 중심.X - 코.X 으로 바꿈

                    //bodyInfos[0].RotationOffset.y = (bodyInfos[0].Position.x - (Lpts.normalized * (NeckLength) + bodyInfos[1].Position).x) * FaceSensitive.y;
                    bodyInfos[0].RotationOffset.y = (bodyInfos[0].Position.x - bodyInfos[1].Position.x) * FaceSensitive.y;

                    if (DebugMode)
                    {
                        float ShoulderLength = Mathf.Abs(Vector2.Distance(ToVector2(6), ToVector2(5)) * 0.5f);
                        float NeckLength = ShoulderLength * NeckLengthMultiCurve.Evaluate(ShoulderLength) * NeckLengthRate;
                        Vector2 Ltilting = new Vector2(results[6].x - results[5].x, results[6].y - results[5].y);//ShoulderTilting
                        Vector2 Lpts = new Vector2(-1 * Ltilting.y, Ltilting.x);//Perpendicular To ShoulderTilting

                        if (Lpts.y > 0)//아래방향일때 반전
                            Lpts.y *= -1;

                        GL.Begin(GL.LINES);
                        Vector3 min = corners[0];
                        Vector3 max = corners[2];
                        GL.Color(Color.red);

                        GL.Vertex(Leap3(min, max, new Vector3(bodyInfos[1].Position.x, 1f - bodyInfos[1].Position.y, 0)));//GL.Vertex(bodyInfos[1].Position);
                        var ForwardNose = (Lpts.normalized * (NeckLength) + bodyInfos[1].Position);
                        GL.Vertex(Leap3(min, max, new Vector3(ForwardNose.x, 1f - ForwardNose.y, 0)));
                        //GL.Vertex(Leap3(min, max, new Vector3(bodyInfos[0].Position.x, 1f - bodyInfos[0].Position.y, 0)));

                        GL.End();
                    }

                }//Head Yaw => 어깨 중심.X - 코.X 

                bodyInfos[0].RotationOffset.x = Mathf.Clamp(bodyInfos[0].RotationOffset.x, -1.33f, 1.33f);
                bodyInfos[0].RotationOffset.z = Mathf.Clamp(bodyInfos[0].RotationOffset.z, -1.33f, 1.33f);

                PredictedIKs.FaceRotation = Quaternion.Euler(bodyInfos[0].RotationOffset * 45);

                PredictedIKs.BodyRotation = new Vector3(0, 0, bodyInfos[1].RotationOffset.z * 45);
            }//Calculate On Visible
            else if (bodyInfos[1].IsVisible)
            {
                float ShoulderLength = Mathf.Abs(Vector2.Distance(ToVector2(6), ToVector2(5)) * 0.5f);
                float NeckLength = ShoulderLength * NeckLengthMultiCurve.Evaluate(ShoulderLength) * NeckLengthRate;
                Vector2 Ltilting = new Vector2(results[6].x - results[5].x, results[6].y - results[5].y);//ShoulderTilting
                Vector2 Lpts = new Vector2(-1 * Ltilting.y, Ltilting.x);//Perpendicular To ShoulderTilting

                //어깨 기울기에 따라 머리 기울기 (Lpts.x * NeckLength)
                // y - 180 / z - Lpts.x / x - 0
                bodyInfos[0].RotationOffset.x = 0;
                bodyInfos[0].RotationOffset.y = 0;
                bodyInfos[0].RotationOffset.z = Mathf.Clamp((Lpts.x * NeckLength * 45 * FaceSensitive.z * 0.5f), -60, 60);

                PredictedIKs.FaceRotation = Quaternion.Euler(bodyInfos[0].RotationOffset);

                PredictedIKs.BodyRotation = new Vector3(0, 180, bodyInfos[1].RotationOffset.z * 45 * -0.5f);
                //Yaw는 값변경은 앞뒤 돌때만 / 뒤돌땐 기울기 방향 반전 / 머리와 어깨 기울기를 나눠서
            }
            else
            {
                bodyInfos[0].RotationOffset = Vector3.zero;
                PredictedIKs.FaceRotation = Quaternion.identity;
                PredictedIKs.BodyRotation = Vector3.zero;

            }//안보일때
        }//Set FaceRotation , BodyRotation

        {
            if (bodyInfos[0].IsVisible && bodyInfos[1].IsVisible)
            {
                PredictedIKs.BodyPosition = (bodyInfos[1].Position - Vector2.one * 0.5f) * gameObject.transform.localScale * new Vector2(1, -1);
            }
            else
                PredictedIKs.BodyPosition = Vector3.zero;
        }//BodyPosition

        {
            if (bodyInfos[0].IsVisible || bodyInfos[1].IsVisible)
            {
                PredictedIKs.BodyPosition += OriginPosition;

                PredictedIKs.BodyPosition.z += (1 - (0.52f / Mathf.Abs(results[5].x - results[6].x))) * gameObject.transform.localScale.z;

                if (bodyInfos[0].IsVisible && !DebugHiddenFace)
                {
                    //PredictedIKs.BodyPosition.z += ((0.12f / Mathf.Abs(results[1].x - results[2].x)) - 1) * MovementMultiply.z;
                }
                else
                {
                    //PredictedIKs.BodyPosition.z += ((0.52f / Mathf.Abs(results[5].x - results[6].x)) - 1) * MovementMultiply.z;
                }
                //Eye : 0.12 / Shoulder : 0.52
            }//Caculate PredictedIKs.BodyPosition.z InVisible 
            else
            {
                PredictedIKs.BodyPosition = OriginPosition;
            }
        }//BodyPosition.z 

        {
            if (results[7].confidence >= threshold)
            {
                PredictedIKs.L_ElbowPosition = ((ToVector2(7) - ToVector2(5)) / 0.4f) * new Vector2(1, -1);
                PredictedIKs.L_ElbowPosition.z = Mathf.Sqrt(Mathf.Abs(Mathf.Pow(PredictedIKs.L_ElbowPosition.x, 2) + Mathf.Pow(PredictedIKs.L_ElbowPosition.y, 2) - 1));
                PredictedIKs.L_ElbowPosition *= 0.5f;

                if (results[9].confidence >= threshold)
                {
                    //팔꿈치 [-0.5f ~ 0.5f] , 손 [-1 ~ 1] => 팔꿈치 0.5f > 손 [0 ~ 1]

                    PredictedIKs.L_WristPosition = ((ToVector2(9) - ToVector2(5)) / 0.8f) * new Vector2(1, -1);                  
                    PredictedIKs.L_WristPosition.z = 1 - (Mathf.Clamp01((ToVector2(7) - ToVector2(5)).magnitude) / 0.4f);//손목과 팔꿈치가 거리가 가까울수록 Z축 증가(팔 앞으로)

                }
                else
                {
                    PredictedIKs.L_WristPosition = PredictedIKs.L_ElbowPosition * 2;
                }
            }
            else
            {
                PredictedIKs.L_ElbowPosition = ArmPositionToInvisible * 0.5f;
                PredictedIKs.L_WristPosition = ArmPositionToInvisible;
            }
                

            if (results[8].confidence >= threshold)
            {
                PredictedIKs.R_ElbowPosition = ((ToVector2(8) - ToVector2(6)) / 0.4f) * new Vector2(-1, -1);
                PredictedIKs.R_ElbowPosition.z = Mathf.Sqrt(Mathf.Abs(Mathf.Pow(PredictedIKs.R_ElbowPosition.x, 2) + Mathf.Pow(PredictedIKs.R_ElbowPosition.y, 2) - 1));
                PredictedIKs.R_ElbowPosition *= 0.5f;

                //손좌표 예측
                if (results[10].confidence >= threshold)
                {
                    PredictedIKs.R_WristPosition = ((ToVector2(10) - ToVector2(6)) / 0.8f) * new Vector2(-1, -1);               
                    PredictedIKs.R_WristPosition.z = 1 - (Mathf.Clamp01((ToVector2(8) - ToVector2(6)).magnitude / 0.4f));//손목과 팔꿈치가 거리가 가까울수록 Z축 증가(팔 앞으로)

                }
                else
                {
                    PredictedIKs.R_WristPosition = PredictedIKs.R_ElbowPosition *2;
                }
            }
            else
            {
                PredictedIKs.R_ElbowPosition = ArmPositionToInvisible * 0.5f;
                PredictedIKs.R_WristPosition = ArmPositionToInvisible;
            }

        }//Arm / UpperArm : 0.4 / LowerArm : 0.4??

        {
            if (results[11].confidence >= threshold && results[12].confidence >= threshold)
            {
                //PredictedIKs.HipPosition = (((ToVector2(7) + ToVector2(5)) * 0.5f).x / ((ToVector2(12) + ToVector2(11)) * 0.5f).x) * Vector3.right;
                PredictedIKs.HipPosition = (((results[12].x + results[11].x) * 0.5f) - ((results[6].x + results[5].x) * 0.5f)) * Vector3.right * 2.5f;
                //어깨 중심과 엉덩이 중심 x 축 차이 / 엉덩이 x축 길이
                //hip 회전시키고 다리를 역방향으로 회전 + object이동

                //PredictedIKs.L_KneeRotation = Quaternion.Euler(PredictedIKs.HipPosition * -45);
                //PredictedIKs.R_KneeRotation = Quaternion.Euler(PredictedIKs.HipPosition * -45);
            }
            else
            {
                PredictedIKs.HipPosition = Vector3.zero;
            }
        }//Hip

        {
            if (results[13].confidence >= threshold)
            {

            }else
            {

            }
        }//Leg / UpperLeg : 0.65?
    }
    */
    #endregion

    IEnumerator PredictionLoop()
    {
        while(EnableCoroutine)
        {
            poseNet.Invoke(webcamTexture);
            results = poseNet.GetResults();

            // set uv
            if (DebugMode)
                cameraView.material = poseNet.transformMat;


            {
                if (DebugMode)
                {

                    //var rect = cameraView.GetComponent<RectTransform>();
                    //rect.GetWorldCorners(corners);
                    Vector3 min = corners[0];
                    Vector3 max = corners[2];

                    GL.Begin(GL.LINES);

                    GL.Color(Color.green);
                    var connections = PoseNet.Connections;
                    int len = connections.GetLength(0);
                    for (int i = 0; i < len; i++)
                    {
                        var a = results[(int)connections[i, 0]];
                        var b = results[(int)connections[i, 1]];
                        if (a.confidence >= threshold && b.confidence >= threshold)
                        {
                            GL.Vertex(Leap3(min, max, new Vector3(a.x, 1f - a.y, 0)));
                            GL.Vertex(Leap3(min, max, new Vector3(b.x, 1f - b.y, 0)));
                        }
                    }

                    GL.End();

                }//Sample Code - Draw Line

                for (int i = 0; i < results.Length; i++)//순서가 항상 같으니 
                {
                    //머리 기준 : 코 / 상체 기준 : 양쪽 어깨 중심 / 하체 기준 : 양쪽 엉덩이 중심
                    //기준 좌표 와 TillingOffset(왼 > 기준 > 오른쪽 과의 높이차이 평균) 을 저장
                    //코와 어깨중심간 Y축거리로 Roll(X축회전) , TillingOffset으로 Pitch(정면축회전 - Z) , 어깨중심 기준 좌표계로 코와 어깨중심간 X축거리 Yaw(위쪽축회전 - Y)

                    bodyInfos[0] = new BodyInfo(BodySlot.Head, threshold, results[0], results[1], results[2], FaceSensitive.z);//중심 : 코 , 좌우 : 귀
                    bodyInfos[1] = new BodyInfo(BodySlot.UpperBody, threshold, results[5], results[6], 3.8f);
                    bodyInfos[2] = new BodyInfo(BodySlot.LowerBody, threshold, results[11], results[12]);
                }

                if (FocusInputBlock)
                {
                    PredictedIKs.FaceRotation = Quaternion.identity;
                    PredictedIKs.BodyRotation = Vector3.zero;
                    PredictedIKs.BodyPosition = OriginPosition;

                    PredictedIKs.L_ElbowPosition = ArmPositionToInvisible * 0.5f;
                    PredictedIKs.L_WristPosition = ArmPositionToInvisible;

                    PredictedIKs.R_ElbowPosition = ArmPositionToInvisible * 0.5f;
                    PredictedIKs.R_WristPosition = ArmPositionToInvisible;

                }
                else
                {

                    {
                        float NoseLength = Vector2.Distance(bodyInfos[0].Position, ToVector2(1));

                        if (bodyInfos[0].IsVisible && !DebugHiddenFace)
                        {
                            //bodyInfos[0].RotationOffset.x = (NeckLength) - Vector2.Distance(bodyInfos[0].Position, bodyInfos[1].Position);//코와 어깨중심간 Y축거리로 Roll(X축회전)
                            bodyInfos[0].RotationOffset.x = ((bodyInfos[0].Position.y - VisibleEarsAverage().y + NoseOffset) / NoseLength) * FaceSensitive.x;
                            //보이는 귀 좌표 평균내서  코.y - 귀평균.y 으로 바꾸기

                            {
                                //어깨 중심 - 오른어깨 의 수직방향 * NeckLength => Yaw가 0 일때 코 위치  ==> 어깨 중심.X - 코.X 으로 바꿈

                                //bodyInfos[0].RotationOffset.y = (bodyInfos[0].Position.x - (Lpts.normalized * (NeckLength) + bodyInfos[1].Position).x) * FaceSensitive.y;
                                bodyInfos[0].RotationOffset.y = (bodyInfos[0].Position.x - bodyInfos[1].Position.x) * FaceSensitive.y;

                                if (DebugMode)
                                {
                                    float ShoulderLength = Mathf.Abs(Vector2.Distance(ToVector2(6), ToVector2(5)) * 0.5f);
                                    float NeckLength = ShoulderLength * NeckLengthMultiCurve.Evaluate(ShoulderLength) * NeckLengthRate;
                                    Vector2 Ltilting = new Vector2(results[6].x - results[5].x, results[6].y - results[5].y);//ShoulderTilting
                                    Vector2 Lpts = new Vector2(-1 * Ltilting.y, Ltilting.x);//Perpendicular To ShoulderTilting

                                    if (Lpts.y > 0)//아래방향일때 반전
                                        Lpts.y *= -1;

                                    GL.Begin(GL.LINES);
                                    Vector3 min = corners[0];
                                    Vector3 max = corners[2];
                                    GL.Color(Color.red);

                                    GL.Vertex(Leap3(min, max, new Vector3(bodyInfos[1].Position.x, 1f - bodyInfos[1].Position.y, 0)));//GL.Vertex(bodyInfos[1].Position);
                                    var ForwardNose = (Lpts.normalized * (NeckLength) + bodyInfos[1].Position);
                                    GL.Vertex(Leap3(min, max, new Vector3(ForwardNose.x, 1f - ForwardNose.y, 0)));
                                    //GL.Vertex(Leap3(min, max, new Vector3(bodyInfos[0].Position.x, 1f - bodyInfos[0].Position.y, 0)));

                                    GL.End();
                                }

                            }//Head Yaw => 어깨 중심.X - 코.X 

                            bodyInfos[0].RotationOffset.x = Mathf.Clamp(bodyInfos[0].RotationOffset.x, -1.33f, 1.33f);
                            bodyInfos[0].RotationOffset.z = Mathf.Clamp(bodyInfos[0].RotationOffset.z, -1.33f, 1.33f);

                            PredictedIKs.FaceRotation = Quaternion.Euler(bodyInfos[0].RotationOffset * 45);

                            PredictedIKs.BodyRotation = new Vector3(0, 0, bodyInfos[1].RotationOffset.z * 45);
                        }//Calculate On Visible
                        else if (bodyInfos[1].IsVisible)
                        {
                            float ShoulderLength = Mathf.Abs(Vector2.Distance(ToVector2(6), ToVector2(5)) * 0.5f);
                            float NeckLength = ShoulderLength * NeckLengthMultiCurve.Evaluate(ShoulderLength) * NeckLengthRate;
                            Vector2 Ltilting = new Vector2(results[6].x - results[5].x, results[6].y - results[5].y);//ShoulderTilting
                            Vector2 Lpts = new Vector2(-1 * Ltilting.y, Ltilting.x);//Perpendicular To ShoulderTilting

                            //어깨 기울기에 따라 머리 기울기 (Lpts.x * NeckLength)
                            // y - 180 / z - Lpts.x / x - 0
                            bodyInfos[0].RotationOffset.x = 0;
                            bodyInfos[0].RotationOffset.y = 0;
                            bodyInfos[0].RotationOffset.z = Mathf.Clamp((Lpts.x * NeckLength * 45 * FaceSensitive.z * 0.5f), -60, 60);

                            PredictedIKs.FaceRotation = Quaternion.Euler(bodyInfos[0].RotationOffset);

                            PredictedIKs.BodyRotation = new Vector3(0, 180, bodyInfos[1].RotationOffset.z * 45 * -0.5f);
                            //Yaw는 값변경은 앞뒤 돌때만 / 뒤돌땐 기울기 방향 반전 / 머리와 어깨 기울기를 나눠서
                        }
                        else
                        {
                            bodyInfos[0].RotationOffset = Vector3.zero;
                            PredictedIKs.FaceRotation = Quaternion.identity;
                            PredictedIKs.BodyRotation = Vector3.zero;

                        }//안보일때
                    }//Set FaceRotation , BodyRotation

                    {
                        if (bodyInfos[0].IsVisible && bodyInfos[1].IsVisible)
                        {
                            PredictedIKs.BodyPosition = (bodyInfos[1].Position - Vector2.one * 0.5f) * gameObject.transform.localScale * new Vector2(1, -1);
                        }
                        else
                            PredictedIKs.BodyPosition = Vector3.zero;
                    }//BodyPosition

                    {
                        if (bodyInfos[0].IsVisible || bodyInfos[1].IsVisible)
                        {
                            PredictedIKs.BodyPosition.z += (1 - (0.52f / Mathf.Abs(results[5].x - results[6].x))) * gameObject.transform.localScale.z;

                            PredictedIKs.BodyPosition = Multiply(PredictedIKs.BodyPosition, MovementScaleOffset);

                            PredictedIKs.BodyPosition += OriginPosition;

                            if (bodyInfos[0].IsVisible && !DebugHiddenFace)
                            {
                                //PredictedIKs.BodyPosition.z += ((0.12f / Mathf.Abs(results[1].x - results[2].x)) - 1) * MovementMultiply.z;
                            }
                            else
                            {
                                //PredictedIKs.BodyPosition.z += ((0.52f / Mathf.Abs(results[5].x - results[6].x)) - 1) * MovementMultiply.z;
                            }
                            //Eye : 0.12 / Shoulder : 0.52
                        }//Caculate PredictedIKs.BodyPosition.z InVisible 
                        else
                        {
                            PredictedIKs.BodyPosition = OriginPosition;
                        }
                    }//BodyPosition.z 

                    {
                        if (results[7].confidence >= threshold)
                        {
                            PredictedIKs.L_ElbowPosition = ((ToVector2(7) - ToVector2(5)) / 0.4f) * new Vector2(1, -1);
                            PredictedIKs.L_ElbowPosition.z = Mathf.Sqrt(Mathf.Abs(Mathf.Pow(PredictedIKs.L_ElbowPosition.x, 2) + Mathf.Pow(PredictedIKs.L_ElbowPosition.y, 2) - 1));
                            PredictedIKs.L_ElbowPosition *= 0.5f;

                            if (results[9].confidence >= threshold)
                            {
                                //팔꿈치 [-0.5f ~ 0.5f] , 손 [-1 ~ 1] => 팔꿈치 0.5f > 손 [0 ~ 1]

                                PredictedIKs.L_WristPosition = ((ToVector2(9) - ToVector2(5)) / 0.8f) * new Vector2(1, -1);
                                PredictedIKs.L_WristPosition.z = 1 - (Mathf.Clamp01((ToVector2(7) - ToVector2(5)).magnitude) / 0.4f);//손목과 팔꿈치가 거리가 가까울수록 Z축 증가(팔 앞으로)

                            }
                            else
                            {
                                PredictedIKs.L_WristPosition = PredictedIKs.L_ElbowPosition * 2;
                            }
                        }
                        else
                        {
                            PredictedIKs.L_ElbowPosition = ArmPositionToInvisible * 0.5f;
                            PredictedIKs.L_WristPosition = ArmPositionToInvisible;
                        }


                        if (results[8].confidence >= threshold)
                        {
                            PredictedIKs.R_ElbowPosition = ((ToVector2(8) - ToVector2(6)) / 0.4f) * new Vector2(-1, -1);
                            PredictedIKs.R_ElbowPosition.z = Mathf.Sqrt(Mathf.Abs(Mathf.Pow(PredictedIKs.R_ElbowPosition.x, 2) + Mathf.Pow(PredictedIKs.R_ElbowPosition.y, 2) - 1));
                            PredictedIKs.R_ElbowPosition *= 0.5f;

                            //손좌표 예측
                            if (results[10].confidence >= threshold)
                            {
                                PredictedIKs.R_WristPosition = ((ToVector2(10) - ToVector2(6)) / 0.8f) * new Vector2(-1, -1);
                                PredictedIKs.R_WristPosition.z = 1 - (Mathf.Clamp01((ToVector2(8) - ToVector2(6)).magnitude / 0.4f));//손목과 팔꿈치가 거리가 가까울수록 Z축 증가(팔 앞으로)

                            }
                            else
                            {
                                PredictedIKs.R_WristPosition = PredictedIKs.R_ElbowPosition * 2;
                            }
                        }
                        else
                        {
                            PredictedIKs.R_ElbowPosition = ArmPositionToInvisible * 0.5f;
                            PredictedIKs.R_WristPosition = ArmPositionToInvisible;
                        }

                    }//Arm / UpperArm : 0.4 / LowerArm : 0.4??

                    {
                        if (results[11].confidence >= threshold && results[12].confidence >= threshold)
                        {
                            //PredictedIKs.HipPosition = (((ToVector2(7) + ToVector2(5)) * 0.5f).x / ((ToVector2(12) + ToVector2(11)) * 0.5f).x) * Vector3.right;
                            PredictedIKs.HipPosition = (((results[12].x + results[11].x) * 0.5f) - ((results[6].x + results[5].x) * 0.5f)) * Vector3.right * 2.5f;
                            //어깨 중심과 엉덩이 중심 x 축 차이 / 엉덩이 x축 길이
                            //hip 회전시키고 다리를 역방향으로 회전 + object이동

                            //PredictedIKs.L_KneeRotation = Quaternion.Euler(PredictedIKs.HipPosition * -45);
                            //PredictedIKs.R_KneeRotation = Quaternion.Euler(PredictedIKs.HipPosition * -45);
                        }
                        else
                        {
                            PredictedIKs.HipPosition = Vector3.zero;
                        }
                    }//Hip

                    {
                        if (results[13].confidence >= threshold)
                        {

                        }
                        else
                        {

                        }
                    }//Leg / UpperLeg : 0.65?
                }
            }//Prediction

            yield return PredictionDelay > 0 ? new WaitForSeconds(PredictionDelay) : null;
        }
    }

    static Vector3 Leap3(in Vector3 a, in Vector3 b, in Vector3 t)
    {
        return new Vector3(
            Mathf.Lerp(a.x, b.x, t.x),
            Mathf.Lerp(a.y, b.y, t.y),
            Mathf.Lerp(a.z, b.z, t.z)
        );
    }
    Vector2 ToVector2(int ResultsIndex)
    {
        return new Vector2(results[ResultsIndex].x, results[ResultsIndex].y);
    }

    Vector2 VisibleEarsAverage()
    {
        Vector2 result = Vector2.zero;

        if (results[3].confidence >= threshold)
        {
            result += new Vector2(results[3].x, results[3].y);
        }
        if (results[4].confidence >= threshold)
        {
            result += new Vector2(results[4].x, results[4].y);
        }

        if (results[3].confidence >= threshold && results[4].confidence >= threshold)
            result *= 0.5f;

        return result;
    }
    Vector2 FarestEarPosition()
    {
        if (results[3].confidence >= threshold && results[4].confidence >= threshold)
        {
            if (Vector2.SqrMagnitude(ToVector2(0) - ToVector2(3)) > Vector2.SqrMagnitude(ToVector2(0) - ToVector2(4)))
            {
                return ToVector2(3);
            }
            else
            {
                return ToVector2(4);
            }
        }else
        {
            if (results[3].confidence >= threshold)
                return ToVector2(3);
            else if (results[4].confidence >= threshold)
                return ToVector2(4);
            else
                return ToVector2(0);
        }
    }

    Vector3 Multiply(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }
}
