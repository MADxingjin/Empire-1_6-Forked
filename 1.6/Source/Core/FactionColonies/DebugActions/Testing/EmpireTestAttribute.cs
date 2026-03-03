using System;

namespace FactionColonies
{
    [AttributeUsage(AttributeTargets.Method)]
    public class EmpireTestAttribute : Attribute
    {
        public string Category { get; }
        public EmpireTestAttribute(string category = "General") => Category = category;
    }
}
