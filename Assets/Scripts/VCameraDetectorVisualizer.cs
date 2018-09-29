using OpenCVForUnity;
using OpenCVForUnityExample;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(VCameraDetector))]
[RequireComponent(typeof(RawImage))]
[RequireComponent(typeof(AspectRatioFitter))]
public class VCameraDetectorVisualizer : MonoBehaviour {
    private VCameraDetector Detector;
    private WebCamTextureToMatHelper VCameraHelper;
    private RawImage Preview;

    private Texture2D DisplayTexture;
    private Mat DisplayMat;
    private AspectRatioFitter Fitter;

    public float scoreThreshold = 0.25f;

    public void Awake() {
        Detector = GetComponent<VCameraDetector>();
        VCameraHelper = GetComponent<WebCamTextureToMatHelper>();
        Preview = GetComponent<RawImage>();
        Fitter = GetComponent<AspectRatioFitter>();
    }

    public void Start() {
        VCameraHelper.onInitialized.AddListener(OnVCameraHelperInitialized);
        VCameraHelper.onDisposed.AddListener(OnVCameraHelperDisposed);
        //VCameraHelper.onErrorOccurred.AddListener(OnVCameraHelperErrorOccurred);
        Utils.setDebugMode(true, true);
    }

    public void OnDestroy() {
        VCameraHelper.Dispose();
        Utils.setDebugMode(false, false);

        VCameraHelper.onInitialized.RemoveListener(OnVCameraHelperInitialized);
        VCameraHelper.onDisposed.RemoveListener(OnVCameraHelperDisposed);
        //VCameraHelper.onErrorOccurred.RemoveListener(OnVCameraHelperErrorOccurred);
    }

    public void OnVCameraHelperInitialized() {
        Fitter.aspectRatio = ((float) VCameraHelper.GetWidth()) / VCameraHelper.GetHeight();

        DisplayMat = new Mat(VCameraHelper.GetWidth(), VCameraHelper.GetHeight(), CvType.CV_8UC4);
        DisplayTexture = new Texture2D(VCameraHelper.GetWidth(), VCameraHelper.GetHeight(), TextureFormat.RGBA32, false);
        Preview.texture = DisplayTexture;

        Debug.Log("Creating display texture with width " + VCameraHelper.GetWidth() + " and height " + VCameraHelper.GetHeight());
        //DisplayTexture = new Texture2D(VCameraHelper.width, VCameraHelper.height, TextureFormat.RGBA32, false);
        //gameObject.GetComponent<Renderer>().material.mainTexture = DisplayTexture;
    }

    public void OnVCameraHelperDisposed() {
        //Debug.Log("DISPOSING VISUALIZER MATRICES");
        if (DisplayMat != null) {
            DisplayMat.Dispose();
            DisplayMat = null;
        }

        if (DisplayTexture != null) {
            Destroy(DisplayTexture);
            DisplayTexture = null;
        }
    }
	
	public void Update() {
        if (DisplayMat != null && DisplayTexture != null) {
            VCameraHelper.GetMat().copyTo(DisplayMat);

            foreach (var category in Detector.LastResults) {
                foreach (var detection in category.Value) {
                    if (detection.Score >= scoreThreshold) {
                        var box = detection.Box.Clamp(DisplayMat.width() - 1, DisplayMat.height() - 1);

                        Imgproc.rectangle(DisplayMat, new Point(box.Left, box.Top),
                                            new Point(box.Right, box.Bottom), new Scalar(0, 255, 0, 255), 2);

                        string label = detection.Category + ": " + detection.Score;

                        int[] baseLine = new int[1];
                        Size labelSize = Imgproc.getTextSize(label, Core.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

                        Imgproc.rectangle(DisplayMat, new Point(box.Left, box.Top),
                            new Point(box.Left + labelSize.width, box.Top + labelSize.height + baseLine[0]),
                            new Scalar(255, 255, 255, 255), Core.FILLED);
                        Imgproc.putText(DisplayMat, label, new Point(box.Left, box.Top + labelSize.height),
                            Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(0, 0, 0, 255));
                    }
                }
            }

            Utils.fastMatToTexture2D(DisplayMat, DisplayTexture);
        }
    }
}
