SET CurrentDir=%~dp0

ILRepack.exe /keyfile:%CurrentDir%Bootcamp.Plugins.snk /parallel /out:%CurrentDir%Result/Bootcamp.Plugins.dll %CurrentDir%Bootcamp.Plugins.dll %CurrentDir%Niam.XRM.Framework.dll