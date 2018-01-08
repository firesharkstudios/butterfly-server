..\InheritDoc\InheritDoc\bin\Release\InheritDoc.exe -f"Butterfly.*" -o

XmlDocMarkdown\tools\XmlDocMarkdown.exe Butterfly.Auth\bin\Release\Butterfly.Auth.dll docs --visibility public --clean

XmlDocMarkdown\tools\XmlDocMarkdown.exe Butterfly.Channel\bin\Release\Butterfly.Channel.dll docs --visibility public --clean
XmlDocMarkdown\tools\XmlDocMarkdown.exe Butterfly.Channel.EmbedIO\bin\Release\Butterfly.Channel.EmbedIO.dll docs --visibility public --clean
XmlDocMarkdown\tools\XmlDocMarkdown.exe Butterfly.Channel.RedHttpServer\bin\Release\Butterfly.Channel.RedHttpServer.dll docs --visibility public --clean

XmlDocMarkdown\tools\XmlDocMarkdown.exe Butterfly.Client.DotNet\bin\Release\Butterfly.Client.DotNet.dll docs --visibility public --clean

XmlDocMarkdown\tools\XmlDocMarkdown.exe Butterfly.Database\bin\Release\Butterfly.Database.dll docs --visibility public --clean
XmlDocMarkdown\tools\XmlDocMarkdown.exe Butterfly.Database.Memory\bin\Release\Butterfly.Database.Memory.dll docs --visibility public --clean
XmlDocMarkdown\tools\XmlDocMarkdown.exe Butterfly.Database.MySql\bin\Release\Butterfly.Database.MySql.dll docs --visibility public --clean
XmlDocMarkdown\tools\XmlDocMarkdown.exe Butterfly.Database.Postgres\bin\Release\Butterfly.Database.Postgres.dll docs --visibility public --clean
XmlDocMarkdown\tools\XmlDocMarkdown.exe Butterfly.Database.SQLite\bin\Release\Butterfly.Database.SQLite.dll docs --visibility public --clean

XmlDocMarkdown\tools\XmlDocMarkdown.exe Butterfly.Util\bin\Release\Butterfly.Util.dll docs --visibility public --clean

XmlDocMarkdown\tools\XmlDocMarkdown.exe Butterfly.WebApi\bin\Release\Butterfly.WebApi.dll docs --visibility public --clean
XmlDocMarkdown\tools\XmlDocMarkdown.exe Butterfly.WebApi.EmbedIO\bin\Release\Butterfly.WebApi.EmbedIO.dll docs --visibility public --clean
XmlDocMarkdown\tools\XmlDocMarkdown.exe Butterfly.WebApi.RedHttpServer\bin\Release\Butterfly.WebApi.RedHttpServer.dll docs --visibility public --clean
