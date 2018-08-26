/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.CodeDom.Compiler;
using System.Reflection;

using Microsoft.CSharp;
using NLog;

namespace Butterfly.Core.Util {
    /*
    public static class CompilerUtil {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static Assembly Compile(string code, params string[] assemblyLocations) {
            var parameters = new CompilerParameters {
                GenerateExecutable = false,
                GenerateInMemory = true
            };

            foreach (string assemblyLocation in assemblyLocations) {
                parameters.ReferencedAssemblies.Add(assemblyLocation);
            }

            using (CSharpCodeProvider compiler = new CSharpCodeProvider()) {
                var result = compiler.CompileAssemblyFromSource(parameters, code);
                if (result.Errors.Count > 0) {
                    foreach (var error in result.Errors) {
                        logger.Error(error);
                    }
                    throw new Exception("Assembly could not be created " + result);
                }
                return result.CompiledAssembly;
            }
        }
    }
    */
}
