using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Agentstrap.Utility
{
    public static class SkyboxImageConverter
    {
        private static readonly string[] SkyboxFileNames =
        {
            "sky512_bk.tex",
            "sky512_dn.tex",
            "sky512_ft.tex",
            "sky512_lf.tex",
            "sky512_rt.tex",
            "sky512_up.tex"
        };

        private const int FaceSize = 512;
        private const long MaximumInputBytes = 64L * 1024L * 1024L;
        private const int MaximumDimension = 16384;

        public static bool HasCustomSkybox()
        {
            try
            {
                if (!Directory.Exists(Paths.CustomSkybox))
                    return false;

                foreach (string fileName in SkyboxFileNames)
                {
                    string path = Path.Combine(Paths.CustomSkybox, fileName);

                    if (!File.Exists(path))
                        return false;

                    FileInfo info = new(path);

                    if (info.Length <= 0)
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Import(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new ArgumentException("No image was selected.", nameof(sourcePath));

            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("The selected image could not be found.", sourcePath);

            FileInfo sourceInfo = new(sourcePath);

            if (sourceInfo.Length <= 0)
                throw new InvalidDataException("The selected image is empty.");

            if (sourceInfo.Length > MaximumInputBytes)
                throw new InvalidDataException("The selected image is too large. Choose an image under 64 MB.");

            byte[] converted = ConvertImage(sourcePath);

            string stagingDirectory = Paths.CustomSkybox + ".new." + Guid.NewGuid().ToString("N");
            string backupDirectory = Paths.CustomSkybox + ".backup." + Guid.NewGuid().ToString("N");

            try
            {
                Directory.CreateDirectory(stagingDirectory);

                foreach (string fileName in SkyboxFileNames)
                {
                    File.WriteAllBytes(
                        Path.Combine(stagingDirectory, fileName),
                        converted
                    );
                }

                if (Directory.Exists(Paths.CustomSkybox))
                {
                    Directory.Move(Paths.CustomSkybox, backupDirectory);
                }

                Directory.Move(stagingDirectory, Paths.CustomSkybox);

                if (Directory.Exists(backupDirectory))
                {
                    DeleteDirectorySafe(backupDirectory);
                }
            }
            catch
            {
                if (!Directory.Exists(Paths.CustomSkybox) &&
                    Directory.Exists(backupDirectory))
                {
                    Directory.Move(backupDirectory, Paths.CustomSkybox);
                }

                throw;
            }
            finally
            {
                if (Directory.Exists(stagingDirectory))
                {
                    DeleteDirectorySafe(stagingDirectory);
                }

                if (Directory.Exists(backupDirectory))
                {
                    DeleteDirectorySafe(backupDirectory);
                }
            }
        }

        public static void Remove()
        {
            if (!Directory.Exists(Paths.CustomSkybox))
                return;

            DeleteDirectorySafe(Paths.CustomSkybox);
        }

        private static byte[] ConvertImage(string sourcePath)
        {
            using FileStream stream = new(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                FileOptions.SequentialScan
            );

            BitmapDecoder decoder = BitmapDecoder.Create(
                stream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad
            );

            if (decoder.Frames.Count == 0)
                throw new InvalidDataException("The selected image has no usable frames.");

            BitmapSource source = decoder.Frames[0];

            if (source.PixelWidth <= 0 || source.PixelHeight <= 0)
                throw new InvalidDataException("The selected image dimensions are invalid.");

            if (source.PixelWidth > MaximumDimension ||
                source.PixelHeight > MaximumDimension)
            {
                throw new InvalidDataException(
                    $"The selected image is too large. Maximum dimension is {MaximumDimension}px."
                );
            }

            int cropSize = Math.Min(source.PixelWidth, source.PixelHeight);

            int cropX = (source.PixelWidth - cropSize) / 2;
            int cropY = (source.PixelHeight - cropSize) / 2;

            CroppedBitmap cropped = new(
                source,
                new Int32Rect(
                    cropX,
                    cropY,
                    cropSize,
                    cropSize
                )
            );

            RenderTargetBitmap rendered = new(
                FaceSize,
                FaceSize,
                96,
                96,
                System.Windows.Media.PixelFormats.Pbgra32
            );

            DrawingVisual visual = new();

            using (var context = visual.RenderOpen())
            {
                context.DrawImage(
                    cropped,
                    new Rect(0, 0, FaceSize, FaceSize)
                );
            }

            rendered.Render(visual);

            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(rendered));

            using MemoryStream output = new();
            encoder.Save(output);

            byte[] result = output.ToArray();

            if (result.Length <= 0)
                throw new InvalidDataException("The image conversion produced no data.");

            return result;
        }

        private static void DeleteDirectorySafe(string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                    return;

                foreach (string file in Directory.EnumerateFiles(
                    directory,
                    "*",
                    SearchOption.AllDirectories))
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                    }
                    catch
                    {
                    }
                }

                Directory.Delete(directory, true);
            }
            catch
            {
            }
        }
    }
}

