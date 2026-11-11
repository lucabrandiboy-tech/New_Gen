using System;
using System.Collections.Generic;
using NewGen.Game.UI;
using NewGen.Sim.Core;
using NewGen.Sim.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace NewGen.Game
{
    /// <summary>Backquote (`) opens it. Dev builds / editor only. Type "help".</summary>
    public sealed class DevConsole : MonoBehaviour
    {
        GameRoot root; GameObject box; InputField input; Text output;
        readonly List<string> lines = new List<string>();
        public bool IsOpen => box != null && box.activeSelf;

        public static DevConsole Create(GameRoot root)
        {
            var go = new GameObject("DevConsole");
            var c = go.AddComponent<DevConsole>(); c.root = root;
            return c;
        }

        void Build()
        {
            box = UiKit.Panel_(root.Hud.transform, "Console", new Color(0f, 0f, 0f, 0.85f));
            UiKit.Place(box, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(1400f, 300f));
            output = UiKit.Text_(box.transform, "", 16, TextAnchor.LowerLeft, UiKit.Accent);
            UiKit.Stretch(output.gameObject, 10, 10, 10, 44);
            var fieldGo = UiKit.Panel_(box.transform, "Input", UiKit.PanelLight);
            UiKit.Place(fieldGo, new Vector2(0f, 0f), new Vector2(10f, 8f), new Vector2(1380f, 30f));
            input = fieldGo.AddComponent<InputField>();
            var txt = UiKit.Text_(fieldGo.transform, "", 16, TextAnchor.MiddleLeft); UiKit.Stretch(txt.gameObject, 6, 6, 2, 2);
            input.textComponent = txt;
            input.onSubmit.AddListener(s => { Run(s); input.text = ""; input.ActivateInputField(); });
            box.SetActive(false);
            Print("dev console. 'help' lists commands.");
        }

        void Update()
        {
            if (!Debug.isDebugBuild && !Application.isEditor) return;
            if (root.Input.DevConsole.WasPressedThisFrame())
            {
                if (box == null) Build();
                box.SetActive(!box.activeSelf);
                if (box.activeSelf) { input.text = ""; input.ActivateInputField(); }
            }
        }

        void Print(string s)
        {
            lines.Add(s); if (lines.Count > 12) lines.RemoveAt(0);
            if (output != null) output.text = string.Join("\n", lines);
        }

        void Run(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd)) return;
            Print("> " + cmd);
            var a = cmd.Trim().Split(' ');
            var sim = root.Sim; var p = sim?.LocalPlayer;
            try
            {
                switch (a[0].ToLowerInvariant())
                {
                    case "help": Print("give <item> [n] | tech <id>|all | teleport <x> <y> | spawn <building> [facing] | heat <0-100> | temp <0-1000> | hp <n> | time | hash | reveal | setpop <n> | event <name>"); break;
                    case "give": if (p == null) break; { int n = a.Length > 2 ? int.Parse(a[2]) : 1; if (root.Db.Item(a[1]) == null) { Print("unknown item"); break; } int left = p.Inventory.Add(a[1], n); p.Version++; Print($"gave {n - left} {a[1]}"); } break;
                    case "tech": if (sim == null) break; if (a[1] == "all") { foreach (var t in root.Db.Techs.Values) Unlock(t.Id); } else Unlock(a[1]); root.Hud.RebuildQuickbar(); Print("ok"); break;
                    case "teleport": if (p == null) break; p.Pos = SubPos.FromTileCenter(new TilePos(int.Parse(a[1]), int.Parse(a[2]))); Print("teleported"); break;
                    case "spawn":
                        {
                            if (sim == null) break; var tile = root.Build.HoverTile; var facing = a.Length > 2 ? (Dir)Enum.Parse(typeof(Dir), a[2], true) : Dir.North;
                            var b = sim.Place(a[1], tile, facing); Print(b == null ? "could not place " + a[1] + " (" + sim.CanPlace(a[1], tile, facing) + ")" : "placed " + a[1]); break;
                        }
                    case "heat": foreach (var b in sim.Buildings) if (b is DataCenterBuilding dc) dc.Heat = dc.Def.MaxHeat * 10 * int.Parse(a[1]); Print("ok"); break;
                    case "temp": if (p != null) p.Temperature = int.Parse(a[1]); break;
                    case "hp": if (p != null) p.Health = int.Parse(a[1]); break;
                    case "time": Print($"tick {sim.Tick}  {sim.SecondsElapsed}s  entities {sim.EntityCount}  chunks {sim.World.LoadedChunkCount}"); break;
                    case "hash": Print("state hash " + Sim.Save.SaveSystem.StateHash(sim)); break;
                    case "reveal": Print("no fog of war yet"); break;
                    case "setpop": Print("factions not built yet"); break;
                    case "event": Print("events not built yet"); break;
                    default: Print("unknown command"); break;
                }
            }
            catch (Exception e) { Print("error: " + e.Message); }
        }

        void Unlock(string id)
        {
            var t = root.Db.Tech(id); if (t == null) { Print("unknown tech " + id); return; }
            root.Sim.Research.Unlocked.Add(id);
            foreach (var b in t.UnlocksBuildings) root.Sim.UnlockedBuildings.Add(b);
            foreach (var r in t.UnlocksRecipes) root.Sim.UnlockedRecipes.Add(r);
        }
    }
}
