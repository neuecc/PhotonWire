PhotonWire
===
Typed Asynchronous RPC Layer for Photon Server + Unity

Document
---
Coming Soon...

Download from Photon Server SDK
https://www.photonengine.com/en-US/OnPremise/Download

under deloy/bin_Win64

for Server Project
lib/ExitGamesLibs.dll
lib/Photon.SocketServer.dll
lib/PhotonHostRuntimeInterfaces.dll

for .NET Clinet Project
lib/ExitGamesLibs.dll
lib/Photon3DotNet.dll

for Unity Project
lib /Photon3Unity3D.dll


ServerApp Copy dll

Build Events -> Post-build event commandline

xcopy "$(TargetDir)*.*" "$(SolutionDir)..\PhotonLibs\$(ProjectName)\bin\" /Y /Q



Debug
-> Start external program:
/* Absolute Dir Paths */\PhotonLibs\bin_Win64\PhotonSocketServer.exe

-> Star Options
/debug PhotonWireSample /config PhotonWire.Sample.ServerApp\bin\PhotonServer.config

-> Working directory:
/* Absolute Path */\PhotonLibs\

PhotonServer.config -> Copyt to Output Directory, Copy always 


Encount 
`Unhandled Exception: System.Reflection.ReflectionTypeLoadException: The classes in the module cannot be loaded.`
Build Settings -> Optimization Api Compatibility Level -> .NET 2.0

