using OpenTK.Mathematics;
using System.Drawing;

namespace Raytracing
{
    public class Raytracer
    {
        const int bouncelimit = 10; // how deep can the ray go?
        const int AntiAliasing = 2; // degree of supersampling, 2^n where n is the value you enter


        public Surface screen, debug;
        public Scene scene;
        public Camera camera;
        public static readonly int screenDivisions = (int)MathF.Sqrt(ThreadPool.ThreadCount);
        public bool[,] screenPortions = new bool[screenDivisions, screenDivisions];
        static readonly Vector3[,] SkyboxTexture = ImageLoader.LoadFromFile("Textures/skybox.jpg");
        static readonly List<Ray>[,] debugRayArray = new List<Ray>[screenDivisions, screenDivisions];
        Vector2i ViewTarget => new Vector2i((int)(debug.width * 0.5f), (int)(debug.height * 0.125f)) - (Vector2i)(new Vector2(camera.Position.Z, camera.Position.X) * M_to_Px);
        Vector2i singleSize => new(screen.width / screenDivisions, screen.height / screenDivisions);

        // Camera initialization values
        static Vector3 eyePosition = new(-2, 0, 0);
        static Vector3 viewingDirection = new(1, 0, 0);
        static Vector3 Up = new(0, 1, 0);
        static readonly float FOV = 100; // in degrees



        // values pertaining to debug view
        const float M_to_Px = 50; // conversion from meter to pixels
        const int raysShown = 12;


