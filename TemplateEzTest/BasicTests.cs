using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TemplateEzNS;
using TemplateEzTest.TestDataTypes;
using Xunit;

namespace TemplateEzTest
{
    public class BasicTests: BaseTest
    {
        [Fact]
        public void BasicTemplateWithFredSmith()
        {
            var templateText = SubSpecialChars(
                "Dear @Model.FirstName @Model.LastName,",
                "",
                "Your Date of birth is @(Model.DateOfBirth.Value.ToString(|MM/dd/yyyy|))"
            );
            var obj = new Person("Fred", "Smith", new DateTime(2001, 11, 11));
            var template = GenerateEzTemplate(templateText, "TemplateEzTest.TestDataTypes.Person");

            var result = template.Execute(obj).ToList();

            Assert.True(CompareStrList(result,
                    "Dear Fred Smith,",
                    "",
                    "Your Date of birth is 11/11/2001"
                ));
        }

        [Fact]
        public void BasicTemplateWithMaryJones()
        {
            var templateText = SubSpecialChars(
                "Dear @Model.FirstName @Model.LastName,",
                "",
                "Your Date of birth is @(Model.DateOfBirth.Value.ToString(|MM/dd/yyyy|))"
            );
            var obj = new Person("Mary", "Jones", new DateTime(2002, 2, 22));
            var template = GenerateEzTemplate(templateText, "TemplateEzTest.TestDataTypes.Person");

            var result = template.Execute(obj).ToList();

            Assert.True(CompareStrList(result,
                "Dear Mary Jones,",
                "",
                "Your Date of birth is 02/22/2002"
            ));
        }

        [Theory]
        [
            InlineData("Blank string", "   ", "   "),
            InlineData("Delete comments", "  // comment should be deleted ", null),
            InlineData("Escape pound", "  \\# Escaped line", "  # Escaped line"),
            InlineData("Simple unaltered text", " simple text", " simple text"),
            InlineData("Transform model reference", " simple text @Model.IsIncluded", " simple text {Model.IsIncluded}"),
            InlineData("Transform model reference in brackets", " simple text @(Model.IsIncluded)", " simple text {Model.IsIncluded}"),
            InlineData("Transform multiple step model ref", " simple text @Model.Description.IsIncluded", " simple text {Model.Description.IsIncluded}"),
            InlineData("Transform multiple model refs", " simple text @Model.IsIncluded @Model.Description", " simple text {Model.IsIncluded} {Model.Description}"),
            InlineData("Transform multiple model refs followed by text", " simple text @Model.IsIncluded @Model.Description followed by text", " simple text {Model.IsIncluded} {Model.Description} followed by text"),
            InlineData("Transform model reference at start", "@Model.IsIncluded @Model.Description at the start", "{Model.IsIncluded} {Model.Description} at the start"),
            InlineData("Deal with inline {}", "{@Model.IsIncluded} @Model.Description special treatment applies with back track", "{{{Model.IsIncluded}}} {Model.Description} special treatment applies with back track"),
            InlineData("Deal with @@", " simple @@ text", " simple @ text"),
            InlineData("Deal with @)", " simple @) text", " simple @) text"),
            InlineData("Deal with {} in text", " simple {text}", " simple {{text}}"),
            InlineData("Deal with quote marks", " simple \"text\"", " simple \"\"text\"\""),
            InlineData("Check that brackets are balanced in handling", "simple @(t(Format(), test)) more", "simple {t(Format(), test)} more"),
        ]
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
        public void TestLineTransform(string description, string source, string response)
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
        {
            var templateText = new[] {""};
            var dummy = new Person("Mary", "Jones", new DateTime(2002, 2, 22));
            var template = GenerateEzTemplate(templateText, "TemplateEzTest.TestDataTypes.Person", true);
            var result = CallPrivateMethod(template, "TransformLine", source, true, false) as string;
            //var resultList = template.Execute(dummy);
            Assert.Equal(response, result);
        }

        [Fact]
        public void TestLibraryLoad()
        {
            var libraryPath = LibraryPath();
            var templateText = SubSpecialChars(
                $"#library {libraryPath}",
                "#code{",
                "\tprivate static string MakeName(string f, string l)",
                "\t{",
                "\t\tvar s = new LibraryForTest.SampleClass();",
                "\t\treturn s.MakeName(f,l);",
                "\t}",
                "#}",
                "Name is @(MakeName(@Model.FirstName,@Model.LastName))"
            );
            var obj = new Person("Mary", "Jones", new DateTime(2002, 2, 22));
            var template = GenerateEzTemplate(templateText, "TemplateEzTest.TestDataTypes.Person");

            var result = template.Execute(obj).ToList();

            Assert.True(CompareStrList(result, "Name is Mary Jones"));
        }

        [Fact]
        public void TestAddingUsing()
        {
            var libraryPath = LibraryPath();
            var templateText = SubSpecialChars(
                $"#library {libraryPath}",
                "#using LibraryForTest",
                "#code{",
                "\tprivate static string MakeName(string f, string l)",
                "\t{",
                "\t\tvar s = new SampleClass();",
                "\t\treturn s.MakeName(f,l);",
                "\t}",
                "#}",
                "Name is @(MakeName(@Model.FirstName,@Model.LastName))"
            );
            var obj = new Person("Mary", "Jones", new DateTime(2002, 2, 22));
            var template = GenerateEzTemplate(templateText, "TemplateEzTest.TestDataTypes.Person");

            var result = template.Execute(obj).ToList();

            Assert.True(CompareStrList(result, "Name is Mary Jones"));
        }

