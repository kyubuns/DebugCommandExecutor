DebugCommandExecutor
===

https://github.com/kyubuns/DebugCommandExecutor/assets/961165/d8955c04-834f-49f7-8c19-d8016fe6b10e

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

# ToDo

- [ ] History
- [ ] Autocomplete