        /// <summary>
        /// Add all the objects of a scene in this constructor
        /// </summary>
        /// <param name="screen"></param>
        /// <param name="debug"></param>
        public Raytracer(Surface screen, Surface debug)
        {
            this.screen = screen;
            this.debug = debug;

            camera = new Camera(screen, eyePosition, viewingDirection, Up, FOV); ;
            scene = new Scene();

            scene.AddObjects(new Sphere(new(3, 0, 0), "Textures/Rockbits4.jpg", MaterialType.Diffuse, 1, 0.5f, 1.5f) { Direction = new(-0.5f, 0, 0.5f) });

            scene.AddObjects(new Sphere(new(3, 0, 3), new Vector3(1, 1, 1), MaterialType.Transparent, 1, 0.8f));

            scene.AddObjects(new Sphere(new(3, 0, -3), new Vector3(1, 1, 1), MaterialType.Mirror, 1, 1));

            scene.AddObjects(new Triangle(new Vector3[3] { new(6, 2, -2), new(6, 6, 0), new(6, 2, 2) }, new Vector3(1, 1, 1), MaterialType.Diffuse, 1f));


            // scene.AddObjects(new Plane(new(0, -1, 0), new(0, 1, 0),"Textures/rockfloor.jpg", null, new(1,1.4f), MaterialType.Diffuse, 0.8f));

            //scene.AddObjects(new Plane(new(8, 0, 0), new(-1, 0, 0), "Textures/shrek.jpg", null, new( 1.78f,1), MaterialType.Diffuse, 0.8f));
            //scene.AddObjects(new Plane(new(0, 0, -5), new(0, 0, 1), "Textures/shrek.jpg", null, new( 1.78f,1), MaterialType.Diffuse, 0.8f));
            //scene.AddObjects(new Plane(new(0, 0, 5), new(0, 0, -1), "Textures/shrek.jpg", null, new( 1.78f,1), MaterialType.Diffuse, 0.8f));
            //scene.AddObjects(new Plane(new(-5, 0, 0), new(1, 0, 0), "Textures/shrek.jpg", null, new( 1.78f, 1), MaterialType.Diffuse, 0.8f));

            scene.AddLight(new Light(new(0, 10, 0), new(1, 1, 1), 100));
            //scene.AddLight(new PlaneLight(new(0, 10, 0), new(0,-1,0), new(1,1,1), 100));

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


            // debug view (top down orthogonal projection)
            Vector2i camPos = new((int)(camera.Position.Z * M_to_Px), (int)(camera.Position.X * M_to_Px));

            debug.Clear(0x000000);

            // Camera
            debug.Box(
                camPos.X + ViewTarget.X - 1,
                camPos.Y + ViewTarget.Y - 1,
                camPos.X + ViewTarget.X + 1,
                camPos.Y + ViewTarget.Y + 1,
                RGBtoINT(Color.Red));
            // Plane
            Vector4 debugPlane = camera.debugPlane * M_to_Px;
            debug.Line(
                (int)debugPlane.X + ViewTarget.X,
                (int)debugPlane.Y + ViewTarget.Y,
                (int)debugPlane.Z + ViewTarget.X,
                (int)debugPlane.W + ViewTarget.Y,
                RGBtoINT(Color.White));
            // Viewing direction
            debug.Line(
                (int)camPos.X + ViewTarget.X,
                (int)camPos.Y + ViewTarget.Y,
                (int)camPos.X + (int)(Vector2.NormalizeFast(new Vector2(camera.Direction.Z, camera.Direction.X)) * M_to_Px).X + ViewTarget.X,
                (int)camPos.Y + (int)(Vector2.NormalizeFast(new Vector2(camera.Direction.Z, camera.Direction.X)) * M_to_Px).Y + ViewTarget.Y,
                RGBtoINT(Color.Blue));



            // Lights
            foreach (Light light in scene.Lightsources)
            {
                debug.Plot((int)(ViewTarget.X + light.Position(camera.Position).Z * M_to_Px), (int)(ViewTarget.Y + light.Position(camera.Position).X * M_to_Px), RGBtoINT(light.Color));
                debug.DrawSphere(ViewTarget + (new Vector2(light.Position(camera.Position).Z * M_to_Px, light.Position(camera.Position).X * M_to_Px)), 0.05f * M_to_Px, RGBtoINT(light.Color));
            }



            // Primitives
            foreach (Primitive prim in scene.Objects)
            {
                if (prim.GetType() == typeof(Sphere))
                    debug.DrawSphere(ViewTarget + (new Vector2(prim.Position.Z * M_to_Px, prim.Position.X * M_to_Px)), ((Sphere)prim).radius * M_to_Px, RGBtoINT(prim.averageColor));

                if (prim.GetType() == typeof(Triangle))
                {
                    Vector3[] vertices = ((Triangle)prim).Vertices;
                    for (int i = 0; i < 3; i++) vertices[i] = (vertices[i] * M_to_Px);
                    debug.Line(
                        ViewTarget.X + (int)vertices[0].Z, ViewTarget.Y + (int)vertices[0].X,
                        ViewTarget.X + (int)vertices[1].Z, ViewTarget.Y + (int)vertices[1].X,
                        RGBtoINT(prim.averageColor)
                        );
                    debug.Line(
                        ViewTarget.X + (int)vertices[1].Z, ViewTarget.Y + (int)vertices[1].X,
                        ViewTarget.X + (int)vertices[2].Z, ViewTarget.Y + (int)vertices[2].X,
                        RGBtoINT(prim.averageColor)
                        );
                    debug.Line(
                        ViewTarget.X + (int)vertices[2].Z, ViewTarget.Y + (int)vertices[2].X,
                        ViewTarget.X + (int)vertices[0].Z, ViewTarget.Y + (int)vertices[0].X,
                        RGBtoINT(prim.averageColor)
                        );
                }
            }


            // Rays
            foreach (List<Ray> list in debugRayArray) if (list != null) foreach (Ray ray in list)
                    {
                        var color = ray.raytype switch
                        {
                            'p' => RGBtoINT(Color.White),
                            '2' => RGBtoINT(Color.Green),
                            's' => RGBtoINT(Color.Yellow),
                            'r' => RGBtoINT(Color.Blue),
                            _ => RGBtoINT(Color.Red),
                        };
                        ;

                        Vector3 raydir = ray.Direction * ray.intersectDistance;

                        debug.Line(
                            (int)(ray.Origin.Z * M_to_Px) + ViewTarget.X,
                            (int)(ray.Origin.X * M_to_Px) + ViewTarget.Y,
                            (int)(ray.Origin.Z * M_to_Px) + (int)((new Vector2(raydir.Z, raydir.X)) * M_to_Px).X + ViewTarget.X,
                            (int)(ray.Origin.X * M_to_Px) + (int)((new Vector2(raydir.Z, raydir.X)) * M_to_Px).Y + ViewTarget.Y,
                            color);
                    }

            debug.Print("White  = Primary Ray", 10, 10, RGBtoINT(Color.White));
            debug.Print("Green  = Reflected Ray", 10, 35, RGBtoINT(Color.Green));
            debug.Print("Blue   = Refracted Ray", 10, 60, RGBtoINT(Color.Blue));
            debug.Print("Yellow = Shadow Ray", 10, 85, RGBtoINT(Color.Yellow));
        }


