using System;
using UnityEngine.Scripting;

namespace DebugCommandExecutor
{
    [AttributeUsage(AttributeTargets.Method)]
    public class DebugCommandAttribute : PreserveAttribute
    {
        public string Summary { get; }

        public DebugCommandAttribute()
        {
            Summary = string.Empty;
        }

        public DebugCommandAttribute(string summary)
        {
            Summary = summary;
        }
    }
}
