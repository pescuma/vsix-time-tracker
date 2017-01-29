using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VSIXTimeTracker.WPF
{
// ReSharper disable once RedundantExtendsListEntry
	public partial class DonutChart : UserControl
	{
		public static DependencyProperty InnerRadiusPercentageProperty = DependencyProperty.Register("InnerRadiusPercentage",
			typeof(double), typeof(DonutChart),
			new FrameworkPropertyMetadata(default(double),
				FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

		public double InnerRadiusPercentage
		{
			get { return (double) GetValue(InnerRadiusPercentageProperty); }
			set
			{
				SetValue(InnerRadiusPercentageProperty, value);
				UpdateSliceValues();
			}
		}

		public static DependencyProperty StrokeProperty = DependencyProperty.Register("Stroke", typeof(Brush),
			typeof(DonutChart),
			new FrameworkPropertyMetadata(default(Brush),
				FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

		public Brush Stroke
		{
			get { return (Brush) GetValue(StrokeProperty); }
			set
			{
				SetValue(StrokeProperty, value);
				UpdateSliceColors();
			}
		}

		public static DependencyProperty StrokeThicknessProperty = DependencyProperty.Register("StrokeThickness",
			typeof(double), typeof(DonutChart),
			new FrameworkPropertyMetadata(default(double),
				FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

		public double StrokeThickness
		{
			get { return (double) GetValue(StrokeThicknessProperty); }
			set
			{
				SetValue(StrokeThicknessProperty, value);
				UpdateSliceColors();
			}
		}

		public static DependencyProperty SeriesProperty = DependencyProperty.Register("Series", typeof(List<Serie>),
			typeof(DonutChart),
			new FrameworkPropertyMetadata(new List<Serie>(),
				FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

		public List<Serie> Series
		{
			get { return (List<Serie>) GetValue(SeriesProperty); }
			set
			{
				SetValue(SeriesProperty, value);
				RecreateSlices();
			}
		}

		public DonutChart()
		{
			InitializeComponent();
			SnapsToDevicePixels = true;
		}

		public void UpdateValues(double[] vals)
		{
			List<Serie> series = Series;

			if (vals.Length != series.Count)
				throw new ArgumentException();

			for (var i = 0; i < vals.Length; i++)
				series[i].Value = vals[i];

			UpdateSliceValues();
		}

		public void UpdateFills(Brush[] fills)
		{
			List<Serie> series = Series;

			if (fills.Length != series.Count)
				throw new ArgumentException();

			for (var i = 0; i < fills.Length; i++)
				series[i].Fill = fills[i];

			UpdateSliceColors();
		}

		private void RecreateSlices()
		{
			Canvas.Children.Clear();

			List<Serie> series = Series;

			if (!series.Any())
				return;

			for (var i = 0; i < series.Count; i++)
				Canvas.Children.Add(new DounutSlice());

			UpdateSliceColors();
			UpdateSliceValues();
		}

		private void UpdateSliceColors()
		{
			List<Serie> series = Series;

			for (var i = 0; i < series.Count; i++)
			{
				Serie serie = series[i];
				var slice = (DounutSlice) Canvas.Children[i];

				slice.Stroke = Stroke;
				slice.StrokeThickness = StrokeThickness;
				slice.Fill = serie.Fill;
				slice.SnapsToDevicePixels = true;
			}
		}

		private void UpdateSliceValues()
		{
			List<Serie> series = Series;

			if (!series.Any())
				return;

			double innerWidth = Width - Padding.Left - Padding.Right;
			double innerHeight = Height - Padding.Top - Padding.Bottom;

			var center = new Point(innerWidth / 2, innerHeight / 2);

			double size = Math.Min(innerWidth, innerHeight);
			double outerRadius = size / 2;
			double innerRadius = InnerRadiusPercentage * outerRadius;

			double total = series.Sum(s => s.Value);
			double currentAngle = -90;

			for (var i = 0; i < series.Count; i++)
			{
				Serie serie = series[i];
				var slice = (DounutSlice) Canvas.Children[i];

				double nextAngle = currentAngle + serie.Value * 360 / total;
				if (double.IsNaN(nextAngle) || double.IsInfinity(nextAngle))
					nextAngle = currentAngle;

				slice.Center = center;
				slice.OuterRadius = outerRadius;
				slice.InnerRadius = innerRadius;
				slice.StartAngle = currentAngle;
				slice.EndAngle = nextAngle;

				currentAngle = nextAngle;
			}
		}
	}
}