        /// <summary>
        /// Returns an unassigned screen quadrant, depending on number of threads available
        /// </summary>
        /// <returns></returns>
        public Vector4i GetAssignment()
        {
            
            for (int y = 0; y < screenDivisions; y++)
                for (int x = 0; x < screenDivisions; x++) if (!screenPortions[x, y])
                    {
                        screenPortions[x, y] = true;
                        Vector4i result = new()
                        {
                            Xy = singleSize * new Vector2i(x, y),
                            Zw = singleSize * new Vector2i(x + 1, y + 1) - Vector2i.One
                        };
                        return result;
                    }
            return -Vector4i.One;
        }


        /// <summary>
        /// Worker for Multithreading
        /// </summary>
        public void RenderWorker()
        {
            List<Ray> debugRays = new();
            // get assigned area, if all areas are assigned, return
            Vector4i AssignedArea = GetAssignment();
            if (AssignedArea == -Vector4.One) return;

            // go through each pixel in the assigned area, start raytracing
            for (int y = AssignedArea.Y; y <= AssignedArea.W; y++)
                for (int x = AssignedArea.X; x <= AssignedArea.Z; x++)
                {
                    Vector3 color = new();

                    // Antialiasing by supersampling
                    for (float yoffset = 0; yoffset < AntiAliasing; yoffset++)
                        for (float xoffset = 0; xoffset < AntiAliasing; xoffset++)
                        {
                            Ray ray = camera.GetRayForPixelAt(x + (xoffset / AntiAliasing), y + (yoffset / AntiAliasing), bouncelimit,
                                y == screen.height / 2 && x % ((int)((screen.width - 1) / raysShown)) == 0 && yoffset == 0 && xoffset == 0);
                            color += RecursiveRayShooter(ray, screen, ref debugRays, out _);
                        }
                    color /= AntiAliasing * AntiAliasing;
                    screen.Plot(x, y, RGBtoINT(ClampToOne(color)));
                }
            debugRays.Reverse();

            // replace the last hashset of rays with the updated one
            debugRayArray[AssignedArea.X / (screen.width / screenDivisions), AssignedArea.Y / (screen.height / screenDivisions)] = debugRays;

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
        public Vector3 RecursiveRayShooter(Ray ray, Surface screen, ref List<Ray> debugRays, out IntersectData intersectData, Light? aimedLight = null)
        {
            Vector3 result = new();
            bool rayBlocked = false;
            List<Primitive> prims = scene.Objects;
            List<Light> lights = scene.Lightsources;
            Primitive? prim = null;
            IntersectData data = new();

            foreach (Primitive candidate in prims)
            {
                IntersectData candidateData = candidate.Intersect(ray);

                if (candidateData.intersectedPrimitive != null && candidateData.distance > 0)
                {
                    if (prim == null) { prim = candidate; data = candidateData; }
                    else if (data.distance > candidateData.distance) 
                    { 
                        prim = candidate;
                        data = candidateData;

                        // if the ray is aimed at a light, check if the primitive blocks the ray between it and the light
                        if (aimedLight != null && !rayBlocked)
                            if (candidateData.distance >= (aimedLight.Position(ray.Origin) - ray.Origin).Length)
                                rayBlocked = true;
                    }
                }
            }

            // if there was a light and nothing blocks it, skip the primitive color calculation and go straight to the light return

            if (prim != null && (aimedLight != null  && rayBlocked || aimedLight == null))
            {
                Vector3 intersectPoint = ray.Origin + data.distance * ray.Direction;

                Vector3 normal = Vector3.Dot(ray.Direction, prim.Normal(intersectPoint)) > 0 ? -prim.Normal(intersectPoint) : prim.Normal(intersectPoint);

                Vector3 smallOffset = normal * 0.001f;

                

                // if the ray bounce limit hasn't been reached, send out new shadow rays
                if (ray.bouncesLeft > 0)
                {
                    IntersectData returndata;
                    Ray reflectedRay = new(intersectPoint + smallOffset, ray.Direction - (2 * Vector3.Dot(ray.Direction, normal)) * normal, ray.bouncesLeft - 1, '2', ray.sendToDebug);

                    // Algorithm derived from the slides
                    if (prim.materialType == MaterialType.Mirror)
                    {
                        result = RecursiveRayShooter(reflectedRay, screen, ref debugRays, out _) * (prim.reflectiveness * prim.GetColorFromTextureAtIntersect(intersectPoint));
                    }

                    // Algorithm derived from the slides
                    if (prim.materialType == MaterialType.DiffuseMirror)
                    {
                        foreach (Light light in lights)
                        {
                            Ray shadowRay = new(intersectPoint + smallOffset, light.Position(ray.Origin) - intersectPoint, ray.bouncesLeft - 1, 's', ray.sendToDebug);
                            Vector3 returnlight = RecursiveRayShooter(shadowRay, screen, ref debugRays, out returndata, light);
                            result += DiffuseCalculation(shadowRay.Direction, prim.Normal(shadowRay.Origin), returnlight, prim.GetColorFromTextureAtIntersect(intersectPoint), (light.Position(intersectPoint) - intersectPoint - (smallOffset * 2)).LengthSquared);
                        }

                        result += RecursiveRayShooter(reflectedRay, screen, ref debugRays, out _) * (prim.reflectiveness * prim.GetColorFromTextureAtIntersect(intersectPoint)) * prim.reflectiveness;
                    }

                    // Algorithm derived from the slides
                    else if (prim.materialType == MaterialType.Diffuse)
                    {
                        foreach (Light light in lights)
                        {
                            Ray shadowRay = new(intersectPoint + smallOffset, light.Position(intersectPoint) - intersectPoint, ray.bouncesLeft - 1, 's', ray.sendToDebug);
                            Vector3 returnlight = RecursiveRayShooter(shadowRay, screen, ref debugRays, out returndata, light);
                            result += DiffuseCalculation(shadowRay.Direction, prim.Normal(shadowRay.Origin), returnlight, prim.GetColorFromTextureAtIntersect(intersectPoint), (light.Position(intersectPoint) - intersectPoint - (smallOffset * 2)).LengthSquared);
                        }

                    }

                    // Algorithm derived from the slides
                    else if (prim.materialType == MaterialType.Gloss)
                    {

                        foreach (Light light in lights)
                        {
                            Ray shadowRay = new(intersectPoint + smallOffset, light.Position(intersectPoint) - intersectPoint, ray.bouncesLeft - 1, 's', ray.sendToDebug);
                            Vector3 returnlight = RecursiveRayShooter(shadowRay, screen, ref debugRays, out returndata, light);
                            result += DiffuseGlossCalculation(shadowRay.Direction * -1, light.Position(shadowRay.Origin) - shadowRay.Origin, normal, returnlight, prim.SpecularColor, prim.GetColorFromTextureAtIntersect(intersectPoint), (light.Position(intersectPoint) - intersectPoint).LengthSquared, prim.specularity, prim.reflectiveness);
                        }

                    }


                    // Algorithm derived from the slides
                    else if (prim.materialType == MaterialType.Transparent)
                    {
                        float n = 1;
                        float nt = prim.refractiveIndex;
                        Vector3 d = ray.Direction;
                        char refractedRayType = 'r';

                        // if the ray originated inside the object, invert breaking indeces and normal
                        if (prim.GetType() == typeof(Sphere)) if ((ray.Origin - prim.Position).Length < ((Sphere)prim).radius)
                        {
                            n = prim.refractiveIndex;
                            nt = 1;
                            reflectedRay.bouncesLeft--;
                            }

                        reflectedRay = new(intersectPoint + smallOffset, ray.Direction - (2 * Vector3.Dot(ray.Direction, normal)) * normal, ray.bouncesLeft - 1, '2', ray.sendToDebug);

                        float R, R0;

                        float underTheRoot = 1 - ((n * n) / (nt * nt)) * (1 - Vector3.Dot(ray.Direction, normal) * Vector3.Dot(ray.Direction, normal));

                        if (underTheRoot >= 0)
                        {
                            R0 = MathF.Pow((nt - n) / (nt + n), 2);
                            R = R0 + MathF.Pow((1f - R0) * (1f - MathF.Cos(Vector3.CalculateAngle(ray.Direction, -normal))), 5);

                            Vector3 refractedRayDirection = (n / nt) * (d - Vector3.Dot(d, normal) * normal) - MathF.Sqrt(underTheRoot) * normal;
                            Ray refractedRay = new(intersectPoint - smallOffset, refractedRayDirection, ray.bouncesLeft - 1, refractedRayType, ray.sendToDebug);

                            // reflected
                            result += RecursiveRayShooter(reflectedRay, screen, ref debugRays, out _) * (prim.reflectiveness * prim.GetColorFromTextureAtIntersect(intersectPoint)) * R;
                            // refracted
                            result += RecursiveRayShooter(refractedRay, screen, ref debugRays, out _) * prim.GetColorFromTextureAtIntersect(intersectPoint) * (1 - R);

                            result *= prim.GetColorFromTextureAtIntersect(intersectPoint);
                        }

                        else result = RecursiveRayShooter(reflectedRay, screen, ref debugRays, out _);
                    }
                }
            }
            // if nothing blocks the ray between the source and the light, return the light color
            if (aimedLight != null && !rayBlocked)
            {
                Vector3 lightPos = aimedLight.Position(ray.Origin);
                if ((aimedLight.GetType() == typeof(PlaneLight) && lightPos.X != float.NegativeInfinity) || aimedLight.GetType() != typeof(PlaneLight))
                {
                    intersectData = new(aimedLight.Color, (lightPos - ray.Origin).Length, null, true);
                    ray.intersectDistance = intersectData.distance;
                    if (ray.sendToDebug) debugRays.Add(ray);
                    return aimedLight.Color;
                }
            }

            // If there was something blocking it, return the resulting data
            if (prim != null)
            {
                intersectData = data;
                ray.intersectDistance = data.distance;
                if (ray.sendToDebug) debugRays.Add(ray);
                return result;
            }

            // if the ray is not aimed at a light, or the ray is blocked and there are no more bounces left, return the skybox
            // or if the ray is shot into narnia, then also return the skybox, or skydome because i coded it to be spherical
            result = GetSkyboxColor(ray);
            intersectData = new(result, data.distance, null);
            ray.intersectDistance = 1000;
            if (ray.sendToDebug) debugRays.Add(ray);
            return result;
        }

        public static Vector3 GetSkyboxColor(Ray ray)
        {
            // This is really just the same math i used for texture mapping a sphere, just without the sphere component
            int x, y;
            Vector3 normal = Vector3.Normalize(ray.Direction);

            x = (int)Math.Round((SkyboxTexture.GetLength(0) - 1) * (1 - (MathF.Atan2(normal.X, normal.Z) / (2 * MathF.PI) + 0.5f)));
            y = (int)Math.Round((SkyboxTexture.GetLength(1) - 1) * -((normal.Y - 1) / 2));

            return SkyboxTexture[x, y];
        }


        // the following algorithms are derived from the slides
        public static Vector3 DiffuseCalculation(Vector3 toLight, Vector3 normal, Vector3 LightColor, Vector3 diffuseColor, float distanceToLightSquared)
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


        // quick helper methods
        public static int RGBtoINT(int r, int g, int b) { return (r << 16) + (g << 8) + b; }

        public static int RGBtoINT(Color color) { return (color.R << 16) + (color.G << 8) + color.B; }

        public static int RGBtoINT(Vector3 vector) { return ((int)(MathF.Min(1, vector.X) * 255) << 16) + ((int)(MathF.Min(1, vector.Y) * 255) << 8) + (int)(MathF.Min(1, vector.Z) * 255); }

        public static Vector3 ClampToOne(Vector3 vector) { return new Vector3(MathF.Min(1, vector.X), MathF.Min(1, vector.Y), MathF.Min(1, vector.Z)); }
    }
}
