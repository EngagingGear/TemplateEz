using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using TemplateEzNS;
using TemplateEzTest.TestDataTypes;
using Xunit;

namespace TemplateEzTest
{
    public class MultiTemplateTests: BaseTest
    {
        [Fact]
        public void MultipleTemplates()
        {
            var libraryPath = LibraryPath();
            var templateText1 = SubSpecialChars("@Model.FirstName @Model.LastName");
            var templateText2 = SubSpecialChars(
                "Dear",
                "#template(Template1, @Model)",
                "Your date of birth is @(Model.DateOfBirth.Value.ToString(|MM/dd/yyyy|))");

            var obj = new Person("Mary", "Jones", new DateTime(2002, 2, 22));
            var tmpl = new TemplateEz(new TemplateDef[]
            {
                new TemplateDef { Name = "Template1", ModelType = "TemplateEzTest.TestDataTypes.Person", TemplateText = templateText1.ToList() },
                new TemplateDef { Name = "Template2", ModelType = "TemplateEzTest.TestDataTypes.Person", TemplateText = templateText2.ToList() },
            });
            var result = tmpl.Execute(obj, "Template1");
            Assert.True(CompareStrList(result, "Mary Jones"));
        }

        [Fact]
        public void MultipleTemplatesWithTemplateCall()
        {
            var libraryPath = LibraryPath();
            var templateText1 = SubSpecialChars("@Model.FirstName @Model.LastName");
            var templateText2 = SubSpecialChars(
                "Dear",
                "#template(Template1, @Model)",
                "Your date of birth is @(Model.DateOfBirth.Value.ToString(|MM/dd/yyyy|))");

            var obj = new Person("Mary", "Jones", new DateTime(2002, 2, 22));
            var tmpl = new TemplateEz(new TemplateDef[]
            {
                new TemplateDef { Name = "Template1", ModelType = "TemplateEzTest.TestDataTypes.Person", TemplateText = templateText1.ToList() },
                new TemplateDef { Name = "Template2", ModelType = "TemplateEzTest.TestDataTypes.Person", TemplateText = templateText2.ToList() },
            });
            var result = tmpl.Execute(obj, "Template2").ToList();

            Assert.True(CompareStrList(result, 
                "Dear",
                "Mary Jones",
                "Your date of birth is 02/22/2002"));
        }

