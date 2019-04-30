using System;
using System.IO;
using System.Threading.Tasks;

using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Notify {
    public static class ScribanFormatter {

        public static Func<string, Dict, string> GetFormatter(string sourceFilePath) {
            return (text, vars) => {
                Template template = Template.Parse(text, sourceFilePath: sourceFilePath);
                return template.RenderWithRelativeIncludes(vars);
            };
        }

        public static string RenderWithRelativeIncludes(this Template me, object model) {
            if (string.IsNullOrEmpty(me.SourceFilePath)) throw new System.Exception("Must call Template.Parse() with sourceFilePath parameter to call RenderWithRelativeIncludes()");

            var scriptObject = new ScriptObject();
            scriptObject.Import(model);
            var context = new TemplateContext {
                TemplateLoader = new CustomTemplateLoader(me.SourceFilePath)
            };
            context.PushGlobal(scriptObject);
            return me.Render(context);
        }

        class CustomTemplateLoader : ITemplateLoader {

            protected readonly string sourceFilePath;

            public CustomTemplateLoader(string sourceFilePath) {
                this.sourceFilePath = sourceFilePath;
            }

            public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) {
                var path = Path.GetDirectoryName(this.sourceFilePath);
                var result = Path.Combine(path, templateName);
                return result;
            }

            public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath) {
                return File.ReadAllText(templatePath);
            }

            public async ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath) {
                string text = File.ReadAllText(templatePath);
                return text;
            }

        }

    }
}
