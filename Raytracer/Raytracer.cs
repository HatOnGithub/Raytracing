using OpenTK.Mathematics;
using System.Drawing;
using System.Windows.Markup;

namespace Raytracing
{
    public class Raytracer
    {
        private const int bouncelimit = 5; // how deep can the ray go?
        private const int AntiAliasing = 2; // degree of supersampling, 2^n where n is the value you enter


        public Surface screen, debug;
        public Scene scene;
        public Camera camera;
        public static readonly int screenDivisions = (int)MathF.Sqrt(ThreadPool.ThreadCount);
        public bool[,] screenPortions = new bool[screenDivisions, screenDivisions];
        private static readonly Vector3[,] SkyboxTexture = ImageLoader.LoadFromFile("Textures/skybox.jpg");
        private static readonly List<Ray>[,] debugRayArray = new List<Ray>[screenDivisions, screenDivisions];

        private Vector2i ViewTarget => new Vector2i((int)(debug.width * 0.5f), (int)(debug.height * 0.125f)) - (Vector2i)(new Vector2(camera.Position.Z, camera.Position.X) * M_to_Px);

        private Vector2i SinglePartitionSize => new(screen.width / screenDivisions, screen.height / screenDivisions);

        // Camera initialization values
        private static Vector3 eyePosition = new(-2, 0, 0);
        private static Vector3 viewingDirection = new(1, 0, 0);
        private static Vector3 Up = new(0, 1, 0);
        private static readonly float FOV = 100; // in degrees



        // values pertaining to debug view
        public float M_to_Px = 50; // conversion from meter to pixels
        public int raysShown => (int)Math.Round(raysShownf);
        public float raysShownf = 12;


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

            scene.AddObjects(new Sphere(new(3, 0, 0.95f), "Textures/Rockbits4.jpg", MaterialType.Diffuse, 0.7f, 0.5f, 1.5f) { Direction = new(-0.5f, 0, 0.5f) });

            scene.AddObjects(new Sphere(new(3, 0, -0.95f), "Textures/copperSphere.jpg", MaterialType.Gloss, 0.7f, 1, 1.6f) { Direction = new(-0.5f, 0, 0.5f) });

            scene.AddObjects(new Sphere(new(3, 0, 3.1f), new Vector3(1, 1, 1), MaterialType.Transparent, 0.7f, 0.8f));

            scene.AddObjects(new Sphere(new(3, 0, -3.1f), new Vector3(1, 1, 1), MaterialType.Mirror, 0.7f, 1));

            scene.AddObjects(new Triangle(new Vector3[3] { new(6, -2, 4), new(5, 6, 0), new(6, -2, -4) }, "Textures/wall.jpg", MaterialType.DiffuseMirror, 0.9f));


            scene.AddObjects(new Plane(new(0, -0.7f, 0), new(0, 1, 0), "Textures/rockfloor.jpg", new(1, 1.4f), MaterialType.Diffuse, 0.8f));

            scene.AddLight(new Light(new(0, 10, -5), new(1, 1, 1), 50));

            scene.AddLight(new Light(new(0, 10, 5), new(1, 1, 1), 50));

            //scene.AddLight(new PlaneLight(new(0, 10, 0), new(0,-1,0), new(1,1,1), 20));

        }

        /// <summary>
        /// Starts the tasks
        /// </summary>
        public void Render()
        {
            // check for unassigned screendivisions, start a worker to fill that division
            foreach (bool segment in screenPortions) if (!segment) Task.Run(RenderWorker);

            RenderDebug();
        }

