using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnityExample;
using RSG;
using System.Linq;
using System;

using OpenCVForUnity;
using System.Threading;

/// MobileNet SSD WebCamTexture Example
/// This example uses Single-Shot Detector (https://arxiv.org/abs/1512.02325) to detect objects in a WebCamTexture image.
/// Referring to https://github.com/opencv/opencv/blob/master/samples/dnn/mobilenet_ssd_python.py.
[RequireComponent(typeof(WebCamTextureToMatHelper))]
[RequireComponent(typeof(ObjectDetector))]
public class VCameraDetector : MonoBehaviour {
    public int DetectionFrameRate = 100;
    private float DetectionPeriod;
    
    private WebCamTextureToMatHelper VCameraHelper;

    private bool Initialized = false;

    // RGB camera matrix
    private Mat CameraMat;
    private Mat CroppedCameraMat;
    private Mat InputMat;

    private OpenCVForUnity.Rect CropRegionRect;
    private CroppedScaledRegion CropRegion;
    private int XStart;
    private int YStart;
    private int CropSize;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public SizedRegion SizedRegion {
        get {
            return new SizedRegion(Width, Height);
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    float rearCameraRequestedFPS;
#endif

    private ObjectDetector Detector;
    private bool DetectorReady = true;
    private float TimeSinceLastDetect = float.PositiveInfinity;
    public Dictionary<COCOCategories, List<DetectResult<COCOCategories>>> LastResults = new Dictionary<COCOCategories, List<DetectResult<COCOCategories>>>();

    public void Awake() {
        Detector = GetComponent<ObjectDetector>();
        VCameraHelper = GetComponent<WebCamTextureToMatHelper>();
    }

    public void Start() {
        VCameraHelper.onInitialized.AddListener(OnVCameraHelperInitialized);
        VCameraHelper.onDisposed.AddListener(OnVCameraHelperDisposed);
        //VCameraHelper.onErrorOccurred.AddListener(OnVCameraHelperErrorOccurred);

        DetectionPeriod = 1.0f / DetectionFrameRate;
        InputMat = new Mat(Detector.Size, Detector.Size, CvType.CV_8UC3);

        VCameraHelper.Initialize();
    }

    /// Raises the webcam texture to mat helper initialized event.
    public void OnVCameraHelperInitialized() {
        Width = VCameraHelper.GetWidth();
        Height = VCameraHelper.GetHeight();

        CameraMat = new Mat(Height, Width, CvType.CV_8UC3);

        int centerX = Width / 2;
        int centerY = Height / 2;

        //Debug.Log("ROWS: " + Height + ", COLS: " + Width);
        CropSize = Mathf.Min(Height, Width);
        //Debug.Log("Crop size: " + CropSize);
        XStart = Mathf.Max(0, centerX - CropSize / 2);
        YStart = Mathf.Max(0, centerY - CropSize / 2);
        //Debug.Log("XStart: " + XStart + ", YStart: " + YStart);
        CropRegionRect = new OpenCVForUnity.Rect(XStart, YStart, CropSize, CropSize);
        CroppedCameraMat = new Mat(CropSize, CropSize, CvType.CV_8UC3);
        CropRegion =
            new CroppedScaledRegion(
                Width, Height,
                new Box(XStart, YStart, XStart + CropSize, YStart + CropSize),
                Detector.Size, Detector.Size
            );
        
        Initialized = true;
    }

    /// Raises the webcam texture to mat helper disposed event.
    public void OnVCameraHelperDisposed() {
        if (CameraMat != null) {
            CameraMat.Dispose();
        }
    }

    /// Raises the webcam texture to mat helper error occurred event.
    public void OnVCameraHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode) {
        Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
    }

    private void DoDetection() {
        if (Initialized) {
            //Debug.Log("Doing detection");
            DetectorReady = false;
            TimeSinceLastDetect = 0.0f;

            Mat rgbaMat = VCameraHelper.GetMat();
            // Remove the alpha channel
            Imgproc.cvtColor(rgbaMat, CameraMat, Imgproc.COLOR_RGBA2RGB);
            
            //Debug.Log("Cropping image");
            // Crop the image to obtain a square shaped region
            Mat croppedRef = new Mat(CameraMat, CropRegionRect);
            croppedRef.copyTo(CroppedCameraMat);
            croppedRef.Dispose();

            //Debug.Log("Resizing image");
            // Resize the image to fit the detector
            Imgproc.resize(CroppedCameraMat, InputMat, new Size(Detector.Size, Detector.Size));

            int frameId = Time.frameCount;

            //Debug.Log("Sending detection request");
            // Start the detection
            Detector.Detect(InputMat).Done((categorizedBoxes) => {
                var newDict = new Dictionary<COCOCategories, List<DetectResult<COCOCategories>>>();
                foreach (KeyValuePair<COCOCategories, List<DetectResult<COCOCategories>>> categoryInfo in categorizedBoxes) {
                    var detections = new List<DetectResult<COCOCategories>>();
                    foreach (DetectResult<COCOCategories> detectResult in categoryInfo.Value) {
                        Box b = CropRegion.InverseTransform(detectResult.Box).Clamp(Width - 1, Height - 1);
                        var remappedDetectResult = new DetectResult<COCOCategories>(detectResult.Category, detectResult.Score, b, SizedRegion, frameId);

                        detections.Add(remappedDetectResult);
                    }
                    if (detections.Count > 0) {
                        newDict[categoryInfo.Key] = detections;
                    }
                }
                
                LastResults = newDict;
                DetectorReady = true;
            });
        }
    }

    public void Update() {
        TimeSinceLastDetect += Time.time;
        if (DetectorReady && TimeSinceLastDetect > DetectionPeriod) {
            DoDetection();
        }
    }

    public void OnDestroy() {
        VCameraHelper.Dispose();
        Utils.setDebugMode(false);

        VCameraHelper.onInitialized.RemoveListener(OnVCameraHelperInitialized);
        VCameraHelper.onDisposed.RemoveListener(OnVCameraHelperDisposed);
        //VCameraHelper.onErrorOccurred.RemoveListener(OnVCameraHelperErrorOccurred);
    }

    /// Raises the change camera button click event.
    public void OnChangeCameraButtonClick() {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!VCameraHelper.IsFrontFacing ()) {
            rearCameraRequestedFPS = VCameraHelper.requestedFPS;
            VCameraHelper.Initialize (!VCameraHelper.IsFrontFacing (), 15, VCameraHelper.rotate90Degree);
        } else {                
            VCameraHelper.Initialize (!VCameraHelper.IsFrontFacing (), rearCameraRequestedFPS, VCameraHelper.rotate90Degree);
        }
#else
        VCameraHelper.requestedIsFrontFacing = !VCameraHelper.IsFrontFacing();
#endif
    }
}
