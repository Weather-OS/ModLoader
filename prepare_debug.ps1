$GamePath="C:\Program Files (x86)\Steam\steamapps\common\worldbox\worldbox_Data\StreamingAssets\mods"
$BuildPath="C:\Users\Inmny\source\repos\NeoModLoader\NeoModLoader\bin\Debug\net481"

Copy-Item -Path "$BuildPath\NeoModLoader.dll" -Destination "$GamePath\NeoModLoader.dll" -Force
Copy-Item -Path "$BuildPath\NeoModLoader.pdb" -Destination "$GamePath\NeoModLoader.pdb" -Force