using OpenTK.Mathematics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace Raytracing
{
    public class Raytracer
    {
        public Surface screen;
        public Surface debug;
        public Scene scene;
        public Camera camera;
        static int bouncelimit = 4;
        static int AntiAliasing = 2; // degree of supersampling, 2^n where n is the value you enter
        static int screenDivisions = (int)MathF.Sqrt(ThreadPool.ThreadCount);

        static bool[,] screenPortions = new bool[screenDivisions, screenDivisions];

        


        public Raytracer(Surface screen, Surface debug)
        {
            this.screen = screen;
            this.debug = debug;
            camera = new Camera(screen, new(-4, 0, 0), new(1, 0, 0), new(0, 1, 0), 100, 1); ;
            scene = new Scene();

            scene.AddObjects(new Sphere(new(3, 0, 0), "Textures/Spherical_texture.jpg", MaterialType.Diffuse, 1, 0.5f, 1.5f) { Direction = new(-0.5f,0,0.5f)});

            scene.AddObjects(new Sphere(new(3, 0, 3), new Vector3(1,1,1), MaterialType.Transparent, 1, 0.8f));

            scene.AddObjects(new Sphere(new(3, 0, -3),  new Vector3(1, 1, 1), MaterialType.Mirror, 1, 1));

            scene.AddObjects(new Triangle(new Vector3[3] { new(6, 0, 4), new(6, 4, 0), new(6, 0, 4) }, new Vector3(1,1, 1), MaterialType.Mirror, 1f));


            scene.AddObjects(new Plane(new(0, -1, 0), new(0, 1, 0),"Textures/CheckerboardPattern.png", new(1,1.4f), MaterialType.Diffuse, 0.8f));

            scene.AddObjects(new Plane(new(8, 0, 0), new(-1, 0, 0), "Textures/CheckerboardPattern.png", new(1, 1), MaterialType.DiffuseMirror, 0.8f));
            scene.AddObjects(new Plane(new(0, 0, -5), new(0, 0, 1), "Textures/CheckerboardPattern.png", new(1, 1), MaterialType.Diffuse, 0.8f));
            scene.AddObjects(new Plane(new(0, 0, 5), new(0, 0, -1), "Textures/CheckerboardPattern.png", new(1, 1), MaterialType.Diffuse, 0.8f));
            scene.AddObjects(new Plane(new(-5, 0, 0), new(1, 0, 0), "Textures/CheckerboardPattern.png", new(1, 1), MaterialType.Diffuse, 0.8f));

            scene.AddLight(new Light(new(-1, 0, 0), new(1, 1, 1), 10));
            scene.AddLight(new PlaneLight(new(0, 50, 0), new(0,-1,0), new(1,1,1), 100));

        }

        /// <summary>
        /// Starts the tasks
        /// </summary>
        public void Render()
        {
            // check for unassigned screendivisions, start a worker to fill that division
            for (int y = 0; y < screenDivisions; y++)
                for (int x = 0; x < screenDivisions; x++) if (!screenPortions[x, y])
                        Task.Run(RenderWorker);
        }


        /// <summary>
        /// Returns an unassigned screen quadrant, depending on number of threads available
        /// </summary>
        /// <returns></returns>
        public Vector4i GetAssignment()
        {
            Vector2i singleSize = new(screen.width / screenDivisions, screen.height / screenDivisions);
            for (int y = 0; y < screenDivisions; y++)
                for (int x = 0; x < screenDivisions; x++) if (!screenPortions[x,y])
                    {
                        screenPortions[x,y] = true;
                        Vector4i result = new();
                        result.Xy = singleSize * new Vector2i(x, y);
                        result.Zw = singleSize * new Vector2i(x + 1, y + 1) - Vector2i.One;
                        return result;
                    }
            return -Vector4i.One;
        }


        /// <summary>
        /// Worker for Multithreading
        /// </summary>
        public void RenderWorker()
        {
            // get assigned area, if all areas are assigned, return
            Vector4i AssignedArea = GetAssignment();
            if (AssignedArea == -Vector4.One) return;

            // go through each pixel in the assigned area, start raytracing
            for (int y = AssignedArea.Y; y <= AssignedArea.W; y++)
                for (int x = AssignedArea.X; x <= AssignedArea.Z; x++)
                {
                    Vector3 color = new();
                    IntersectData returnData;

                    // Antialiasing by supersampling
                    for (float yoffset = 0; yoffset < AntiAliasing; yoffset++)
                        for (float xoffset = 0; xoffset < AntiAliasing; xoffset++)
                        {
                            Ray ray = camera.GetRayForPixelAt(x + (xoffset / AntiAliasing), y + (yoffset / AntiAliasing));
                            color += RecursiveRayShooter(ray, screen, bouncelimit, out returnData);
                        }
                    color /= AntiAliasing * AntiAliasing;
                    screen.Plot(x, y, RGBtoINT(ClampToOne(color / camera.exposure)));
                }

            // when done, unassign itself
            screenPortions[AssignedArea.X / (screen.width / screenDivisions), AssignedArea.Y / (screen.height / screenDivisions)] = false;
        }


        /// <summary>
        /// Recursively shoot rays into the scene
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="prims"></param>
        /// <param name="lights"></param>
        /// <param name="bouncesLeft"></param>
        /// <param name="aimedLight"></param>
        /// <returns></returns>
        public Vector3 RecursiveRayShooter(Ray ray, Surface screen, int bouncesLeft, out IntersectData intersectData, Light? aimedLight = null)
        {
            Vector3 result = new();
            bool rayBlocked = false;

            Random random = new Random();

            List<Primitive> prims = scene.Objects;
            List<Light> lights = scene.Lightsources;

            KeyValuePair<Primitive, IntersectData>? closest = null;
            foreach(Primitive candidate in prims)
            {
                IntersectData tempdata = candidate.Intersect(ray);

                if (tempdata.intersectedPrimitive != null && tempdata.distance > 0)
                {
                    if (closest == null) closest = new(candidate, tempdata);
                    else if (closest.Value.Value.distance > tempdata.distance) closest = new(candidate, tempdata);
                }
            }


            if (closest != null)
            {
                Primitive prim = closest.Value.Key;
                IntersectData data = closest.Value.Value;

                Vector3 intersectPoint = ray.Origin + data.distance * ray.Direction;
                Vector3 smallOffset = prim.Normal(intersectPoint) * 0.001f;

                // if the ray is aimed at a light, check if the primitive blocks the ray between it and the light
                if (aimedLight != null)
                    if (data.distance <= Vector3.Dot(aimedLight.Position(ray.Origin) - intersectPoint - smallOffset, ray.Direction))
                        rayBlocked = true;

                // if the ray bounce limit hasn't been reached, send out new shadow rays
                if (bouncesLeft > 0)
                {
                    IntersectData returndata;
                    Ray reflectedRay = new(intersectPoint + smallOffset, ray.Direction - (2 * Vector3.Dot(ray.Direction, prim.Normal(intersectPoint))) * prim.Normal(intersectPoint), "s");

                    if (prim.materialType == MaterialType.Mirror)
                    {
                        result = RecursiveRayShooter(reflectedRay, screen, bouncesLeft - 1, out returndata) * (prim.reflectiveness * prim.GetColorFromTextureAtIntersect(intersectPoint));
                    }

                    if (prim.materialType == MaterialType.DiffuseMirror)
                    {
                        foreach (Light light in lights)
                        {
                            Ray shadowRay = new(intersectPoint + smallOffset, light.Position(ray.Origin) - intersectPoint);
                            Vector3 returnlight = RecursiveRayShooter(shadowRay, screen, bouncesLeft - 1, out returndata, light);
                            result += DiffuseCalculation(shadowRay.Direction, prim.Normal(shadowRay.Origin), returnlight, prim.GetColorFromTextureAtIntersect(intersectPoint), (light.Position(intersectPoint) - intersectPoint - (smallOffset * 2)).LengthSquared, prim.reflectiveness);
                        }

                        result += RecursiveRayShooter(reflectedRay, screen, bouncesLeft - 1, out returndata) * (prim.reflectiveness * prim.GetColorFromTextureAtIntersect(intersectPoint)) * prim.reflectiveness;
                    }

                    else if (prim.materialType == MaterialType.Diffuse)
                    {
                        foreach (Light light in lights)
                        {
                            Ray shadowRay = new(intersectPoint + smallOffset, light.Position(intersectPoint) - intersectPoint);
                            Vector3 returnlight = RecursiveRayShooter(shadowRay, screen, bouncesLeft - 1, out returndata, light);   
                            result += DiffuseCalculation(shadowRay.Direction, prim.Normal(shadowRay.Origin), returnlight, prim.GetColorFromTextureAtIntersect(intersectPoint), (light.Position(intersectPoint) - intersectPoint - (smallOffset * 2)).LengthSquared, prim.reflectiveness);
                        }

                    }

                    else if (prim.materialType == MaterialType.Gloss)
                    {

                        foreach (Light light in lights)
                        {
                            Ray shadowRay = new(intersectPoint + smallOffset, light.Position(intersectPoint) - intersectPoint);
                            Vector3 returnlight = RecursiveRayShooter(shadowRay, screen, bouncesLeft - 2, out returndata, light);
                            result += DiffuseGlossCalculation(shadowRay.Direction * -1, light.Position(shadowRay.Origin) - shadowRay.Origin, prim.Normal(intersectPoint), returnlight, prim.SpecularColor, prim.GetColorFromTextureAtIntersect(intersectPoint), (light.Position(intersectPoint) - intersectPoint).LengthSquared, prim.specularity, prim.reflectiveness);
                        }

                    }

                    else if (prim.materialType == MaterialType.Transparent)
                    {
                        Vector3 normal = prim.Normal(intersectPoint);
                        float n = 1;
                        float nt = prim.refractiveIndex;

                        if (Vector3.Dot(ray.Direction, normal) > 0)
                        {
                            n = prim.refractiveIndex;
                            nt = 1;
                            normal *= -1;
                        }

                        

                        float R, R0;
                        float cosinePhi = MathF.Sqrt(1 - (MathF.Pow(n / nt, 2) * (1 - MathF.Pow(Vector3.Dot(ray.Direction, normal), 2))) );

                        if (cosinePhi != float.NaN)
                        {
                            float cosTheta = Vector3.Dot(-ray.Direction, normal);

                            R0 = MathF.Pow((nt - n) / (nt + n), 2);
                            R = R0 + MathF.Pow((1f - R0) * (1f - cosTheta), 5f);

                            Vector3 sinePhi = (n / nt) * (ray.Direction - (Vector3.Dot(ray.Direction, normal) * normal));
                            Vector3 refractedRayDirection = sinePhi - (cosinePhi * normal);
                            Ray refractedRay = new(intersectPoint - smallOffset, refractedRayDirection, "r");

                            // reflected
                            result += RecursiveRayShooter(reflectedRay, screen, bouncesLeft - 1, out returndata) * (prim.reflectiveness * prim.GetColorFromTextureAtIntersect(intersectPoint)) * R;
                            // refracted
                            result += RecursiveRayShooter(refractedRay, screen, bouncesLeft - 1, out returndata) * prim.GetColorFromTextureAtIntersect(intersectPoint) * (1 - R) ;

                            result *= prim.GetColorFromTextureAtIntersect(intersectPoint);
                        }
                        
                        else 
                        {
                            reflectedRay = new(intersectPoint + smallOffset, ray.Direction - (2 * Vector3.Dot(ray.Direction, normal)) * normal, "s");
                            result = RecursiveRayShooter(reflectedRay, screen, bouncesLeft - 1, out returndata);
                        } 
                    }
                }
            }
            // if nothing blocks the ray between the source and the light, return the light color
            if (aimedLight != null && !rayBlocked)
            {
                Vector3 lightPos = aimedLight.Position(ray.Origin);
                if ((aimedLight.GetType() == typeof(PlaneLight) && lightPos.X != float.NegativeInfinity) || aimedLight.GetType() != typeof(PlaneLight))
                {

                    intersectData = new(aimedLight.Color, (ray.Origin - lightPos).Length, null, true);
                    return aimedLight.Color;
                }
                intersectData = new(Scene.AmbientLight, float.NegativeInfinity, null);
                return Scene.AmbientLight;
            }

            intersectData = new(Scene.AmbientLight, float.NegativeInfinity, null);
            return result;
        }


        public static Vector3 DiffuseCalculation( Vector3 toLight, Vector3 normal, Vector3 LightColor, Vector3 diffuseColor, float distanceToLightSquared, float reflectiveness)
        {
            // calculate attenuation
            toLight.NormalizeFast();
            Vector3 distanceAttenuatedLight = LightColor / distanceToLightSquared;
            float AmountReflected = MathF.Max(0, Vector3.Dot(normal, toLight));
            return (Scene.AmbientLight * diffuseColor) + distanceAttenuatedLight * AmountReflected * diffuseColor;
        }

        public static Vector3 DiffuseGlossCalculation(Vector3 toOrigin, Vector3 toLight, Vector3 normal, Vector3 LightColor, Vector3 specularColor, Vector3 diffuseColor, float distanceToLightSquared, float specularity, float reflectiveness)
        {
            // normalise the vectors
            toOrigin.NormalizeFast(); toLight.NormalizeFast();

            // calculate attenuation
            Vector3 distanceAttenuatedLight = LightColor / distanceToLightSquared;

            // calculate diffuse and specular components
            Vector3 diffuseComponent = MathF.Max(0, Vector3.Dot(normal, toLight)) * diffuseColor * reflectiveness;
            Vector3 specularComponent = MathF.Pow(MathF.Max(0, Vector3.Dot(toOrigin, Vector3.NormalizeFast(toLight - 2 * Vector3.Dot(toLight, normal) * normal))), specularity) * specularColor;
            return (Scene.AmbientLight * diffuseColor) + distanceAttenuatedLight * (diffuseComponent + specularComponent);
        }

        

        public static int RGBtoINT(int r, int g, int b) { return (r << 16) + (g << 8) + b; }

        public static int RGBtoINT(Color color) { return (color.R << 16) + (color.G << 8) + color.B; }

        public static int RGBtoINT(Vector3 vector) { return ((int)(MathF.Min(1, vector.X) * 255) << 16) + ((int)(MathF.Min(1, vector.Y) * 255) << 8) + (int)(MathF.Min(1, vector.Z) * 255); } 

        public static Vector3 ClampToOne(Vector3 vector) { return new Vector3(MathF.Min(1, vector.X), MathF.Min(1, vector.Y), MathF.Min(1, vector.Z)); }
    }
}
