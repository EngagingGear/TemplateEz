using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

// ReSharper disable once CheckNamespace
namespace TemplateEzNS
{
    /// <summary>
    /// Class to generate simple template files. Calling the constructor with a template
    /// compiles that into a program that can then be used to execute and generate templates
    /// </summary>
    public class TemplateEz
    {
        /// <summary>
        /// The generated assembly. Note that there is no way to unload this assembly. However
        /// except for very complex templates it only take 4K of memory.
        /// </summary>
        private Assembly _assembly;

        /// <summary>
        ///  This is the code that will be compiled to make the template function
        /// </summary>
        private string _compileCode;

        /// <summary>
        /// This is true if there was a failed attempt to compile the code. It is false
        /// if the code has not been compiled yet, or if it has been compiled successfully.
        /// </summary>
        private bool _compileFailed;

        /// <summary>
        /// Any extra libraries to load when running the template
        /// </summary>
        private HashSet<string> _extraLibraries;

        /// <summary>
        /// Regex for validating template names
        /// </summary>
        private readonly Regex _nameRegex = new Regex("^[a-zA-Z_0-9]+$");

        /// <summary>
        /// Decorated list of templates
        /// </summary>
        private List<DecoratedTemplateDef> _decoratedTemplates;

        /// <summary>
        /// Determines if lines have indent added, many for testing functions
        /// </summary>
        public bool NoWrapInOutputAdd { get; set; }

        /// <summary>
        /// Constructor. Create a template object by taking the template text and compiling it
        /// into a function that, when run, generates the required template text.
        /// </summary>
        /// <param name="templateLines">Lines of the template code</param>
        /// <param name="modelType">Type of the model that you are passing into the template</param>
        /// <param name="extraLibraries">Any extra libraries to load. If this is null it
        /// by default includes the currently executing assembly, so that the types in this
        /// program are available in your template code.</param>
        /// <param name="lazyCompile">If true the compile is delayed until the first
        /// use of the template, however, this is not generally recommended, since you should
        /// get any error handling out of the way first.
        /// </param>
        /// <param name="includeCallingAssembly">If true, includes the assembly of the calling
        /// function in the compile. This is a convenience meaning types in the calling assembly
        /// are available in the template.</param>
        /// <param name="noWrapInOutputAdd">SEt true to remove indent from the compiled code, mainly
        /// for testing code.</param>
        public TemplateEz(IEnumerable<string> templateLines, string modelType = null,
            List<string> extraLibraries = null, bool lazyCompile = false,
            bool includeCallingAssembly = true, bool noWrapInOutputAdd = false)
        {
            var template = new TemplateDef()
            {
                Name = "Main",
                TemplateText = templateLines.ToList(),
                ModelType = modelType
            };
            NoWrapInOutputAdd = noWrapInOutputAdd;
            Init(new List<TemplateDef> { template },
                extraLibraries, lazyCompile, includeCallingAssembly,
                Assembly.GetCallingAssembly().Location);
        }

        /// <summary>
        /// Constructor. Create a set of templates by taking the template text and compiling it
        /// into a function that, when run, generates the required template text.
        /// </summary>
        /// <param name="templateDefinitions">The templates to use</param>
        /// <param name="extraLibraries">Any extra libraries to load. If this is null it
        /// by default includes the currently executing assembly, so that the types in this
        /// program are available in your template code.</param>
        /// <param name="lazyCompile">If true the compile is delayed until the first
        /// use of the template, however, this is not generally recommended, since you should
        /// get any error handling out of the way first.
        /// </param>
        /// <param name="includeCallingAssembly">If true, includes the assembly of the calling
        /// function in the compile. This is a convenience meaning types in the calling assembly
        /// are available in the template.</param>
        /// <param name="noWrapInOutputAdd">SEt true to remove indent from the compiled code, mainly
        /// for testing code.</param>
        public TemplateEz(IEnumerable<TemplateDef> templateDefinitions,
            List<string> extraLibraries = null,
            bool lazyCompile = false,
            bool includeCallingAssembly = true,
            bool noWrapInOutputAdd = false)
        {
            NoWrapInOutputAdd = noWrapInOutputAdd;
            Init(templateDefinitions, extraLibraries, lazyCompile, includeCallingAssembly,
                Assembly.GetCallingAssembly().Location);
        }

