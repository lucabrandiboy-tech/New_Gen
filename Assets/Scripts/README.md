# Code layout

| Folder | What it is | Unity assembly |
|---|---|---|
| `Sim/` | The game rules. **No Unity code at all** — plain C#. | Assembly-CSharp |
| `Art/` | Sprite registry used at runtime. | Assembly-CSharp |
| `Game/` | Everything Unity: drawing, input, camera, UI. | Assembly-CSharp |
| `Editor/` | Editor-only tools (art pipeline, scene creator). | Assembly-CSharp-Editor |

There are deliberately **no .asmdef files**. Unity then compiles everything into its two default
assemblies, which automatically reference every installed package — one less thing to break.
Compile speed is irrelevant at this project size.

The "no Unity in `Sim/`" rule is enforced instead by `Tools/simtest`, which compiles `Sim/` on its
own with no Unity available. If anyone adds `using UnityEngine;` in `Sim/`, that build fails.
