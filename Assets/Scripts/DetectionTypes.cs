using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity;
using RSG;
using UnityEngine;

public struct Box {
    public float Left;
    public float Bottom;
    public float Top;
    public float Right;

    public float Width {
        get {
            return Right - Left;
        }
    }

    public float Height {
        get {
            return Bottom - Top;
        }
    }

    public Vector2 Center {
        get {
            return new Vector2((Left + Right) / 2.0f, (Top + Bottom) / 2.0f);
        }
    }

    public Vector2 Size {
        get {
            return new Vector2(Right - Left, Bottom - Top);
        }
    }

    public bool IsValid {
        get {
            return Left <= Right && Top <= Bottom;
        }
    }

    public Box(float l, float t, float r, float b) {
        Left = l;
        Bottom = b;
        Top = t;
        Right = r;
    }

    public Box(Vector2 p1, Vector2 p2) {
        Left = p1.x;
        Top = p1.y;
        Right = p2.x;
        Bottom = p2.y;
    }

    public Box(Rect2d rect) {
        Left = (float) rect.x;
        Top = (float)  rect.y;
        Right = (float) (rect.x + rect.width);
        Bottom = (float) (rect.y + rect.height);
    }

    public Box Clamp(int width, int height) {
        float maxX = width;
        float maxY = height;
        return new Box(Mathf.Clamp(Left, 0.0f, maxX), Mathf.Clamp(Top, 0.0f, maxY),
                       Mathf.Clamp(Right, 0.0f, maxX), Mathf.Clamp(Bottom, 0.0f, maxY));
    }

    public bool Contains(Vector2 p) {
        return Left <= p.x && p.x <= Right && Top <= p.y && p.y <= Bottom;
    }

    public float Area {
        get {
            return Mathf.Abs(Left - Right) * Mathf.Abs(Top - Bottom);
        }
    }

    public Vector2 P1 {
        get {
            return new Vector2(Left, Top);
        }
    }

    public Vector2 P2 {
        get {
            return new Vector2(Right, Bottom);
        }
    }

    public static Box? operator &(Box a, Box b) {
        float x1 = Mathf.Max(a.Left, b.Left);
        float y1 = Mathf.Max(a.Top, b.Top);

        float x2 = Mathf.Min(a.Right, b.Right);
        float y2 = Mathf.Min(a.Bottom, b.Bottom);

        float dx = x2 - x1;
        float dy = y2 - y1;
        
        if (dx >= 0.0f && dy >= 0.0f) {
            return new Box(x1, y1, x2, y2);
        } else {
            return null;
        }
    }

    public static explicit operator Rect2d(Box b) {
        return new Rect2d(new Point(b.Left, b.Top), new Point(b.Right, b.Bottom));
    }

    public static explicit operator Box(Rect2d r) {
        return new Box((float)r.x, (float)r.y, (float)(r.x + r.width), (float)(r.y + r.height));
    }

    public static Box operator +(Vector2 v, Box b) {
        return new Box(b.P1 + v, b.P2 + v);
    }

    public static Box operator +(Box b, Vector2 v) {
        return v + b;
    }

    public static Box operator -(Box b, Vector2 v) {
        return b + (-v);
    }

    public static Box operator *(Box b, float s) {
        Vector2 center = b.Center;
        Vector2 newHalfSize = (b.Size / 2.0f) * s;
        return new Box(center - newHalfSize, center + newHalfSize);
    }

    public static Box operator *(float s, Box b) {
        return b * s;
    }

    public override string ToString() {
        return Left + "," + Top + "," + Right + "," + Bottom;
    }
}

public interface IDetectResult {
    Box Box { get; }
    float Score { get; }
}

public struct DetectResult<T> : IDetectResult {
    public T Category;
    public float Score;
    public Box Box;
    public SizedRegion FrameSize;
    public readonly int FrameId;

    Box IDetectResult.Box {
        get {
            return Box;
        }
    }

    float IDetectResult.Score {
        get {
            return Score;
        }
    }

    public DetectResult(T category, float score, float left, float bottom, float top, float right,
                        SizedRegion frameSize, int frameId) {
        Category = category;
        Score = score;
        Box = new Box(left, top, right, bottom);
        FrameSize = frameSize;
        FrameId = frameId;
    }

    public DetectResult(T category, float score, Box box, SizedRegion frameSize, int frameId) {
        Category = category;
        Score = score;
        Box = box;
        FrameSize = frameSize;
        FrameId = frameId;
    }

    public override string ToString() {
        return "Category: " + Category + ", Score: " + Score + ", Box: " + Box + ", Frame size: " + FrameSize + ", Frame ID: " + FrameId;
    }
}

public interface IDetector<T> {
    Promise<Dictionary<COCOCategories, List<DetectResult<COCOCategories>>>> Detect(Mat rgbImage);
}

public struct SizedRegion {
    public readonly int Width;
    public readonly int Height;

    public SizedRegion(int width, int height) {
        Width = width;
        Height = height;
    }

    public Vector2 Transform(Vector2 p, int targetWidth, int targetHeight) {
        return (p / (new Vector2(Width - 1, Height - 1))) * new Vector2(targetWidth - 1, targetHeight - 1);
    }

    public Box Transform(Box b, int targetWidth, int targetHeight) {
        return new Box(Transform(b.P1, targetWidth - 1, targetHeight - 1), Transform(b.P2, targetWidth - 1, targetHeight - 1));
    }

    public Vector2 InverseTransform(Vector2 p, int fromWidth, int fromHeight) {
        return (p / (new Vector2(fromWidth - 1, fromHeight - 1))) * new Vector2(Width - 1, Height - 1);
    }

    public Vector2 Clamp(Vector2 p) {
        return new Vector2(Mathf.Clamp(p.x, 0.0f, Width - 1), Mathf.Clamp(p.y, 0.0f, Height - 1));
    }

    public Box Clamp(Box b) {
        return new Box(Clamp(b.P1), Clamp(b.P2));
    }

    public Vector2 Center {
        get {
            return new Vector2(Width / 2.0f, Height / 2.0f);
        }
    }

    public override string ToString() {
        return "W: " + Width + ", H: " + Height;
    }
}
