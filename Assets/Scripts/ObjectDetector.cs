using System;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity;
using UnityEngine;
using TensorFlowLite;
using RSG;
using System.Threading;
using System.Linq;
using System.Collections.Concurrent;

public class ObjectDetector : MonoBehaviour, IDetector<COCOCategories> {
    public const int MAX_NUM_DETECTIONS = 10;
    public int Size = 300;

    public SizedRegion FrameRegion {
        get {
            return new SizedRegion(Size, Size);
        }
    }

    private readonly int NumCocoCategories = Enum.GetNames(typeof(COCOCategories)).Length;

    public TextAsset Model;

    private struct DetectionTask {
        public int FrameId;
        public byte[] Data;
        public Promise<Dictionary<COCOCategories, List<DetectResult<COCOCategories>>>> ResultsPromise;
    }

    private BlockingCollection<DetectionTask> TaskPipeline = new BlockingCollection<DetectionTask>(new ConcurrentQueue<DetectionTask>());
    private Thread Worker;
    private volatile bool StopWork = false;

    private Interpreter Interpreter;

    public void Awake() {
        EasyThreading.EnsureInstance();
    }

    public void Start() {
        Interpreter = new Interpreter(Model.bytes);

        int numThreads = 1;
        switch (SystemInfo.processorCount) {
            case 1:
            case 2:
            case 3:
                numThreads = 1;
                break;

            default:
                numThreads = SystemInfo.processorCount - 2;
                break;
        }

        //Interpreter.SetNumThreads(numThreads);
        
        // Using the NNAPI seems to cause a crash. The neural net most likely has some operations that
        // are not supported by this API
        //Interpreter.UseNNAPI(true);
        
        Interpreter.ResizeInputTensor(0, new int[] { 1, Size, Size, 3 });
        Interpreter.AllocateTensors();
        Worker = new Thread(DoWork);
        Worker.Start();
    }

    public void OnDestroy() {
        StopWork = true;
    }

    public Promise<Dictionary<COCOCategories, List<DetectResult<COCOCategories>>>> Detect(Mat rgbImg) {
        if (rgbImg.width() != Size || rgbImg.height() != Size) {
            Debug.Log("Input to SSDQuantized300x300Detector had an invalid size: " + rgbImg.width() + ", " + rgbImg.height());
            throw new ArgumentException("Input to SSDQuantized300x300Detector had an invalid size");
        }
        byte[] rawData = new byte[Size * Size * 3];
        rgbImg.get(0, 0, rawData);
        var promise = new Promise<Dictionary<COCOCategories, List<DetectResult<COCOCategories>>>>();

        var task = new DetectionTask { FrameId = Time.frameCount, Data = rawData, ResultsPromise = promise };
        TaskPipeline.Add(task);

        return promise;
    }

    private void DoWork() {
        while (!StopWork) {
            var task = TaskPipeline.Take();
            //Debug.Log("Received work task");
            var rgbImg = task.Data;

            //Debug.Log("Setting input tensor");
            Interpreter.SetInputTensorData(0, rgbImg);
            //Debug.Log("Invoking neural network");
            Interpreter.Invoke();

            float[,,] outputLocations = new float[1, MAX_NUM_DETECTIONS, 4];
            float[,] outputClasses = new float[1, MAX_NUM_DETECTIONS];
            float[,] outputScores = new float[1, MAX_NUM_DETECTIONS];
            float[] numDetections = new float[1];

            //Debug.Log("Getting output data");
            Interpreter.GetOutputTensorData(0, outputLocations);
            Interpreter.GetOutputTensorData(1, outputClasses);
            Interpreter.GetOutputTensorData(2, outputScores);
            Interpreter.GetOutputTensorData(3, numDetections);
            
            var CategoryPartition = new Dictionary<COCOCategories, List<DetectResult<COCOCategories>>>();

            for (int i = 0; i < MAX_NUM_DETECTIONS; i++) {
                float left =   Mathf.Clamp01(outputLocations[0, i, 1]) * (Size - 1);
                float top =    Mathf.Clamp01(outputLocations[0, i, 0]) * (Size - 1);
                float right =  Mathf.Clamp01(outputLocations[0, i, 3]) * (Size - 1);
                float bottom = Mathf.Clamp01(outputLocations[0, i, 2]) * (Size - 1);

                int detectedClassId = Mathf.RoundToInt(outputClasses[0, i] + 1.0f);
                float outputScore = Mathf.Clamp01(outputScores[0, i]);

                COCOCategories category;
                if (0 <= detectedClassId && detectedClassId < NumCocoCategories) {
                    category = (COCOCategories)detectedClassId;
                    if (!Enum.IsDefined(typeof(COCOCategories), category)) {
                        category = COCOCategories.background;
                    }
                } else {
                    category = COCOCategories.background;
                }

                //Debug.Log("Detection of category " + category.ToString() + " found");

                // We don't care about background boxes
                if (category != COCOCategories.background) {
                    var result = new DetectResult<COCOCategories>(category, outputScore, left, bottom, top, right,
                                                                  FrameRegion, task.FrameId);
                    if (!CategoryPartition.ContainsKey(category)) {
                        CategoryPartition[category] = new List<DetectResult<COCOCategories>>() { result };
                    } else {
                        CategoryPartition[category].Add(result);
                    }
                }
            }
            
            //Debug.Log("Detection complete, now dispatching to main thread");
            EasyThreading.Dispatch(() => {
                //Debug.Log("Resolving results promise");
                task.ResultsPromise.Resolve(CategoryPartition);
            });
        }
    }
}
