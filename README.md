DebugCommandExecutor
===

`https://github.com/kyubuns/DebugCommandExecutor.git?path=Assets/DebugCommandExecutor`

https://github.com/kyubuns/DebugCommandExecutor/assets/961165/e88c7f76-1262-43e1-b89a-e96e87b15cd9

---

## Features

- Just define the method with DebugCommandAttributes.
- Debug commands can be sent to players from Editor on different devices on the same network.
  - [Attach in the Console Window is the trigger.](https://docs.unity3d.com/2022.3/Documentation/Manual/Console.html)
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

## Recommend

- By registering shortcuts, you can quickly input debugging commands at any time!

![CleanShot_20240304-134813@2x](https://github.com/kyubuns/DebugCommandExecutor/assets/961165/dd338efd-f618-431d-b8f7-9eaad7b86516)

## Target Environment

- Unity2022.3 or later

## License

MIT License (see [LICENSE](LICENSE))
