using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Util {
    public class StructuredParser {
        protected readonly List<Token> tokens = new List<Token>();

        public StructuredParser AddToken(string name, Regex regex, bool required) {
            this.tokens.Add(new Token(name, regex, required));
            return this;
        }

        public Dict Parse(string text) {
            int pos = 0;

            var matches = new List<Match>();
            foreach (var token in this.tokens) {
                var match = GetMatch(token, text, pos);
                if (match == null) {
                    if (token.required) throw new System.Exception($"Could not find section {token.name} in {text}");
                }
                else {
                    pos = match.Index + match.Length;
                }
                matches.Add(match);
            }

            Dict dict = new Dict();
            for (int i=0; i<tokens.Count; i++) {
                if (matches[i]!=null) {
                    var nextMatch = matches.Skip(i + 1).Where(x => x != null).FirstOrDefault();
                    var beg = matches[i].Index + matches[i].Length;
                    if (nextMatch==null) {
                        dict[tokens[i].name] = text.Substring(beg);
                        break;
                    }
                    else {
                        dict[tokens[i].name] = text.Substring(beg, nextMatch.Index - beg);
                    }
                }
            }

            return dict;
        }

        protected Match GetMatch(Token token, string text, int pos) {
            var matches = token.regex.Matches(text, pos);
            for (int i = 0; i < matches.Count; i++) {
                var preText = text.Substring(0, matches[i].Index);
                if (StringX.IsBalanced(preText)) {
                    return matches[i];
                }
            }
            return null;
        }

        public class Token {
            public readonly string name;
            public readonly Regex regex;
            public readonly bool required;

            public Token(string name, Regex regex, bool required) {
                this.name = name;
                this.regex = regex;
                this.required = required;
            }
        }
    }
}