        /// <summary>
        /// Constructor. Create a set of templates by taking the template text and compiling it
        /// into a function that, when run, generates the required template text.
        /// </summary>
        /// <param name="templateDefinitions">The templates to use</param>
        /// <param name="extraLibraries">Any extra libraries to load. If this is null it
        ///     by default includes the currently executing assembly, so that the types in this
        ///     program are available in your template code.</param>
        /// <param name="lazyCompile">If true the compile is delayed until the first
        ///     use of the template, however, this is not generally recommended, since you should
        ///     get any error handling out of the way first.
        /// </param>
        /// <param name="includeCallingAssembly">If true, includes the assembly of the calling
        ///     function in the compile. This is a convenience meaning types in the calling assembly
        ///     are available in the template.</param>
        /// <param name="callingAssembly">The calling assembly of the constructor</param>
        private void Init(IEnumerable<TemplateDef> templateDefinitions,
            List<string> extraLibraries,
            bool lazyCompile,
            bool includeCallingAssembly,
            string callingAssembly)
        {
            // Code for compiling built in here.
            var codeBuilder = new StringBuilder(10000); // string builder for code
            try
            {
                var extraUsing = new HashSet<string>();
                var extraCode = new List<string>();
                _decoratedTemplates = new List<DecoratedTemplateDef>();
                _extraLibraries = new HashSet<string>();
                if (extraLibraries == null)
                    extraLibraries = new List<string>();
                _extraLibraries.UnionWith(extraLibraries);
                if (includeCallingAssembly && callingAssembly != null)
                    _extraLibraries.Add(callingAssembly);

                foreach (var template in templateDefinitions)
                {
                    _decoratedTemplates.Add(new DecoratedTemplateDef
                    {
                        Template = template,
                        ModelType = template.ModelType,
                    });
                    extraUsing.UnionWith(GetExtraUsingLinesFromTemplate(template.TemplateText));
                    extraCode.AddRange(GetExtraCodeFromTemplate(template.TemplateText));
                    _extraLibraries.UnionWith(GetExtraLibrariesFromTemplate(template.TemplateText));
                }
                // Check for basic things like the model type and libraries exist.
                PreValidate(_extraLibraries.ToList(), _decoratedTemplates);

                // Join together the default from and back end with the transformed template code.
                // We use substitution to insert various features.
                codeBuilder.Append(string.Join(Environment.NewLine, _frontTemplate)
                    .Replace("##EXTRA_USING##",
                        string.Join(Environment.NewLine,
                            extraUsing.Select(u => $"using {u};")))
                    .Replace("##EXTRA_CODE##",
                        string.Join(Environment.NewLine, extraCode))
                );
                foreach (var template in _decoratedTemplates)
                {
                    codeBuilder.Append(string.Join(Environment.NewLine, _frontFnTemplate)
                        .Replace("##MODEL_TYPE##", template.ModelType)
                        .Replace("##FN_NAME##", template.Template.Name));
                    foreach (var line in template.Template.TemplateText)
                    {
                        if (line != _sentinelForEmptyLine)
                        {
                            var transformed = TransformLine(line, NoWrapInOutputAdd);
                            if (!string.IsNullOrWhiteSpace(transformed))
                                codeBuilder.Append(transformed);
                        }
                    }
                    codeBuilder.Append(string.Join(Environment.NewLine, _endFnTemplate));
                }

                codeBuilder.Append(string.Join(Environment.NewLine, _endTemplate));

                _compileCode = codeBuilder.ToString();
                if (!lazyCompile)
                    Compile(_compileCode, _extraLibraries);
            }
            catch (TemplateEzException e)
            {
                // If inner function throws, we rethrow since it has more details
                e.Code = codeBuilder.ToString();
                throw;
            }
            catch (Exception e)
            {
                throw new TemplateEzException(e.Message, e) { Code = codeBuilder.ToString() };
            }
        }