        public void RenderDebug()
        {

            // debug view (top down orthogonal projection)
            Vector2i camPos = new((int)(camera.Position.Z * M_to_Px), (int)(camera.Position.X * M_to_Px));

            debug.Clear(0x000000);

            // Camera
            debug.Box(
                camPos.X + ViewTarget.X - 1, camPos.Y + ViewTarget.Y - 1,
                camPos.X + ViewTarget.X + 1, camPos.Y + ViewTarget.Y + 1,
                RGBtoINT(Color.Red));

            // Plane
            Vector4
                p0p1 = new Vector4(camera.p0.Z, camera.p0.X, camera.p1.Z, camera.p1.X) * M_to_Px,
                p0p2 = new Vector4(camera.p0.Z, camera.p0.X, camera.p2.Z, camera.p2.X) * M_to_Px,
                p1p3 = new Vector4(camera.p1.Z, camera.p1.X, camera.p3.Z, camera.p3.X) * M_to_Px,
                p2p3 = new Vector4(camera.p2.Z, camera.p2.X, camera.p3.Z, camera.p3.X) * M_to_Px,
                hor = (p0p1 + p2p3) / 2,
                ver = (p0p2 + p1p3) / 2;
            
            //0---1
            //|-+-|
            //2---3
            debug.Line(
                (int)p0p1.X + ViewTarget.X, (int)p0p1.Y + ViewTarget.Y, (int)p0p1.Z + ViewTarget.X, (int)p0p1.W + ViewTarget.Y,
                RGBtoINT(Color.White));
            debug.Line(
                (int)p0p2.X + ViewTarget.X, (int)p0p2.Y + ViewTarget.Y, (int)p0p2.Z + ViewTarget.X, (int)p0p2.W + ViewTarget.Y,
                RGBtoINT(Color.White));
            debug.Line(
                (int)p1p3.X + ViewTarget.X, (int)p1p3.Y + ViewTarget.Y, (int)p1p3.Z + ViewTarget.X, (int)p1p3.W + ViewTarget.Y,
                RGBtoINT(Color.White));
            debug.Line(
                (int)p2p3.X + ViewTarget.X, (int)p2p3.Y + ViewTarget.Y, (int)p2p3.Z + ViewTarget.X, (int)p2p3.W + ViewTarget.Y,
                RGBtoINT(Color.White));
            debug.Line(
                (int)hor.X + ViewTarget.X, (int)hor.Y + ViewTarget.Y, (int)hor.Z + ViewTarget.X, (int)hor.W + ViewTarget.Y,
                RGBtoINT(Color.White));
            debug.Line(
                (int)ver.X + ViewTarget.X, (int)ver.Y + ViewTarget.Y, (int)ver.Z + ViewTarget.X, (int)ver.W + ViewTarget.Y,
                RGBtoINT(Color.White));

            // Viewing direction
            debug.Line(
                camPos.X + ViewTarget.X, camPos.Y + ViewTarget.Y,
                camPos.X + (int)(Vector2.NormalizeFast(new Vector2(camera.Direction.Z, camera.Direction.X)) * M_to_Px).X + ViewTarget.X,
                camPos.Y + (int)(Vector2.NormalizeFast(new Vector2(camera.Direction.Z, camera.Direction.X)) * M_to_Px).Y + ViewTarget.Y,
                RGBtoINT(Color.Blue));
            debug.DrawSphere(new(camPos.X + ViewTarget.X, camPos.Y + ViewTarget.Y), M_to_Px, M_to_Px, RGBtoINT(Color.White));


            // Lights
            foreach (Light light in scene.Lightsources)
            {
                debug.Plot((int)(ViewTarget.X + light.Position(camera.Position).Z * M_to_Px), (int)(ViewTarget.Y + light.Position(camera.Position).X * M_to_Px), RGBtoINT(light.Color));
                debug.DrawSphere(ViewTarget + (new Vector2(light.Position(camera.Position).Z * M_to_Px, light.Position(camera.Position).X * M_to_Px)), 0.005f * M_to_Px * light.Intensity, M_to_Px, RGBtoINT(light.Color / 2), true);
                debug.DrawSphere(ViewTarget + (new Vector2(light.Position(camera.Position).Z * M_to_Px, light.Position(camera.Position).X * M_to_Px)), 0.05f * M_to_Px, M_to_Px, RGBtoINT(light.Color));
            }

            // Primitives
            foreach (Primitive prim in scene.Objects)
            {
                if (prim.GetType() == typeof(Sphere))
                    debug.DrawSphere(ViewTarget + (new Vector2(prim.Position.Z * M_to_Px, prim.Position.X * M_to_Px)), ((Sphere)prim).radius * M_to_Px, M_to_Px, RGBtoINT(prim.averageColor));

                if (prim.GetType() == typeof(Triangle))
                {
                    Vector3[] vertices = new Vector3[3];
                    ((Triangle)prim).vertices.CopyTo(vertices, 0);
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
                            'p' => RGBtoINT(Color.Red),
                            '2' => RGBtoINT(Color.Green),
                            's' => RGBtoINT(Color.Yellow),
                            'r' => RGBtoINT(Color.Blue),
                            _ => RGBtoINT(Color.Black),
                        };

                        Vector3 raydir = ray.Direction * ray.intersectDistance;

                        debug.Line(
                            (int)(ray.Origin.Z * M_to_Px) + ViewTarget.X,
                            (int)(ray.Origin.X * M_to_Px) + ViewTarget.Y,
                            (int)(ray.Origin.Z * M_to_Px) + (int)((new Vector2(raydir.Z, raydir.X)) * M_to_Px).X + ViewTarget.X,
                            (int)(ray.Origin.X * M_to_Px) + (int)((new Vector2(raydir.Z, raydir.X)) * M_to_Px).Y + ViewTarget.Y,
                            color);
                    }

            debug.Print("Red    = Primary Ray", 10, 10, RGBtoINT(Color.Red));
            debug.Print("Green  = Reflected Ray", 10, 35, RGBtoINT(Color.Green));
            debug.Print("Blue   = Refracted Ray", 10, 60, RGBtoINT(Color.Blue));
            debug.Print("Yellow = Shadow Ray", 10, 85, RGBtoINT(Color.Yellow));
            debug.Print("Number of Rays Shown: " + raysShown, 10, 110, RGBtoINT(Color.White));
            debug.Print("Pixels per Meter: " + Math.Round((decimal)M_to_Px, 2).ToString(), 10, 135, RGBtoINT(Color.White));
            debug.Print("Field of View: " + Math.Round(camera.CurrentFOV, 1), 10, 160, RGBtoINT(Color.White));
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
                            Xy = SinglePartitionSize * new Vector2i(x, y),
                            Zw = SinglePartitionSize * new Vector2i(x + 1, y + 1) - Vector2i.One
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
                                y == screen.height / 2 && x % ((int)((screen.width - 1) / (raysShown - 1))) == 0 && yoffset == 0 && xoffset == 0);
                            color += RecursiveRayShooter(ray, screen, ref debugRays);
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
        public Vector3 RecursiveRayShooter(Ray ray, Surface screen, ref List<Ray> debugRays, Light? aimedLight = null)
        {
            Vector3 result = new();
            List<Light> lights = scene.Lightsources;
            Primitive? prim = null;
            IntersectData data = new();

            foreach (Primitive candidate in scene.Objects)
            {
                IntersectData candidateData = candidate.Intersect(ray);

                if (candidateData.intersectedPrimitive != null && candidateData.distance > 0)
                {
                    if (prim == null) { prim = candidate; data = candidateData; }
                    else if (data.distance > candidateData.distance)
                    {
                        prim = candidate;
                        data = candidateData;
                    }
                }
            }

            // if there was a light and nothing blocks it, skip the primitive color calculation and go straight to the light return

            if (prim != null)
            {
                Vector3
                    intersectPoint = ray.Origin + data.distance * ray.Direction,
                    normal = Vector3.Dot(ray.Direction, prim.Normal(intersectPoint)) > 0 ? -prim.Normal(intersectPoint) : prim.Normal(intersectPoint),
                    epsilon = normal * 0.0001f;

                // if the ray bounce limit hasn't been reached, send out new shadow rays
                if (ray.bouncesLeft > 0)
                {
                    Ray reflectedRay = new(intersectPoint + epsilon, ray.Direction - (2 * Vector3.Dot(ray.Direction, normal)) * normal, ray.bouncesLeft - 1, '2', ray.sendToDebug);

                    switch (prim.materialType)
                    {
                        case MaterialType.Mirror:
                            result = RecursiveRayShooter(reflectedRay, screen, ref debugRays) * (prim.reflectiveness * prim.GetColorFromTextureAtIntersect(intersectPoint));
                            break;

                        case MaterialType.DiffuseMirror:
                            foreach (Light light in lights)
                            {
                                Ray shadowRay = new(intersectPoint + epsilon, light.Position(intersectPoint) - intersectPoint, ray.bouncesLeft - 1, 's', ray.sendToDebug);
                                Vector3 returnlight = RecursiveRayShooter(shadowRay, screen, ref debugRays, light);
                                result += DiffuseCalculation(shadowRay.Direction, prim.Normal(shadowRay.Origin), returnlight, prim.GetColorFromTextureAtIntersect(intersectPoint), (light.Position(intersectPoint) - intersectPoint - (epsilon * 2)).LengthSquared);
                            }
                            result += RecursiveRayShooter(reflectedRay, screen, ref debugRays) * (prim.reflectiveness * prim.GetColorFromTextureAtIntersect(intersectPoint)) * prim.reflectiveness;
                            break;

                        case MaterialType.Diffuse:
                            foreach (Light light in lights)
                            {
                                Ray shadowRay = new(intersectPoint + epsilon, light.Position(intersectPoint) - intersectPoint, ray.bouncesLeft - 1, 's', ray.sendToDebug);
                                Vector3 returnlight = RecursiveRayShooter(shadowRay, screen, ref debugRays, light);
                                result += DiffuseCalculation(shadowRay.Direction, prim.Normal(shadowRay.Origin), returnlight, prim.GetColorFromTextureAtIntersect(intersectPoint), (light.Position(intersectPoint) - intersectPoint - (epsilon * 2)).LengthSquared);
                            }
                            break;

                        case MaterialType.Gloss:
                            foreach (Light light in lights)
                            {
                                Ray shadowRay = new(intersectPoint + epsilon, light.Position(intersectPoint) - intersectPoint, ray.bouncesLeft - 1, 's', ray.sendToDebug);
                                Vector3 returnlight = RecursiveRayShooter(shadowRay, screen, ref debugRays, light);
                                result += DiffuseGlossCalculation(ray.Direction, light.Position(intersectPoint) - intersectPoint, normal, returnlight, prim.SpecularColor, prim.GetColorFromTextureAtIntersect(intersectPoint), (light.Position(intersectPoint) - intersectPoint).LengthSquared, prim.specularity, prim.reflectiveness);
                            }
                            break;

                        case MaterialType.Transparent:

                            bool originInside = prim is Sphere sphere && (ray.Origin - prim.Position).Length < sphere.radius;

                            float
                                n = originInside ? prim.refractiveIndex : 1,
                                nt = originInside ? 1 : prim.refractiveIndex,
                                underTheRoot = 1 - ((n * n) / (nt * nt)) * (1 - Vector3.Dot(ray.Direction, normal) * Vector3.Dot(ray.Direction, normal));

                            if (underTheRoot >= 0)
                            {
                                float
                                    R0 = MathF.Pow((nt - n) / (nt + n), 2),
                                    R = R0 + MathF.Pow((1f - R0) * (1f - MathF.Cos(Vector3.CalculateAngle(ray.Direction, -normal))), 5);

                                Vector3
                                    d = ray.Direction,
                                    refractedRayDirection = (n / nt) * (d - Vector3.Dot(d, normal) * normal) - MathF.Sqrt(underTheRoot) * normal;

                                reflectedRay = new(intersectPoint + epsilon, ray.Direction - (2 * Vector3.Dot(ray.Direction, normal)) * normal, ray.bouncesLeft - 1, '2', ray.sendToDebug);
                                Ray refractedRay = new(intersectPoint - epsilon, refractedRayDirection, originInside ? reflectedRay.bouncesLeft -= 2 : reflectedRay.bouncesLeft--, 'r', ray.sendToDebug);

                                // reflected
                                result = 
                                    (RecursiveRayShooter(reflectedRay, screen, ref debugRays) * (prim.reflectiveness * prim.GetColorFromTextureAtIntersect(intersectPoint)) * R)
                                    + (RecursiveRayShooter(refractedRay, screen, ref debugRays) * prim.GetColorFromTextureAtIntersect(intersectPoint) * (1 - R))
                                    * prim.GetColorFromTextureAtIntersect(intersectPoint);
                            }
                            else result = RecursiveRayShooter(reflectedRay, screen, ref debugRays);

                            break;
                    }
                }

                ray.intersectDistance = data.distance;
                if (ray.sendToDebug) debugRays.Add(ray);
                return result;
            }


            // if nothing blocks the ray between the source and the light, return the light color
            if (aimedLight != null)
            {
                Vector3 lightPos = aimedLight.Position(ray.Origin);
                if ((aimedLight is PlaneLight && lightPos.X != float.NegativeInfinity) || aimedLight is not PlaneLight)
                {
                    ray.intersectDistance = (ray.Origin - lightPos).Length;
                    if (ray.sendToDebug) debugRays.Add(ray);
                    return aimedLight.Color;
                }
            }

            // if the ray is not aimed at a light, or the ray is blocked and there are no more bounces left, return the skybox
            // or if the ray is shot into narnia, then also return the skybox, or skydome because i coded it to be spherical
            result = GetSkyboxColor(ray);
            ray.intersectDistance = 1000;
            if (ray.sendToDebug) debugRays.Add(ray);
            return result;
        }

