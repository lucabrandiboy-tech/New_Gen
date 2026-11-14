# Offline compile check for the Unity-facing code

Claude has no Unity, so this compiles `Assets/Scripts/Sim`, `Assets/Scripts/Art` and
`Assets/Scripts/Game` against **hand-written stand-ins** for the Unity APIs we use
(`UnityStubs.cs`, `InputStubs.cs`).

From the repo root:

    bash Tools/check.sh            # everything: sim tests + both compile checks

or individually:

    dotnet build Tools/unitycheck/unitycheck.csproj          # runtime code, UNITY_EDITOR off
    dotnet build Tools/unitycheck/unitycheck-editor.csproj   # runtime + editor code, UNITY_EDITOR on

**It catches:** typos, wrong member names on our own classes, ambiguous types, missing
`using`s, wrong argument counts, bad overrides — most of our own mistakes.

**It does NOT catch:** using a real Unity API incorrectly, because a stub's signature can
differ from Unity's. Only Unity itself proves the project compiles. Assembly-definition
(.asmdef) reference problems are also invisible here.

If Unity reports an error this harness missed, fix the stub as well so it is caught next time.

Both the player path (`UNITY_EDITOR` off) and the editor path (on) are compiled, so code inside
`#if UNITY_EDITOR` is checked too.

**Biggest blind spot: assembly definitions.** The `.asmdef` files decide which Unity packages each
of our assemblies may use. A missing reference there compiles fine here and fails in Unity.