        /// <summary>
        /// Check the extra libraries all exist, and that model type has been set.
        /// </summary>
        /// <param name="extraLibraries"></param>
        /// <param name="templates"></param>
        private void PreValidate(List<string> extraLibraries, List<DecoratedTemplateDef> templates)
        {
            var names = new HashSet<string>();
            foreach (var template in templates)
            {
                // Note here we validate the model names. We could load all the assemblies
                // and check that that type exists somewhere, however, this would leave all
                // those assemblies loaded, which seems wasteful. If the type is wrong it will
                // fail in the compile step.
                if (names.Contains(template.Template.Name))
                    throw new TemplateEzException(
                        $"Two templates have the same name {template.Template.Name}." +
                        " They must be unique.");

                names.Add(template.Template.Name);
                if (!_nameRegex.IsMatch(template.Template.Name))
                    throw new TemplateEzException(
                        $"Invalid template name \"{template.Template.Name}\"." +
                        " It may only contain letters digits or underscores.");

                if (string.IsNullOrWhiteSpace(template.ModelType))
                    throw new TemplateEzException(
                        $"No model type specified for {template.Template.Name}, " +
                        "pass a model type to the constructor");
            }

            foreach (var lib in extraLibraries)
            {
                if (!File.Exists(lib))
                    throw new TemplateEzException("Reference to non existent library " + lib);
            }
        }

