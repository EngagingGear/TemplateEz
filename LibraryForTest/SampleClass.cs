using System;

/// <summary>
///  This library exists only as something for the unit tests to load.
/// </summary>
namespace LibraryForTest
{
    public class SampleClass
    {
        public string MakeName(string first, string last)
        {
            return first + " " + last;
        }

        public string MakeAppointmentDescription(string description, DateTime date)
        {
            return $@"{date:MM/dd/yyyy}: {description}";
        }
    }
}
