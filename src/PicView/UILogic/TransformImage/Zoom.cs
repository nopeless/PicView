﻿using PicView.ChangeTitlebar;
using PicView.Properties;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static PicView.ChangeImage.Navigation;

namespace PicView.UILogic.TransformImage
{
    internal static class ZoomLogic
    {
        public static ScaleTransform? ScaleTransform;
        public static TranslateTransform? TranslateTransform;
        
        public static BitmapScalingMode DefaultScalingMode = (BitmapScalingMode) ConfigureWindows.GetMainWindow.MainImage.GetValue(RenderOptions.BitmapScalingModeProperty);
        private static Point _origin;
        private static Point _start;

        internal static double _zoomValue = 1;

        /// <summary>
        /// Used to determine final point when zooming,
        /// since DoubleAnimation changes value of TranslateTransform continuously.
        /// </summary>
        internal static double ZoomValue {
            get {
                return _zoomValue;
            }
            private set {
                _zoomValue = value;
                
                TriggerScalingModeUpdate();
            }
        }

        public static void TriggerScalingModeUpdate()
        {
            var scalingMode = _zoomValue >= 1 && Settings.Default.IsScalingSetToNearestNeighbor ? BitmapScalingMode.NearestNeighbor : DefaultScalingMode;

            ConfigureWindows.GetMainWindow.MainImage.SetValue(RenderOptions.BitmapScalingModeProperty, scalingMode);
        }

        /// <summary>
        /// Returns zoom percentage. if 100%, return empty string
        /// </summary>
        internal static string ZoomPercentage
        {
            get
            {
                if (ScaleTransform == null || ZoomValue is 1)
                {
                    return string.Empty;
                }

                var zoom = Math.Round(ZoomValue * 100);

                return zoom + "%";
            }
        }

        internal static bool IsZoomed
        {
            get
            {
                if (ScaleTransform is null)
                {
                    return false;
                }

                return ZoomValue is not 1;
            }
        }

        /// <summary>
        /// Returns aspect ratio as a formatted string
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        internal static string StringAspect(int width, int height)
        {
            if (width is 0 || height is 0)
                return ") ";

            var gcd = GCD(width, height);
            var x = width / gcd;
            var y = height / gcd;

            if (x > 48 || y > 18)
            {
                return ") ";
            }

            return $", {x} : {y}) ";
        }

        /// <summary>
        /// Greatest Common Divisor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        // ReSharper disable once InconsistentNaming
        internal static int GCD(int x, int y)
        {
            while (true)
            {
                if (y == 0) return x;
                var x1 = x;
                x = y;
                y = x1 % y;
            }
        }

        /// <summary>
        /// Manipulates the required elements to allow zooming
        /// by modifying ScaleTransform and TranslateTransform
        /// </summary>
        internal static void InitializeZoom()
        {
            // Initialize transforms
            ConfigureWindows.GetMainWindow.MainImageBorder.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
                {
                    new ScaleTransform(),
                    new TranslateTransform()
                }
            };

            ConfigureWindows.GetMainWindow.ParentContainer.ClipToBounds =
                ConfigureWindows.GetMainWindow.MainImageBorder.ClipToBounds = true;

            // Set transforms to UI elements
            ScaleTransform = (ScaleTransform)((TransformGroup)
                    ConfigureWindows.GetMainWindow.MainImageBorder.RenderTransform)
                .Children.First(tr => tr is ScaleTransform);

            TranslateTransform = (TranslateTransform)((TransformGroup)
                    ConfigureWindows.GetMainWindow.MainImageBorder.RenderTransform)
                .Children.First(tr => tr is TranslateTransform);
        }

        /// <summary>
        /// Prepares the image for panning by capturing the mouse position when the left mouse button is pressed.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
        // ReSharper disable once UnusedParameter.Global
        internal static void PreparePanImage(object sender, MouseButtonEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.IsActive == false)
            {
                return;
            }

