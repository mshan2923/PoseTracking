﻿/* 
*   MoveNet
*   Copyright (c) 2022 NatML Inc. All Rights Reserved.
*/

namespace NatSuite.Examples {

    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.Video;
    using NatSuite.ML;
    using NatSuite.ML.Features;
    using NatSuite.ML.Vision;
    using NatSuite.ML.Visualizers;

    public class MoveNetSample : MonoBehaviour {

        [Header(@"NatML")]
        public string accessKey = "oGbyql8YU_s_omed4-mJT";

        [Header(@"Tracking")]
        public VideoClip videoClip;
        public bool smoothing;

        [Header(@"UI")]
        public MoveNetVisualizer visualizer;

        async void Start () {
            // Fetch model data from NatML
            Debug.Log("Fetching model data from NatML...");
            var modelData = await MLModelData.FromHub("@natsuite/movenet", accessKey);
            // Create MoveNet predictor
            using var model = modelData.Deserialize();
            using var predictor = new MoveNetPredictor(model, smoothing);
            // Load video
            var vidoeFeature = new MLVideoFeature(videoClip.originalPath);
            (vidoeFeature.mean, vidoeFeature.std) = modelData.normalization;
            Texture2D videoTexture = null;
            // Enumerate video frames // 비디오 프레임 나열
            foreach (var (imageFeature, timestamp) in vidoeFeature) {
                // Detect pose
                var pose = predictor.Predict(imageFeature);
                // Visualize
                videoTexture = imageFeature.ToTexture(videoTexture);
                visualizer.Render(videoTexture, pose);
                // Wait till the next frame
                await Task.Yield();
            }
        }
    }
}