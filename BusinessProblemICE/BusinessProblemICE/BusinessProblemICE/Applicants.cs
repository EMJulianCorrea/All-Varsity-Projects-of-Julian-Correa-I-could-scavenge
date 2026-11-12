using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessProblemICE
{
     public class Applicants
     {
        public string Name { get; set; }
        public string Task { get; set; }
        public int Age { get; set; }

        public Applicants(string name, string task, int age)
        {
            Name = name;
            Task = task;
            Age = age;
        }
    }
}
