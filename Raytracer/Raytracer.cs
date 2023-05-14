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
        Surface screen;
        Surface debug;
        Scene scene;
        Camera camera;
        int bouncelimit = 5;

        public Raytracer(Surface screen, Surface debug)
        {
            this.screen = screen;
            this.debug = debug;
            camera = new Camera(screen, new(0, 0, 0), new(1, 0, 0), new(0, 1, 0), 100); ;


            scene = new Scene();
            scene.AddObjects(new Sphere(new(5, 0, 0), new(1, 0, 0), MaterialType.Diffuse, 1));
            scene.AddObjects(new Sphere(new(5, 0, 3), new(0, 1, 0), MaterialType.Gloss, 1));
            scene.AddObjects(new Sphere(new(5, 0, -3), new(1, 1, 1), MaterialType.Mirror, 1));
            scene.AddObjects(new Sphere(new(5, 3, -3), new(1, 0, 1), MaterialType.Gloss, 1));
            //scene.AddObjects(new Plane(new(0, -500, 0), new(0, 1, 0), new(0.5f, 0.5f, 0.5f), MaterialType.Mirror, 0.95f));
            scene.AddLight(new(new(0, 5, 0), new(1, 1, 1), 100));
            scene.AddLight(new(new(0,-5, 2), new(1,1,1), 10));

        }

        public void Render()
        {
            List<Primitive> primitives = scene.GetObjects();
            List<Light> lights = scene.GetLights();

            for (int y = 0; y < screen.height; y++)
            {
                for (int x = 0; x < screen.width; x++)
                {
                    Ray ray = camera.GetRayForPixelAt(x, y);
                    Vector3 color = RecursiveRayShooter(ray, primitives, lights, bouncelimit);
                    screen.Plot(x, y, RGBtoINT(color)); 
                }
            }
        }


        public Vector3 RecursiveRayShooter(Ray ray, List<Primitive> prims, List<Light> lights, int bouncesLeft, Light? aimedLight = null)
        {
            Vector3 result = new();
            bool rayBlocked = false;

            foreach(Primitive prim in prims)
            {
                IntersectData data = prim.Intersect(ray);

                if (data.intersectedPrimitive != null && data.distance > 0)
                {
                    Vector3 intersectPoint = ray.Origin + data.distance * ray.Direction;
                    Vector3 smallOffset = data.intersectedPrimitive.Normal(intersectPoint) * 0.001f;

                    // if the ray is aimed at a light, check if the primitive blocks the ray between it and the light
                    if (aimedLight != null) 
                        if (data.distance <= Vector3.Dot(aimedLight.Position - intersectPoint - smallOffset, ray.Direction)) 
                            rayBlocked = true;

                    // if the ray bounce limit hasn't been reached, send out new shadow rays
                    if (bouncesLeft > 0)
                    {
                        if (prim.materialType == MaterialType.Mirror)
                            result = RecursiveRayShooter(new(intersectPoint + smallOffset, ray.Direction * data.intersectedPrimitive.Normal(intersectPoint)), prims, lights, bouncesLeft - 1) * (prim.reflectiveness * prim.Color);

                        else if (prim.materialType == MaterialType.Diffuse)
                        {

                            foreach (Light light in lights)
                            {
                                Ray shadowRay = new(intersectPoint + smallOffset, light.Position - intersectPoint);
                                Vector3 returnlight = RecursiveRayShooter(shadowRay, prims, lights, bouncesLeft - 1, light);
                                result += DiffuseCalculation(shadowRay.Direction, prim.Normal(shadowRay.Origin), returnlight, prim.Color, (light.Position - intersectPoint - (smallOffset * 2)).LengthSquared, prim.reflectiveness);

                            }

                        }
                            
                        else if (prim.materialType == MaterialType.Gloss)
                        {
                            foreach (Light light in lights)
                            {
                                Ray shadowRay = new(intersectPoint + smallOffset, light.Position - intersectPoint);
                                Vector3 returnlight = RecursiveRayShooter(shadowRay, prims, lights, bouncesLeft - 1, light);
                                result += DiffuseCalculation(shadowRay.Direction, prim.Normal(shadowRay.Origin), returnlight, prim.Color, (light.Position - intersectPoint - (smallOffset * 2)).LengthSquared, prim.reflectiveness);
                                result += GlossCalculation(shadowRay.Direction * -1, light.Position - intersectPoint, prim.Normal(shadowRay.Origin), returnlight, (light.Position - shadowRay.Origin).LengthSquared, prim.specularity);
                            }
                            
                        }      
                    }
                }
            }

            // if nothing blocks the ray between the source and the light, return the light color
            if (aimedLight != null && !rayBlocked)
            {
                return aimedLight.Color;
            }


            return result;
        }


        public Vector3 DiffuseCalculation( Vector3 LightsourceDirection, Vector3 normal, Vector3 LightColor, Vector3 diffuseColor, float distanceToLightSquared, float reflectiveness)
        {
            return Scene.AmbientLight + (LightColor / (distanceToLightSquared ) * MathF.Max(0,Vector3.Dot(normal, LightsourceDirection))) * diffuseColor * reflectiveness;
        }
        public Vector3 GlossCalculation(Vector3 toOrigin, Vector3 toLight, Vector3 normal, Vector3 LightColor, float distanceToLightSquared, float specularity)
        {
            return (LightColor / distanceToLightSquared ) * MathF.Pow(MathF.Max(0, Vector3.Dot(toOrigin, Vector3.Normalize(toLight - 2* Vector3.Dot(toLight,normal) * normal))), specularity);
        }



        public static int RGBtoINT(int r, int g, int b) { return (r << 16) + (g << 8) + b; }

        public static int RGBtoINT(Color color) { return (color.R << 16) + (color.G << 8) + color.B; }

        public static int RGBtoINT(Vector3 vector) { return ((int)(MathF.Min(1, vector.X) * 255) << 16) + ((int)(MathF.Min(1, vector.Y) * 255) << 8) + (int)(MathF.Min(1, vector.Z) * 255); } 


    }
}
