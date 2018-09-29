using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;

public class CroppedScaledRegion {
    public readonly int Width;
    public readonly int Height;
    public readonly Box RegionOfInterest;
    public readonly float TargetWidth;
    public readonly float TargetHeight;

    public Box TransformedRegionOfInterest {
        get {
            var p1_prime = Transform(RegionOfInterest.P1);
            var p2_prime = Transform(RegionOfInterest.P2);

            return new Box(p1_prime, p2_prime);
        }
    }

    private readonly Vector2 InverseTranslation;
    private readonly Vector2 InverseScaler;

    public CroppedScaledRegion(int width, int height, Box regionOfInterest, float targetWidth, float targetHeight) {
        var p1 = regionOfInterest.P1;
        var p2 = regionOfInterest.P2;

        Width = width;
        Height = height;
        RegionOfInterest = regionOfInterest;
        TargetWidth = targetWidth;
        TargetHeight = targetHeight;

        InverseScaler = (p2 - p1) / new Vector2(TargetWidth, TargetHeight);
        InverseTranslation = p1;
    }

    public Vector2 Transform(Vector2 p) {
        return (p - InverseTranslation) / InverseScaler;
    }

    public Box Transform(Box b) {
        return new Box(Transform(b.P1), Transform(b.P2));
    }

    public Vector2 InverseTransform(Vector2 pprime) {
        return pprime * InverseScaler + InverseTranslation;
    }

    public Box InverseTransform(Box box) {
        return new Box(InverseTransform(box.P1), InverseTransform(box.P2));
    }
}