            // Report position for image drag
            ConfigureWindows.GetMainWindow.MainImage.CaptureMouse();
            _start = e.GetPosition(ConfigureWindows.GetMainWindow.ParentContainer);
            _origin = new Point(TranslateTransform.X, TranslateTransform.Y);
        }

        /// <summary>
        /// Pans the image by modifying its X,Y coordinates, keeping it in bounds.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        // ReSharper disable once UnusedParameter.Global
        internal static void PanImage(object sender, MouseEventArgs e)
        {
            if (!ConfigureWindows.GetMainWindow.MainImage.IsMouseCaptured || !ConfigureWindows.GetMainWindow.IsActive ||
                ScaleTransform.ScaleX is 1)
            {
                return;
            }

            // Drag image by modifying X,Y coordinates
            var dragMousePosition = _start - e.GetPosition(ConfigureWindows.GetMainWindow);

            var newXproperty = _origin.X - dragMousePosition.X;
            var newYproperty = _origin.Y - dragMousePosition.Y;

            // Keep panning it in bounds
            if (Settings.Default.AutoFitWindow &&
                !Settings.Default
                    .Fullscreen) // TODO develop solution where you can keep window in bounds when using normal window behavior and fullscreen
            {
                var actualScrollWidth = ConfigureWindows.GetMainWindow.Scroller.ActualWidth;
                var actualBorderWidth = ConfigureWindows.GetMainWindow.MainImageBorder.ActualWidth;
                var actualScrollHeight = ConfigureWindows.GetMainWindow.Scroller.ActualHeight;
                var actualBorderHeight = ConfigureWindows.GetMainWindow.MainImageBorder.ActualHeight;

                var isXOutOfBorder = actualScrollWidth < actualBorderWidth * ScaleTransform.ScaleX;
                var isYOutOfBorder = actualScrollHeight < actualBorderHeight * ScaleTransform.ScaleY;
                var maxX = actualScrollWidth - actualBorderWidth * ScaleTransform.ScaleX;
                var maxY = actualScrollHeight - actualBorderHeight * ScaleTransform.ScaleY;

                if (isXOutOfBorder && newXproperty < maxX || isXOutOfBorder == false && newXproperty > maxX)
                {
                    newXproperty = maxX;
                }

                if (isXOutOfBorder && newYproperty < maxY || isXOutOfBorder == false && newYproperty > maxY)
                {
                    newYproperty = maxY;
                }

                if (isXOutOfBorder && newXproperty > 0 || isXOutOfBorder == false && newXproperty < 0)
                {
                    newXproperty = 0;
                }

                if (isYOutOfBorder && newYproperty > 0 || isYOutOfBorder == false && newYproperty < 0)
                {
                    newYproperty = 0;
                }
            }

            // TODO Don't pan image out of screen border
            TranslateTransform.X = newXproperty;
            TranslateTransform.Y = newYproperty;

            e.Handled = true;
        }

        /// <summary>
        /// Resets the zoom of the <see cref="ConfigureWindows.GetMainWindow.MainImage"/> to its original size.
        /// </summary>
        /// <param name="animate">Determines whether to animate the reset or not.</param>
        internal static void ResetZoom(bool animate = true)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Source == null
                || ScaleTransform == null
                || TranslateTransform == null)
            {
                return;
            }

            ZoomValue = 1;

            if (animate)
            {
                BeginZoomAnimation(1);
            }
            else
            {
                ScaleTransform.ScaleX = ScaleTransform.ScaleY = 1.0;
                TranslateTransform.X = TranslateTransform.Y = 0.0;
                return;
            }

            Tooltip.CloseToolTipMessage();

            // Display non-zoomed values
            if (Pics.Count == 0)
            {
                // Display values from web
                SetTitle.SetTitleString((int)ConfigureWindows.GetMainWindow.MainImage.Source.Width,
                    (int)ConfigureWindows.GetMainWindow.MainImage.Source.Height);
            }
            else
            {
                SetTitle.SetTitleString((int)ConfigureWindows.GetMainWindow.MainImage.Source.Width,
                    (int)ConfigureWindows.GetMainWindow.MainImage.Source.Height, FolderIndex, null);
            }
        }

        /// <summary>
        /// Zooms in or out the <see cref="ConfigureWindows.GetMainWindow.MainImage"/> by the given amount.
        /// </summary>
        /// <param name="isZoomIn">Determines whether to zoom in or out.</param>
        internal static void Zoom(bool isZoomIn)
        {
            // Disable zoom if cropping tool is active
            if (UC.GetCroppingTool != null && UC.GetCroppingTool.IsVisible)
            {
                return;
            }

            var currentZoom = ScaleTransform.ScaleX;
            var zoomSpeed = Settings.Default.ZoomSpeed;

            switch (currentZoom)
            {
                // Increase speed based on the current zoom level
                case > 14 when isZoomIn:
                    return;

                case > 4:
                    zoomSpeed += 1.5;
                    break;

                case > 3.2:
                    zoomSpeed += 1;
                    break;

                case > 1.6:
                    zoomSpeed += 0.5;
                    break;
            }

            if (!isZoomIn)
            {
                zoomSpeed = -zoomSpeed;
            }

            currentZoom += zoomSpeed;
            currentZoom = Math.Max(0.09, currentZoom);
            if (Settings.Default.AvoidZoomingOut && currentZoom < 1.0)
            {
                ResetZoom();
            }
            else
            {
                Zoom(currentZoom);
            }
        }

        /// <summary>
        /// Zooms the main image to the specified zoom value.
        /// </summary>
        /// <param name="value">The new zoom value.</param>
        private static void Zoom(double value)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Source is null)
                return;

            ZoomValue = value;

            BeginZoomAnimation(ZoomValue);

            // Displays zoom-percentage in the center window
            if (!string.IsNullOrEmpty(ZoomPercentage))
            {
                Tooltip.ShowTooltipMessage(ZoomPercentage, true);
            }
            else
            {
                Tooltip.CloseToolTipMessage();
            }

            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Normal, () =>
            {
                // Display updated values
                if (Pics.Count == 0)
                {
                    //  values from web
                    SetTitle.SetTitleString((int)ConfigureWindows.GetMainWindow.MainImage.Source.Width,
                        (int)ConfigureWindows.GetMainWindow.MainImage.Source.Height);
                }
                else
                {
                    SetTitle.SetTitleString((int)ConfigureWindows.GetMainWindow.MainImage.Source.Width,
                        (int)ConfigureWindows.GetMainWindow.MainImage.Source.Height, FolderIndex, null);
                }
            });
        }

        /// <summary>
        /// Begins the zoom animation for the main image.
        /// </summary>
        /// <param name="zoomValue">The zoom value to animate to.</param>
        private static void BeginZoomAnimation(double zoomValue)
        {
            var relative = Mouse.GetPosition(ConfigureWindows.GetMainWindow.MainImageBorder);

            // Calculate new position
            var absoluteX = relative.X * ScaleTransform.ScaleX + TranslateTransform.X;
            var absoluteY = relative.Y * ScaleTransform.ScaleY + TranslateTransform.Y;

            // Reset to zero if value is one, which is reset
            var newTranslateValueX = Math.Abs(zoomValue - 1) > .1 ? absoluteX - relative.X * zoomValue : 0;
            var newTranslateValueY = Math.Abs(zoomValue - 1) > .1 ? absoluteY - relative.Y * zoomValue : 0;

            var duration = new Duration(TimeSpan.FromSeconds(.25));

            var scaleAnim = new DoubleAnimation(zoomValue, duration)
            {
                // Set stop to make sure animation doesn't hold ownership of scale-transform
                FillBehavior = FillBehavior.Stop
            };

            // Set intended values after animations
            scaleAnim.Completed += delegate { ScaleTransform.ScaleX = ScaleTransform.ScaleY = zoomValue; };

            var translateAnimX = new DoubleAnimation(TranslateTransform.X, newTranslateValueX, duration)
            {
                // Set stop to make sure animation doesn't hold ownership of translateTransform
                FillBehavior = FillBehavior.Stop
            };

            translateAnimX.Completed += delegate { TranslateTransform.X = newTranslateValueX; };

            var translateAnimY = new DoubleAnimation(TranslateTransform.Y, newTranslateValueY, duration)
            {
                // Set stop to make sure animation doesn't hold ownership of translateTransform
                FillBehavior = FillBehavior.Stop
            };

            translateAnimY.Completed += delegate { TranslateTransform.Y = newTranslateValueY; };

            // Start animations

            ScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            ScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

            TranslateTransform.BeginAnimation(TranslateTransform.XProperty, translateAnimX);
            TranslateTransform.BeginAnimation(TranslateTransform.YProperty, translateAnimY);
        }
    }
}