using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder;
using TemplateEzNS;
using TemplateEzTest.TestDataTypes;
using Xunit;

namespace TemplateEzTest
{
    public class MultiTemplateNegativeTests: BaseTest
    {
        [Fact]
        public void DuplicateTemplateNames()
        {
            var libraryPath = LibraryPath();
            var templateText1 = SubSpecialChars("@Model.FirstName @Model.LastName");
            var templateText2 = SubSpecialChars(
                "Dear",
                "#template(Template1, @Model)",
                "Your date of birth is @(Model.DateOfBirth.ToString(|MM/dd/yyyy|))");

            var obj = new Person("Mary", "Jones", new DateTime(2002, 2, 22));
            var hasException = false;
            try
            {
                var tmpl = new TemplateEz(new TemplateDef[]
                {
                    new TemplateDef { Name = "Template1", ModelType = "TemplateEzTest.TestDataTypes.Person", TemplateText = templateText1.ToList() },
                    new TemplateDef { Name = "Template1", ModelType = "TemplateEzTest.TestDataTypes.Person", TemplateText = templateText2.ToList() },
                });

            }
            catch (Exception)
            {
                hasException = true;
            }
            Assert.True(hasException);
        }

        [Fact]
        public void NonExistentTemplateNames()
        {
            var libraryPath = LibraryPath();
            var templateText1 = SubSpecialChars("@Model.FirstName @Model.LastName");
            var templateText2 = SubSpecialChars(
                "Dear",
                "#template(Template3, @Model)",
                "Your date of birth is @(Model.DateOfBirth.ToString(|MM/dd/yyyy|))");

            var obj = new Person("Mary", "Jones", new DateTime(2002, 2, 22));
            var hasException = false;
            try
            {
                var tmpl = new TemplateEz(new TemplateDef[]
                {
                    new TemplateDef { Name = "Template1", ModelType = "TemplateEzTest.TestDataTypes.Person", TemplateText = templateText1.ToList() },
                    new TemplateDef { Name = "Template2", ModelType = "TemplateEzTest.TestDataTypes.Person", TemplateText = templateText2.ToList() },
                });

            }
            catch (Exception)
            {
                hasException = true;
            }
            Assert.True(hasException);
        }

        [Fact]
        public void InvalidTemplateName()
        {
            var libraryPath = LibraryPath();
            var templateText1 = SubSpecialChars("@Model.FirstName @Model.LastName");
            var templateText2 = SubSpecialChars(
                "Dear",
                "#template(Template1, @Model)",
                "Your date of birth is @(Model.DateOfBirth.ToString(|MM/dd/yyyy|))");

            var obj = new Person("Mary", "Jones", new DateTime(2002, 2, 22));
            var hasException = false;
            try
            {
                var tmpl = new TemplateEz(new TemplateDef[]
                {
                    new TemplateDef { Name = "Template$", ModelType = "TemplateEzTest.TestDataTypes.Person", TemplateText = templateText1.ToList() },
                    new TemplateDef { Name = "Template1", ModelType = "TemplateEzTest.TestDataTypes.Person", TemplateText = templateText2.ToList() },
                });

            }
            catch (Exception)
            {
                hasException = true;
            }
            Assert.True(hasException);
        }

        [Fact]
        public void NestedTemplateCalledWithWrongName()
        {
            var templateText1 = new[] {
                "#using System.Linq",
                "#if(Model == null || !Model.Any())  ",
                "",
                "#else",
                "#{",
                "<p>You have a few up and comping appointments</p>",
                "<ul>",
                "#foreach(var appt in Model)",
                "<li>@(appt.ToString())</li>",
                "</ul>",
                "#}"
                };

            var templateText2 = SubSpecialChars(
                "#using System.Linq",
                "Dear @Model.FirstName @Model.LastName",
                "Your date of birth is @(Model.DateOfBirth.Value.ToString(|MM/dd/yyyy|))",
                "#template(WrongTemplateName, Model.Appointments)"
                );

            var model = new Person("Mary", "Jones", new DateTime(2002, 2, 22), new List<Appointment>()
            {
                new() {Date = new DateTime(2020, 1, 1), Description = "Appointment 1"},
                new() {Date = new DateTime(2020, 1, 2), Description = "Appointment 2"},
            });

            Assert.Throws<TemplateEzException>(() =>
            {
                var template = new TemplateEz(new TemplateDef[]
                {
                    new() { Name = "Template1", ModelType = "IEnumerable<TemplateEzTest.TestDataTypes.Appointment>", TemplateText = templateText1.ToList() },
                    new() { Name = "Template2", ModelType = "TemplateEzTest.TestDataTypes.Person", TemplateText = templateText2.ToList() },
                });
                template.Execute(model, "Template2");
            });
        }

