using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Maxx53.Games
{
    class Utils
    {
        //Метод для вырезания прямоугольной области из картинки
        public static Bitmap CropImage(Image img, Rectangle cropArea)
        {
            Bitmap bmpImage = new Bitmap(img);
            Bitmap bmpCrop = bmpImage.Clone(cropArea, bmpImage.PixelFormat);
            return bmpCrop;
        }

        //Метод для высококачественного изменения размера изображения
        public static Image ResizeImage(Image image, int width, int height)
        {
            //Если изменять нечего, возвращаем исходное изображение без изменений
            if ((image.Width == width && image.Height == height) || (width == 0 && height == 0))
                return new Bitmap(image);

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

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        //Простой метод для рассчета расстояния между 2мя точками
        public static double GetDistance(Point p1, Point p2)
        {
            double xDelta = p1.X - p2.X;
            double yDelta = p1.Y - p2.Y;

            //Теорема Пифагора
            return Math.Sqrt(Math.Pow(xDelta, 2) + Math.Pow(yDelta, 2));
        }


        public static void SaveBinary(string p, object o)
        {
            if (o != null)
            {
                using (Stream stream = File.Create(p))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.Serialize(stream, o);
                }
            }
        }

        public static object LoadBinary(string p)
        {
            try
            {
                using (Stream stream = File.Open(p, FileMode.Open))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    var res = bin.Deserialize(stream);
                    return res;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