        public static Vector3 GetSkyboxColor(Ray ray)
        {
            // This is really just the same math i used for texture mapping a sphere, just without the sphere component
            return SkyboxTexture[
                (int)Math.Round((SkyboxTexture.GetLength(0) - 1) * (1 - (MathF.Atan2(ray.Direction.X, ray.Direction.Z) / (2 * MathF.PI) + 0.5f))), 
                (int)Math.Round((SkyboxTexture.GetLength(1) - 1) * -(ray.Direction.Y - 1) / 2)];
        }


        // the following algorithms are derived from the slides
        public static Vector3 DiffuseCalculation(Vector3 toLight, Vector3 normal, Vector3 LightColor, Vector3 diffuseColor, float distanceToLightSquared)
        {
            // calculate attenuation
            float AmountReflected = MathF.Max(0, Vector3.Dot(normal, Vector3.NormalizeFast(toLight)));
            return (Scene.AmbientLight * diffuseColor) + LightColor / distanceToLightSquared * AmountReflected * diffuseColor;
        }

        public static Vector3 DiffuseGlossCalculation(Vector3 toOrigin, Vector3 toLight, Vector3 normal, Vector3 LightColor, Vector3 specularColor, Vector3 diffuseColor, float distanceToLightSquared, float specularity, float reflectiveness)
        {
            // normalise the vectors
            toOrigin.NormalizeFast(); toLight.NormalizeFast();

            Vector3 
                distanceAttenuatedLight = LightColor / distanceToLightSquared,
                diffuseComponent = MathF.Max(0, Vector3.Dot(normal, toLight)) * diffuseColor * reflectiveness,
                specularComponent = MathF.Pow(MathF.Max(0, Vector3.Dot(toOrigin, toLight - 2 * Vector3.Dot(toLight, normal) * normal)), specularity) * specularColor;
            
            return (Scene.AmbientLight * diffuseColor) + distanceAttenuatedLight * (diffuseComponent + specularComponent);
        }

        // quick helper methods
        public static int RGBtoINT(int r, int g, int b) { return (r << 16) + (g << 8) + b; }

        public static int RGBtoINT(Color color) { return (color.R << 16) + (color.G << 8) + color.B; }

        public static int RGBtoINT(Vector3 vector) { return ((int)(MathF.Min(1, vector.X) * 255) << 16) + ((int)(MathF.Min(1, vector.Y) * 255) << 8) + (int)(MathF.Min(1, vector.Z) * 255); }

        public static Vector3 ClampToOne(Vector3 vector) { return new Vector3(MathF.Min(1, vector.X), MathF.Min(1, vector.Y), MathF.Min(1, vector.Z)); }
    }
}
