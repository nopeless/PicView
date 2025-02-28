﻿using ImageMagick;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using PicView.ChangeImage;
using PicView.UILogic;
using PicView.UILogic.TransformImage;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PicView.ImageHandling
{
    internal static class GetImageData
    {
        internal static async Task<string[]?> RetrieveData(FileInfo? fileInfo)
        {
            if (fileInfo is not null && Navigation.Pics[Navigation.FolderIndex] != fileInfo.FullName)
            {
                return null;
            }

            string name, directoryName, fullname, creationTime, lastWriteTime, lastAccessTime;

            if (fileInfo is null)
            {
                name = string.Empty;
                directoryName = string.Empty;
                fullname = string.Empty;
                creationTime = string.Empty;
                lastWriteTime = string.Empty;
                lastAccessTime = string.Empty;
            }
            else
            {
                try
                {
                    name = Path.GetFileNameWithoutExtension(fileInfo.Name);
                    directoryName = fileInfo.DirectoryName ?? "";
                    fullname = fileInfo.FullName;
                    creationTime = fileInfo.CreationTime.ToString(CultureInfo.CurrentCulture);
                    lastWriteTime = fileInfo.LastWriteTime.ToString(CultureInfo.CurrentCulture);
                    lastAccessTime = fileInfo.LastAccessTime.ToString(CultureInfo.CurrentCulture);
                }
                catch (Exception)
                {
                    name = string.Empty;
                    directoryName = string.Empty;
                    fullname = string.Empty;
                    creationTime = string.Empty;
                    lastWriteTime = string.Empty;
                    lastAccessTime = string.Empty;
                }
            }

            BitmapSource? bitmapSource = null;

            if (Navigation.Pics.Count > 0 && Navigation.Pics.Count > Navigation.FolderIndex)
            {
                var preloadValue = PreLoader.Get(Navigation.FolderIndex);
                if (preloadValue is null)
                {
                    await PreLoader.AddAsync(Navigation.FolderIndex).ConfigureAwait(false);
                    preloadValue = new PreLoader.PreLoadValue(null, fileInfo);
                    if (fileInfo is not null && Navigation.Pics[Navigation.FolderIndex] != fileInfo.FullName)
                    {
                        return null;
                    }
                }

                while (preloadValue.BitmapSource is null)
                {
                    await Task.Delay(50).ConfigureAwait(false);
                    if (fileInfo is not null && Navigation.Pics[Navigation.FolderIndex] != fileInfo.FullName)
                    {
                        return null;
                    }
                }

                bitmapSource = preloadValue.BitmapSource;
            }
            else
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    () => { bitmapSource = ImageDecoder.GetRenderedBitmapFrame(); });
            }

            if (fileInfo is not null && Navigation.Pics[Navigation.FolderIndex] != fileInfo.FullName)
            {
                return null;
            }

            if (bitmapSource is null)
            {
                return null;
            }

            var inchesWidth = bitmapSource.PixelWidth / bitmapSource.DpiX;
            var inchesHeight = bitmapSource.PixelHeight / bitmapSource.DpiY;
            var cmWidth = inchesWidth * 2.54;
            var cmHeight = inchesHeight * 2.54;

            var firstRatio = bitmapSource.PixelWidth / ZoomLogic.GCD(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
            var secondRatio = bitmapSource.PixelHeight /
                              ZoomLogic.GCD(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
            string ratioText;
            if (firstRatio == secondRatio)
            {
                ratioText = $"{firstRatio}:{secondRatio} ({Application.Current.Resources["Square"]})";
            }
            else if (firstRatio > secondRatio)
            {
                ratioText = $"{firstRatio}:{secondRatio} ({Application.Current.Resources["Landscape"]})";
            }
            else
            {
                ratioText = $"{firstRatio}:{secondRatio} ({Application.Current.Resources["Portrait"]})";
            }

            var megaPixels = ((float)bitmapSource.PixelHeight * bitmapSource.PixelWidth / 1000000)
                .ToString("0.##", CultureInfo.CurrentCulture) + " " + Application.Current.Resources["MegaPixels"];

            var printSizeCm = cmWidth.ToString("0.##", CultureInfo.CurrentCulture) + " x " +
                              cmHeight.ToString("0.##", CultureInfo.CurrentCulture)
                              + " " + Application.Current.Resources["Centimeters"];

            var printSizeInch = inchesWidth.ToString("0.##", CultureInfo.CurrentCulture) + " x " +
                                inchesHeight.ToString("0.##", CultureInfo.CurrentCulture)
                                + " " + Application.Current.Resources["Inches"];

            var dpi = string.Empty;

            if (fileInfo is null)
            {
                return new[]
                {
                    name,
                    directoryName,
                    fullname,
                    creationTime,
                    lastWriteTime,
                    lastAccessTime,

                    "",

                    bitmapSource.PixelWidth.ToString(),
                    bitmapSource.PixelHeight.ToString(),

                    dpi,

                    megaPixels,

                    printSizeCm,
                    printSizeInch,

                    ratioText,

                    "0",
                };
            }

            // exif
            var gps = string.Empty;

            var latitude = string.Empty;
            var latitudeValue = string.Empty;

            var longitude = string.Empty;
            var longitudeValue = string.Empty;

            var altitude = string.Empty;
            var altitudeValue = string.Empty;

            var googleLink = string.Empty;
            var bingLink = string.Empty;

            var title = string.Empty;
            var titleValue = string.Empty;

            var subject = string.Empty;
            var subjectValue = string.Empty;

            var authors = string.Empty;
            var authorsValue = string.Empty;

            var dateTaken = string.Empty;
            var dateTakenValue = string.Empty;

            var programName = string.Empty;
            var programNameValue = string.Empty;

            var copyrightName = string.Empty;
            var copyrightValue = string.Empty;

            var resolutionUnit = string.Empty;
            var resolutionUnitValue = string.Empty;

            var colorRepresentation = string.Empty;
            var colorRepresentationValue = string.Empty;

            var compression = string.Empty;
            var compressionValue = string.Empty;

            var compressionBits = string.Empty;
            var compressionBitsValue = string.Empty;

            var cameraMaker = string.Empty;
            var cameroMakerValue = string.Empty;

            var cameraModel = string.Empty;
            var cameroModelValue = string.Empty;

            var fstop = string.Empty;
            var fstopValue = string.Empty;

            var exposure = string.Empty;
            var exposureValue = string.Empty;

            var isoSpeed = string.Empty;
            var isoSpeedValue = string.Empty;

            var exposureBias = string.Empty;
            var exposureBiasValue = string.Empty;

            var focal = string.Empty;
            var focalValue = string.Empty;

            var maxAperture = string.Empty;
            var maxApertureValue = string.Empty;

            var flashMode = string.Empty;
            var flashModeValue = string.Empty;

            var flashEnergy = string.Empty;
            var flashEnergyValue = string.Empty;

            var flength35 = string.Empty;
            var flength35Value = string.Empty;

            var meteringMode = string.Empty;
            var meteringModeValue = string.Empty;

            var lensManufacturer = string.Empty;
            var lensManufacturerValue = string.Empty;

            var lensmodel = string.Empty;
            var lensmodelValue = string.Empty;

            var flashManufacturer = string.Empty;
            var flashManufacturerValue = string.Empty;

            var flashModel = string.Empty;
            var flashModelValue = string.Empty;

            var camSerialNumber = string.Empty;
            var camSerialNumberValue = string.Empty;

            var contrast = string.Empty;
            var contrastValue = string.Empty;

            var brightness = string.Empty;
            var brightnessValue = string.Empty;

            var lightSource = string.Empty;
            var lightSourceValue = string.Empty;

            var exposureProgram = string.Empty;
            var exposureProgramValue = string.Empty;

            var saturation = string.Empty;
            var saturationValue = string.Empty;

            var sharpness = string.Empty;
            var sharpnessValue = string.Empty;

            var whiteBalance = string.Empty;
            var whiteBalanceValue = string.Empty;

            var photometricInterpolation = string.Empty;
            var photometricInterpolationValue = string.Empty;

            var digitalZoom = string.Empty;
            var digitalZoomValue = string.Empty;

            var exifversion = string.Empty;
            var exifversionValue = string.Empty;

            var so = ShellObject.FromParsingName(fileInfo.FullName);
            var bitDepth = so.Properties.GetProperty(SystemProperties.System.Image.BitDepth).ValueAsObject;
            var stars = so.Properties.GetProperty(SystemProperties.System.Rating).ValueAsObject;

            var dpiX = so.Properties.GetProperty(SystemProperties.System.Image.HorizontalResolution).ValueAsObject;
            var dpiY = so.Properties.GetProperty(SystemProperties.System.Image.VerticalResolution).ValueAsObject;
            if (dpiX is not null && dpiY is not null)
            {
                dpi = Math.Round((double)dpiX) + " x " + Math.Round((double)dpiY) + " " +
                      Application.Current.Resources["Dpi"];
            }

            bitDepth ??= string.Empty;
            stars ??= string.Empty;

            var magickImage = new MagickImage();
            try
            {
                if (fileInfo.Length < 214783648)
                    await magickImage.ReadAsync(fileInfo);
                else
                    // ReSharper disable once MethodHasAsyncOverload
                    magickImage.Read(fileInfo);
            }
            catch (Exception)
            {
                return new[]
                {
                    name,
                    directoryName,
                    fullname,
                    creationTime,
                    lastWriteTime,
                    lastAccessTime,

                    bitDepth.ToString() ?? "",

                    bitmapSource.PixelWidth.ToString() ?? "",
                    bitmapSource.PixelHeight.ToString() ?? "",

                    dpi,

                    megaPixels,

                    printSizeCm,
                    printSizeInch,

                    ratioText,

                    stars.ToString() ?? "",
                };
            }

            var exifData = magickImage.GetExifProfile();
            magickImage.Dispose();

            latitude = so.Properties.GetProperty(SystemProperties.System.GPS.Latitude).Description.DisplayName;
            longitude = so.Properties.GetProperty(SystemProperties.System.GPS.Longitude).Description.DisplayName;
            altitude = so.Properties.GetProperty(SystemProperties.System.GPS.Altitude).Description.DisplayName;

            var _title = so.Properties.GetProperty(SystemProperties.System.Title);
            title = _title.Description.DisplayName;
            if (_title.ValueAsObject is not null)
            {
                titleValue = _title.ValueAsObject.ToString() ?? "";
            }

            var _subject = so.Properties.GetProperty(SystemProperties.System.Subject);
            subject = _subject.Description.DisplayName;
            if (_subject.ValueAsObject is not null)
            {
                subjectValue = _subject.ValueAsObject.ToString() ?? "";
            }

            var _author = so.Properties.GetProperty(SystemProperties.System.Author);
            authors = _author.Description.DisplayName;
            if (_author.ValueAsObject is not null)
            {
                var authorsArray = (string[])_author.ValueAsObject;

                switch (authorsArray.Length)
                {
                    case 1:
                        authorsValue = authorsArray[0];
                        break;

                    case >= 2:
                    {
                        var sb = new StringBuilder();
                        for (var i = 0; i < authorsArray.Length; i++)
                        {
                            if (i == 0)
                            {
                                sb.Append(authorsArray[0]);
                            }
                            else
                            {
                                sb.Append(", " + authorsArray[i]);
                            }
                        }

                        authorsValue = sb.ToString();
                        break;
                    }
                }
            }

            var _dateTaken = so.Properties.GetProperty(SystemProperties.System.Photo.DateTaken);
            dateTaken = _dateTaken.Description.DisplayName;
            if (_dateTaken.ValueAsObject is not null)
            {
                dateTakenValue = _dateTaken.ValueAsObject.ToString() ?? "";
            }

            var _program = so.Properties.GetProperty(SystemProperties.System.ApplicationName);
            programName = _program.Description.DisplayName;
            if (_program.ValueAsObject is not null)
            {
                programNameValue = _program.ValueAsObject.ToString() ?? "";
            }

            var _copyright = so.Properties.GetProperty(SystemProperties.System.Copyright);
            copyrightName = _copyright.Description.DisplayName;
            if (_copyright.ValueAsObject is not null)
            {
                copyrightValue = _copyright.ValueAsObject.ToString() ?? "";
            }

            var _resolutionUnit = so.Properties.GetProperty(SystemProperties.System.Image.ResolutionUnit);
            resolutionUnit = _resolutionUnit.Description.DisplayName;
            if (_resolutionUnit.ValueAsObject is not null)
            {
                resolutionUnitValue = _resolutionUnit.ValueAsObject.ToString() ?? "";
            }

            colorRepresentation = so.Properties.GetProperty(SystemProperties.System.Image.ColorSpace).Description
                .DisplayName;

            compression = so.Properties.GetProperty(SystemProperties.System.Image.Compression).Description.DisplayName;
            compressionBits = so.Properties.GetProperty(SystemProperties.System.Image.CompressedBitsPerPixel)
                .Description.DisplayName;

            var camManu = so.Properties.GetProperty(SystemProperties.System.Photo.CameraManufacturer);
            cameraMaker = camManu.Description.DisplayName;
            if (camManu.ValueAsObject is not null)
            {
                cameroMakerValue = camManu.ValueAsObject.ToString() ?? "";
            }

            var cam = so.Properties.GetProperty(SystemProperties.System.Photo.CameraModel);
            cameraModel = cam.Description.DisplayName;
            if (cam.ValueAsObject is not null)
            {
                cameroModelValue = cam.ValueAsObject.ToString() ?? "";
            }

            var flashManu = so.Properties.GetProperty(SystemProperties.System.Photo.FlashManufacturer);
            flashManufacturer = flashManu.Description.DisplayName;
            if (flashManu.ValueAsObject is not null)
            {
                flashManufacturerValue = flashManu.ValueAsObject.ToString() ?? "";
            }

            var flashM = so.Properties.GetProperty(SystemProperties.System.Photo.FlashModel);
            flashModel = flashM.Description.DisplayName;
            if (flashM.ValueAsObject is not null)
            {
                flashModelValue = flashManu.ValueAsObject.ToString() ?? "";
            }

            fstop = so.Properties.GetProperty(SystemProperties.System.Photo.FNumber).Description.DisplayName;

            var expo = so.Properties.GetProperty(SystemProperties.System.Photo.ExposureTime);
            exposure = expo.Description.DisplayName;
            if (expo.ValueAsObject is not null)
            {
                exposureValue = expo.ValueAsObject.ToString() ?? "";
            }

            var iso = so.Properties.GetProperty(SystemProperties.System.Photo.ISOSpeed);
            isoSpeed = iso.Description.DisplayName;
            if (iso.ValueAsObject is not null)
            {
                isoSpeedValue = iso.ValueAsObject.ToString() ?? "";
            }

            meteringMode = so.Properties.GetProperty(SystemProperties.System.Photo.MeteringMode).Description
                .DisplayName;

            exposureBias = so.Properties.GetProperty(SystemProperties.System.Photo.ExposureBias).Description
                .DisplayName;

            maxAperture = so.Properties.GetProperty(SystemProperties.System.Photo.MaxAperture).Description.DisplayName;

            focal = so.Properties.GetProperty(SystemProperties.System.Photo.FocalLength).Description.DisplayName;

            flashMode = so.Properties.GetProperty(SystemProperties.System.Photo.Flash).Description.DisplayName;

            flashEnergy = so.Properties.GetProperty(SystemProperties.System.Photo.FlashEnergy).Description.DisplayName;

            var f35 = so.Properties.GetProperty(SystemProperties.System.Photo.FocalLengthInFilm);

            var serial = so.Properties.GetProperty(SystemProperties.System.Photo.CameraSerialNumber);
            camSerialNumber = serial.Description.DisplayName;
            if (serial.ValueAsObject is not null)
            {
                camSerialNumberValue = serial.ValueAsObject.ToString() ?? "";
            }

            flength35 = f35.Description.DisplayName;
            if (f35.ValueAsObject is not null)
            {
                flength35Value = f35.ValueAsObject.ToString() ?? "";
            }

            var lensm = so.Properties.GetProperty(SystemProperties.System.Photo.LensManufacturer);
            lensManufacturer = lensm.Description.DisplayName;
            if (lensm.ValueAsObject is not null)
            {
                lensManufacturerValue = lensm.ValueAsObject.ToString() ?? "";
            }

            var _lensmodel = so.Properties.GetProperty(SystemProperties.System.Photo.LensModel);
            lensmodel = _lensmodel.Description.DisplayName;
            if (_lensmodel.ValueAsObject is not null)
            {
                lensmodelValue = _lensmodel.ValueAsObject.ToString() ?? "";
            }

            contrast = so.Properties.GetProperty(SystemProperties.System.Photo.Contrast).Description.DisplayName;

            var bright = so.Properties.GetProperty(SystemProperties.System.Photo.Brightness);
            brightness = bright.Description.DisplayName;
            if (bright.ValueAsObject is not null)
            {
                brightnessValue = bright.ValueAsObject.ToString() ?? "";
            }

            lightSource = so.Properties.GetProperty(SystemProperties.System.Photo.LightSource).Description.DisplayName;

            exposureProgram = so.Properties.GetProperty(SystemProperties.System.Photo.ExposureProgram).Description
                .DisplayName;

            saturation = so.Properties.GetProperty(SystemProperties.System.Photo.Saturation).Description.DisplayName;

            sharpness = so.Properties.GetProperty(SystemProperties.System.Photo.Sharpness).Description.DisplayName;

            whiteBalance = so.Properties.GetProperty(SystemProperties.System.Photo.WhiteBalance).Description
                .DisplayName;

            photometricInterpolation = so.Properties
                .GetProperty(SystemProperties.System.Photo.PhotometricInterpretation).Description.DisplayName;

            digitalZoom = so.Properties.GetProperty(SystemProperties.System.Photo.DigitalZoom).Description.DisplayName;

            var exifv = so.Properties.GetProperty(SystemProperties.System.Photo.EXIFVersion);
            exifversion = exifv.Description.DisplayName;
            if (exifv.ValueAsObject is not null)
            {
                exifversionValue = exifv.ValueAsObject.ToString() ?? "";
            }

            if (exifData is not null)
            {
                var gpsLong = exifData.GetValue(ExifTag.GPSLongitude);
                var gpsLongRef = exifData.GetValue(ExifTag.GPSLongitudeRef);
                var gpsLatitude = exifData.GetValue(ExifTag.GPSLatitude);
                var gpsLatitudeRef = exifData.GetValue(ExifTag.GPSLatitudeRef);

                if (gpsLong is not null && gpsLongRef is not null && gpsLatitude is not null &&
                    gpsLatitudeRef is not null)
                {
                    latitudeValue = GetCoordinates(gpsLatitudeRef.ToString(), gpsLatitude.Value).ToString();
                    longitudeValue = GetCoordinates(gpsLongRef.ToString(), gpsLong.Value).ToString();

                    googleLink = @"https://www.google.com/maps/search/?api=1&query=" + latitudeValue + "," +
                                 longitudeValue;
                    bingLink = @"https://bing.com/maps/default.aspx?cp=" + latitudeValue + "~" + longitudeValue +
                               "&style=o&lvl=1&dir=0&scene=1140291";
                }

                var gpsAltitude = exifData?.GetValue(ExifTag.GPSAltitude);
                if (gpsAltitude is not null)
                {
                    altitudeValue = gpsAltitude.Value.ToString();
                }

                var colorSpace = exifData.GetValue(ExifTag.ColorSpace);
                if (colorSpace is not null)
                {
                    colorRepresentationValue = colorSpace.Value switch
                    {
                        1 => "sRGB",
                        2 => "Adobe RGB",
                        65535 => "Uncalibrated",
                        _ => "Unknown"
                    };
                }

                var compr = exifData.GetValue(ExifTag.Compression);
                if (compr is not null)
                {
                    compressionValue = compr.Value.ToString();
                }

                var comprBits = exifData.GetValue(ExifTag.CompressedBitsPerPixel);
                if (comprBits is not null)
                {
                    compressionBitsValue = comprBits.Value.ToString();
                }

                var fNumber = exifData?.GetValue(ExifTag.FNumber);
                if (fNumber is not null)
                {
                    fstopValue = fNumber.Value.ToString();
                }

                var bias = exifData.GetValue(ExifTag.ExposureBiasValue);
                if (bias is not null)
                {
                    exposureBiasValue = bias.Value.ToString();
                }

                var maxApart = exifData.GetValue(ExifTag.MaxApertureValue);
                if (maxApart is not null)
                {
                    maxApertureValue = maxApart.Value.ToString();
                }

                var fcal = exifData.GetValue(ExifTag.FocalLength);
                if (fcal is not null)
                {
                    focalValue = fcal.Value.ToString();
                }

                var flash = exifData.GetValue(ExifTag.Flash);
                if (flash is not null)
                {
                    flashModeValue = flash.Value.ToString();
                }

                var fenergy = exifData.GetValue(ExifTag.FlashEnergy);
                if (fenergy is not null)
                {
                    flashEnergyValue = fenergy.Value.ToString();
                }

                var metering = exifData.GetValue(ExifTag.MeteringMode);
                if (metering is not null)
                {
                    meteringModeValue = metering.Value.ToString();
                }

                var _contrast = exifData.GetValue(ExifTag.Contrast);
                if (_contrast is not null)
                {
                    contrastValue = _contrast.Value.ToString();
                }

                var light = exifData.GetValue(ExifTag.LightSource);
                if (light is not null)
                {
                    lightSourceValue = light.Value.ToString();
                }

                var expoPro = exifData.GetValue(ExifTag.ExposureProgram);
                if (expoPro is not null)
                {
                    exposureProgramValue = expoPro.Value.ToString();
                }

                var satu = exifData.GetValue(ExifTag.Saturation);
                if (satu is not null)
                {
                    saturationValue = satu.Value.ToString();
                }

                var sharp = exifData.GetValue(ExifTag.Sharpness);
                if (sharp is not null)
                {
                    sharpnessValue = sharp.Value.ToString();
                }

                var whiteB = exifData.GetValue(ExifTag.WhiteBalance);
                if (whiteB is not null)
                {
                    whiteBalanceValue = whiteB.Value.ToString();
                }

                var photometric = exifData.GetValue(ExifTag.PhotometricInterpretation);
                if (photometric is not null)
                {
                    photometricInterpolationValue = photometric.Value.ToString();
                }

                var digizoom = exifData.GetValue(ExifTag.DigitalZoomRatio);
                if (digizoom is not null)
                {
                    digitalZoomValue = digizoom.Value.ToString();
                }
            }

            so.Dispose();

            return new[]
            {
                // Fileinfo
                name,
                directoryName,
                fullname,
                creationTime,
                lastWriteTime,
                lastAccessTime,

                bitDepth.ToString() ?? "",

                bitmapSource.PixelWidth.ToString() ?? "",
                bitmapSource.PixelHeight.ToString() ?? "",

                dpi,

                megaPixels,

                printSizeCm,
                printSizeInch,

                ratioText,

                stars.ToString() ?? "",

                latitude, latitudeValue,
                longitude, longitudeValue,
                bingLink, googleLink,
                altitude, altitudeValue,

                title, titleValue,
                subject, subjectValue,

                authors, authorsValue,
                dateTaken, dateTakenValue,

                programName, programNameValue,
                copyrightName, copyrightValue,

                resolutionUnit, resolutionUnitValue,
                colorRepresentation, colorRepresentationValue,

                compression, compressionValue,
                compressionBits, compressionBitsValue,

                cameraMaker, cameroMakerValue,
                cameraModel, cameroModelValue,

                fstop, fstopValue,
                exposure, exposureValue,

                isoSpeed, isoSpeedValue,
                exposureBias, exposureBiasValue,

                maxAperture, maxApertureValue,

                focal, focalValue,
                flength35, flength35Value,

                flashMode, flashModeValue,
                flashEnergy, flashEnergyValue,

                meteringMode, meteringModeValue,

                lensManufacturer, lensManufacturerValue,
                lensmodel, lensmodelValue,

                flashManufacturer, flashManufacturerValue,
                flashModel, flashModelValue,

                camSerialNumber, camSerialNumberValue,

                contrast, contrastValue,
                brightness, brightnessValue,

                lightSource, lightSourceValue,

                exposureProgram, exposureProgramValue,

                saturation, saturationValue,
                sharpness, sharpnessValue,

                whiteBalance, whiteBalanceValue,
                photometricInterpolation, photometricInterpolationValue,

                digitalZoom, digitalZoomValue,

                exifversion, exifversionValue,
            };
        }

        private static double GetCoordinates(string gpsRef, Rational[] rationals)
        {
            if (rationals[0].Denominator == 0 || rationals[1].Denominator == 0 || rationals[2].Denominator == 0)
            {
                return 0;
            }

            double degrees = rationals[0].Numerator / rationals[0].Denominator;
            double minutes = rationals[1].Numerator / rationals[1].Denominator;
            double seconds = rationals[2].Numerator / rationals[2].Denominator;

            var coordinate = degrees + (minutes / 60d) + (seconds / 3600d);
            if (gpsRef is "S" or "W")
                coordinate *= -1;
            return coordinate;
        }
    }
}