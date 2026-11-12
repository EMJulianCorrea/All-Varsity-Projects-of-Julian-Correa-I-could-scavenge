namespace ICETask3StudentSpecialties
{
    internal class Program
    {
        static void Main(string[] args)
        {
            List<StudentSpecialty> special = new List<StudentSpecialty>();
            List<Student> student = new List<Student>();
            string input = "";
            string inputted = "";
            int specialtyLength = 0;
            int fnChoosen = 0;
            string[] words = inputted.Split(' ');
            input = "Specialties:\n";
            while ( !inputted.Contains("Done"))
                {
                specialtyLength = specialtyLength + 1;
                    Console.WriteLine("Please add a specialty as 2 words with the faculty number, all seperated with spaces. Please enter 'Students:' to continue");
                inputted = Console.ReadLine();
                if (inputted != "Done")
                {
                    input = input + inputted + "\n";
                    words = inputted.Split(' ');
                    fnChoosen = int.Parse(words[2]);
                    special.Add(new StudentSpecialty { SpecialtyName = words[0] + " " + words[1], FacultyNumber = fnChoosen });
                }
                }
            input = input + "\nStudents:\n";
            while ( !inputted.Contains("Done"))
            {
                Console.WriteLine("Please add the name, surname and faculty number of a student with each seperated by spaces. Enter 'Done' to continue");
                inputted = Console.ReadLine();
                if (inputted.Contains("Done"))
                input = input + inputted + "\n";
                fnChoosen = int.Parse(words[2]);
                student.Add(new Student { StudentName = words[0] + " " + words[1], FacultyNumber = fnChoosen });
            }
            Console.WriteLine("\nInputted Data:\n\n{0}", input);
            var studentSpecialty = from students in student
                                join specials in special
                                on students.FacultyNumber equals specials.FacultyNumber
                                select new { students.StudentName, students.FacultyNumber, specials.SpecialtyName };
            Console.WriteLine("Output: \n");
            foreach (var ss in studentSpecialty)
                {
                    Console.WriteLine($"{ss.StudentName} {ss.FacultyNumber} {ss.SpecialtyName}");
                }
        }
    }
}
