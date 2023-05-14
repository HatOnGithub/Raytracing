using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace Raytracing
{
    public class Scene
    {
        List<Primitive> Objects;
        List<Light> Lightsources;
        public static Vector3 AmbientLight = new(0.01f, 0.01f, 0.01f);

        public Scene()
        {
            Objects = new List<Primitive>();
            Lightsources = new List<Light>();
        }

        public void AddObjects(Primitive obj) { Objects.Add(obj); }
        public void AddLight(Light light) { Lightsources.Add(light); }
        public List<Primitive> GetObjects() { return Objects; }
        public List<Light> GetLights() { return Lightsources; }
    }
}
