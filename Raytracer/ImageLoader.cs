using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Raytracing
{
    internal static class ImageLoader
    {
        public static Vector3[,] LoadFromFile(string path)
        {
            var image = Image.Load<Rgb24>("../../../" + path);
            Vector3[,] result = new Vector3[image.Width, image.Height];
            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    Rgb24 pixel = image[x, y];
                    result[x, y] = new Vector3(pixel.ToScaledVector4().X, pixel.ToScaledVector4().Y, pixel.ToScaledVector4().Z);
                }
            return result;
        }

    }
}