        [Fact]
        public void NestedTemplateCallUnclosed()
        {
            var templateText1 = new[] {
                "#using System.Linq",
                "#if(Model == null || !Model.Any())  ",
                "",
                "#else",
                "#{",
                "<p>You have a few up and comping appointments</p>",
                "<ul>",
                "#foreach(var appt in Model)",
                "<li>@(appt.ToString())</li>",
                "</ul>",
                "#}"
                };

            var templateText2 = SubSpecialChars(
                "#using System.Linq",
                "Dear @Model.FirstName @Model.LastName",
                "Your date of birth is @(Model.DateOfBirth.Value.ToString(|MM/dd/yyyy|))",
                "#template(Template1, Model.Appointments"
                );

            var model = new Person("Mary", "Jones", new DateTime(2002, 2, 22), new List<Appointment>()
            {
                new() {Date = new DateTime(2020, 1, 1), Description = "Appointment 1"},
                new() {Date = new DateTime(2020, 1, 2), Description = "Appointment 2"},
            });

            var exception = Assert.Throws<TemplateEzException>(() =>
            {
                var template = new TemplateEz(new TemplateDef[]
                {
                    new() { Name = "Template1", ModelType = "IEnumerable<TemplateEzTest.TestDataTypes.Appointment>", TemplateText = templateText1.ToList() },
                    new() { Name = "Template2", ModelType = "TemplateEzTest.TestDataTypes.Person", TemplateText = templateText2.ToList() },
                });
                template.Execute(model, "Template2");
            });
            Assert.Equal("Unterminated #template line: \"#template(Template1, Model.Appointments\"", exception.Message);

        }

        [Fact]
        public void NestedTemplateCallMalformed()
        {
            var templateText1 = new[] {
                "#using System.Linq",
                "#if(Model == null || !Model.Any())  ",
                "",
                "#else",
                "#{",
                "<p>You have a few up and comping appointments</p>",
                "<ul>",
                "#foreach(var appt in Model)",
                "<li>@(appt.ToString())</li>",
                "</ul>",
                "#}"
                };

            var templateText2 = SubSpecialChars(
                "#using System.Linq",
                "Dear @Model.FirstName @Model.LastName",
                "Your date of birth is @(Model.DateOfBirth.Value.ToString(|MM/dd/yyyy|))",
                "#template"
                );

            var model = new Person("Mary", "Jones", new DateTime(2002, 2, 22), new List<Appointment>()
            {
                new() {Date = new DateTime(2020, 1, 1), Description = "Appointment 1"},
                new() {Date = new DateTime(2020, 1, 2), Description = "Appointment 2"},
            });

            Assert.Throws<TemplateEzException>(() =>
            {
                var template = new TemplateEz(new TemplateDef[]
                {
                    new() { Name = "Template1", ModelType = "IEnumerable<TemplateEzTest.TestDataTypes.Appointment>", TemplateText = templateText1.ToList() },
                    new() { Name = "Template2", ModelType = "TemplateEzTest.TestDataTypes.Person", TemplateText = templateText2.ToList() },
                });
                template.Execute(model, "Template2");
            });
        }


