
# https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script
# Obtain script and install the 2.1.2 version behind a corporate proxy (Windows only):
# Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -Proxy $env:HTTP_PROXY -ProxyUseDefaultCredentials -OutFile 'dotnet-install.ps1';

Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1'  -OutFile 'dotnet-install.ps1'
./dotnet-install.ps1 -InstallDir '~/.dotnet/x64' -Version '2.2.104'

# sudo ./dotnet-install.ps1 -InstallDir 'c:\Program Files\dotnet' -Version '2.2.104'

# -ProxyAddress $env:HTTP_PROXY -ProxyUseDefaultCredentials;
# link: https://dotnetcli.azureedge.net/dotnet/Sdk/2.2.104/dotnet-sdk-2.2.104-win-x64.zip