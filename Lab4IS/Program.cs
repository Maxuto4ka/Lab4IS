using System;
using System.Collections.Generic;
using System.Linq;

public struct ProfessorInfo
{
    public List<string> Subjects;
    public int MaxHours;
    public int CurrentHours;
}

public struct CSPVariable
{
    public string Group;
    public string Weekday;
    public string Time;
}

public class Program
{
    static readonly List<string> Subjects = new List<string> { "English", "history", "math", "science", "literature", "biology", "I.T", "physics", "chemistry", "geography" };
    static readonly List<string> Groups = new List<string> { "G1", "G2", "G3" };
    static readonly Dictionary<string, List<int>> GroupPrograms = new Dictionary<string, List<int>>
    {
        { "G1", new List<int> { 4, 2, 2, 1, 2, 1, 0, 2, 0, 1} },
        { "G2", new List<int> { 2, 1, 5, 2, 0, 1, 0, 2, 1, 1 } },
        { "G3", new List<int> { 2, 1, 1, 1, 0, 3, 3, 0, 3, 1 } }
    };
    static readonly List<string> Rooms = new List<string> { "A1", "A2", "A3" };
    static readonly List<string> Times = new List<string> { "8:40-10:15", "10:35-12:10", "12:20-13:55" };
    static readonly List<string> Weekdays = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };

    static Dictionary<CSPVariable, List<(string Professor, string Subject, string Room)>> Domain;

    static Dictionary<CSPVariable, (string Professor, string Subject, string Room)> Schedule;

    static Dictionary<string, List<int>> GroupSubjectCount = new Dictionary<string, List<int>>();

    static void InitializeGroupSubjectCount()
    {
        GroupSubjectCount = GroupPrograms.ToDictionary(
            entry => entry.Key,
            entry => new List<int>(new int[Subjects.Count])
        );
    }

    static void InitializeDomain(Dictionary<string, ProfessorInfo> professorsInfo)
    {
        Domain = new Dictionary<CSPVariable, List<(string Professor, string Subject, string Room)>>();
        Random random = new Random();

        foreach (var group in Groups)
        {
            foreach (var weekday in Weekdays)
            {
                foreach (var time in Times)
                {
                    var variable = new CSPVariable { Group = group, Weekday = weekday, Time = time };
                    var values = new List<(string Professor, string Subject, string Room)>();

                    // Всі можливі комбінації
                    var allCombinations = new List<(string Professor, string Subject, string Room)>();
                    foreach (var professor in professorsInfo.Keys)
                    {
                        var professorInfo = professorsInfo[professor];
                        {
                            foreach (var subject in professorInfo.Subjects)
                            {
                                foreach (var room in Rooms)
                                {
                                    allCombinations.Add((professor, subject, room));
                                }
                            }
                        }
                    }

                    // Перемішуємо комбінації для випадкового вибору
                    allCombinations = allCombinations.OrderBy(x => random.Next()).ToList();
                    values.AddRange(allCombinations);

                    Domain[variable] = values;
                }
            }
        }
    }


    static bool IsConsistent(CSPVariable variable, (string Professor, string Subject, string Room) value, Dictionary<CSPVariable, (string Professor, string Subject, string Room)> assignment, Dictionary<string, ProfessorInfo> professorsInfo)
    {
        foreach (var assignedVar in assignment.Keys)
        {
            var assignedValue = assignment[assignedVar];

            // Викладач не може викладати одночасно у різних аудиторіх
            if (assignedValue.Professor == value.Professor &&
                assignedVar.Weekday == variable.Weekday &&
                assignedVar.Time == variable.Time)
            {
                return false;
            }

            // Аудиторія не може використовуватись одночасно
            if (assignedValue.Room == value.Room &&
                assignedVar.Weekday == variable.Weekday &&
                assignedVar.Time == variable.Time)
            {
                return false;
            }
        }

        // Перевірка, чи не перевищується максимальна кількість годин викладача
        var professor = value.Professor;
        if (professorsInfo[professor].CurrentHours + 1 > professorsInfo[professor].MaxHours)
        {
            return false;
        }
        // Перевірка відповідності програмі групи
        int subjectIndex = Subjects.IndexOf(value.Subject);
        if (subjectIndex >= 0)
        {
            string group = variable.Group;
            if (GroupSubjectCount[group][subjectIndex] >= GroupPrograms[group][subjectIndex])
            {
                return false; // Кількість пар цього предмета перевищує програму
            }
        }

        return true;
    }

    static bool BacktrackingSearch(Dictionary<CSPVariable, (string Professor, string Subject, string Room)> assignment, Dictionary<string, ProfessorInfo> professorsInfo)
    {
        if (assignment.Count == Domain.Count)
            return true;

        var unassigned = Domain.Keys.Where(k => !assignment.ContainsKey(k)).ToList();
        var variable = unassigned.First();

        foreach (var value in Domain[variable])
        {
            if (IsConsistent(variable, value, assignment, professorsInfo))
            {
                var professorInfo = professorsInfo[value.Professor];
                professorInfo.CurrentHours++;
                professorsInfo[value.Professor] = professorInfo;

                assignment[variable] = value;
                // Оновлюємо лічильник занять для групи
                string group = variable.Group;
                int subjectIndex = Subjects.IndexOf(value.Subject);
                if (subjectIndex >= 0)
                {
                    GroupSubjectCount[group][subjectIndex]++;
                }

                if (BacktrackingSearch(assignment, professorsInfo))
                    return true;

                // Відновлюємо лічильник занять для групи
                assignment.Remove(variable);
                professorInfo.CurrentHours--;
                if (subjectIndex >= 0)
                {
                    GroupSubjectCount[group][subjectIndex]--;
                }
            }
        }

        return false;
    }
    static void PrintSchedule(Dictionary<CSPVariable, (string Professor, string Subject, string Room)> schedule)
    {
        foreach (var weekday in Weekdays)
        {
            Console.WriteLine($"\n{weekday}:");
            foreach (var time in Times)
            {
                var pairsAtTime = schedule
                    .Where(pair => pair.Key.Weekday == weekday && pair.Key.Time == time)
                    .Select(pair =>
                        $"Group: {pair.Key.Group}, Professor: {pair.Value.Professor}, Subject: {pair.Value.Subject}, Room: {pair.Value.Room}")
                    .ToList();

                if (pairsAtTime.Any())
                {
                    Console.WriteLine($"Time: {time}");
                    Console.WriteLine(string.Join("; ", pairsAtTime));
                }
            }
        }
    }


    public static void Main(string[] args)
    {
        var professorsInfo = new Dictionary<string, ProfessorInfo>
        {
            { "James", new ProfessorInfo { Subjects = new List<string> { "math", "science", "I.T", "physics" }, MaxHours = 5, CurrentHours = 0 } },
            { "Robert", new ProfessorInfo { Subjects = new List<string> { "math", "science", "physics" }, MaxHours = 3, CurrentHours = 0 } },
            { "John", new ProfessorInfo { Subjects = new List<string> { "math", "physics" }, MaxHours = 3, CurrentHours = 0 } },
            { "William", new ProfessorInfo { Subjects = new List<string> { "chemistry", "history", "biology", "literature" }, MaxHours = 5, CurrentHours = 0 } },
            { "Thomas", new ProfessorInfo { Subjects = new List<string> { "science", "chemistry", "biology" }, MaxHours = 5, CurrentHours = 0 } },
            { "Paul", new ProfessorInfo { Subjects = new List<string> { "history", "English", "geography" }, MaxHours = 3, CurrentHours = 0 } },
            { "Kevin", new ProfessorInfo { Subjects = new List<string> { "math", "I.T" }, MaxHours = 3, CurrentHours = 0 } },
            { "Anna", new ProfessorInfo { Subjects = new List<string> { "chemistry", "biology", "science" }, MaxHours = 5, CurrentHours = 0 } },
            { "Sophia", new ProfessorInfo { Subjects = new List<string> { "geography", "English"}, MaxHours = 5, CurrentHours = 0 } },
            { "Olivia", new ProfessorInfo { Subjects = new List<string> { "English", "literature", "history" }, MaxHours = 3, CurrentHours = 0 } },
            { "Emma", new ProfessorInfo { Subjects = new List<string> { "math", "I.T" }, MaxHours = 3, CurrentHours = 0 } },
            { "Charlotte", new ProfessorInfo { Subjects = new List<string> { "history", "geography" }, MaxHours = 3, CurrentHours = 0 } },
            { "Lily", new ProfessorInfo { Subjects = new List<string> { "literature", "English" }, MaxHours = 3, CurrentHours = 0 } }
        };
        InitializeGroupSubjectCount();
        InitializeDomain(professorsInfo);

        Schedule = new Dictionary<CSPVariable, (string Professor, string Subject, string Room)>();

        if (BacktrackingSearch(Schedule, professorsInfo))
        {
            PrintSchedule(Schedule);
        }
        else
        {
            Console.WriteLine("No solution found!");
        }
    }
}