        [Fact]
        public void DynamicObjectAsModelHasErrorInPropertyName()
        {
            var templateText1 = new[] {
                "#using System.Linq",
                "#if(Model == null || !Model.Any())  ",
                "",
                "#else",
                "#{",
                "<p>You have a few up and comping appointments</p>",
                "<ul>",
                "#foreach(var appt in Model)",
                "<li>@(appt.ToString())</li>",
                "</ul>",
                "#}"
                };

            var entryTemplateText = SubSpecialChars(
                "#using System.Linq",
                "#var person = Model.Person as TemplateEzTest.TestDataTypes.Person;",
                "Dear @person.FirstName @person.LastName",
                "Your date of birth is @(person.DateOfBirth.Value.ToString(|MM/dd/yyyy|))",
                "#template(Template1, person.Appointments)"
                );

            var person = new Person("Mary", "Jones", new DateTime(2002, 2, 22), new List<Appointment>()
            {
                new() {Date = new DateTime(2020, 1, 1), Description = "Appointment 1"},
                new() {Date = new DateTime(2020, 1, 2), Description = "Appointment 2"},
            });

            dynamic model = new ExpandoObject();
            model.PersonTypo = person;


            Assert.Throws<TargetInvocationException>(() =>
            {
                var template = new TemplateEz(new TemplateDef[]
                {
                    new() { Name = "Template1", ModelType = "IEnumerable<TemplateEzTest.TestDataTypes.Appointment>", TemplateText = templateText1.ToList() },
                    new() { Name = "EntryTemplate", ModelType = "dynamic", TemplateText = entryTemplateText.ToList() },
                });

                template.Execute(model, "EntryTemplate");
            });
        }

        [Fact]
        public void DeeplyNestedTemplateMalformed()
        {
            var templateText1 = new[] {
                "#using System.Linq",
                "#using System.Linq",
                "#if(Model == null || !Model.Any())  ",
                "",
                "#else",
                "#{",
                "<p>Employees</p>",
                "<ul>",
                "#foreach(var empl in Model)",
                "#{",
                "<li>",
                "<div>@(empl.ToString())</div>",
                "#template",
                "</li>",
                "#}",
                "</ul>",
                "#}"
            };


            var templateText2 = new[] {
                "#using System.Linq",
                "#if(Model == null || !Model.Any())  ",
                "",
                "#else",
                "#{",
                "<p>You have a few up and comping appointments</p>",
                "<ul>",
                "#foreach(var appt in Model)",
                "<li>@(appt.ToString())</li>",
                "</ul>",
                "#}"
                };

            var entryTemplateText = SubSpecialChars(
                "#using System.Linq",
                "Department @Model.Name",
                "#template(Template1, Model.Employees)"
                );


            var employee1 = new Person("Mary", "Jones", new DateTime(2002, 2, 22), new List<Appointment>()
            {
                new() {Date = new DateTime(2020, 1, 1), Description = "Appointment 1"},
                new() {Date = new DateTime(2020, 1, 2), Description = "Appointment 2"},
            });

            var employee2 = new Person("John", "Smith", new DateTime(2003, 8, 2), new List<Appointment>()
            {
                new() {Date = new DateTime(2020, 1, 1), Description = "Appointment 1"},
                new() {Date = new DateTime(2020, 1, 2), Description = "Appointment 2"},
            });

            var department = new Department("Sales", new List<Person>()
            {
                employee1,
                employee2
            });

            Assert.Throws<TemplateEzException>(() =>
            {
                var template = new TemplateEz(new TemplateDef[]
                {
                    new() { Name = "EntryTemplate", ModelType = "TemplateEzTest.TestDataTypes.Department", TemplateText = entryTemplateText.ToList() },
                    new() { Name = "Template1", ModelType = "IEnumerable<TemplateEzTest.TestDataTypes.Person>", TemplateText = templateText1.ToList() },
                    new() { Name = "Template2", ModelType = "IEnumerable<TemplateEzTest.TestDataTypes.Appointment>", TemplateText = templateText2.ToList() },
                });

                template.Execute(department, "EntryTemplate");
            });
        }

