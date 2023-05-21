using OpenTK.Mathematics;

namespace Raytracing
{
    public class Scene
    {
        public List<Primitive> Objects;
        public List<Light> Lightsources;
        public static Vector3 AmbientLight = new(0.01f, 0.01f, 0.01f);

        public Scene()
        {
            Objects = new List<Primitive>();
            Lightsources = new List<Light>();
        }

        public void AddObjects(Primitive obj) { Objects.Add(obj); }
        public void AddLight(Light light) { Lightsources.Add(light); }
    }
}
