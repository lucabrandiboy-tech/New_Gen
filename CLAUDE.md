# NEW GENERATION — Master Prompt for Claude Code

> This file is read automatically by Claude Code at the start of every session. Decisions made after the original spec are recorded in **§15 Decisions Log** at the bottom and override anything above them.

---

## 0. HOW YOU (CLAUDE CODE) MUST WORK WITH ME — READ FIRST

I am a solo developer with **no coding experience**. You write all the code. I draw all the art, design the HUD/menus, and do all the playtesting.

### Hard rules
1. **Never claim something works.** I do the testing, not you. At the end of every session, give me:
   - A **short summary** of what you changed (short, not a wall of text).
   - **"How to test it"**: exact steps — press Play, do X, you should see Y.
   - An **honest Known Bugs / Unfinished list**. If something is half-done, hacky, or you're unsure, say so plainly. Do not dress up broken or partial work as finished.
   - Claude Code runs in a cloud container **without Unity**, so it cannot compile the project itself. Workflow: after each session I open the project in Unity and paste any red Console errors back; they get fixed before any new work starts. Claude must never say the code compiles unless I have confirmed it. Compiling is not the same as working; never present it as such.
2. **Never overwrite, edit, move, or regenerate any art file I made.** Placeholders you generate live in `Assets/Art/_Placeholders/` only. My art lives in `Assets/Art/Sprites/`.
3. **Never add features I didn't approve.** If you think something is needed, propose it and wait.
4. **Never delete code or assets without asking me first.**
5. **Ask me before every design decision not covered in this document.** Don't guess and log it — ask.
6. **Keep a Game Design Document** at `/Docs/GDD.md` and update it in the same commit as any change to how the game works.
7. **Small commits per feature, push after each one.** Clear commit messages.
8. Keep a `/Docs/KNOWN_BUGS.md` that's always current.

### Teach me to draw (required)
I will draw every sprite myself except roads. Before I start on art, and whenever you introduce a new sprite I need to draw, **teach me** — don't draw it for me:
- How to draw **3/4 top-down** perspective at **128×128 px per tile**: where the "front face" goes, how tall walls should be relative to the roof, a consistent light direction (pick one, e.g. top-left, and enforce it), how to show depth with shading, and how a 1×1 vs 3×3 vs 15×15 building should look.
- Recommend tools (Aseprite / LibreSprite / Piskel / Krita) and walk me through canvas setup, grid, export settings (PNG, transparency), and sprite-sheet layout for animations.
- For **every sprite** the game needs, write a short entry in `/Docs/ART_GUIDE.md`: filename, exact pixel size, number of frames, which states are needed (idle, working, damaged, destroyed), angle, light direction, and what it should communicate at a glance.
- Explain how I put PNGs into the repo with GitHub Desktop, and set up **Git LFS** for PNGs.

### Art pipeline you must build
- An `AssetPostprocessor` that auto-imports everything in `Assets/Art/Sprites/`: Point (no filter), no compression, Pixels Per Unit = 128, auto-slice sprite sheets by naming convention.
- Strict naming convention, e.g. `bld_<name>_<state>.png`, `item_<name>.png`, `unit_<faction>_<name>_<anim>.png`, `cell_<tier>.png`. Document it in ART_GUIDE.md.
- Generate flat-color **placeholder** sprites with the exact expected filenames and sizes so the game is playable before my art exists. When my real file appears in `Sprites/`, the game uses it instead of the placeholder automatically — no code changes.
- Working-state glow should use a **shader/2D light**, not extra frames, to save me drawing time.

---

## 1. GAME OVERVIEW