        [Fact]
        public void DeeplyNestedTemplateCallUnclosed()
        {
            var templateText1 = new[] {
                "#using System.Linq",
                "#using System.Linq",
                "#if(Model == null || !Model.Any())  ",
                "",
                "#else",
                "#{",
                "<p>Employees</p>",
                "<ul>",
                "#foreach(var empl in Model)",
                "#{",
                "<li>",
                "<div>@(empl.ToString())</div>",
                "#template(Template2, empl.Appointments",
                "</li>",
                "#}",
                "</ul>",
                "#}"
            };


            var templateText2 = new[] {
                "#using System.Linq",
                "#if(Model == null || !Model.Any())  ",
                "",
                "#else",
                "#{",
                "<p>You have a few up and comping appointments</p>",
                "<ul>",
                "#foreach(var appt in Model)",
                "<li>@(appt.ToString())</li>",
                "</ul>",
                "#}"
                };

            var entryTemplateText = SubSpecialChars(
                "#using System.Linq",
                "Department @Model.Name",
                "#template(Template1, Model.Employees)"
                );


            var employee1 = new Person("Mary", "Jones", new DateTime(2002, 2, 22), new List<Appointment>()
            {
                new() {Date = new DateTime(2020, 1, 1), Description = "Appointment 1"},
                new() {Date = new DateTime(2020, 1, 2), Description = "Appointment 2"},
            });

            var employee2 = new Person("John", "Smith", new DateTime(2003, 8, 2), new List<Appointment>()
            {
                new() {Date = new DateTime(2020, 1, 1), Description = "Appointment 1"},
                new() {Date = new DateTime(2020, 1, 2), Description = "Appointment 2"},
            });

            var department = new Department("Sales", new List<Person>()
            {
                employee1,
                employee2
            });

            var exception =  Assert.Throws<TemplateEzException>(() =>
            {
                var template = new TemplateEz(new TemplateDef[]
                {
                    new() { Name = "EntryTemplate", ModelType = "TemplateEzTest.TestDataTypes.Department", TemplateText = entryTemplateText.ToList() },
                    new() { Name = "Template1", ModelType = "IEnumerable<TemplateEzTest.TestDataTypes.Person>", TemplateText = templateText1.ToList() },
                    new() { Name = "Template2", ModelType = "IEnumerable<TemplateEzTest.TestDataTypes.Appointment>", TemplateText = templateText2.ToList() },
                });

                template.Execute(department, "EntryTemplate");
            });
            Assert.Equal("Unterminated #template line: \"#template(Template2, empl.Appointments\"", exception.Message);
        }

        [Fact]
        public void NestedTemplateCalledWithInvalidName()
        {
            var templateText1 = new[] {
                "#using System.Linq",
                "#if(Model == null || !Model.Any())  ",
                "",
                "#else",
                "#{",
                "<p>You have a few up and comping appointments</p>",
                "<ul>",
                "#foreach(var appt in Model)",
                "<li>@(appt.ToString())</li>",
                "</ul>",
                "#}"
            };

            var templateText2 = SubSpecialChars(
                "#using System.Linq",
                "Dear @Model.FirstName @Model.LastName",
                "Your date of birth is @(Model.DateOfBirth.Value.ToString(|MM/dd/yyyy|))",
                "#template(Template1, Model.Appointments)"
            );

            var model = new Person("Mary", "Jones", new DateTime(2002, 2, 22), new List<Appointment>()
            {
                new() {Date = new DateTime(2020, 1, 1), Description = "Appointment 1"},
                new() {Date = new DateTime(2020, 1, 2), Description = "Appointment 2"},
            });

            var exception = Assert.Throws<TemplateEzException>(() =>
            {
                var template = new TemplateEz(new TemplateDef[]
                {
                    new() { Name = "A!B", ModelType = "IEnumerable<TemplateEzTest.TestDataTypes.Appointment>", TemplateText = templateText1.ToList() },
                    new() { Name = "Template2", ModelType = "TemplateEzTest.TestDataTypes.Person", TemplateText = templateText2.ToList() },
                });
                template.Execute(model, "Template2");
            });
            Assert.Equal($"Invalid template name \"A!B\"." +
                         " It may only contain letters digits or underscores.", exception.Message);
        }

