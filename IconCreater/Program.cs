using Microsoft.Win32;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace IconCreater
{
    internal class App
    {
        private const string _args2048 = "2048";
        private const string _args1024 = "1024";
        private const string _args512 = "512";
        private const string _args256 = "256";
        private const string _args128 = "128";
        
        //https://learn.microsoft.com/ru-ru/windows/apps/design/style/iconography/app-icon-construction
        private readonly Dictionary<string, int[]> _sizesImageInIcon = new Dictionary<string, int[]>()
        {
            { "2048", new int[] { 2048, 1024, 512, 256,128, 96, 80, 72, 64, 60, 48, 40, 36, 32, 30, 24, 20, 16 } },
            { "1024", new int[] { 1024, 512, 256, 128, 96, 80, 72, 64, 60, 48, 40, 36, 32, 30, 24, 20, 16 } },
            { "512", new int[] { 512, 256, 128, 96, 80, 72, 64, 60, 48, 40, 36, 32, 30, 24, 20, 16 } },
            { "256", new int[] { 256, 128, 96, 80, 72, 64, 60, 48, 40, 36, 32, 30, 24, 20, 16 } },
            { "128", new int[] { 128, 96, 80, 72, 64, 60, 48, 40, 36, 32, 30, 24, 20, 16 } },
        };
        [STAThread]
        static void Main(string[] args)
        {
            App app = new App();
            int[] sizesImageInIconDefault;
           
            if (args.Length is not 1 || args.Length is 0) sizesImageInIconDefault = app._sizesImageInIcon["1024"];
            else sizesImageInIconDefault = app._sizesImageInIcon[args[0]];
            
            IconCreater iconCreater = new IconCreater(sizesImageInIconDefault);
            iconCreater.SaveNewIcon();
        }
        
    }

    // <summary>
    /// Adapted from this gist: https://gist.github.com/darkfall/1656050
    /// Provides helper methods for imaging
    /// </summary>
    internal class IconCreater
    {
        private readonly int[] _sizeIcon;
        private readonly string _dirctoryProccess = Path.GetDirectoryName(Environment.ProcessPath) ?? throw new NullReferenceException("Directory is null");
        public IconCreater(int[] sizeIcon)
        {
            _sizeIcon = sizeIcon;
        }
        internal void SaveNewIcon()
        {
            if (DialogFiles.OpenFile("Выберите изображение", out string? inputImageDirectory) is not true || inputImageDirectory is not string directoryInput) return;

            ImagingHelper.ConvertToIcon(inputPath: directoryInput,outputPath: Path.Combine(_dirctoryProccess, Path.ChangeExtension($"{Path.GetRandomFileName()}", "ico")), _sizeIcon);
        }
    }

    /// <summary>
    /// Adapted from this gist: https://gist.github.com/darkfall/1656050
    /// Provides helper methods for imaging
    /// </summary>
    internal static class ImagingHelper
    {
        /// <summary>
        /// Converts a PNG image to a icon (ico) with all the sizes windows likes
        /// </summary>
        /// <param name="inputBitmap">The input bitmap</param>
        /// <param name="output">The output stream</param>
        /// <returns>Wether or not the icon was succesfully generated</returns>
        public static bool ConvertToIcon(Bitmap inputBitmap, Stream output, int[]? sizesIcon = null)
        {
            if (inputBitmap is null) return false;

            int[] sizes = sizesIcon ?? new int[] { 256, 128, 96, 80, 72, 64, 60, 48, 40, 36, 32, 30, 24, 16 };

            // Generate bitmaps for all the sizes and toss them in streams
            List<MemoryStream> imageStreams = new List<MemoryStream>();
            foreach (int size in sizes)
            {
                Bitmap newBitmap = ResizeImage(inputBitmap, size, size);
                if (newBitmap is null) return false;
                MemoryStream memoryStream = new MemoryStream();
                newBitmap.Save(memoryStream, ImageFormat.Png);
                imageStreams.Add(memoryStream);
            }

            BinaryWriter iconWriter = new BinaryWriter(output);
            if (output is null || iconWriter is null)return false;

            int offset = 0;

            // 0-1 reserved, 0
            iconWriter.Write((byte)0);
            iconWriter.Write((byte)0);

            // 2-3 image type, 1 = icon, 2 = cursor
            iconWriter.Write((short)1);

            // 4-5 number of images
            iconWriter.Write((short)sizes.Length);

            offset += 6 + (16 * sizes.Length);

            for (int i = 0; i < sizes.Length; i++)
            {
                // image entry 1
                // 0 image width
                iconWriter.Write((byte)sizes[i]);
                // 1 image height
                iconWriter.Write((byte)sizes[i]);

                // 2 number of colors
                iconWriter.Write((byte)0);

                // 3 reserved
                iconWriter.Write((byte)0);

                // 4-5 color planes
                iconWriter.Write((short)0);

                // 6-7 bits per pixel
                iconWriter.Write((short)32);

                // 8-11 size of image data
                iconWriter.Write((int)imageStreams[i].Length);

                // 12-15 offset of image data
                iconWriter.Write((int)offset);

                offset += (int)imageStreams[i].Length;
            }

            for (int i = 0; i < sizes.Length; i++)
            {
                // write image data
                // png data must contain the whole png data file
                iconWriter.Write(imageStreams[i].ToArray());
                imageStreams[i].Close();
            }

            iconWriter.Flush();

            return true;
        }

        /// <summary>
        /// Converts a PNG image to a icon (ico)
        /// </summary>
        /// <param name="input">The input stream</param>
        /// <param name="output">The output stream</param
        /// <returns>Wether or not the icon was succesfully generated</returns>
        public static bool ConvertToIcon(Stream input, Stream output, int[]? sizesIcon = null)
        {
            Bitmap inputBitmap = (Bitmap)Bitmap.FromStream(input);
            return ConvertToIcon(inputBitmap, output, sizesIcon);
        }

        /// <summary>
        /// Converts a PNG image to a icon (ico)
        /// </summary>
        /// <param name="inputPath">The input path</param>
        /// <param name="outputPath">The output path</param>
        /// <returns>Wether or not the icon was succesfully generated</returns>
        public static bool ConvertToIcon(string inputPath, string outputPath, int[]? sizesIcon = null)
        {
            using FileStream inputStream = new FileStream(inputPath, FileMode.Open);
            using FileStream outputStream = new FileStream(outputPath, FileMode.OpenOrCreate);
            return ConvertToIcon(inputStream, outputStream, sizesIcon);
        }



        /// <summary>
        /// Converts an image to a icon (ico)
        /// </summary>
        /// <param name="inputImage">The input image</param>
        /// <param name="outputPath">The output path</param>
        /// <returns>Wether or not the icon was succesfully generated</returns>
        public static bool ConvertToIcon(Image inputImage, string outputPath)
        {
            using FileStream outputStream = new FileStream(outputPath, FileMode.OpenOrCreate);
            return ConvertToIcon(new Bitmap(inputImage), outputStream);
        }


        /// <summary>
        /// Resize the image to the specified width and height.
        /// Found on stackoverflow: https://stackoverflow.com/questions/1922040/resize-an-image-c-sharp
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using ImageAttributes wrapMode = new ImageAttributes();
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
            }

            return destImage;
        }
    }
    internal static class DialogFiles
    {
        internal static bool OpenFile(string Title, out string? SelectedFile, string Filter = "Все файлы (*.*)|*.*")
        {
            OpenFileDialog? fileDialog = new OpenFileDialog
            {
                Title = Title,
                Filter = Filter,
            };


            if (fileDialog.ShowDialog() is not true)
            {
                SelectedFile = null;
                return false;
            }

            SelectedFile = fileDialog.FileName;

            return true;
        }

        internal static bool OpenFiles(string Title, out IEnumerable<string> SelectedFile, string Filter = "Все файлы (*.*)|*.*")
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Title = Title,
                Filter = Filter,
            };


            if (fileDialog.ShowDialog() is not true)
            {
                SelectedFile = Enumerable.Empty<string>();
                return false;
            }

            SelectedFile = fileDialog.FileNames;

            return true;

        }

        internal static string? SaveFileDirectory(string Title, string Filter = "Все файлы (*.*)|*.*", IEnumerable<string>? Extension = null)
        {
            SaveFileDialog? fileDialog = new SaveFileDialog
            {
                Title = Title,
                Filter = Filter,
            };


            if (fileDialog.ShowDialog() is not true)
            {
                return null;
            }

            return fileDialog.FileName;// todo Оценить необходимость фильтрации типов                      
        }
    }
}