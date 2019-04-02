/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using NLog;

using Butterfly.Core.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Notify {
    public class NotifyMessage {

        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public readonly string from;
        public readonly string to;
        public readonly string subject;
        public readonly string bodyText;
        public readonly string bodyHtml;
        public readonly byte priority;

        public Dict extraData = null;
        public Func<string, Dict, string> formatter = null;

        public NotifyMessage(string from, string to, string subject, string bodyText, string bodyHtml = null, byte priority = 0) {
            this.from = from;
            this.to = to;
            this.subject = subject;
            this.bodyText = bodyText;
            this.bodyHtml = bodyHtml;
            this.priority = 0;
            this.extraData = null;
        }

        protected const string TEXT_SECTION_NAME = "Text";
        protected const string HTML_SECTION_NAME = "Html";
        protected const string SECTION_DELIM = "===";

        public NotifyMessage Evaluate(dynamic vars) {
            Dict values;
            if (vars is Dict) {
                values = vars as Dict;
            }
            else {
                values = DynamicX.ToDictionary(vars);
            }

            Func<string, Dict, string> myFormatter = this.formatter;
            if (myFormatter==null) {
                myFormatter = (formatText, formatVars) => formatVars.Format(formatText);
            }

            string from = myFormatter(this.from, values);
            string to = myFormatter(this.to, values);
            string subject = myFormatter(this.subject, values);
            string bodyText = myFormatter(this.bodyText, values);
            string bodyHtml = myFormatter(this.bodyHtml, values);
            return new NotifyMessage(from, to, subject, bodyText, bodyHtml, this.priority);
        }

        public static NotifyMessage ParseFile(string fileName) {
            if (!File.Exists(fileName)) throw new Exception("Could not find file '" + fileName + "'");
            string text = File.ReadAllText(fileName);
            return Parse(text);
        }

        public static NotifyMessage Parse(string text) {
            string from = null;
            string to = null;
            string subject = null;
            byte priority = 0;
            Dictionary<string, string> sectionByName = new Dictionary<string, string>();

            string[] lines = Regex.Split(text, "\r\n|\r|\n");
            for (int i=0; i<lines.Length; i++) {
                if (string.IsNullOrWhiteSpace(lines[i])) {
                    int lineStart = i + 1;
                    while (true) {
                        int lineEnd = Array.FindIndex(lines, lineStart, line => line.StartsWith(SECTION_DELIM));

                        string section;
                        if (lineEnd == -1) {
                            section = String.Join(Environment.NewLine, new ArraySegment<string>(lines, lineStart, lines.Length - lineStart));
                        }
                        else {
                            section = String.Join(Environment.NewLine, new ArraySegment<string>(lines, lineStart, lineEnd - lineStart));
                        }

                        string sectionName;
                        if (sectionByName.Count == 0) sectionName = TEXT_SECTION_NAME;
                        else if (sectionByName.Count == 1) sectionName = HTML_SECTION_NAME;
                        else {
                            sectionName = lines[lineStart - 1].Substring(SECTION_DELIM.Length).Trim();
                        }

                        sectionByName[sectionName] = section;

                        if (lineEnd == -1) break;
                        lineStart = lineEnd + 1;
                    }

                    break;
                }
                else {
                    int pos = lines[i].IndexOf(':');
                    if (pos>0) {
                        string name = lines[i].Substring(0, pos).Trim().ToUpper();
                        string value = lines[i].Substring(pos + 1).Trim();
                        switch (name) {
                            case "FROM":
                                from = value;
                                break;
                            case "TO":
                                to = value;
                                break;
                            case "SUBJECT":
                                subject = value;
                                break;
                            case "PRIORITY":
                                priority = byte.Parse(value);
                                break;
                            default:
                                logger.Error("Unknown field '" + name + "'");
                                break;
                        }
                    }
                }
            }

            return new NotifyMessage(from, to, subject, sectionByName.GetAs(TEXT_SECTION_NAME, (string)null), sectionByName.GetAs(HTML_SECTION_NAME, (string)null), priority);
        }
    }
}
