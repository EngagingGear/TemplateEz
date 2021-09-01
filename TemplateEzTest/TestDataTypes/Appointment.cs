using System;

namespace TemplateEzTest.TestDataTypes
{
    public class Appointment
    {
        public string Description { get; set; }
        public DateTime Date { get; set; }

        public override string ToString()
        {
            return $@"{Date:MM/dd/yyyy}: {Description}";
        }
    }
}