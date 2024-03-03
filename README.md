DebugCommandExecutor
===

WIP

```csharp
[DebugCommand("Echo text")]
public static void Echo(string text)
{
    Debug.Log(text);
}

[DebugCommand]
public static void Add(int a, int b)
{
    Debug.Log($"{a} + {b} = {a + b}");
}
```

`https://github.com/kyubuns/DebugCommandExecutor.git?path=Assets/DebugCommandExecutor`
