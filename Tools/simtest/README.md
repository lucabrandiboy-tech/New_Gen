# Sim self-checks

Compiles `Assets/Scripts/Sim` without Unity and runs assertions.

From the repo root:

    dotnet run --project Tools/simtest

Exit code 0 = all checks passed. This only proves the simulation logic; the Unity layer still has to be tested in Unity.