        /// <summary>
        /// Search the template for any extra libraries required.
        /// </summary>
        /// <param name="templateText"></param>
        /// <returns></returns>
        private List<string> GetExtraLibrariesFromTemplate(List<string> templateText)
        {
            var result = new List<string>();
            for (var lineNum = 0; lineNum < templateText.Count; lineNum++)
            {
                var line = templateText[lineNum].Trim();
                if (line.ToLower().StartsWith("#library"))
                {
                    var lib = line.Substring("#library".Length).Trim();
                    result.Add(lib);
                    templateText[lineNum] = _sentinelForEmptyLine;
                }
                // ReSharper disable once StringLiteralTypo
                else if (line.ToLower().StartsWith("#libfor"))
                {
                    // ReSharper disable once StringLiteralTypo
                    var usingItem = line.Substring("#libfor".Length).Trim();
                    try
                    {
                        var libLocation = Assembly.Load(new AssemblyName(usingItem)).Location;
                        result.Add(libLocation);
                        templateText[lineNum] = _sentinelForEmptyLine;
                    }
                    catch (FileNotFoundException)
                    {
                        // ReSharper disable once StringLiteralTypo
                        throw new TemplateEzException($"Could not find library for \"{line}\"");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Search the template for any extra code blocks.
        /// </summary>
        /// <param name="templateText"></param>
        /// <returns></returns>
        private List<string> GetExtraCodeFromTemplate(List<string> templateText)
        {
            var gathering = false;
            var result = new List<string>();
            var blockStartLineNum = -1;
            for (var lineNum = 0; lineNum < templateText.Count; lineNum++)
            {
                var line = templateText[lineNum].Trim();
                if (!gathering && line.ToLower().Replace(" ", "") == "#code{")
                {
                    templateText[lineNum] = _sentinelForEmptyLine;
                    gathering = true;
                    blockStartLineNum = lineNum;
                }
                else if (gathering && line == "#}")
                {
                    templateText[lineNum] = _sentinelForEmptyLine;
                    gathering = false;
                }
                else if (gathering)
                {
                    result.Add(templateText[lineNum]);
                    templateText[lineNum] = _sentinelForEmptyLine;
                }
            }

            if (gathering)
                throw new TemplateEzException($"Unterminated #code{{ block in template starting at line {blockStartLineNum}");
            return result;
        }

        /// <summary>
        /// Search the template for any extra using lines it needs.
        /// </summary>
        /// <param name="templateText"></param>
        /// <returns></returns>
        private List<string> GetExtraUsingLinesFromTemplate(List<string> templateText)
        {
            var result = new List<string>();
            for (var lineNum = 0; lineNum < templateText.Count; lineNum++)
            {
                var line = templateText[lineNum].Trim();
                if (line.ToLower().StartsWith("#using"))
                {
                    // The replace fixes a common error where the #using line ends in a ;
                    var lib = line.Substring("#using".Length).Trim().Replace(";", "");
                    result.Add(lib);
                    templateText[lineNum] = _sentinelForEmptyLine;
                }
            }
            return result;
        }

        /// <summary>
        /// This function takes a line from the template and transforms it into the equivalent
        /// code line, which is to say either it leaves it as is if it is an # line, or transforms
        /// it to an output.Add(...) line that will output that line into the generated template.
        /// However, it must also look for # blocks within the line so that they are transformed
        /// into actual variables. For for example this like:
        /// Username: @Model.Username
        /// is transformed into:
        /// output.Add($"Username: {Model.Username}");
        /// Or:
        /// UserName: @(Model.FirstName + " " + Model.LastName)
        /// Into:
        /// output.Add($"Username: {Model.FirstName + " " + Model.LastName}");
        /// There are a lot of tricky edge cases, so this is modeled as a simple state machine.
        /// </summary>
        /// <param name="line">Line to transform</param>
        /// <param name="noWrapInOutputAdd">Don't add the output.Add(...) stuff, this is mainly
        /// to simplify testing.</param>
        /// <param name="recursionGuard">Normally true, but is needed in one special case where
        /// we make a recursive call to prevent an infinite loop</param>
        /// <returns>The transformed line</returns>
        private string TransformLine(string line, bool noWrapInOutputAdd = false, bool recursionGuard = false)
        {
            // First check for # lines
            if (line.Trim().StartsWith("#template("))
            {
                // It is a template line we must parse
                var parameters = line.Trim().Substring("#template(".Length);
                if (!parameters.EndsWith(")"))
                    throw new TemplateEzException($"Unterminated #template line: \"{line}\"");
                parameters = parameters.Substring(0, parameters.Length - 1);
                var split = parameters.Split(',');
                if (split.Length != 2)
                    throw new TemplateEzException($"Invalid #template line: \"{line}\"");
                var templateName = split[0];
                var model = split[1];
                if (_decoratedTemplates.All(dt => dt.Template.Name != templateName))
                    throw new TemplateEzException($"Unknown template on #template line: \"{line}\"");
                return $"{_indent}output.AddRange(Run_{templateName}({model}));{Environment.NewLine}";
            }
            if (line.Trim().StartsWith("//"))
            {
                // Remove comment lines
                return null;
            }

            // Special case -- we retain blank lines
            if (line.Trim() == string.Empty)
            {
                if (noWrapInOutputAdd)
                    return line;
                else
                    return _indent + "output.Add(@$\"" + line + "\");" + Environment.NewLine;
            }

            if (!recursionGuard && line.Trim().StartsWith("#"))
            {
                var indexOf = line.IndexOf("#", StringComparison.Ordinal);
                return _indent + line.Substring(0, indexOf) + line.Substring((indexOf + 1)) + Environment.NewLine;
            }

            if (!recursionGuard && line.Trim().StartsWith("\\#"))
            {
                // Here we strip off a \# and then reprocess, however, we need a special recursion
                // guard so that we know that the \ has been stripped
                var indexOf = line.IndexOf("\\#", StringComparison.Ordinal);
                return TransformLine(line.Substring(0, indexOf) + line.Substring((indexOf + 1)),
                    noWrapInOutputAdd, true);
            }
            else
            {
                // Now transform a main body line using a state machine. The state is captured
                // in the following variables.
                var readingCode = false;
                var readFirstCharOfCode = false;
                var pendingCloseBracketOpenBracket = false;
                var openBracketCount = 0;
                var buildString = new StringBuilder(1000);
                var pendingCode = new StringBuilder(100);
                // Keep the initial whitespace to indent the code properly
                var initialWhitespaceCount = 0;
                int chIndex;
                for (chIndex = 0; chIndex < line.Length; chIndex++)
                    if (char.IsWhiteSpace(line[chIndex]))
                        buildString.Append(line[chIndex]);
                    else
                        break;
                buildString.Append(new string(' ', initialWhitespaceCount));
                // ReSharper disable RedundantAssignment
                for (; chIndex < line.Length; chIndex++)
                {
                    var c = line[chIndex];
                    if (readingCode && c == '(')
                        openBracketCount++;
                    else if (readingCode && c == ')')
                        openBracketCount--;

                    if (!readingCode && c == '@')
                    {
                        // Start scanning for an identifier
                        readingCode = true;
                        readFirstCharOfCode = false;
                        pendingCloseBracketOpenBracket = false;
                    }
                    else if (readingCode && !readFirstCharOfCode && c == '(')
                    {
                        // Second character -- if an open bracket expect a terminating one.
                        readFirstCharOfCode = true;
                        pendingCloseBracketOpenBracket = true;
                    }
                    else if (readingCode && !readFirstCharOfCode && c == '@')
                    {
                        // Look for an escaped @@ and if so break out of the identifier
                        readingCode = readFirstCharOfCode = pendingCloseBracketOpenBracket = false;
                        buildString.Append("@");
                        pendingCode.Clear();
                    }
                    else if (readingCode && readFirstCharOfCode &&
                             pendingCloseBracketOpenBracket && c == ')' && openBracketCount == 0)
                    {
                        // End if a bracketed identifier
                        buildString.Append($"{{{pendingCode}}}");
                        pendingCode.Clear();
                        readingCode = readFirstCharOfCode = pendingCloseBracketOpenBracket = false;
                    }
                    else if (readingCode && !readFirstCharOfCode && c == ')')
                    {
                        // Weird case of @) just put out @) though this is probably an error
                        buildString.Append("@)");
                        pendingCode.Clear();
                        readingCode = readFirstCharOfCode = pendingCloseBracketOpenBracket = false;
                    }
                    else if (readingCode && !readFirstCharOfCode && c != '(')
                    {
                        // Starting reading an identifier that isn't bracketed
                        pendingCode.Append(c);
                        pendingCloseBracketOpenBracket = false;
                        readFirstCharOfCode = true;
                    }
                    else if (readingCode && !pendingCloseBracketOpenBracket &&
                             (char.IsLetter(c) || char.IsNumber(c) || c == '.'))
                    {
                        // Reading an identifier
                        pendingCode.Append(c);
                    }
                    else if (readingCode && readFirstCharOfCode && pendingCloseBracketOpenBracket &&
                             (c != ')' || openBracketCount > 0))
                    {
                        // Reading a code bock within a set of brackets
                        pendingCode.Append(c);
                    }
                    else if (readingCode)
                    {
                        // all other cases we have terminated the code
                        buildString.Append($"{{{pendingCode}}}");
                        pendingCode.Clear();
                        readingCode = readFirstCharOfCode = pendingCloseBracketOpenBracket = false;
                        // Note we have to back track here to reprocess this character with reading code as false
                        chIndex--;
                    }
                    else if (c == '\"')
                    {
                        // Deal with special cases in an $"" string
                        buildString.Append("\"\"");
                    }
                    else if (c == '{')
                    {
                        // Deal with special cases in an $"" string
                        buildString.Append("{{");
                    }
                    else if (c == '}')
                    {
                        // Deal with special cases in an $"" string
                        buildString.Append("}}");
                    }
                    else
                    {
                        buildString.Append(c);
                    }
                    // ReSharper enable RedundantAssignment
                }

                if (readingCode && !pendingCloseBracketOpenBracket && readFirstCharOfCode)
                {
                    // clean up if identifier ends the line
                    buildString.Append($"{{{pendingCode}}}");
                    readingCode = readFirstCharOfCode = pendingCloseBracketOpenBracket = false;
                }
                else if (readingCode)
                {
                    // We ran off the end without finding an expected ')'
                    throw new TemplateEzException("This template line has unterminated code " + line);
                }

                var finishedLine = buildString.ToString();
                if (string.IsNullOrWhiteSpace(finishedLine))
                    return null;
                else if (noWrapInOutputAdd)
                    return new string(' ', initialWhitespaceCount) + finishedLine;
                else
                    return _indent +
                           "output.Add(@$\"" + finishedLine + "\");" + Environment.NewLine;
            }
        }

        /// <summary>
        ///  Compile the template into an assembly.
        /// </summary>
        /// <param name="programCode">Text to compile</param>
        /// <param name="extraLibraries">Paths to any extra libraries to include</param>
        private void Compile(string programCode, HashSet<string> extraLibraries)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(programCode);
            if (extraLibraries == null)
                extraLibraries = new HashSet<string>();
            extraLibraries.Add(typeof(object).Assembly.Location);
            extraLibraries.Add((typeof(ExpandoObject).Assembly.Location));
            extraLibraries.Add((Assembly.Load(new AssemblyName("Microsoft.CSharp")).Location));
            extraLibraries.Add((Assembly.Load(new AssemblyName("netstandard")).Location));
            extraLibraries.Add((Assembly.Load(new AssemblyName("mscorlib")).Location));
            extraLibraries.Add((Assembly.Load(new AssemblyName("System.Runtime")).Location));
            extraLibraries.Add((Assembly.Load(new AssemblyName("System.IO.Filesystem")).Location));
            extraLibraries.Add((Assembly.Load(new AssemblyName("Newtonsoft.Json")).Location));
            extraLibraries.Add((Assembly.Load(new AssemblyName("System.ObjectModel")).Location));
            extraLibraries.Add((Assembly.Load(new AssemblyName("System.Collections")).Location));
            extraLibraries.Add((Assembly.Load(new AssemblyName("System.Linq")).Location));
            // Add standard libraries;
            var metadataReferences = new List<MetadataReference>();
            foreach (var lib in extraLibraries)
                metadataReferences.Add(MetadataReference.CreateFromFile(lib));
            var compilation = CSharpCompilation.Create(
                    _generatedAssemblyName,
                    new[] { syntaxTree },
                    metadataReferences,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var memoryStream = new MemoryStream())
            {
                var result = compilation.Emit(memoryStream);

                if (result.Success)
                {
                    // Load the assembly into memory
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    _assembly = Assembly.Load(memoryStream.ToArray());
                }
                else
                {
                    _compileFailed = true;
                    throw new TemplateEzException("Compile failed")
                    {
                        Code = programCode,
                        CompileErrors = result
                    };
                }
            }
        }


        /// <summary>
        ///  Execute an existing template and return the generated text.
        /// </summary>
        /// <param name="model">Model to pass in</param>
        /// <param name="generatedFunctionName">Name of template to execute</param>

        /// <returns>The generated template</returns>
        public List<string> Execute(object model, string generatedFunctionName = "Main")
        {
            // Ensure it is compiled
            if (_assembly == null && _compileFailed)
                throw new TemplateEzException("Trying to execute a template with failed compilation");
            if (_assembly == null)
                Compile(_compileCode, _extraLibraries);

            // Find the function in the assembly by reflection and call it passing in the source data as
            // a parameter.
            Type testClassType = _assembly.GetType($"{_generatedNamespaceName}.{_generatedClassName}");
            var methodInfo = testClassType.GetMethod("Run_" + generatedFunctionName)
                       ?? throw new TemplateEzException($"Trying to execute unknown template {generatedFunctionName}.");

            var result = (List<string>)methodInfo.Invoke(null, new[] { model });
            return result;
        }

        private readonly string _sentinelForEmptyLine = "##DELETE_ME_43c6ec3d-e563-4441-8093-f9e029b969de";
        private readonly string _generatedAssemblyName = "EZTemplateAssembly";
        private readonly string _generatedNamespaceName = "EZTemplate";
        private readonly string _generatedClassName = "Program";
        private readonly string _indent = "            ";
        private readonly string[] _frontTemplate =
        {
            "using System;",
            "using System.Collections.Generic;",
            "##EXTRA_USING##",
            "namespace EZTemplate",
            "{",
            "    public class Program",
            "    {",
            "##EXTRA_CODE##",
            "",
            };
        private readonly string[] _frontFnTemplate =
        {
            "        public static List<string> Run_##FN_NAME##(##MODEL_TYPE## Model)",
            "        {",
            "            List<string> output = new List<string>();",
            "",
        };

        private readonly string[] _endFnTemplate =
        {
            "            return output;",
            "        }",
            "",
        };
        private readonly string[] _endTemplate =
        {
            "    }",
            "}"
        };
    }

    /// <summary>
    /// Definition of a template where you pass in more than one.
    /// </summary>
    public struct TemplateDef
    {
        public string Name;
        public List<string> TemplateText;
        public string ModelType;
    }

    /// <summary>
    /// Decorated template - TemplateDef with extra information
    /// </summary>
    internal struct DecoratedTemplateDef
    {
        public string ModelType { get; set; }
        public TemplateDef Template { get; set; }
    }

    /// <summary>
    /// Exception thrown from template generation, containing all the information
    /// needed to debug it.
    /// </summary>
    public class TemplateEzException : Exception
    {
        public TemplateEzException(string message, Exception inner = null) : base(message, inner)
        {
        }

        /// <summary>
        /// THis is the code of the template that was generated
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// If the compile failed this is the error messages
        /// </summary>
        public EmitResult CompileErrors { get; set; }
    }
}
