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

namespace Raytracing
{
    public class Raytracer
    {
        public static Surface screen;
        public static Surface debug;
        public static Scene scene;
        public static Camera camera;
        static int bouncelimit = 4;
        static int AntiAliasing = 0; // 0 = none, 1 = 4x, 2 = 8x, 3 = 16x
        static int screenDivisions = (int)MathF.Sqrt(ThreadPool.ThreadCount);

        static bool[,] screenPortions = new bool[screenDivisions, screenDivisions];

        

        public static List<Primitive> primitives => scene?.GetObjects();
        public static List<Light> lights => scene?.GetLights();

        public Raytracer(Surface screen, Surface debug)
        {
            Raytracer.screen = screen;
            Raytracer.debug = debug;
            camera = new Camera(screen, new(-5, 0, 0), new(1, 0, 0), new(0, 1, 0), 90); ;


            scene = new Scene();
            scene.AddObjects(new Sphere(new(5, 0, 0), new(1, 0, 0), MaterialType.Diffuse, 1));
            scene.AddObjects(new Sphere(new(5, 0, 3), new(0, 1, 0), MaterialType.Gloss, 1));
            scene.AddObjects(new Sphere(new(6, 0, -3), new(1, 1, 1), MaterialType.Mirror, 1, 0.95f));
            scene.AddObjects(new Sphere(new(5, 3, -3), new(1, 0, 1), MaterialType.Gloss, 1));

            scene.AddObjects(new Plane(new(5, -1, 0), new(0, 1, 0), new(0.5f, 0.5f, 0.5f), MaterialType.Diffuse, 0.95f));
            scene.AddObjects(new Plane(new(10, 0, 0), new(-1, 0, 0), new(1, 1, 1), MaterialType.Mirror, 0.8f));
            scene.AddObjects(new Plane(new(-15, 0, 0), new(1, 0, 0), new(1, 1, 1), MaterialType.Mirror, 0.8f));

            scene.AddObjects(new Triangle(new Vector3[3] { new(8, 0, 2), new(8, 4, 0), new(8, 0, -2) }, new(0.9f, 1, 0.9f), MaterialType.Mirror, 1f));
            scene.AddLight(new(new(5, 5, 7), new(1, 1, 1), 100));



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
        public static Vector4i GetAssignment()
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
        public static void RenderWorker()
        {
            Vector4i AssignedArea = GetAssignment();
            if (AssignedArea == -Vector4.One) return;
            for (int y = AssignedArea.Y; y <= AssignedArea.W; y++)
                for (int x = AssignedArea.X; x <= AssignedArea.Z; x++)
                {
                    Vector3 color = new();


                    // Antialiasing
                    //for (float yoffset = 0; yoffset <= AntiAliasing; yoffset++)
                    //    for (float xoffset = 0; xoffset <= AntiAliasing; xoffset++)
                    //    {
                    //        Ray ray = camera.GetRayForPixelAt(x + (-0.5f + xoffset / AntiAliasing), y + (-0.5f + xoffset / AntiAliasing));
                    //        color += RecursiveRayShooter(ray, primitives, lights, bouncelimit);
                    //    }

                    Ray ray = camera.GetRayForPixelAt(x, y );
                    IntersectData returnData;
                    color += RecursiveRayShooter(ray, primitives, lights, bouncelimit, out returnData);
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
        public static Vector3 RecursiveRayShooter(Ray ray, List<Primitive> prims, List<Light> lights, int bouncesLeft, out IntersectData intersectData, Light? aimedLight = null )
        {
            Vector3 result = new();
            bool rayBlocked = false;
            Random random = new Random();

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
                        result = RecursiveRayShooter(reflectedRay, prims, lights, bouncesLeft - 1, out returndata) * (prim.reflectiveness * prim.Color);
                        foreach (Light light in lights)
                        {
                            Ray shadowRay = new(intersectPoint + smallOffset, light.Position - intersectPoint);
                            Vector3 returnlight = RecursiveRayShooter(shadowRay, prims, lights, bouncesLeft - 1, out returndata, light);
                            result += GlossCalculation(shadowRay.Direction * -1, light.Position - intersectPoint, prim.Normal(shadowRay.Origin), returnlight, (light.Position - shadowRay.Origin).LengthSquared, prim.specularity);
                        }
                    }
                    else if (prim.materialType == MaterialType.Diffuse)
                    {
                        Vector3 reflectedLight = RecursiveRayShooter(reflectedRay, prims, lights, bouncesLeft - 1, out returndata);
                        result += DiffuseCalculation(reflectedRay.Direction, prim.Normal(reflectedRay.Origin), reflectedLight, prim.Color, returndata.distance * returndata.distance, prim.reflectiveness);
                        foreach (Light light in lights)
                        {
                            Ray shadowRay = new(intersectPoint + smallOffset, light.Position - intersectPoint);
                            Vector3 returnlight = RecursiveRayShooter(shadowRay, prims, lights, bouncesLeft - 1, out returndata, light);
                            result += DiffuseCalculation(shadowRay.Direction, prim.Normal(shadowRay.Origin), returnlight, prim.Color, (light.Position - intersectPoint - (smallOffset * 2)).LengthSquared, prim.reflectiveness);

                        }

                    }

                    else if (prim.materialType == MaterialType.Gloss)
                    {
                        Vector3 reflectedLight = RecursiveRayShooter(reflectedRay, prims, lights, bouncesLeft - 1, out returndata);
                        result += DiffuseCalculation(reflectedRay.Direction, prim.Normal(reflectedRay.Origin), reflectedLight, prim.Color, returndata.distance, prim.reflectiveness);

                        foreach (Light light in lights)
                        {
                            Ray shadowRay = new(intersectPoint + smallOffset, light.Position - intersectPoint);
                            Vector3 returnlight = RecursiveRayShooter(shadowRay, prims, lights, bouncesLeft - 2, out returndata, light);
                            result += DiffuseCalculation(shadowRay.Direction, prim.Normal(shadowRay.Origin), returnlight, prim.Color, (light.Position - intersectPoint - (smallOffset * 2)).LengthSquared, prim.reflectiveness);
                            result += GlossCalculation(shadowRay.Direction * -1, light.Position - intersectPoint, prim.Normal(shadowRay.Origin), returnlight, (light.Position - shadowRay.Origin).LengthSquared, prim.specularity);
                        }

                    }
                }
            }
            // if nothing blocks the ray between the source and the light, return the light color
            if (aimedLight != null && !rayBlocked)
            {
                intersectData = closest.Value.Value;
                return aimedLight.Color;
            }

            intersectData = new(Scene.AmbientLight, float.NegativeInfinity, null);
            return result;
        }


        public static Vector3 DiffuseCalculation( Vector3 LightsourceDirection, Vector3 normal, Vector3 LightColor, Vector3 diffuseColor, float distanceToLightSquared, float reflectiveness)
        {
            return Scene.AmbientLight * diffuseColor + (LightColor / (distanceToLightSquared ) * MathF.Max(0,Vector3.Dot(normal, LightsourceDirection))) * diffuseColor * reflectiveness;
        }


        // Specular reflection using Phong Method
        public static Vector3 GlossCalculation(Vector3 toOrigin, Vector3 toLight, Vector3 normal, Vector3 LightColor, float distanceToLightSquared, float specularity)
        {
            return Scene.AmbientLight + (LightColor / distanceToLightSquared ) * MathF.Pow(MathF.Max(0, Vector3.Dot(toOrigin, Vector3.Normalize(toLight - 2* Vector3.Dot(toLight,normal) * normal))), specularity);
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
