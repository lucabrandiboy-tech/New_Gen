# New Generation

2D factory-automation + RTS conquest game. Unity 6 LTS, URP 2D.
Docs: [`CLAUDE.md`](CLAUDE.md) (spec + decisions) · [`Docs/GDD.md`](Docs/GDD.md) · [`Docs/ART_GUIDE.md`](Docs/ART_GUIDE.md) · [`Docs/KNOWN_BUGS.md`](Docs/KNOWN_BUGS.md)

## Running the game (owner)
1. In Unity: **New Generation → Art → Generate Placeholders** (once, and again whenever the sprite list changes).
2. **New Generation → Create Game Scene** (once). It makes `Assets/Scenes/Game.unity` with one `Game` object.
3. Press **Play** in that scene. Everything else (camera, UI, terrain, input) is created by `GameRoot` at runtime.
4. Sim self-checks (no Unity needed): `dotnet run --project Tools/simtest` from the repo root.

## First-time setup (owner)
1. Install **Unity Hub**, then **Unity 6 LTS** (6000.x) with "Windows Build Support".
2. Install **GitHub Desktop** + **Git LFS**, clone this repo.
3. In Unity Hub: **New project → "Universal 2D"** template. Name it `NewGenTemp`, save it **anywhere except** the repo folder. Wait for it to open, then close Unity.
4. Copy the folders `Assets`, `Packages`, `ProjectSettings` from `NewGenTemp` into your `New_Gen` folder. Say **yes / merge** if asked about `Assets`.
5. Unity Hub → **Add → Add project from disk** → pick the `New_Gen` folder. Open it.
6. Check the Console (Window → General → Console) for red errors.
7. GitHub Desktop → commit "Unity project created" → push. Delete `NewGenTemp`.
