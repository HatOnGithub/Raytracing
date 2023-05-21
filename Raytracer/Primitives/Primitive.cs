using OpenTK.Mathematics;

namespace Raytracing
{
    public enum MaterialType
    {
        Diffuse,
        Gloss,
        DiffuseMirror,
        Mirror,
        Transparent
    }

    public abstract class Primitive
    {

        public Vector3 Position;
        public Vector3[,] Texture;
        public Vector3 SpecularColor = Vector3.One;
        public Vector3 Up, Direction;
        public Vector3 averageColor;
        public MaterialType materialType;
        public float reflectiveness = 0.5f;
        public float specularity = 10;
        public float refractiveIndex = 1.6f;
        public Vector3 ambientColor { get { return Scene.AmbientLight; } }

        public Primitive(Vector3 position, Vector3 Color, MaterialType materialType, float reflectiveness = 0.5f)
        {
            Position = position;
            Texture = new Vector3[1, 1] { { Color } };
            averageColor = Color;
            this.materialType = materialType;
            this.reflectiveness = reflectiveness;
        }

        public Primitive(Vector3 position, string path, MaterialType materialType, float reflectiveness = 0.5f)
        {
            Position = position;
            Texture = ImageLoader.LoadFromFile(path);
            averageColor = GetAverageColor(Texture);
            this.materialType = materialType;
            this.reflectiveness = reflectiveness;
        }

        /// <summary>
        /// returns intersectdata using the ray
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        public abstract IntersectData Intersect(Ray ray);

        public Vector3 GetAverageColor(Vector3[,] Texture)
        {
            Vector3 result = new();
            foreach (Vector3 value in Texture) result += value;
            return result / Texture.Length;
        }


        /// <summary>
        /// returns the normal at the given position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public abstract Vector3 Normal(Vector3 position);

        /// <summary>
        /// Maps the position to a color from the Texture
        /// </summary>
        /// <param name="intersectPoint"></param>
        /// <returns></returns>
        public abstract Vector3 GetColorFromTextureAtIntersect(Vector3 intersectPoint);

    }
}
