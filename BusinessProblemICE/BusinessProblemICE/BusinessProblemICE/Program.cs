namespace BusinessProblemICE
{
    internal class Program
    {
        static void Main(string[] args)
        {
            /* Human Resource Management: Organise all recruit within the business */
            var groupA = new RecruitCollection();
            var groupB = new RecruitCollection();
            Console.WriteLine("Enter name of new recruit:");
            string name = Console.ReadLine();
            Console.WriteLine($"What section is {name} assigned to? 0: Development 1: Management");
            bool group;
            if ((int.Parse(Console.ReadLine())) % 2 != 0)
            {
                group = true;
            }
            else
            {
                group = false;
            }
            Console.WriteLine("Enter their role/task for the company:");
            string task = Console.ReadLine();
            Console.WriteLine("Enter their age:");
            int age = int.Parse(Console.ReadLine());
            if (group == true)
            {
                groupB.Add(new Applicants(name, task, age));
            }
            else
            {
                groupA.Add(new Applicants(name, task, age));
            }

            groupA.Add(new Applicants("Stan", "Designer", 24));
            groupA.Add(new Applicants("John", "Coder", 31));
            groupA.Add(new Applicants("Melany", "Tester", 22));

            groupB.Add(new Applicants("Jen", "Manager", 44));
            groupB.Add(new Applicants("Pat", "Accountant", 52));
            groupB.Add(new Applicants("Cam", "Promoter", 27));

            // Use indexer
            Console.WriteLine($"\nFirst recruit in groupB: {groupB[0].Name} the {groupB[0].Task} - aged {groupB[0].Age}");
            Console.WriteLine($"First recruit in groupA: {groupA[0].Name} the {groupA[0].Task} - aged {groupA[0].Age}");

            // Combine two collections using overloaded +
            var allRecruits = groupA + groupB;

            // Use extension method
            Console.WriteLine($"\nAverage Age: {allRecruits.AverageAge():F2}");

            // Use anonymous types to generate pass/fail report
            var report = allRecruits.Select(s => new
            {
                s.Name,
                Status = s.Age >= 40 ? "Old" : "Young"
            });

            Console.WriteLine("\nAge report:");
            foreach (var item in report)
            {
                Console.WriteLine($"Name: {item.Name}, Status: {item.Status}");
            }
        }
    }
}
