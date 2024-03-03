using System;
using System.Diagnostics;
using UnityEngine.Scripting;

namespace DebugCommandExecutor
{
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
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
