# Object detection example for Unity

![Detection demo](https://raw.githubusercontent.com/calebh/ObjectDetectionExample/master/screenshot.png)

A demo app is availabe in the GitHub releases section: https://github.com/calebh/ObjectDetectionExample/releases

This project is a demonstration of using neural networks for object detection on Android in Unity. The detection runs in real time on a Pixel 2, which makes it suitable for augmented reality apps/games. One thing to note is that this network will consume 100% of the CPU and cause your phone to overheat. Dedicated neural network chips (like the one in the iPhone) should eliminate this problem in the future.

# License

The code is under the MIT License. If you used this code or found it helpful, please attribute the author - Caleb Helbling.

# How it works

Video feed from a phone camera is sent to a [SSD MobileNet](https://github.com/tensorflow/models/blob/master/research/object_detection/g3doc/detection_model_zoo.md), which is capable of detecting objects from many different categories. A full list of categories it can detect are given below. The object detection algorithm is too slow to run in realtime, so it is executed on a separate thread to prevent dropped frames.

The [TFLite Experimental plugin for Unity](https://github.com/tensorflow/tensorflow/tree/master/tensorflow/contrib/lite/experimental/examples/unity/TensorFlowLitePlugin) is used to run the MobileNet. The network can also be executed by OpenCV for Unity's DNN module. Currently this project only supports Android devices. iOS uses a separate deep neural network library called Metal, which should theoretically give good performance thanks to hardware acceleration.

The neural network input is 300x300 pixels. This means that the network will not be able to detect far away objects since they will be very tiny. With the appropriate cropping, it should be possible to detect more distant objects.

# Dependencies

This project was created in Unity 2018.1.6f1

[OpenCV for Unity](https://assetstore.unity.com/packages/tools/integration/opencv-for-unity-21088) - this library is required but is not available in this repo because it is a paid package. You'll need to import this package from the asset store.

[RSG.Promise](https://www.nuget.org/packages/RSG.Promise/) - this NuGet package can be installed via [NuGet for Unity](https://assetstore.unity.com/packages/tools/utilities/nuget-for-unity-104640)

[NatCam](https://assetstore.unity.com/packages/tools/integration/natcam-webcam-api-52154) could be used to improve camera frame acquisition performance, but it isn't currently integrated

NatCamWithOpenCVForUnityExample: https://github.com/EnoxSoftware/NatCamWithOpenCVForUnityExample/releases

# Supported categories

The network seems to be quite good at detecting people - probably because many of the training images (COCO Datset) had people.

background, person, bicycle, car, motorcycle, airplane, bus, train, truck, boat, traffic light, fire hydrant, stop sign, parking meter, bench, bird, cat, dog, horse, sheep, cow, elephant, bear, zebra, giraffe, backpack, umbrella, handbag, tie, suitcase, frisbee, skis, snowboard, sports ball, kite, baseball bat, baseball glove, skateboard, surfboard, tennis racket, bottle, wine glass, cup, fork, knife, spoon, bowl, banana, apple, sandwich, orange, broccoli, carrot, hot dog, pizza, donut, cake, chair, couch, potted plant, bed, dining table, toilet, tv, laptop, mouse, remote, keyboard, cell phone, microwave, oven, toaster, sink, refrigerator, book, clock, vase, scissors, teddy bear, hair drier, toothbrush