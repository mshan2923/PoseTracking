using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NatSuite.ML;
using NatSuite.ML.Features;
using NatSuite.ML.Vision;
using NatSuite.ML.Visualizers;
using UnityEngine.UI;
using NatSuite.ML.Hub;
using UnityEditor;

public class Predictor : MonoBehaviour//=================MoveNetVisualizer 참고해서 부모클래스로 사전설정하기 / tf lite 프로젝트 처럼
{
    public enum PosePart
    {
        Nose, LeftEye, RightEye, LeftEar, RightEar,
        LeftShoulder, RightShoulder, LeftElbow, RightElbow, LeftWrist, RightWrist,
        LeftHip, RightHip, LeftKnee, RightKnee, LeftAnkle, RightAnkle
    }
    [System.Serializable]
    public class DetectInfo
    {
        public PosePart Part;
        public Vector2 Position;
        public float Confidence;

        public DetectInfo(PosePart part, Vector2 pos = new())
        {
            Part = part;
            Position = pos;
        }

        public void Set(PosePart part)
        {
            Part = part;
        }
        public void Set(Vector3 Pos)
        {
            Position = new Vector2(Pos.x, Pos.y);
            Confidence = Pos.z;
        }
    }

    [Header(@"NatML")]
    public string accessKey = "oGbyql8YU_s_omed4-mJT";

    [Header(@"Tracking")]

    public int WebcamIndex = 0;
    WebCamTexture webcamTexture;
    public RectTransform MainCanvas;
    public RawImage cameraView = null;

    public bool smoothing;

    [Header(@"UI")]
    public MoveNetVisualizer visualizer;

    public bool Active = true;
    public float Aspect = 1.333f;

    [Range(0, 1)]
    public float LoopDelay = 0.1f;

    MLModelData modelData;
    MLModel model;
    MoveNetPredictor predictor;
    MLImageFeature ImageFeature;

    //public MLModelData Test;
    //https://docs.microsoft.com/ko-kr/windows/ai/windows-ml/tutorials/tensorflow-convert-model
    //이걸로 tflite 파일을 onnx를 변환해 위도우에서 사용가능 , tflite은 안드만 가능 하다고 ㅁ
    [Space(10), Header(@"Data"), Range(0f, 1f)]
    public float threshold = 0.3f;
    public DetectInfo[] results = new DetectInfo[17];

    [Space(10), Header(@"Debug")]
    public bool ActiveDebug = true;
    public GameObject DebugObj;
    public Map<PosePart, GameObject> DebugPoints = new();
    [Range(0f, 100f)]
    public float DebugZPos = 10f;
    public float TestPlaneDis = 100;



    private void OnEnable()
    {
        Camera.main.gameObject.transform.position = Vector3.zero;
        MainCanvas.GetComponent<Canvas>().planeDistance = 100;
    }

    async void Start()
    {
        // Fetch model data from NatML
        Debug.Log("Fetching model data from NatML... \n Download data is Once time");

        {
            string cameraName = WebCamTexture.devices[WebcamIndex].name;
            webcamTexture = new WebCamTexture(cameraName, 640, 480);
            webcamTexture.requestedFPS = 30;

            webcamTexture.Play();

            if (cameraView != null)
                cameraView.texture = webcamTexture;

            for (int i = 0; i < 17; i++)
            {
                results[i] = new DetectInfo((PosePart)i, Vector3.zero);
            }

            Aspect = webcamTexture.texelSize.y / webcamTexture.texelSize.x;
        }//Before Start Tracking Do CameraSetup , result Setup

        modelData = await MLModelData.FromHub("@natsuite/movenet", accessKey);//한번만 다운됨 (이후에도 계속됨)

        // Create MoveNet predictor
        model = modelData.Deserialize();
        predictor = new MoveNetPredictor(model, smoothing);


        //DetectLoop();
        StartCoroutine(DetectCoroutine());
    }

    // Update is called once per frame
    void Update()
    {

    }

    async void DetectLoop()
    {
        Texture2D imageTexture = null;
        while (Active)
        {
            if (webcamTexture != null)
            {

                ImageFeature = new MLImageFeature(webcamTexture.GetPixels32(), 640, 480);
                (ImageFeature.mean, ImageFeature.std) = modelData.normalization;

                // Detect pose
                var pose = predictor.Predict(ImageFeature);
                // Vsialize
                imageTexture = ImageFeature.ToTexture(imageTexture);
                visualizer.Render(imageTexture, pose);

            }

            await System.Threading.Tasks.Task.Yield();//Wait Next Frame
        }
    }//FPS : 20

