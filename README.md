Tunnel3D is a plugin for Godot Game Engine which adds 1 new node and 4 new resources.

Install Instructions:

1. Ensure you have Godot MONO v4.4+ installed


2. Download the contents of this repo
   
   <img width="410" height="353" alt="image" src="https://github.com/user-attachments/assets/dab61735-a6ed-4c9a-b4ea-873deeaeeb2b" />


3. Create a folder in the Godot Editor named "addons" in res:// and move the Tunnel3D(-main) folder into the addons folder.
   
   <img width="256" height="207" alt="image" src="https://github.com/user-attachments/assets/77552851-316a-4059-80ff-10cab4a9eab8" />


4. Navigate to Project -> Project Settings -> Plugins -> Tunnel3D ON
 
   <img width="394" height="345" alt="image" src="https://github.com/user-attachments/assets/22aae442-6ae3-425b-bd8f-24513efcdbcb" />
  
   <img width="1197" height="728" alt="image" src="https://github.com/user-attachments/assets/637bec44-6d69-475e-94a2-3d77f35dd1de" />

ERROR WORKAROUND:

  If you receive an error "Unable to load addon script from path: 'res://addons/Tunnel3D-main/plugin/Tunnel3DPlugin.cs'." or one of similar nature, then you will need to build the project.
  Press the Build button or (Alt+B)
  
   <img width="275" height="76" alt="image" src="https://github.com/user-attachments/assets/d962cf0f-891f-4282-a2d0-83a5acf2a356" />


  If the Build button is not present, then you are either not on the MONO version of Godot, or you need to create at least one C# script to trigger the engine to create the necessary .csproj and .sln files. You may delete this script afterwards.

   <img width="432" height="251" alt="image" src="https://github.com/user-attachments/assets/d723ebf8-7525-4d92-a527-82c6de383f02" />


5. Find the new node "Tunnel3D" and new resources "Tunnel3DConnectionGenerator", "Tunnel3DGenerationData", "Tunnel3DMeshData" and "Tunnel3DVoxelData".


Basic Use Instructions:

1. Create a Tunnel3D node in the Scene Tree.
   
   <img width="269" height="89" alt="image" src="https://github.com/user-attachments/assets/5597fdb4-453e-492e-8759-123795665301" />


2. Fill the Exported Resources in the Inspector with the associated tunnel generation information.
   
<img width="276" height="334" alt="image" src="https://github.com/user-attachments/assets/03f6898c-97c1-467c-82bc-2323cd4e1a5f" />


4. Press the Tool Buttons in the Inspector in the following order if you intend to use all the functionality. "Generate Tunnel Data", "Generate Voxel Data", "Generate Tunnel Mesh", "Generate Mesh Children"


5. Wait until the Tunnel has generated.
