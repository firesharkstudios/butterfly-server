cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly"
..\InheritDoc\InheritDoc\bin\Release\InheritDoc.exe -kF5PUPQ-SV2BEJ-UDJGKI-SKX4BJ-RDTQ25-I3ICQQ -f"Butterfly.*" -o

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Auth"
..\packages\NuGet.CommandLine.4.4.1\tools\NuGet.exe pack Butterfly.Auth.csproj -IncludeReferencedProjects -Prop Configuration=Release -OutputDirectory \NuGet.local

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Channel"
..\packages\NuGet.CommandLine.4.4.1\tools\NuGet.exe pack Butterfly.Channel.csproj -IncludeReferencedProjects -Prop Configuration=Release -OutputDirectory \NuGet.local

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Channel.EmbedIO"
..\packages\NuGet.CommandLine.4.4.1\tools\NuGet.exe pack Butterfly.Channel.EmbedIO.csproj -IncludeReferencedProjects -Prop Configuration=Release -OutputDirectory \NuGet.local

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Channel.RedHttpServer"
..\packages\NuGet.CommandLine.4.4.1\tools\NuGet.exe pack Butterfly.Channel.RedHttpServer.csproj -IncludeReferencedProjects -Prop Configuration=Release -OutputDirectory \NuGet.local

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Database"
..\packages\NuGet.CommandLine.4.4.1\tools\NuGet.exe pack Butterfly.Database.csproj -IncludeReferencedProjects -Prop Configuration=Release -OutputDirectory \NuGet.local

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Database.Memory"
..\packages\NuGet.CommandLine.4.4.1\tools\NuGet.exe pack Butterfly.Database.Memory.csproj -IncludeReferencedProjects -Prop Configuration=Release -OutputDirectory \NuGet.local

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Database.MySQL"
..\packages\NuGet.CommandLine.4.4.1\tools\NuGet.exe pack Butterfly.Database.MySQL.csproj -IncludeReferencedProjects -Prop Configuration=Release -OutputDirectory \NuGet.local

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Database.Postgres"
..\packages\NuGet.CommandLine.4.4.1\tools\NuGet.exe pack Butterfly.Database.Postgres.csproj -IncludeReferencedProjects -Prop Configuration=Release -OutputDirectory \NuGet.local

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Database.SQLite"
..\packages\NuGet.CommandLine.4.4.1\tools\NuGet.exe pack Butterfly.Database.SQLite.csproj -IncludeReferencedProjects -Prop Configuration=Release -OutputDirectory \NuGet.local

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.WebApi"
..\packages\NuGet.CommandLine.4.4.1\tools\NuGet.exe pack Butterfly.WebApi.csproj -IncludeReferencedProjects -Prop Configuration=Release -OutputDirectory \NuGet.local

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.WebApi.EmbedIO"
..\packages\NuGet.CommandLine.4.4.1\tools\NuGet.exe pack Butterfly.WebApi.EmbedIO.csproj -IncludeReferencedProjects -Prop Configuration=Release -OutputDirectory \NuGet.local

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.WebApi.RedHttpServer"
..\packages\NuGet.CommandLine.4.4.1\tools\NuGet.exe pack Butterfly.WebApi.RedHttpServer.csproj -IncludeReferencedProjects -Prop Configuration=Release -OutputDirectory \NuGet.local

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Util"
..\packages\NuGet.CommandLine.4.4.1\tools\NuGet.exe pack Butterfly.Util.csproj -IncludeReferencedProjects -Prop Configuration=Release -OutputDirectory \NuGet.local

