cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Channel"
nuget pack Butterfly.Channel.csproj -IncludeReferencedProjects -Prop Configuration=Debug

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Channel.EmbedIO"
nuget pack Butterfly.Channel.EmbedIO.csproj -IncludeReferencedProjects -Prop Configuration=Debug

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Channel.RedHttpServer"
nuget pack Butterfly.Channel.RedHttpServer.csproj -IncludeReferencedProjects -Prop Configuration=Debug

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Database"
nuget pack Butterfly.Database.csproj -IncludeReferencedProjects -Prop Configuration=Debug

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Database.Memory"
nuget pack Butterfly.Database.Memory.csproj -IncludeReferencedProjects -Prop Configuration=Debug

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Database.MySQL"
nuget pack Butterfly.Database.MySQL.csproj -IncludeReferencedProjects -Prop Configuration=Debug

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Database.Postgres"
nuget pack Butterfly.Database.Postgres.csproj -IncludeReferencedProjects -Prop Configuration=Debug

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Database.SQLite"
nuget pack Butterfly.Database.SQLite.csproj -IncludeReferencedProjects -Prop Configuration=Debug

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.WebApi"
nuget pack Butterfly.WebApi.csproj -IncludeReferencedProjects -Prop Configuration=Debug

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.WebApi.EmbedIO"
nuget pack Butterfly.WebApi.EmbedIO.csproj -IncludeReferencedProjects -Prop Configuration=Debug

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.WebApi.RedHttpServer"
nuget pack Butterfly.WebApi.RedHttpServer.csproj -IncludeReferencedProjects -Prop Configuration=Debug

cd "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Util"
nuget pack Butterfly.Util.csproj -IncludeReferencedProjects -Prop Configuration=Debug

#new-item "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Nuget.local" -itemtype directory

Copy-Item "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\*\*.nupkg" -Destination "C:\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Nuget.local\" -force