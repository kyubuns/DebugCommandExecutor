# DebugCommandExecutor

## 挙動互換性

- コマンド名は大文字・小文字を区別しない。
- オーバーロード解決では引数の個数が完全一致する候補を先に試し、該当候補がない場合だけ末尾の既定値を補える候補を試す。同じ優先度では検出順を維持し、変換に失敗した候補の次を試す。
- コマンド文字列はダブルクォート外の `char.IsWhiteSpace` で分割し、ダブルクォート自体は除去する。空の引用値は引数にせず、閉じていない引用符から末尾までは同じ引数として扱う。エスケープ構文はない。
- コマンド探索の対象は、Runtime assembly を参照する assembly 内の `[DebugCommand]` が付いた public static method だけである。

## UI

- Runtime package は uGUI と TextMeshPro に依存しない状態を保つ。`DebugCommandWindow` は Editor IMGUI であり、SampleScene に Canvas、uGUI Graphic、TMP component はない。