        [Fact]
        public void TestAddingUsingWithUnnecessarySemicolon()
        {
            var libraryPath = LibraryPath();
            var templateText = SubSpecialChars(
                $"#library {libraryPath}",
                "#using LibraryForTest;",
                "#code{",
                "\tprivate static string MakeName(string f, string l)",
                "\t{",
                "\t\tvar s = new SampleClass();",
                "\t\treturn s.MakeName(f,l);",
                "\t}",
                "#}",
                "Name is @(MakeName(@Model.FirstName,@Model.LastName))"
            );
            var obj = new Person("Mary", "Jones", new DateTime(2002, 2, 22));
            var template = GenerateEzTemplate(templateText, "TemplateEzTest.TestDataTypes.Person");

            var result = template.Execute(obj).ToList();

            Assert.True(CompareStrList(result, "Name is Mary Jones"));
        }

        [Fact]
        public void TestIfStatement()
        {
            var templateText = SubSpecialChars(
                $"" +
                "#code{",
                "\t\tprivate static DateTime testDateOfBirth = new DateTime(2002, 2, 22);", 
                "#}",
                $"#if(Model.DateOfBirth == null)",
                "Please let us know what your birthday is",
                "#else if(Model.DateOfBirth == testDateOfBirth)",
                "Happy Birthday!!",
                "#else",
                "Your birthday is @(Model.DateOfBirth.Value.ToString(|MM/dd/yyyy|))"
            );

            var model1 = new Person("Mary", "Jones", null);
            var model2 = new Person("John", "Smith", new DateTime(2002, 2, 22));
            var model3 = new Person("Adam", "Smith", new DateTime(2002, 2, 11));

            var template = new TemplateEz(SubSpecialChars(templateText), "TemplateEzTest.TestDataTypes.Person");
            var result1 = template.Execute(model1).ToList();
            var result2 = template.Execute(model2).ToList();
            var result3 = template.Execute(model3).ToList();


            Assert.True(CompareStrList(result1,
                "Please let us know what your birthday is"
            ));

            Assert.True(CompareStrList(result2,
                "Happy Birthday!!"
            ));

            Assert.True(CompareStrList(result3,
                "Your birthday is 02/11/2002"
            ));
        }

        [Fact]
        public void TestIfStatementWithLazyCompile()
        {
            var templateText = SubSpecialChars(
                $"" +
                "#code{",
                "\t\tprivate static DateTime testDateOfBirth = new DateTime(2002, 2, 22);", 
                "#}",
                $"#if(Model.DateOfBirth == null)",
                "Please let us know what your birthday is",
                "#else if(Model.DateOfBirth == testDateOfBirth)",
                "Happy Birthday!!",
                "#else",
                "Your birthday is @(Model.DateOfBirth.Value.ToString(|MM/dd/yyyy|))"
            );

            var model1 = new Person("Mary", "Jones", null);
            var model2 = new Person("John", "Smith", new DateTime(2002, 2, 22));
            var model3 = new Person("Adam", "Smith", new DateTime(2002, 2, 11));

            var template = new TemplateEz(SubSpecialChars(templateText), "TemplateEzTest.TestDataTypes.Person", lazyCompile: true);
            var result1 = template.Execute(model1).ToList();
            var result2 = template.Execute(model2).ToList();
            var result3 = template.Execute(model3).ToList();


            Assert.True(CompareStrList(result1,
                "Please let us know what your birthday is"
            ));

            Assert.True(CompareStrList(result2,
                "Happy Birthday!!"
            ));

            Assert.True(CompareStrList(result3,
                "Your birthday is 02/11/2002"
            ));
        }

        [Fact]
        public void TestForStatement()
        {
            var templateText = SubSpecialChars(
                "#using System.Linq",
                "#if (Model.Appointments.Any())",
               "#{",
                    "<p>Dear @Model.FirstName @Model.LastName,</p>",
	                "<p>You have a few up and comping appointments</p>",
	                "<ul>",
                       "#foreach(var appt in Model.Appointments)",
		                  "<li>@(appt.ToString())</li>",
	                "</ul>",
                "#}"
            );

            var obj = new Person("Mary", "Jones", new DateTime(2002, 2, 22), new List<Appointment>()
            {
                new() {Date = new DateTime(2020, 1, 1), Description = "Appointment 1"},
                new() {Date = new DateTime(2020, 1, 2), Description = "Appointment 2"},
            });

            var template = GenerateEzTemplate(templateText, "TemplateEzTest.TestDataTypes.Person");

            var result = template.Execute(obj).ToList();

            Assert.True(CompareStrList(result,
                "<p>Dear Mary Jones,</p>",
                "<p>You have a few up and comping appointments</p>",
                "<ul>",
                "<li>01/01/2020: Appointment 1</li>",
                "<li>01/02/2020: Appointment 2</li>",
                "</ul>"
            ));
        }
    }
}
