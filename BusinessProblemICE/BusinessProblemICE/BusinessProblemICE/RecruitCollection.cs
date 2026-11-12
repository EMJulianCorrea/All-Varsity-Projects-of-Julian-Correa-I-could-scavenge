using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessProblemICE
{
    public class RecruitCollection : IEnumerable<Applicants>
    {
        private List<Applicants> recruits = new List<Applicants>();

        public void Add(Applicants ap)
        {
            recruits.Add(ap);
        }

        // Indexer
        public Applicants this[int index]
        {
            get => recruits[index];
            set => recruits[index] = value;
        }

        // Operator Overloading: merge two collections
        public static RecruitCollection operator +(RecruitCollection a, RecruitCollection b)
        {
            RecruitCollection result = new RecruitCollection();
            foreach (var recruit in a) result.Add(recruit);
            foreach (var recruit in b) result.Add(recruit);
            return result;
        }

        public IEnumerator<Applicants> GetEnumerator() => recruits.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    // Extension Method
    public static class RecruitCollectionExtensions
    {
        public static double AverageAge(this RecruitCollection collection)
        {
            if (!collection.Any()) return 0;
            return collection.Average(s => s.Age);
        }
    }
}
