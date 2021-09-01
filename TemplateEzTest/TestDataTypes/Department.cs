using System.Collections.Generic;

namespace TemplateEzTest.TestDataTypes
{
    public class Department
    {
        public Department(string name, List<Person> employees = null)
        {
            Name = name;
            Employees= employees ?? new List<Person>();
        }

        public string Name { get; set; }
        public List<Person> Employees { get; }
    }
}