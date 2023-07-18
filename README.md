# U2Eyes
## Description ##

We have created a software U2Eyes based on the original project [UnityEyes](https://www.cl.cam.ac.uk/research/rainbow/projects/unityeyes/ "UnityEyes"). U2Eyes generates synthetic binocular images intended for eye tracking and gaze estimation purposes which include essential eyeball physiology elements and model binocular vision dynamics. The software exports images with annotation such as head pose, gaze direction information, or 2D and 3D landmarks of both eyes amongst others.

The software may receive as input a series of configuration files (in xml format) to automatize the generation of a database:
  *	A user is identified (userid.xml file) by a different face shape (PCA model offered by UnityEyes), skin-textures, eye-textures, iris size, kappa angles, amongst other parameters.
  *	The head pose (headpose.xml) is a combination between head center spatial location (x, y, z coordinates) and face orientation (yaw, roll and pitch angles). It also defines the position of the point the user is looking at.
  *	The scene (scene.xml) which contains information about light color/direction/intensity and exposure/rotation of the scene (18 different scene models available).
  *	The camera (camera.xml) which defines intrinsic parameters of the camera as well as the final resolution of the image.

The software will also generate a series of output files:
  *	The generated image (in png format) with the resolution indicated in "camera.xml"
  *	The point of interests (poi_data.xml) containing the whole information about 2D/3D landmarks of eyelids, iris and pupil contour points and centers, caruncle and eye corners.
  *	All the above-mentionned input files (default ones in case these are not passed as argument)

## References ##
• Sonia Porta, Benoît Bossavit, Rafael Cabeza, Andoni Larumbe-Bergera, Gonzalo Garde, Arantxa Villanueva, U2Eyes: a binocular dataset for eye tracking and gaze estimation. 2019 OpenEDS Workshop: Eye Tracking for VR and AR. International Conference on Computer Vision (ICCV ’19). Seoul, Korea

## License ##
This project is licensed under a Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License. 

Should you use this software in scientific publication, please cite the aforementioned papers.

## How to use ##
The main file is: SynthesEyesServer.cs

Execution with command line:
- /i = draw Point of Interest images
- /c + path to camera.xml
- /e + path to scene.xml
- /h + path to headpose.xml
- /u + path to userid.xml

In the app press key:
- 'g' to randomize gaze
- 'e' to randomize lighting
- 'h' to randomize headpose
- 'u' to randomize face
- 's' to export images
