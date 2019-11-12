﻿using System.Collections.Generic;

namespace InkRecognizerExplorer
{
    public class InkRecognitionUnit
    {
        public IList<Alternate> alternates { get; set; }
        public BoundingRectangle boundingRectangle { get; set; }
        public string category { get; set; }
        public int id { get; set; }
        public int parentId { get; set; }
        public string recognizedText { get; set; }
        public IList<RotatedBoundingRectangle> rotatedBoundingRectangle { get; set; }
        public IList<int> strokeIds { get; set; }
        public IList<int?> childIds { get; set; }
        public Center center { get; set; }
        public int? confidence { get; set; }
        public IList<Point> points { get; set; }
        public string recognizedObject { get; set; }
        public int? rotationAngle { get; set; }
    }

    public class Alternate
    {
        public string category { get; set; }
        public string recognizedString { get; set; }
    }

    public class BoundingRectangle
    {
        public double height { get; set; }
        public double topX { get; set; }
        public double topY { get; set; }
        public double width { get; set; }
    }

    public class RotatedBoundingRectangle
    {
        public double x { get; set; }
        public double y { get; set; }
    }

    public class Center
    {
        public double x { get; set; }
        public double y { get; set; }
    }

    public class Point
    {
        public double x { get; set; }
        public double y { get; set; }
    }
}