    IEnumerator DetectCoroutine()
    {
        Texture2D imageTexture = null;

        while (Active)
        {
            if (webcamTexture != null)
            {

                ImageFeature = new MLImageFeature(webcamTexture.GetPixels32(), 640, 480);
                (ImageFeature.mean, ImageFeature.std) = modelData.normalization;

                // Detect pose
                var pose = predictor.Predict(ImageFeature);
                // Vsialize
                imageTexture = ImageFeature.ToTexture(imageTexture);
                visualizer.Render(imageTexture, pose);

                for (int i = 0; i < 17; i++)
                {
                    results[i].Set(pose[i]);
                }

                if (DebugObj != null)
                {
                    if (ActiveDebug)
                    {
                        for (int i = 0; i < 17; i++)
                        {
                            if (DebugObj != null)
                            {
                                if (DebugPoints.Count < 17)
                                {
                                    var obj = GameObject.Instantiate(DebugObj, gameObject.transform);
                                    obj.name = ((PosePart)i).ToString();
                                    DebugPoints.Add((PosePart)i, obj);
                                }
                                else if (DebugPoints.Count == 17)
                                {
                                    var obj = DebugPoints.GetVaule(i);
                                    obj.SetActive(pose[i].z > threshold);//신뢰값에 따라 보일지 안보일지

                                    //obj.transform.position = Vector2.Scale(MainCanvas.sizeDelta, (results[i].Position - Vector2.one * 0.5f)) * 0.1f;
                                    //Rect.NormalizedToPoint(MainCanvas.rect, results[i].Position) * 0.01f;

                                    obj.transform.position = RateToWorldPos(i , DebugZPos);//if Camera.pos is (0,0,0)
                                }
                            }
                        }
                    }
                    else
                    {
                        if (DebugPoints.Count > 0)
                        {
                            for (int i = 0; i < 17; i++)
                            {
                                Destroy(DebugPoints.GetVaule(i));
                            }
                            DebugPoints.Clear();
                        }
                    }
                }//DebugPoints
                
            }

            yield return Time.deltaTime > LoopDelay ? null : new WaitForSeconds(LoopDelay);//DeltaTime 보다 작은경우 Null
        }
    }// Delay : 0.1 , FPS : 25

    /// <summary>
    /// Z_Pos  : 100 is UI World Position / Canvas.planeDistnace default vaule is 100
    /// </summary>
    /// <param name="resultIndex"></param>
    /// <param name="Z_Pos"></param>
    /// <returns></returns>
    public Vector3 RateToWorldPos(int resultIndex, float Z_Pos = 10)
    {
        //Min : pointerSize * 0.5f / Max : (Canvas.Size.y * Vector2.one) - (pointerSize * 0.5f)

        //return (pointerSize * 0.5f + ((Canvas.sizeDelta.y * Vector2.one) - pointerSize) * rate) * new Vector2(1, -1);//피벗 : 좌상단
        //return (rate - Vector2.one * 0.5f) * MainCanvas.sizeDelta.y * new Vector2(1, -1);//Project : PoseTracker Vertion

        Vector3 pos = Vector2.Scale(MainCanvas.sizeDelta, (results[resultIndex].Position - (Vector2.one * 0.5f))) * 0.1f;
        pos = Vector3.Scale(pos, new Vector3(1, Aspect, 1));
        pos.z = MainCanvas.position.z;

        //if (Canvs.planeDistance != 100)
        //  pos = new Vector3(pos.x * Canvas.planeDistance / 100, pos.y * Canvas.planeDistance / 100, pos.z);

        return 0.01f * Z_Pos * pos;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Predictor.DetectInfo))]
public class DetectInfoEditor : PropertyDrawer
{
    Rect DrawRect;
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2;
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        //base.OnGUI(position, property, label);
        DrawRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        DrawRect = Expand.EditorExpand.RateRect(position, DrawRect, 0, 2);
        EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("Part"), GUIContent.none , true);

        DrawRect = Expand.EditorExpand.RateRect(position, DrawRect, 1, 2);
        EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("Position"), GUIContent.none, true);

        DrawRect = Expand.EditorExpand.NextLine(position, DrawRect, 10, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("Confidence"), true);
    }
}
#endif