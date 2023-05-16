using OpenTK.Graphics.ES11;
using OpenTK.Mathematics;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK.Audio.OpenAL.Extensions.Creative.EFX;

namespace Raytracing
{
    public class Raytracer
    {
        public Surface screen;
        public Surface debug;
        public Scene scene;
        public Camera camera;
        static int bouncelimit = 3;
        static int AntiAliasing = 4;
        static int screenDivisions = (int)MathF.Sqrt(ThreadPool.ThreadCount);

        static bool[,] screenPortions = new bool[screenDivisions, screenDivisions];

        


        public Raytracer(Surface screen, Surface debug)
        {
            this.screen = screen;
            this.debug = debug;
            camera = new Camera(screen, new(-5, 0, 0), new(1, 0, 0), new(0, 1, 0), 90); ;
            scene = new Scene();

            scene.AddObjects(new Sphere(new(5, 0, 0), new(1, 0, 0), MaterialType.Diffuse, 1));
            scene.AddObjects(new Sphere(new(5, 0, 3), new(0, 1, 0), MaterialType.Gloss, 1));
            scene.AddObjects(new Sphere(new(6, 0, -3), new(1, 1, 1), MaterialType.Mirror, 1, 0.95f));

            scene.AddObjects(new Triangle(new Vector3[3] { new(8, 0, 2), new(7, 4, 0), new(8, 0, -2) }, new(0.9f, 1, 0.9f), MaterialType.Mirror, 1f));

            scene.AddObjects(new Plane(new(5, -1, 0), new(0, 1, 0), new(0.5f, 0.5f, 0.5f), MaterialType.Diffuse, 0.95f));
            scene.AddObjects(new Plane(new(10, 0, 0), new(-1, 0, 0), new(1, 1, 1), MaterialType.Mirror, 0.8f));
            scene.AddObjects(new Plane(new(-15, 0, 0), new(1, 0, 0), new(1, 1, 1), MaterialType.Mirror, 0.8f));

            scene.AddLight(new(new(3, 5, 0), new(1, 1, 1), 25));

        }

        /// <summary>
        /// Starts the threads
        /// </summary>
        public void Render()
        {
            for (int i = 0; i < screenDivisions * screenDivisions; i++)
                Task.Run(RenderWorker);
        }


        /// <summary>
        /// Returns an assigned screen quadrant, depending on number of threads available
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
            Vector4i AssignedArea = GetAssignment();
            if (AssignedArea == -Vector4.One) return;
            for (int y = AssignedArea.Y; y <= AssignedArea.W; y++)
                for (int x = AssignedArea.X; x <= AssignedArea.Z; x++)
                {
                    Vector3 color = new();

                    IntersectData returnData;

                    // Antialiasing
                    for (float yoffset = 0; yoffset < AntiAliasing; yoffset++)
                        for (float xoffset = 0; xoffset < AntiAliasing; xoffset++)
                        {
                            Ray ray = camera.GetRayForPixelAt(x + (xoffset / AntiAliasing), y + (yoffset / AntiAliasing));
                            color += RecursiveRayShooter(ray, screen, bouncelimit, out returnData);
                        }
                    color /= (float)(AntiAliasing * AntiAliasing);
                    screen.Plot(x, y, RGBtoINT(color));
                }
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
        public Vector3 RecursiveRayShooter(Ray ray, Surface screen, int bouncesLeft, out IntersectData intersectData, Light? aimedLight = null )
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
                    if (data.distance <= Vector3.Dot(aimedLight.Position - intersectPoint - smallOffset, ray.Direction))
                        rayBlocked = true;

                // if the ray bounce limit hasn't been reached, send out new shadow rays
                if (bouncesLeft > 0)
                {
                    IntersectData returndata;
                    Ray reflectedRay = new(intersectPoint + smallOffset, ray.Direction - (2 * Vector3.Dot(ray.Direction, prim.Normal(intersectPoint))) * prim.Normal(intersectPoint), "s");

                    if (prim.materialType == MaterialType.Mirror)
                    {
                        result = RecursiveRayShooter(reflectedRay, screen, bouncesLeft - 1, out returndata) * (prim.reflectiveness * prim.DiffuseColor);
                    }
                    else if (prim.materialType == MaterialType.Diffuse)
                    {
                        foreach (Light light in lights)
                        {
                            Ray shadowRay = new(intersectPoint + smallOffset, light.Position - intersectPoint);
                            Vector3 returnlight = RecursiveRayShooter(shadowRay, screen, bouncesLeft - 1, out returndata, light);
                            result += DiffuseCalculation(shadowRay.Direction, prim.Normal(shadowRay.Origin), returnlight, prim.DiffuseColor, (light.Position - intersectPoint - (smallOffset * 2)).LengthSquared, prim.reflectiveness);

                        }

                    }

                    else if (prim.materialType == MaterialType.Gloss)
                    {

                        foreach (Light light in lights)
                        {
                            Ray shadowRay = new(intersectPoint + smallOffset, light.Position - intersectPoint);
                            Vector3 returnlight = RecursiveRayShooter(shadowRay, screen, bouncesLeft - 2, out returndata, light);
                            result += DiffuseGlossCalculation(shadowRay.Direction * -1, light.Position - shadowRay.Origin, prim.Normal(shadowRay.Origin), returnlight, prim.SpecularColor, prim.DiffuseColor, (light.Position - intersectPoint).LengthSquared, prim.specularity, prim.reflectiveness);
                        }

                    }
                }
            }
            // if nothing blocks the ray between the source and the light, return the light color
            if (aimedLight != null && !rayBlocked)
            {
                intersectData = new(aimedLight.Color, (ray.Origin - aimedLight.Position).Length , null, true);
                return aimedLight.Color;
            }

