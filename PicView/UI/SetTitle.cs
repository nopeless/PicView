﻿using System.IO;
using System.Text;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.FileFunctions;
using static PicView.Library.Fields;
using static PicView.UI.TransformImage.ZoomLogic;

namespace PicView.UI
{
    internal static class SetTitle
    {
        /// <summary>
        /// Returns string with file name, folder position,
        /// zoom, aspect ratio, resolution and file size
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static string[] TitleString(int width, int height, int index)
        {
            var s1 = new StringBuilder(90);
            s1.Append(Path.GetFileName(Pics[index])).Append(" ").Append(index + 1).Append("/").Append(Pics.Count).Append(" files")
                    .Append(" (").Append(width).Append(" x ").Append(height).Append(StringAspect(width, height)).Append(GetSizeReadable(new FileInfo(Pics[index]).Length));

            if (!string.IsNullOrEmpty(ZoomPercentage))
            {
                s1.Append(", ").Append(ZoomPercentage);
            }

            s1.Append(" - ").Append(AppName);

            var array = new string[3];
            array[0] = s1.ToString();
            s1.Remove(s1.Length - (AppName.Length + 3), AppName.Length + 3);   // Remove AppName + " - "
            array[1] = s1.ToString();
            s1.Replace(Path.GetFileName(Pics[index]), Pics[index]);
            array[2] = s1.ToString();
            return array;
        }

        /// <summary>
        /// Sets title string with file name, folder position,
        /// zoom, aspect ratio, resolution and file size
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        internal static void SetTitleString(int width, int height, int index)
        {
            var titleString = TitleString(width, height, index);
            TheMainWindow.Title = titleString[0];
            TheMainWindow.Bar.Text = titleString[1];
            TheMainWindow.Bar.ToolTip = titleString[2];
        }

        /// <summary>
        /// Returns string with file name,
        /// zoom, aspect ratio and resolution
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string[] TitleString(int width, int height, string path)
        {
            var s1 = new StringBuilder();
            s1.Append(path).Append(" (").Append(width).Append(" x ").Append(height).Append(StringAspect(width, height));

            if (!string.IsNullOrEmpty(ZoomPercentage))
            {
                s1.Append(", ").Append(ZoomPercentage);
            }

            s1.Append(" - ").Append(AppName);

            var array = new string[2];
            array[0] = s1.ToString();
            s1.Remove(s1.Length - (AppName.Length + 3), AppName.Length + 3);   // Remove AppName + " - "
            array[1] = s1.ToString();
            return array;
        }

        /// <summary>
        /// Sets title string with file name,
        /// zoom, aspect ratio and resolution
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static void SetTitleString(int width, int height, string path)
        {
            var titleString = TitleString(width, height, path);
            TheMainWindow.Title = titleString[0];
            TheMainWindow.Bar.Text = titleString[1];
            TheMainWindow.Bar.ToolTip = titleString[1];
        }

        /// <summary>
        /// Use name from title bar to set title string with,
        /// zoom, aspect ratio and resolution
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        internal static void SetTitleString(int width, int height)
        {
            string path = Library.Utilities.GetURL(TheMainWindow.Bar.Text);

            path = string.IsNullOrWhiteSpace(path) ? "Custom image" : path;

            var titleString = TitleString(width, height, path);
            TheMainWindow.Title = titleString[0];
            TheMainWindow.Bar.Text = titleString[1];
            TheMainWindow.Bar.ToolTip = titleString[1];
        }




    }
}