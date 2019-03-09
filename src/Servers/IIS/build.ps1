
dotnet restore IISIntegration.sln

msbuild src\AspNetCoreModuleV2\InProcessRequestHandler\InProcessRequestHandler.vcxproj
msbuild src\AspNetCoreModuleV2\InProcessRequestHandler\InProcessRequestHandler.vcxproj /p:Configuration=Debug;
msbuild src\AspNetCoreModuleV2\OutOfProcessRequestHandler\OutOfProcessRequestHandler.vcxproj /property:Platform=x64

msbuild src\AspNetCoreModuleV2\OutOfProcessRequestHandler\OutOfProcessRequestHandler.vcxproj  /property:Platform=Win32
msbuild src\AspNetCoreModuleV2\AspNetCore\AspNetCore.vcxproj /property:Platform=Win32

# msbuild AspNetCoreModuleV2\InProcessRequestHandler\InProcessRequestHandler.vcxproj 
# dotnet build IISIntegration.sln --no-restore
# /v:d > iis.log

dotnet build IIS\src\Microsoft.AspNetCore.Server.IIS.csproj 
# dotnet pack IIS\src\Microsoft.AspNetCore.Server.IIS.csproj  -o $PWD

dotnet build E:\Beta\dot64\AspNetCore\src\Servers\IIS\IntegrationTesting.IIS\src\