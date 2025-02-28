﻿using PicView.Animations;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using PicView.Views.UserControls.Gallery;
using System.Windows;
using System.Windows.Media;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.UC;

namespace PicView.PicGallery
{
    internal static class GalleryNavigation
    {
        #region int calculations

        internal static void SetSize(double numberOfItems)
        {
            PicGalleryItemSize = WindowSizing.MonitorInfo.WorkArea.Width / numberOfItems;

            PicGalleryItemSizeS = PicGalleryItemSize - 15;
        }

        internal static double PicGalleryItemSize { get; private set; }
        internal static double PicGalleryItemSizeS { get; private set; }

        internal const int ScrollbarSize = 22;

        internal static int HorizontalItems
        {
            get
            {
                if (GetPicGallery == null || PicGalleryItemSize == 0)
                {
                    return 0;
                }

                return (int)Math.Floor(GetPicGallery.Width / PicGalleryItemSize);
            }
        }

        internal static int VerticalItems
        {
            get
            {
                if (GetPicGallery == null || PicGalleryItemSize == 0)
                {
                    return 0;
                }

                return (int)Math.Floor((ConfigureWindows.GetMainWindow.ParentContainer.ActualHeight -
                                        GetPicGallery.Container.Margin.Top) / PicGalleryItemSize);
            }
        }

        internal static int ItemsPerPage
        {
            get
            {
                if (GetPicGallery == null)
                {
                    return 0;
                }

                return (int)Math.Floor(HorizontalItems * GetPicGallery.Height / PicGalleryItemSize);
            }
        }

        internal static double CenterScrollPosition
        {
            get
            {
                if (GetPicGallery == null || PicGalleryItemSize <= 0)
                {
                    return 0;
                }

                if (GetPicGallery.Container.Children.Count <= SelectedGalleryItem)
                {
                    return 0;
                }

                var selectedScrollTo = GetPicGallery.Container.Children[SelectedGalleryItem]
                    .TranslatePoint(new Point(), GetPicGallery.Container);

                // ReSharper disable once PossibleLossOfFraction
                return selectedScrollTo.X - (HorizontalItems + 1) / 2 * PicGalleryItemSize +
                       PicGalleryItemSizeS / 2; // Scroll to overlap half of item
            }
        }

        #endregion int calculations

        #region ScrollToGalleryCenter

        /// <summary>
        /// Scrolls to center of current item
        /// </summary>
        internal static void ScrollToGalleryCenter()
        {
            var centerScrollPosition = CenterScrollPosition;
            if (centerScrollPosition == 0)
            {
                return;
            }

            GetPicGallery.Scroller.ScrollToHorizontalOffset(centerScrollPosition);
        }

        /// <summary>
        /// Scrolls the gallery horizontally based on the specified parameters.
        /// </summary>
        /// <param name="next">Specifies whether to scroll to the next or the previous page.</param>
        /// <param name="end">Specifies whether to scroll to the end of the gallery.</param>
        /// <param name="speedUp">Specifies whether to scroll at a faster speed.</param>
        /// <param name="animate">Specifies whether to animate the scrolling.</param>
        internal static void ScrollGallery(bool next, bool end, bool speedUp, bool animate)
        {
            GetPicGallery.Scroller.CanContentScroll = !animate; // Base animations on CanContentScroll

            if (end)
            {
                if (next)
                {
                    GetPicGallery.Scroller.ScrollToRightEnd();
                }
                else
                {
                    GetPicGallery.Scroller.ScrollToLeftEnd();
                }
            }
            else
            {
                if (animate)
                {
                    var speed = speedUp
                        ? PicGalleryItemSize * HorizontalItems * 0.8
                        : PicGalleryItemSize * HorizontalItems / 1.2;
                    var offset = next ? -speed : speed;

                    var newOffset = GetPicGallery.Scroller.HorizontalOffset + offset;

                    GetPicGallery.Scroller.ScrollToHorizontalOffset(newOffset);
                }
                else
                {
                    var speed = speedUp
                        ? PicGalleryItemSize * HorizontalItems * 1.2
                        : PicGalleryItemSize * HorizontalItems * 0.3;
                    var offset = next ? -speed : speed;

                    var newOffset = GetPicGallery.Scroller.HorizontalOffset + offset;

                    GetPicGallery.Scroller.ScrollToHorizontalOffset(newOffset);
                }
            }
        }

        #endregion ScrollToGalleryCenter

        #region Select and deselect behaviour

        /// <summary>
        /// Select and deselect PicGalleryItem
        /// </summary>
        /// <param name="x">location</param>
        /// <param name="selected">selected or deselected</param>
        internal static void SetSelected(int x, bool selected, bool navigate = false)
        {
            if (GetPicGallery is not null && x > GetPicGallery.Container.Children.Count - 1 || x < 0)
            {
                return;
            }

            // Select next item
            var nextItem = GetPicGallery.Container.Children[x] as PicGalleryItem;

            if (selected)
            {
                nextItem.InnerBorder.BorderBrush = Application.Current.Resources["ChosenColorBrush"] as SolidColorBrush;
                if (GalleryFunctions.IsGalleryOpen && navigate)
                {
                    AnimationHelper.SizeAnim(nextItem, false, PicGalleryItemSizeS, PicGalleryItemSize);
                }
                else
                {
                    nextItem.InnerBorder.Width = nextItem.InnerBorder.Height = PicGalleryItemSize;
                }
            }
            else
            {
                nextItem.InnerBorder.BorderBrush = Application.Current.Resources["BorderBrush"] as SolidColorBrush;
                if (GalleryFunctions.IsGalleryOpen && navigate)
                {
                    AnimationHelper.SizeAnim(nextItem, true, PicGalleryItemSize, PicGalleryItemSizeS);
                }
                else
                {
                    nextItem.InnerBorder.Width = nextItem.InnerBorder.Height =
                        Settings.Default.IsBottomGalleryShown && !GalleryFunctions.IsGalleryOpen
                            ? PicGalleryItemSize
                            : PicGalleryItemSizeS;
                }
            }
        }

        #endregion Select and deselect behaviour

        #region Gallery Navigation

        internal enum Direction
        {
            Up,
            Down,
            Left,
            Right
        }

        internal static int SelectedGalleryItem { get; set; }

        internal static void NavigateGallery(Direction direction)
        {
            var backup = SelectedGalleryItem;

            switch (direction)
            {
                case Direction.Up:
                    SelectedGalleryItem--;
                    break;

                case Direction.Down:
                    SelectedGalleryItem++;
                    break;

                case Direction.Left:
                    SelectedGalleryItem -= VerticalItems;
                    break;

                case Direction.Right:
                    SelectedGalleryItem += VerticalItems;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }

            if (SelectedGalleryItem >= Pics.Count - 1)
            {
                SelectedGalleryItem = Pics.Count - 1;
            }

            if (SelectedGalleryItem < 0)
            {
                SelectedGalleryItem = 0;
            }

            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() => { SetSelected(SelectedGalleryItem, true, true); });

            if (backup != SelectedGalleryItem && backup != FolderIndex)
            {
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
                {
                    SetSelected(backup, false, true); // deselect
                });
            }

            if (direction is Direction.Up or Direction.Down)
            {
                return;
            }

            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
            {
                // Keep item in center of ScrollViewer
                GetPicGallery.Scroller.ScrollToHorizontalOffset(CenterScrollPosition);
            });
        }

        #endregion Gallery Navigation
    }
}