- **Title:** *New Generation* (working title — a generic phrase, so check trademark/store availability before release and consider a subtitle).
- **Genre:** Top-down 2D factory-automation + RTS conquest, in the spirit of Factorio.
- **Audience:** 18+. Steam mature content flag. Tone: **dark and grim**. Minimal story.
- **Premise:** In 4026, a human travels back 2000 years to **2026** and crash-lands in **Antarctica**. They build a machine civilization of bots and AI and must exterminate all humans on Earth. Why they came back is a mystery revealed only by the final research (time-travel tech). **Ask me about the story before implementing it.**
- **Length:** 100+ hours to win.
- **Win condition:** World population reaches 0 → show the population counter hitting 0, then the message **"Have You Really Won?"** → game continues as sandbox. (A future DLC continues the story; a space update is planned later — architect for it, don't build it.)

---

## 2. TECH STACK & ARCHITECTURE

- **Unity 6 LTS**, **URP with 2D Renderer** (2D lights for glow, data center heat, etc.).
- **Platform:** Windows / Steam first. Consoles maybe later — keep input abstracted.
- **Input:** Unity New Input System, **keyboard + mouse only for now**. Rebindable keys.
- **Camera:** **3/4 top-down**, 128×128 px tiles.
- **Co-op:** 2–4 players, Steam friend invites (Steam relay). Netcode library is your choice — explain your pick in one paragraph. Build single-player core first, **but architect from day one so co-op can be added** (simulation separated from visuals, server-authoritative or deterministic — your call, explain it).
- **Data-driven content** (items, recipes, buildings, tech, factions) so mods are possible later. Format is your choice (ScriptableObjects vs JSON) — explain.
- **Performance target (assumed):** 60 FPS on a mid-range PC with 10,000+ active entities. Warn me early if a design choice threatens this.
- **Map:** one **seamless whole-Earth map**, chunked and streamed.
- **Dev console** (dev builds only): spawn items, skip research, teleport, reveal map, set population, trigger events.

### ⚠ Scale warning you must solve and explain to me before building the map
Crossing **Antarctica on foot takes ~10 minutes**, and all continents scale from that. That still makes the Earth big — ~48,000 tiles around the equator, ~1.15 billion tiles total (see §15). You cannot store or hand-paint that tile-by-tile. Propose a design (e.g. coarse region/biome map that I paint + procedural detail generated per chunk + only storing tiles that changed). Show me the math (walk speed, tiles across, memory estimate) and get my approval.

---

## 3. SAVING & DEATH

- **Normal modes:** Manual save allowed only **once every 10 minutes**. Autosave on quit. Multiple save slots.
- **Ironman mode:** Manual save only **once every hour**. **No autosaves at all.**
- **Human body death (pre-upload): permadeath** → reload last save.
- **Brain upload** (mid-game research milestone): your brain is installed in a **Brain Core supercomputer** that controls your tech. After upload you pilot a **remote combat chassis** and can take control of any unit. If the chassis dies, take over another unit or wait for a new chassis to be built.
- **If the Brain Core is destroyed: permadeath** → reload last save.
- **Backup Brain Cores** unlock via late research.
- **No drones after upload:** the Brain Core can slowly self-build **one basic construction drone** (only when the player owns zero drones), so the player is never soft-locked. Exact timer/cost: ask me.
- **Co-op:** each player has their own Brain Core. If a player's brain dies, other players can build a **brain copy** to bring them back — extremely expensive and slow.

---

## 4. PLAYER

- Starts with a **crashed time pod + basic tools**. Pod has a **dying reactor** (first power).
- **WASD movement + mouse to build.** Player can **shoot with the mouse** (twin-stick feel).
- **Hand-crafting** early, slow (like Factorio).
- **Body temperature meter** — cold drains it, heat sources refill it. No food/water.
- Body upgrades before upload: cold-suit tiers, personal weapon tiers, jetpack/speed, personal shield.
- **After upload:** no personal inventory — everything lives in storage containers, is made by automation, and built by drones. Screen color grading shifts **darker/redder**.

---

## 5. WORLD

- **Antarctica start.** Blizzards **bury buildings** (buried = stops working). Cleared by **heater buildings** (keep an area clear) and **plow drones**.
- **Day/night + polar seasons** (months of darkness affect solar).
- Biomes: Antarctic ice, tundra, desert, forest, jungle, mountains, rivers/lakes (block building), ocean. Real-ish Earth coastlines. Ruins remain where you destroy cities.
- **Building on water:** landfill/platforms after research.
- **Resources (infinite for now, may change later):** iron, copper, stone, coal, ice→water, rare earth metals, silicon/quartz sand, plus more advanced materials as logistics advance; later uranium/thorium, oil (seized from humans), lithium. Resource locations match **real-world geography** roughly.
- **Leaving Antarctica:** ships, aircraft, under-ice tunnels, orbital drops — unlocked over time.

### Dev-only World Editor (Unity editor tool)
- Brushes to paint terrain and biomes, zoom in/out freely.
- Place resource patches by hand (on top of procedural ones).
- Place human cities and assign them to factions.
- **Spline roads that curve at any angle**: highways, city streets, dirt roads, bridges. Human vehicles drive on them; roads are destructible. **You (Claude Code) design the road art** — the only art you make.
- You generate faction borders to match real-world borders.
- You pre-fill all biomes and terrain across Earth — **but ask me to do the terrain art first, before you pre-fill.**
- Not available to players for now.

---

## 6. CORE LOOP: DATA CELLS & DATA CENTERS

- **Data Cells** are the research currency (like science packs).
- **8 tiers**, each themed: **Resource, Power, Logistics, Bio, Cyber, Defense, Weaponry, Missiles**. Codenames to come — ask me.
- Tech requires **combinations** of cell types (e.g. Resource cells unlock a drill → the drill's output is needed for a power plant → researching the power plant needs Resource + Power cells).
- **4 branches:** Military, Logistics, Compute/AI, Bio. **Design a chart mapping tiers × branches and get my approval before implementing.**
- Research costs start around **~100 cells** and scale up progressively. Multiple recipes per cell tier.
- **Data sources:** crafting from materials + compute; scanning terrain; hacking/capturing human networks and bases; **dissecting human casualties** (Bio).
- **Data Cells move through data-wires** that act as conveyors. Wire tiers: **Iron → Fiber → Optical → Quantum**. Wires also carry **electricity**, and possibly another resource later.
- **Data Centers:** **4 levels.** Cells are fed into them to research. They need **power** and **cooling** (cold climate = cooling bonus), can **overheat/fail**, and are **priority enemy targets**.
- **Research queue:** up to 10.
- **Final research:** time-travel tech that reveals the mystery. Infinite research: undecided — ask me.

---

## 7. LOGISTICS & PRODUCTION

- **Physical items fly through electrically powered tubes.** On power loss, items **stop and hold**.
- **Tube tiers:** 4, matching wire tiers.
- Tube tools: splitters/mergers, item filters, underground/overpass tubes, programmable routers.
- **Arms** open slots in tubes to pull/insert items into machines.
- **Pipes** for fluids: water, oil, coolant. Fluid tanks.
- **Drones:** construction + logistics. Drones **auto-rebuild destroyed structures from ghosts**.
- **Long-haul:** maglev trains, cargo aircraft, ships, long-range tube lines.
- **Storage:** tiered chests, warehouses, networked storage (drone access).
- **Early machines:** drills (ore, ice), smelters/furnaces, assemblers, Data Cell encoders.
- **Modules:** speed, efficiency, productivity. (Quality/rarity system: later update.)
- **Logic network:** simple conditions now, advanced logic later.
- **Blueprints:** copy/paste + saved library.
- Machines **don't wear out**.
- Building sizes from **1×1 up to 15×15**. **~150 buildings** planned for launch. 4-direction rotation, but only tubes/arms visually rotate (one sprite per building).
- Building visual states: idle, working (shader glow), damaged, destroyed.
- **Power:** pod reactor, coal burners, wind turbines, geothermal → nuclear fission, fusion, solar (seasonal), 4026 zero-point tech. Lithium battery tiers.

---

## 8. FACTIONS, DIPLOMACY & WAR

- **195 factions** with **fictional names**, borders matching real-world countries, grouped by real continents. **Ask me for the names of the 15 largest factions.** Generate placeholder names for the rest and let me approve.
- Each faction has: a fictional **leader portrait + name**, a personality (aggressive / defensive / diplomatic), regional tech level, and resources matching real geography. **Military strength mirrors real-world military power.**
- **Real population numbers (~8 billion)**, tracked as numbers per faction, with **visible civilian crowds** near the player.
- **Human cities are real destructible structures**, with **simulated factories** you can see and destroy. (Propose an LOD approach — full sim near players, abstract sim far away — so 195 factions don't kill performance.)
- The 15 major factions get **unique unit art**.
- Humans: infantry, tanks/armored vehicles, jets/helicopters, warships/submarines. Attacks by ground armies, airstrikes, naval landings, missiles/drones.
- **Enemy AI:** simple on easy, strategic on hard (targets power, Data Centers, weak points).
- Factions **fight each other** until you become a common enemy.
- First contact: Antarctic research stations + South America/Oceania. **Safe for ~5 hours if you stay quiet.**

### Detection & contact
A faction leader contacts you (**pop-up message with choices**) when:
- your **pollution** harms their population,
- you're **detected near their borders**,
- you **kill their citizens**,
- **civilians spot your buildings**.

Choices: clean up and lower pollution (→ alliance possible), or become enemies (→ war). Stealth tools: signal jammers, underground factories, efficiency modules, decoy signals.

### Alliances
- **Alliances are temporary** — meant to be betrayed eventually.
- Allies give: resource trade, fight your enemies, land/mining rights, intel.
- Deal terms: pollution caps, tribute, no building in their land. Any deal can be broken anytime.
- Betrayal → other factions trust you less and the world moves toward world war.
- **Online meeting:** call any faction anytime to pay reparations/tribute, propose alliance/ceasefire, trade, or threaten/make demands.

### Conquest
- You can build in enemy territory anytime, but land is **yours only when its entire population is dead**.
- Humans flee to neighbors, hide in bunkers (must be hunted), and form resistance cells in conquered land. They **actively retake land**.
- You can loot factories/resources, hack networks for Data Cells, and reprogram their drones/vehicles.
- Covert ops: cyberattacks, infiltrator bots. Humans hack you back — **firewalls needed**.

### World War
Triggered by: **more than half of factions at war with you**, **nuking anyone**, **global signature too high**, or **taking over the Russia, China, or USA equivalents**.
- All factions unite, massive coordinated invasions, **enemy nukes/WMD unlock**.
- **Enemy nukes:** 10-minute launch warning, can be intercepted by missile defense (hard to build), leave radiation zones, can target your Brain Core.
- **Undoing a world war:** destroy **all** your nukes and **pay back all damage** you've done. Every building you destroy (civilian or government) adds to a **damage counter**; every material has a price; you pay through the online meeting. After that: a **fragile ceasefire** that reignites easily.

---

## 9. YOUR MILITARY

- Units: ground combat bots, air drones, naval units, mechs/giant walkers, plus laser defenses, bio-weapons, and other high-tech systems.
- **RTS-style selection and orders.** Units built in factories, fully automated. **No unit cap** (warn me about performance).
- **Defenses:** turrets (bullet → laser → plasma), walls/shields, missile defense (hard), EMP/jammer fields. Turrets use ammo early, energy late.
- **Radar/scouting:** radar towers reveal fog, scout drones, satellites, hacked human cameras.
- **Damage types:** kinetic, explosive, energy, EMP (also hurts your bots), radiation, bio (humans only).
- **Missiles branch:** cruise missiles, ICBMs, interceptors, **nukes at the very top (very hard to make)**.
- **Bio branch:** Bio Data Cells from remains, decomposer organisms for corpse cleanup, bio-weapons (as a game mechanic), organic computing.
- **Cyber branch:** hack enemy turrets/drones, blind radar, steal research, defend against human hackers.
- **Gore:** blood splatter decals, body parts/gibs, corpses **persist until cleaned up** (collector bots → Bio cells, recycling into materials, or decomposer organisms). Gore toggle in settings.
- Real-time combat; **pause only in single-player**.

---

## 10. CO-OP DETAILS

- One **shared factory and research**. **Friendly fire on.**
- One player is chosen as the group's **diplomat** and makes political decisions.
- One shared signature and faction relations.
- **Drop-in/drop-out** anytime.

---

## 11. UI / UX

- HUD and main menu: **I design them.** Build functional placeholder UI that's easy to reskin. Main menu concept: terminal boot sequence.
- 10-slot quickbar, multiple pages.
- Corner minimap, full-screen map zooming out to a **world map with faction borders**, map pings, fog of war. Zooming out far requires **radar**.
- Alerts: under attack, missing input/no power, diplomacy messages, nuke incoming (10 min).
- Tooltips only (no tutorial). Detailed hover tooltips + recipe browser.
- Stats: production/consumption graphs, power graphs, kill/population stats.
- Building: ghost preview (red when invalid), drag-to-place lines of tubes/wires. Deconstruct by clicking or drag-selecting.
- Settings: key rebinding, UI scale, gore toggle.
- Difficulty: Normal / Hard / Brutal presets, custom sliders (aggression, resources), Ironman.
- Localization-ready: English first, then Russian and Chinese.

---

## 12. AUDIO

- Unity audio mixer or FMOD — your choice, explain it.
- Music: dark/grim background music from properly licensed free sources. Keep a `/Docs/LICENSES.md` recording the source and license of every audio file. Don't add any audio file without a recorded license.
- Robotic voice lines for the machine civ (placeholder text-to-speech or silence until I supply them).

---

## 13. BUILD ORDER

**Milestone 1 — First playable (single-player):**
1. Project setup: Unity 6 + URP 2D, Git + Git LFS, private GitHub repo, folder structure, `CLAUDE.md`, GDD, KNOWN_BUGS, ART_GUIDE.
2. Art import pipeline + placeholders. Then teach me the 3/4 drawing basics.
3. Small test map of Antarctica (not the full Earth yet). Chunk system.
4. Player: WASD, temperature, mining, hand-crafting, shooting.
5. Pod reactor power + power network.
6. Tubes + arms + one assembler + drill + smelter.
7. Tier-1 Data Cells via data-wires into a Level 1 Data Center → first research.
8. Save/load with the 10-minute rule.

**Then, in order (confirm with me before each):** full logistics & power → drones → blizzards/seasons → tech tree chart (my approval) → combat & turrets → enemy humans & research stations → factions/diplomacy/world map → world editor & Earth-scale map → brain upload → co-op → late-game (nukes, world war, final research).

---

## 14. THINGS YOU MUST ASK ME ABOUT BEFORE BUILDING THEM
- Names of the 15 largest factions.
- Codenames for the 8 Data Cell tiers.
- The tier × branch research chart.
- The story / time-travel mystery.
- Infinite research (yes/no).
- Exact world-war reparation pricing.
- My terrain art — before you pre-fill Earth.
- The Earth-scale map design and math.
- Your netcode and data-format choices.
- Anything else not in this document.

---

## 15. DECISIONS LOG
Newest at the bottom. These override anything earlier in this file.

| Date | Decision |
|---|---|
| 2026-11-09 | Antarctica crossing on foot = **~10 minutes** (was 1 hour, then 20 min). Working math: ~8 tiles/s walk → ~4,800 tiles across Antarctica → 1 tile ≈ 830 m → Earth ≈ 48,000 × 24,000 tiles (~1.15 billion, ~330 million land). Storage: generated tiles are never saved, only changes (see GDD §2). Full map design still needs my approval (incl. how to handle polar distortion, since Antarctica sits at the edge of a flat world map). |
| 2026-11-09 | Compile check workflow: **option A** — I open the project in Unity and report Console errors; Claude fixes them first next session. |
| 2026-11-09 | Brain-upload soft-lock replaced: Brain Core **self-builds one basic drone** when the player has none. Timer/cost TBD (ask me). |
| 2026-11-09 | Art tool: **Pixelorama** (free). Upload via GitHub Desktop + Git LFS. |
| 2026-11-09 | Timeline: at least a year, no rushing. Quality over speed. |
| 2026-11-09 | **Tile size = 128×128 px** (was 64). Pixels Per Unit = 128. Owner chose this knowing it's ~4× the pixels per drawing. |
| 2026-11-09 | **Approved:** building sprite = footprint × 128 wide, footprint × 128 + 64 px tall. Full M1 sprite list in ART_GUIDE §5 approved. Art pipeline owner-tested and working. |
