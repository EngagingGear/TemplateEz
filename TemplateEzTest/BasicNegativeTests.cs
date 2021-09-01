using System;
using System.Linq;
using TemplateEzNS;
using TemplateEzTest.TestDataTypes;
using Xunit;

namespace TemplateEzTest
{
    public class BasicNegativeTests: BaseTest
    {
        [Fact]
        public void ErrorInTemplateCode()
        {
            var templateText = SubSpecialChars(
                "#if(string.IsNullOrEmpty(@Model.FirstName)",
                "Dear @Model.FirstName @Model.LastName,"
            );
            var obj = new Person("Fred", "Smith", new DateTime(2001, 11, 11));
            var hadException = false;
            try
            {
                var template = GenerateEzTemplate(templateText, "TemplateEzTest.TestDataTypes.Person");
            }
            catch (Exception)
            {
                hadException = true;
            }
            Assert.True(hadException);
        }
        [Fact]
        public void UnterminatedCodeBlock()
        {
            var templateText = SubSpecialChars(
                "#code{",
                "    private static MakeName(string firstName, string lastName)",
                "    { return @firstName + | | + lastName; }",
                "Dear @Model.FirstName @Model.LastName,"
            );
            var obj = new Person("Fred", "Smith", new DateTime(2001, 11, 11));
            var hadException = false;
            try
            {
                var template = GenerateEzTemplate(templateText, "TemplateEzTest.TestDataTypes.Person");
            }
            catch (Exception)
            {
                hadException = true;
            }
            Assert.True(hadException);
        }

        [Fact]
        public void TestLibraryLoadInvalidLibrary()
        {
            var libraryPath = InvalidLibraryPath();
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
            var hadException = false;
            try
            {
                var template = GenerateEzTemplate(templateText, "TemplateEzTest.TestDataTypes.Person");
            }
            catch (Exception)
            {
                hadException = true;
            }
            Assert.True(hadException);
        }

        [Fact]
        public void TestLibraryLoadInvalidLibraryPath()
        {
            var templateText = SubSpecialChars(
                $"#library junk.non-existent.library",
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

            var model = new Person("Mary", "Jones", new DateTime(2002, 2, 22));
            var exception = Assert.Throws<TemplateEzException>(() =>
            {
                var template = GenerateEzTemplate(templateText, "TemplateEzTest.TestDataTypes.Person");
                template.Execute(model, "Template2");
            });

            Assert.Equal($"Reference to non existent library junk.non-existent.library", exception.Message);
        }

        [Fact]
        public void TestLibForLoadInvalidLibrary()
        {
            var templateText = SubSpecialChars(
                "#libfor junk.non-existent.library",
                "Name is @(MakeName(@Model.FirstName,@Model.LastName))"
            );
            var obj = new Person("Mary", "Jones", new DateTime(2002, 2, 22));
            var hadException = false;
            try
            {
                var template = GenerateEzTemplate(templateText, "TemplateEzTest.TestDataTypes.Person");
            }
            catch (Exception)
            {
                hadException = true;
            }
            Assert.True(hadException);
        }

        [Fact]
        public void TestInvalidIfStatementInCodeBlock()
        {
            var templateText = SubSpecialChars(
                $"" +
                "#code{",
                "\t\tprivate static DateTime testDateOfBirth = new DateTime(2002, 2, 22);",
                "#}",
                $"#if(Model.DateOfBirth == null",
                "Please let us know what your birthday is",
                "#else if(Model.DateOfBirth == testDateOfBirth)",
                "Happy Birthday!!",
                "#else",
                "Your birthday is @(Model.DateOfBirth.Value.ToString(|MM/dd/yyyy|))"
            );

            var model = new Person("Mary", "Jones", null);
            var exception = Assert.Throws<TemplateEzException>(() =>
            {
                var template = new TemplateEz(SubSpecialChars(templateText), "TemplateEzTest.TestDataTypes.Person");
                template.Execute(model);
            });

            Assert.Equal($"Compile failed", exception.Message);
        }

        [Fact]
        public void TestInvalidIfStatementInCodeBlockWithLazyCompile()
        {
            var templateText = SubSpecialChars(
                $"" +
                "#code{",
                "\t\tprivate static DateTime testDateOfBirth = new DateTime(2002, 2, 22);",
                "#}",
                $"#if(Model.DateOfBirth == null",
                "Please let us know what your birthday is",
                "#else if(Model.DateOfBirth == testDateOfBirth)",
                "Happy Birthday!!",
                "#else",
                "Your birthday is @(Model.DateOfBirth.Value.ToString(|MM/dd/yyyy|))"
            );

            var model = new Person("Mary", "Jones", null);

            var template = new TemplateEz(SubSpecialChars(templateText), "TemplateEzTest.TestDataTypes.Person", lazyCompile: true);

            var exception = Assert.Throws<TemplateEzException>(() =>
            {
                template.Execute(model);
            });

            Assert.Equal($"Compile failed", exception.Message);
        }

        [Fact]
        public void TestLibForWithMissingFile()
        {
            var templateText = SubSpecialChars(
                $"#libfor LibraryForTest.Dll",
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

            var exception = Assert.Throws<TemplateEzException>(() =>
            {
                var template = GenerateEzTemplate(templateText, "TemplateEzTest.TestDataTypes.Person");
                template.Execute(obj);
            });

            Assert.Equal($"Could not find library for \"#libfor LibraryForTest.Dll\"", exception.Message);
        }

    }
}