            intersectData = new(Scene.AmbientLight, float.NegativeInfinity, null);
            return result;
        }


        public static Vector3 DiffuseCalculation( Vector3 toLight, Vector3 normal, Vector3 LightColor, Vector3 diffuseColor, float distanceToLightSquared, float reflectiveness)
        {
            Vector3 distanceAttenuatedLight = LightColor / distanceToLightSquared;
            float AmountReflected = MathF.Max(0, Vector3.Dot(normal, toLight));
            return (Scene.AmbientLight * diffuseColor) + distanceAttenuatedLight * AmountReflected * diffuseColor;
        }

        public static Vector3 DiffuseGlossCalculation(Vector3 toOrigin, Vector3 toLight, Vector3 normal, Vector3 LightColor, Vector3 specularColor, Vector3 diffuseColor, float distanceToLightSquared, float specularity, float reflectiveness)
        {
            Vector3 distanceAttenuatedLight = LightColor / distanceToLightSquared;
            Vector3 diffuseComponent = MathF.Max(0, Vector3.Dot(normal, toLight)) * diffuseColor * reflectiveness;
            Vector3 specularComponent = MathF.Pow(MathF.Max(0, Vector3.Dot(toOrigin, Vector3.Normalize(toLight - 2 * Vector3.Dot(toLight, normal) * normal))), specularity) * specularColor;
            return (Scene.AmbientLight * diffuseColor) + distanceAttenuatedLight * (diffuseComponent + specularComponent);
        }

        public void MoveCamera(Vector3 relativeDirection, float degreePitch = 0, float degreeYaw = 0, float degreeRoll = 0, float fovChange = 0)
        {
            // movement calculation
            camera.Position += new Vector3(Vector3.Dot(relativeDirection, camera.Direction), Vector3.Dot(relativeDirection, camera.Up), Vector3.Dot(relativeDirection, camera.Right));
            camera.UpdateVectors();
            // roll calculation
            camera.Up = MathF.Cos(degreeRoll * (MathF.PI / 180)) * camera.Up + MathF.Sin(degreeRoll * (MathF.PI / 180)) * (Vector3.Cross(camera.Up, camera.Direction));
            camera.UpdateVectors();
            // pitch calculation
            camera.Up = MathF.Cos(degreePitch * (MathF.PI / 180)) * camera.Up + MathF.Sin(degreePitch * (MathF.PI / 180)) * (Vector3.Cross(camera.Up, camera.Right));
            camera.Direction = MathF.Cos(degreePitch * (MathF.PI / 180)) * camera.Direction + MathF.Sin(degreePitch * (MathF.PI / 180)) * (Vector3.Cross(camera.Direction, camera.Right));
            camera.UpdateVectors();
            // yaw calculation
            camera.Direction = MathF.Cos(degreeYaw * (MathF.PI / 180)) * camera.Direction + MathF.Sin(degreeYaw * (MathF.PI / 180)) * (Vector3.Cross(camera.Direction, camera.Up));
            camera.UpdateVectors();
            // FOV calcualtion
            camera.ChangeFOV(fovChange);
            camera.UpdateVectors();
        }

        public static int RGBtoINT(int r, int g, int b) { return (r << 16) + (g << 8) + b; }

        public static int RGBtoINT(Color color) { return (color.R << 16) + (color.G << 8) + color.B; }

        public static int RGBtoINT(Vector3 vector) { return ((int)(MathF.Min(1, vector.X) * 255) << 16) + ((int)(MathF.Min(1, vector.Y) * 255) << 8) + (int)(MathF.Min(1, vector.Z) * 255); } 
    }
}
