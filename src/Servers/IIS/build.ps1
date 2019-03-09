
dotnet restore IISIntegration.sln

msbuild src\AspNetCoreModuleV2\InProcessRequestHandler\InProcessRequestHandler.vcxproj
msbuild src\AspNetCoreModuleV2\InProcessRequestHandler\InProcessRequestHandler.vcxproj /p:Configuration=Debug;
# msbuild AspNetCoreModuleV2\InProcessRequestHandler\InProcessRequestHandler.vcxproj 
# dotnet build IISIntegration.sln --no-restore

dotnet msbuild IIS\src\Microsoft.AspNetCore.Server.IIS.csproj 
# /v:d > iis.log