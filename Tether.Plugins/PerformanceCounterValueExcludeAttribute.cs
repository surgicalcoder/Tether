using System;

namespace Tether.Plugins
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class PerformanceCounterValueExcludeAttribute : Attribute
    {
    }
}