        [Fact]
        public void NestedTemplateCalledWithEmptyModelType()
        {
            var templateText1 = new[] {
                "#using System.Linq",
                "#if(Model == null || !Model.Any())  ",
                "",
                "#else",
                "#{",
                "<p>You have a few up and comping appointments</p>",
                "<ul>",
                "#foreach(var appt in Model)",
                "<li>@(appt.ToString())</li>",
                "</ul>",
                "#}"
            };

            var templateText2 = SubSpecialChars(
                "#using System.Linq",
                "Dear @Model.FirstName @Model.LastName",
                "Your date of birth is @(Model.DateOfBirth.Value.ToString(|MM/dd/yyyy|))",
                "#template(Template1, Model.Appointments)"
            );

            var model = new Person("Mary", "Jones", new DateTime(2002, 2, 22), new List<Appointment>()
            {
                new() {Date = new DateTime(2020, 1, 1), Description = "Appointment 1"},
                new() {Date = new DateTime(2020, 1, 2), Description = "Appointment 2"},
            });

            var exception = Assert.Throws<TemplateEzException>(() =>
            {
                var template = new TemplateEz(new TemplateDef[]
                {
                    new() { Name = "Template1", ModelType = "", TemplateText = templateText1.ToList() },
                    new() { Name = "Template2", ModelType = "TemplateEzTest.TestDataTypes.Person", TemplateText = templateText2.ToList() },
                });
                template.Execute(model, "Template2");
            });
            Assert.Equal($"No model type specified for Template1, " +
                         "pass a model type to the constructor", exception.Message);
        }

        [Fact]
        public void NestedTemplateCalledWithOneParameter()
        {
            var templateText1 = new[] {
                "#using System.Linq",
                "#if(Model == null || !Model.Any())  ",
                "",
                "#else",
                "#{",
                "<p>You have a few up and comping appointments</p>",
                "<ul>",
                "#foreach(var appt in Model)",
                "<li>@(appt.ToString())</li>",
                "</ul>",
                "#}"
            };

            var templateText2 = SubSpecialChars(
                "#using System.Linq",
                "Dear @Model.FirstName @Model.LastName",
                "Your date of birth is @(Model.DateOfBirth.Value.ToString(|MM/dd/yyyy|))",
                "#template(Template1)"
            );

            var model = new Person("Mary", "Jones", new DateTime(2002, 2, 22), new List<Appointment>()
            {
                new() {Date = new DateTime(2020, 1, 1), Description = "Appointment 1"},
                new() {Date = new DateTime(2020, 1, 2), Description = "Appointment 2"},
            });

            var exception = Assert.Throws<TemplateEzException>(() =>
            {
                var template = new TemplateEz(new TemplateDef[]
                {
                    new() { Name = "Template1", ModelType = "IEnumerable<TemplateEzTest.TestDataTypes.Appointment>", TemplateText = templateText1.ToList() },
                    new() { Name = "Template2", ModelType = "TemplateEzTest.TestDataTypes.Person", TemplateText = templateText2.ToList() },
                });
                template.Execute(model, "Template2");
            });
            Assert.Equal($"Invalid #template line: \"#template(Template1)\"", exception.Message);
        }

        [Fact]
        public void NestedTemplateCalledWithMoreThanTwoParameters()
        {
            var templateText1 = new[] {
                "#using System.Linq",
                "#if(Model == null || !Model.Any())  ",
                "",
                "#else",
                "#{",
                "<p>You have a few up and comping appointments</p>",
                "<ul>",
                "#foreach(var appt in Model)",
                "<li>@(appt.ToString())</li>",
                "</ul>",
                "#}"
            };

            var templateText2 = SubSpecialChars(
                "#using System.Linq",
                "Dear @Model.FirstName @Model.LastName",
                "Your date of birth is @(Model.DateOfBirth.Value.ToString(|MM/dd/yyyy|))",
                "#template(Template1, Model.Appointments, Model)"
            );

            var model = new Person("Mary", "Jones", new DateTime(2002, 2, 22), new List<Appointment>()
            {
                new() {Date = new DateTime(2020, 1, 1), Description = "Appointment 1"},
                new() {Date = new DateTime(2020, 1, 2), Description = "Appointment 2"},
            });

            var exception = Assert.Throws<TemplateEzException>(() =>
            {
                var template = new TemplateEz(new TemplateDef[]
                {
                    new() { Name = "Template1", ModelType = "IEnumerable<TemplateEzTest.TestDataTypes.Appointment>", TemplateText = templateText1.ToList() },
                    new() { Name = "Template2", ModelType = "TemplateEzTest.TestDataTypes.Person", TemplateText = templateText2.ToList() },
                });
                template.Execute(model, "Template2");
            });
            Assert.Equal($"Invalid #template line: \"#template(Template1, Model.Appointments, Model)\"", exception.Message);
        }

    }
}
