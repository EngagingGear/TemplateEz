using System;
using System.Collections.Generic;

namespace TemplateEzTest.TestDataTypes
{
    public class Person
    {
        public Person(string firstName, string lastName, DateTime? dob, List<Appointment> appointments = null)
        {
            FirstName = firstName;
            LastName = lastName;
            DateOfBirth = dob;
            Appointments = appointments ?? new List<Appointment>();
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public List<Appointment> Appointments { get; }

        public override string ToString()
        {
            return $"{FirstName} {LastName}";
        }
    }

}
