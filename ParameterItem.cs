using System;

namespace PowerSimBuilder
{
    public class ParameterItem
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
        
        public override string ToString()
        {
            return $"{Name}: {Value} {Unit}";
        }
    }
}