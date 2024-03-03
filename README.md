DebugCommandExecutor
===

`https://github.com/kyubuns/DebugCommandExecutor.git?path=Assets/DebugCommandExecutor`

https://github.com/kyubuns/DebugCommandExecutor/assets/961165/d8955c04-834f-49f7-8c19-d8016fe6b10e

## Define Debug Commands

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

## Execute

Tools > Debug Command Executor > Window

## Note

- UNITY_EDITOR or DEVELOPMENT_BUILD only.

## ToDo

- [ ] Autocomplete

## Target Environment

- Unity2022.3 or later

## License

MIT License (see [LICENSE](LICENSE))