        [Fact]
        public void OneTemplateCanCallOtherAndPassPropertyOfModelAsModelForNestedTemplate()
        {
            var templateText1 = new [] {
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

            var template = new TemplateEz(new TemplateDef[]
            {
                new() { Name = "Template1", ModelType = "IEnumerable<TemplateEzTest.TestDataTypes.Appointment>", TemplateText = templateText1.ToList() },
                new() { Name = "Template2", ModelType = "TemplateEzTest.TestDataTypes.Person", TemplateText = templateText2.ToList() },
            });

            var result = template.Execute(model, "Template2").ToList();

            Assert.True(CompareStrList(result, 
                "Dear Mary Jones",
                "Your date of birth is 02/22/2002",
                "<p>You have a few up and comping appointments</p>",
                "<ul>",
                "<li>01/01/2020: Appointment 1</li>",
                "<li>01/02/2020: Appointment 2</li>",
                "</ul>"
                ));
        }

        [Fact]
        public void DuplicateLibraryDeclarationsEliminatedForMultipleTemplates()
        {
            var libraryPath = LibraryPath();
            var templateText1 = new [] {
                $"#library {libraryPath}",
                "#using LibraryForTest;",
                "#code{",
                "\tprivate static string MakeAppointmentDescription(string description, DateTime date)",
                "\t{",
                "\t\tvar s = new SampleClass();",
                "\t\treturn s.MakeAppointmentDescription(description,date);",
                "\t}",
                "#}",
                "#using System.Linq",
                "#if(Model == null || !Model.Any())",
                "",
                "#else",
                "#{",
                "<p>You have a few up and comping appointments</p>",
                "<ul>",
                "#foreach(var appt in Model)",
                "<li>@(MakeAppointmentDescription(appt.Description, appt.Date))</li>",
                "</ul>",
                "#}"
                };

            var templateText2 = SubSpecialChars(
                $"#library {libraryPath}",
                "#using LibraryForTest;",
                "#code{",
                "\tprivate static string MakeName(string f, string l)",
                "\t{",
                "\t\tvar s = new SampleClass();",
                "\t\treturn s.MakeName(f,l);",
                "\t}",
                "#}",
                "#using System.Linq",
                "Dear @(MakeName(Model.FirstName, Model.LastName))",
                "Your date of birth is @(Model.DateOfBirth.Value.ToString(|MM/dd/yyyy|))",
                "#template(Template1, Model.Appointments)"
                );

            var model = new Person("Mary", "Jones", new DateTime(2002, 2, 22), new List<Appointment>()
            {
                new() {Date = new DateTime(2020, 1, 1), Description = "Appointment 1"},
                new() {Date = new DateTime(2020, 1, 2), Description = "Appointment 2"},
            });

            var template = new TemplateEz(new TemplateDef[]
            {
                new() { Name = "Template1", ModelType = "IEnumerable<TemplateEzTest.TestDataTypes.Appointment>", TemplateText = templateText1.ToList() },
                new() { Name = "Template2", ModelType = "TemplateEzTest.TestDataTypes.Person", TemplateText = templateText2.ToList() },
            });

            var result = template.Execute(model, "Template2").ToList();

            Assert.True(CompareStrList(result, 
                "Dear Mary Jones",
                "Your date of birth is 02/22/2002",
                "<p>You have a few up and comping appointments</p>",
                "<ul>",
                "<li>01/01/2020: Appointment 1</li>",
                "<li>01/02/2020: Appointment 2</li>",
                "</ul>"
                ));
        }


        [Fact]
        public void DeeplyNestedTemplateCall()
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
                "#template(Template2, empl.Appointments)",
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

            var template = new TemplateEz(new TemplateDef[]
            {
                new() { Name = "EntryTemplate", ModelType = "TemplateEzTest.TestDataTypes.Department", TemplateText = entryTemplateText.ToList() },
                new() { Name = "Template1", ModelType = "IEnumerable<TemplateEzTest.TestDataTypes.Person>", TemplateText = templateText1.ToList() },
                new() { Name = "Template2", ModelType = "IEnumerable<TemplateEzTest.TestDataTypes.Appointment>", TemplateText = templateText2.ToList() },
            });

            var result = template.Execute(department, "EntryTemplate").ToList();

            Assert.True(CompareStrList(result,
                "Department Sales",
                "<p>Employees</p>",
                "<ul>",
                "<li>",
                "<div>Mary Jones</div>",
                "<p>You have a few up and comping appointments</p>",
                "<ul>",
                "<li>01/01/2020: Appointment 1</li>",
                "<li>01/02/2020: Appointment 2</li>",
                "</ul>",
                "</li>",
                "<li>",
                "<div>John Smith</div>",
                "<p>You have a few up and comping appointments</p>",
                "<ul>",
                "<li>01/01/2020: Appointment 1</li>",
                "<li>01/02/2020: Appointment 2</li>",
                "</ul>",
                "</li>",
                "</ul>"
                ));
        }


        [Fact]
        public void DynamicObjectAsModel()
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
            model.Person = person;

            var template = new TemplateEz(new TemplateDef[]
            {
                new() { Name = "Template1", ModelType = "IEnumerable<TemplateEzTest.TestDataTypes.Appointment>", TemplateText = templateText1.ToList() },
                new() { Name = "EntryTemplate", ModelType = "dynamic", TemplateText = entryTemplateText.ToList() },
            });

            var result = template.Execute(model, "EntryTemplate");

            Assert.True(CompareStrList(result,
                "Dear Mary Jones",
                "Your date of birth is 02/22/2002",
                "<p>You have a few up and comping appointments</p>",
                "<ul>",
                "<li>01/01/2020: Appointment 1</li>",
                "<li>01/02/2020: Appointment 2</li>",
                "</ul>"
                ));
        }

    }
}
