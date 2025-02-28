﻿using PicView.ChangeImage;
using PicView.ChangeTitlebar;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.ProcessHandling;
using PicView.UILogic;
using PicView.Views.UserControls.Gallery;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static PicView.ChangeImage.Navigation;
using static PicView.PicGallery.GalleryLoad;
using static PicView.UILogic.Tooltip;

namespace PicView.FileHandling
{
    internal static class CopyPaste
    {
        /// <summary>
        /// Duplicates the current file and handles naming collisions by appending a number inside parentheses.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task DuplicateFile()
        {
            if (ErrorHandling.CheckOutOfRange())
            {
                return;
            }

            await DuplicateFile(Pics[FolderIndex]).ConfigureAwait(false);
        }

        /// <summary>
        /// Duplicates the specified file and handles naming collisions by appending a number inside parentheses.
        /// </summary>
        /// <param name="currentFile">The path of the file to be duplicated.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static async Task DuplicateFile(string currentFile)
        {
            // Display it's loading to the user
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(SetTitle.SetLoadingString);

            try
            {
                string newFile =
                    await Task.Run(() =>
                    {
                        var dir = Path.GetDirectoryName(currentFile);
                        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(currentFile);
                        var extension = Path.GetExtension(currentFile);

                        int i = 1;

                        // Check if the original filename already contains parentheses
                        if (fileNameWithoutExtension.Contains("(") && fileNameWithoutExtension.EndsWith(")"))
                        {
                            // Extract the number from the existing parentheses
                            var lastParenIndex = fileNameWithoutExtension.LastIndexOf("(", StringComparison.Ordinal);
                            var numberStr = fileNameWithoutExtension.Substring(lastParenIndex + 1,
                                fileNameWithoutExtension.Length - lastParenIndex - 2);

                            if (int.TryParse(numberStr, out int existingNumber))
                            {
                                i = existingNumber + 1;
                                fileNameWithoutExtension = fileNameWithoutExtension[..lastParenIndex].TrimEnd();
                            }
                        }

                        // Generate a new filename with an incremented number inside parentheses
                        do
                        {
                            newFile = Path.Combine(dir, $"{fileNameWithoutExtension}({i++}){extension}");
                        } while (File.Exists(newFile));

                        // Copy the file to the new location
                        File.Copy(currentFile, newFile);
                        return newFile;
                    });

                // Add the new file to Pics and Gallery, clear Preloader to refresh cache
                var nextIndex = GetNextIndex(NavigateTo.Next, false);
                Pics.Insert(nextIndex, newFile);

                // Add next item to gallery if applicable
                if (UC.GetPicGallery is not null)
                {
                    var thumbData = await Task.FromResult(GalleryThumbHolder.GetThumbData(nextIndex)).ConfigureAwait(false);
                    await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                    {
                        var item = new PicGalleryItem(thumbData.BitmapSource, nextIndex, false);
                        UC.GetPicGallery.Container.Children.Insert(nextIndex, item);
                    });
                }

                var preloadValue = PreLoader.Get(FolderIndex);
                PreLoader.Clear();
                await PreLoader.AddAsync(FolderIndex, preloadValue.FileInfo, preloadValue.BitmapSource).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(DuplicateFile)} {currentFile} exception:\n{exception.Message}");
#endif
                ShowTooltipMessage(exception.Message);
            }
            finally
            {
                // Revert to the previous title since it's no longer loading
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(SetTitle.SetTitleString);
            }

