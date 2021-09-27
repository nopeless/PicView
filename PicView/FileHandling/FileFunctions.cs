﻿using PicView.UILogic;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PicView.FileHandling
{
    internal static class FileFunctions
    {

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern void SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr bindingContext, [Out] out IntPtr pidl, uint sfgaoIn, [Out] out uint psfgaoOut);

        public static void OpenFolderAndSelectItem(string folderPath, string file)
        {
            IntPtr nativeFolder;
            uint psfgaoOut;
            SHParseDisplayName(folderPath, IntPtr.Zero, out nativeFolder, 0, out psfgaoOut);

            if (nativeFolder == IntPtr.Zero)
            {
                // Log error, can't find folder
                return;
            }

            IntPtr nativeFile;
            SHParseDisplayName(Path.Combine(folderPath, file), IntPtr.Zero, out nativeFile, 0, out psfgaoOut);

            IntPtr[] fileArray;
            if (nativeFile == IntPtr.Zero)
            {
                // Open the folder without the file selected if we can't find the file
                fileArray = Array.Empty<IntPtr>();
            }
            else
            {
                fileArray = new IntPtr[] { nativeFile };
            }

            int v = SHOpenFolderAndSelectItems(nativeFolder, (uint)fileArray.Length, fileArray, 0);

            Marshal.FreeCoTaskMem(nativeFolder);
            if (nativeFile != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(nativeFile);
            }
        }

        /// <summary>
        /// Returns true if directory
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static bool CheckIfDirectoryOrFile(string path)
        {
            var getAttributes = File.GetAttributes(path);
            return getAttributes.HasFlag(FileAttributes.Directory);
        }

        internal static bool RenameFile(string path, string newPath)
        {
            try
            {
                new FileInfo(newPath).Directory.Create(); // create directory if not exists
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine(e.Message);
#endif
            }
            try
            {
                File.Move(path, newPath, true);
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine(e.Message);
#endif
                return false;
            }
            return true;
        }


        internal static async Task<bool> RenameFileWithErrorChecking(string newPath)
        {
            if (!FileFunctions.RenameFile(ChangeImage.Navigation.Pics[ChangeImage.Navigation.FolderIndex], newPath))
            {
                return false;
            }

            ChangeImage.Preloader.Remove(ChangeImage.Navigation.FolderIndex);
            ChangeImage.Navigation.Pics.Remove(ChangeImage.Navigation.Pics[ChangeImage.Navigation.FolderIndex]);

            // Check if the file is not in the same folder
            if (Path.GetDirectoryName(newPath) != Path.GetDirectoryName(ChangeImage.Navigation.Pics[ChangeImage.Navigation.FolderIndex]))
            {
                if (ChangeImage.Navigation.Pics.Count < 1)
                {
                    await ChangeImage.Navigation.LoadPiFromFileAsync(newPath).ConfigureAwait(false);
                }
                await ChangeImage.Navigation.PicAsync().ConfigureAwait(false);
                return true;
            }
            
            ChangeImage.Navigation.Pics.Add(newPath);
            await ChangeImage.Navigation.LoadPiFromFileAsync(newPath).ConfigureAwait(false);
            return true;
        }


        /// <summary>
        /// Returns the human-readable file size for an arbitrary, 64-bit file size
        /// The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
        /// </summary>
        /// <param name="i">FileInfo.Length</param>
        /// <returns></returns>
        /// Credits to http://www.somacon.com/p576.php
        internal static string GetSizeReadable(long i)
        {
            string sign = i < 0 ? "-" : string.Empty;
            char prefix;
            double value;

            if (i >= 0x40000000) // Gigabyte
            {
                prefix = 'G';
                value = i >> 20;
            }
            else if (i >= 0x100000) // Megabyte
            {
                prefix = 'M';
                value = i >> 10;
            }
            else if (i >= 0x400) // Kilobyte
            {
                prefix = 'K';
                value = i;
            }
            else
            {
                return i.ToString(sign + "0 B", CultureInfo.CurrentCulture); // Byte
            }
            value /= 1024;

            return sign + value.ToString("0.## ", CultureInfo.CurrentCulture) + prefix + 'B';
        }

        internal static bool FilePathHasInvalidChars(string path)
        {
            return !string.IsNullOrEmpty(path) && path.IndexOfAny(Path.GetInvalidPathChars()) >= 0;
        }

        internal static string MakeValidFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return Regex.Replace(name, invalidRegStr, "_");
        }

        internal static string Shorten(string name, int amount)
        {
            if (name.Length >= 25)
            {
                name = name.Substring(0, amount);
                name += "...";
            }
            return name;
        }

        internal static string GetDefaultExeConfigPath(ConfigurationUserLevel userLevel)
        {
            try
            {
                var UserConfig = ConfigurationManager.OpenExeConfiguration(userLevel);
                return UserConfig.FilePath;
            }
            catch (ConfigurationException e)
            {
                return e.Filename;
            }
        }

        internal static string GetWritingPath()
        {
            return Path.GetDirectoryName(GetDefaultExeConfigPath(ConfigurationUserLevel.PerUserRoamingAndLocal));
        }

        internal static string GetURL(string value)
        {
            try
            {
                var linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                return linkParser.Match(value).ToString();
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine(e.Message);
#endif
                return string.Empty;
            }
        }

    }
}