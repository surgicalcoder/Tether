using System;

namespace Tether.Plugins
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class PerformanceCounterValueAttribute : Attribute
    {
        public PerformanceCounterValueAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }

        public PerformanceCounterValueAttribute()
        {
        }

        public PerformanceCounterValueAttribute(string propertyName, int divisor)
        {
            Divisor = divisor;
            PropertyName = propertyName;
        }

        public int Divisor { get; set; }
        public string PropertyName { get; set; }
    }
}