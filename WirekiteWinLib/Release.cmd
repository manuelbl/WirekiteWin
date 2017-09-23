SET VSINSTALLDIR=C:\Program Files (x86)\Microsoft Visual Studio\2017\Community
SET VisualStudioVersion=15.0
"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild" /p:Configuration=Release /t:rebuild
nuget pack WirekiteWinLib.nuspec
