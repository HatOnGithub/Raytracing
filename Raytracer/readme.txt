Raytracer by J.T.D. Chen (#7910878)

Implemented Minimum Requirements:
	- Camera:
		- Arbitrary field of view
		- Arbitrary positions and viewing direction
		- Camera can be aimed using movements keys (See Application)

	- Primitives:
		- Sphere
		- Plane 
	- Lights:
		- Multiple arbitrary light support
	- Materials:
		- Phong's shading model
		- Diffuse shading
		- Ambient Lighting
		- Adjustable Recursion Cap for Specular Shading
	- Demonstration Scene:
		- Added every type of object/material
	- Application:
		- Camera Movement using the following keys:
		[WASD] horizontal movement relative to the camera looking direction
		[SPACE/LSHIFT] Vertical Movement relative to the camera Up direction
		[RF] Pitch
		[QE] Yaw
		[ZX] Roll

Bonus Features:
	- Antialiasing using multisampling
	- Multithreading
	- Triangle Primitive - including texturing and phong lighting
	- Texture mapping on all objects
	- Refraction (including reflected rays)
	- A Skybox without adding a sphere or box object
	