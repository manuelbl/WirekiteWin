/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace Codecrete.Wirekite.Test.UI
{
    public class AnalogStick : Control
    {
        static AnalogStick()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AnalogStick), new FrameworkPropertyMetadata(typeof(AnalogStick)));
        }

        private static FrameworkPropertyMetadata xDirectionMetaData =
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, null, null);

        public static readonly DependencyProperty XDirectionProperty = DependencyProperty.Register("XDirection",
            typeof(double), typeof(AnalogStick), xDirectionMetaData);

        private static FrameworkPropertyMetadata yDirectionMetaData =
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, null, null);

        public static readonly DependencyProperty YDirectionProperty = DependencyProperty.Register("YDirection",
            typeof(double), typeof(AnalogStick), yDirectionMetaData);


        public double XDirection
        {
            get
            {
                return (double)GetValue(XDirectionProperty);
            }
            set
            {
                SetValue(XDirectionProperty, value);
            }
        }


        public double YDirection
        {
            get
            {
                return (double)GetValue(YDirectionProperty);
            }
            set
            {
                SetValue(YDirectionProperty, value);
            }
        }


        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            double diameter = Math.Min(ActualWidth, ActualHeight);

            Pen pen = new Pen(Foreground, 3.0);
            drawingContext.DrawEllipse(Brushes.Transparent, pen, new Point(ActualWidth / 2, ActualHeight / 2), diameter/2 - 2, diameter/2 - 2);

            pen = new Pen(Brushes.DarkBlue, 5.0)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };

            double angle = Math.Atan2(YDirection, XDirection);
            double maxLength = Math.Max(Math.Abs(Math.Sin(angle)), Math.Abs(Math.Cos(angle)));
            double x = ActualWidth / 2 + XDirection * maxLength * (diameter / 2 - 3);
            double y = ActualHeight / 2 - YDirection * maxLength * (diameter / 2 - 3);
            drawingContext.DrawLine(pen, new Point(ActualWidth / 2, ActualHeight / 2), new Point(x, y));
        }
    }
}
