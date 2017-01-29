using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VSIXTimeTracker.WPF
{
	internal class DounutSlice : Shape
	{
		public static DependencyProperty InnerRadiusProperty = DependencyProperty.Register("InnerRadius", typeof(double),
			typeof(DounutSlice),
			new FrameworkPropertyMetadata(default(double),
				FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

		public double InnerRadius
		{
			get { return (double) GetValue(InnerRadiusProperty); }
			set { SetValue(InnerRadiusProperty, value); }
		}

		public static DependencyProperty OuterRadiusProperty = DependencyProperty.Register("OuterRadius", typeof(double),
			typeof(DounutSlice),
			new FrameworkPropertyMetadata(default(double),
				FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

		public double OuterRadius
		{
			get { return (double) GetValue(OuterRadiusProperty); }
			set { SetValue(OuterRadiusProperty, value); }
		}

		public static DependencyProperty StartAngleProperty = DependencyProperty.Register("StartAngle", typeof(double),
			typeof(DounutSlice),
			new FrameworkPropertyMetadata(default(double),
				FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

		public double StartAngle
		{
			get { return (double) GetValue(StartAngleProperty); }
			set { SetValue(StartAngleProperty, value); }
		}

		public static DependencyProperty EndAngleProperty = DependencyProperty.Register("EndAngle", typeof(double),
			typeof(DounutSlice),
			new FrameworkPropertyMetadata(default(double),
				FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

		public double EndAngle
		{
			get { return (double) GetValue(EndAngleProperty); }
			set { SetValue(EndAngleProperty, value); }
		}

		public static DependencyProperty CenterProperty = DependencyProperty.Register("Center", typeof(Point),
			typeof(DounutSlice),
			new FrameworkPropertyMetadata(default(Point),
				FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

		public Point Center
		{
			get { return (Point) GetValue(CenterProperty); }
			set { SetValue(CenterProperty, value); }
		}

		protected override Geometry DefiningGeometry
		{
			get
			{
				var geometry = new StreamGeometry();
				geometry.FillRule = FillRule.EvenOdd;

				using (StreamGeometryContext context = geometry.Open())
				{
					DrawGeometry(context);
				}

				geometry.Freeze();

				return geometry;
			}
		}

		private void DrawGeometry(StreamGeometryContext context)
		{
			bool large = Math.Abs(EndAngle - StartAngle) > 180.0;

			Point innerArcStartPoint = ToPoint(Center, InnerRadius, StartAngle);
			Point innerArcMiddlePoint = ToPoint(Center, InnerRadius, (StartAngle + EndAngle) / 2);
			Point innerArcEndPoint = ToPoint(Center, InnerRadius, EndAngle);

			Point outerArcStartPoint = ToPoint(Center, OuterRadius, StartAngle);
			Point outerArcMiddlePoint = ToPoint(Center, OuterRadius, (StartAngle + EndAngle) / 2);
			Point outerArcEndPoint = ToPoint(Center, OuterRadius, EndAngle);

			var outerArcSize = new Size(OuterRadius, OuterRadius);
			var innerArcSize = new Size(InnerRadius, InnerRadius);

			context.BeginFigure(innerArcStartPoint, true, true);
			context.LineTo(outerArcStartPoint, true, true);
			if (large)
				context.ArcTo(outerArcMiddlePoint, outerArcSize, 0, false, SweepDirection.Clockwise, true, true);
			context.ArcTo(outerArcEndPoint, outerArcSize, 0, false, SweepDirection.Clockwise, true, true);
			context.LineTo(innerArcEndPoint, true, true);
			if (large)
				context.ArcTo(innerArcMiddlePoint, innerArcSize, 0, false, SweepDirection.Counterclockwise, true, true);
			context.ArcTo(innerArcStartPoint, innerArcSize, 0, false, SweepDirection.Counterclockwise, true, true);
		}

		public static Point ToPoint(Point center, double radius, double angleDegrees)
		{
			double angleRad = angleDegrees * Math.PI / 180.0;

			double x = radius * Math.Cos(angleRad);
			double y = radius * Math.Sin(angleRad);

			return new Point(center.X + x, center.Y + y);
		}
	}
}