DebugCommandExecutor
===

`https://github.com/kyubuns/DebugCommandExecutor.git?path=Assets/DebugCommandExecutor`

https://github.com/kyubuns/DebugCommandExecutor/assets/961165/3ecae0d5-6cc6-4bfc-b78a-2ac99280ff80

---

## Features

- Just define the method with DebugCommandAttributes.
- Debug commands can be sent to players from Editor on different devices on the same network.
  - [Attach in the Console Window is the trigger.](https://docs.unity3d.com/ja/2022.3/Manual/Console.html)
- Autocomplete (Tab, Shift-Tab)
- History (UpArrow, DownArrow)

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

- `UNITY_EDITOR` or `DEVELOPMENT_BUILD` only.

## Target Environment

- Unity2022.3 or later

## License

MIT License (see [LICENSE](LICENSE))