            await PreLoader.PreLoadAsync(FolderIndex, Pics.Count).ConfigureAwait(false);
        }

        /// <summary>
        /// Copy image location to clipboard
        /// </summary>
        internal static void CopyFilePath()
        {
            Clipboard.SetText(Pics[FolderIndex]);
            ShowTooltipMessage(Application.Current.Resources["FileCopyPathMessage"] as string);
        }

        /// <summary>
        /// Add file to clipboard
        /// </summary>
        internal static void CopyFile()
        {
            if (Pics?.Count <= 0)
            {
                // Check if from URL and download it
                var url = FileFunctions.RetrieveFromURL();
                if (!string.IsNullOrEmpty(url))
                {
                    CopyFile(ArchiveExtraction.TempFilePath);
                }
                else
                {
                    CopyBitmap();
                }
            }
            else if (Pics?.Count > FolderIndex)
            {
                CopyFile(Pics[FolderIndex]);
            }
        }

        internal static void CopyFile(string path)
        {
            var paths = new StringCollection { path };
            Clipboard.SetFileDropList(paths);
            ShowTooltipMessage(Application.Current.Resources["FileCopy"], UC.FileMenuOpen);
        }

        internal static void CopyBitmap(int? id = null)
        {
            void Set(BitmapSource source)
            {
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    var bmp = ImageFunctions.BitmapSourceToBitmap(source);
                    ClipboardHelper.SetClipboardImage(bmp, bmp, null);
                    ShowTooltipMessage(Application.Current.Resources["CopiedImage"]);
                }));
            }

            if (id is null)
            {
                BitmapSource? pic = null;
                if (ConfigureWindows.GetMainWindow.MainImage.Source != null)
                {
                    if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
                    {
                        pic = ImageDecoder.GetRenderedBitmapFrame();
                    }
                    else
                    {
                        pic = (BitmapSource)ConfigureWindows.GetMainWindow.MainImage.Source;
                    }

                    if (pic == null)
                    {
                        ShowTooltipMessage(Application.Current.Resources["UnknownError"]);
                        return;
                    }
                }

                Set(pic);
                ShowTooltipMessage(Application.Current.Resources["CopiedImage"]);
            }
            else
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var preloadValue = PreLoader.Get(id.Value);
                        BitmapSource bitmap;
                        if (preloadValue is null)
                        {
                            bitmap = await ImageDecoder.ReturnBitmapSourceAsync(new FileInfo(Pics[id.Value]));
                        }
                        else
                        {
                            bitmap = preloadValue.BitmapSource ??
                                     await ImageDecoder.ReturnBitmapSourceAsync(new FileInfo(Pics[id.Value]));
                        }

                        try
                        {
                            Set(bitmap);
                        }
                        catch (Exception e)
                        {
                            ShowTooltipMessage(e.Message);
                        }

                        ShowTooltipMessage(Application.Current.Resources["CopiedImage"]);
                    }
                    catch (Exception e)
                    {
                        ShowTooltipMessage(e.Message);
                    }
                }).ConfigureAwait(false);
            }
        }

        internal static void Copy()
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Source == null) return;

            if (ErrorHandling.CheckOutOfRange())
            {
                CopyBitmap();
            }
            else
            {
                if (ConfigureWindows.GetMainWindow.MainImage.Effect is not null)
                {
                    CopyBitmap();
                }
                else
                {
                    CopyFile(Pics[FolderIndex]);
                }
            }
        }

        /// <summary>
        /// Retrieves the data from the clipboard and attempts to load image, if possible
        /// </summary>
        internal static async Task PasteAsync()
        {
            if (Clipboard.ContainsFileDropList()) // file
            {
                var files = Clipboard.GetFileDropList().Cast<string>().ToArray();

                if (files == null)
                {
                    return;
                }

                await LoadPic.LoadPicFromStringAsync(files[0]).ConfigureAwait(false);

                for (var i = 1; i < files.Length; i++) // If Clipboard has more files
                {
                    ProcessLogic.StartProcessWithFileArgument(files[i]);
                }
            }
            else if (Clipboard.ContainsImage()) // Clipboard Image
            {
                await UpdateImage
                    .UpdateImageAsync((string)Application.Current.Resources["ClipboardImage"], Clipboard.GetImage())
                    .ConfigureAwait(false);
            }
            else // text/string/adddress
            {
                var s = Clipboard.GetText(TextDataFormat.Text);

                if (string.IsNullOrEmpty(s))
                {
                    return;
                }

                var check = ErrorHandling.CheckIfLoadableString(s);
                switch (check)
                {
                    case "": return;
                    default:
                        await LoadPic.LoadPiFromFileAsync(check).ConfigureAwait(false);
                        return;

                    case "web":
                        await HttpFunctions.LoadPicFromUrlAsync(s).ConfigureAwait(false);
                        return;

                    case "base64":
                        await UpdateImage.UpdateImageFromBase64PicAsync(s).ConfigureAwait(false);
                        return;

                    case "directory":
                        await LoadPic.LoadPicFromFolderAsync(s).ConfigureAwait(false);
                        return;
                }
            }
        }

        /// <summary>
        /// Add file to move/paste clipboard
        /// </summary>
        internal static void Cut(string? path = null)
        {
            string filePath;
            if (path is null)
            {
                if (Pics.Count <= 0 || FolderIndex >= Pics.Count)
                {
                    return;
                }

                filePath = Pics[FolderIndex];
            }
            else
            {
                filePath = path;
            }

            var fileDropList = new StringCollection { filePath };

            var moveEffect = new byte[] { 2, 0, 0, 0 };
            var dropEffect = new MemoryStream();
            dropEffect.Write(moveEffect, 0, moveEffect.Length);

            var data = new DataObject();
            data.SetFileDropList(fileDropList);
            data.SetData("Preferred DropEffect", dropEffect);

            Clipboard.Clear();
            Clipboard.SetDataObject(data, true);
            ShowTooltipMessage(Application.Current.Resources["FileCutMessage"]);
        }
    }
}