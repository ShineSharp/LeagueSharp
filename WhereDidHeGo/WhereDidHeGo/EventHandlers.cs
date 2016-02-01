using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace WhereDidHeGo
{
    public class EventHandlers
    {
        public static void Initialize()
        {
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast; //for stealth spells
            Obj_AI_Base.OnCreate += Obj_AI_Base_OnCreate; //for lb passive & rengar ult
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.IsEnemy)
            {
                if ((sender.Name.Contains("Rengar_Base_R_Alert") && ObjectManager.Player.HasBuff("rengarralertsound")) || sender.Name == "LeBlanc_Base_P_poof.troy")
                    AntiStealth.TryDeStealth(sender.Position, 3);
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            lock (Data.StealthPoses)
            {
                Data.StealthPoses.RemoveAll(p => Utils.TickCount - p.Item1 > 4000);
                foreach (var p in Data.StealthPoses)
                    if (Data.Config.Item("DRAWCIRCLE").GetValue<bool>())
                        Render.Circle.DrawCircle(p.Item2, 75, System.Drawing.Color.DarkRed);
            }

            lock (Data.PingPoses)
            {
                foreach (var p in Data.PingPoses)
                {
                    switch (Data.Config.Item("PINGMODE").GetValue<StringList>().SelectedIndex)
                    {
                        case 0:
                            {
                                for (int i = 0; i < Data.Config.Item("PINGCOUNT").GetValue<Slider>().Value; i++)
                                    Game.ShowPing(PingCategory.EnemyMissing, p, true);
                            }
                            break;
                        case 1:
                            {
                                for (int i = 0; i < Data.Config.Item("PINGCOUNT").GetValue<Slider>().Value; i++)
                                    Game.SendPing(PingCategory.EnemyMissing, p);
                            }
                            break;
                    }

                }

                Data.PingPoses.Clear();
            }

            lock (Data.StealthPaths)
            {
                if (Data.Config.Item("DRAWWAPOINTS").GetValue<bool>())
                {
                    foreach (var p in Data.StealthPaths)
                    {
                        if (p.Item2.Count > 1)
                        {
                            for (int i = 0; i < p.Item2.Count - 1; i++)
                            {
                                Vector2 posFrom = Drawing.WorldToScreen(p.Item2[i].To3D());
                                Vector2 posTo = Drawing.WorldToScreen(p.Item2[i + 1].To3D());
                                Drawing.DrawLine(posFrom, posTo, 2, System.Drawing.Color.Aqua);
                            }

                            Vector2 pos = Drawing.WorldToScreen(p.Item2[p.Item2.Count - 1].To3D());
                            Drawing.DrawText(pos.X, pos.Y, System.Drawing.Color.Black, p.Item1.ToString("0.00")); //arrival time
                            Render.Circle.DrawCircle(p.Item2[p.Item2.Count - 1].To3D(), 75f, System.Drawing.Color.Aqua); //end circle
                        }
                    }
                }
            }

            if (Data.Teemo != null && !Data.Teemo.IsVisible)
                Render.Circle.DrawCircle(Data.Teemo.Position, 75f, System.Drawing.Color.Green);

            foreach(var obj in Data.StealthObjects)
            {
                Render.Circle.DrawCircle(obj.Position.To3D2(), obj.Radius, System.Drawing.Color.Red, 10);
                Vector2 pos = Drawing.WorldToScreen(obj.Position.To3D2());
                Drawing.DrawText(pos.X, pos.Y, System.Drawing.Color.Black, obj.DisplayName);
                Drawing.DrawText(pos.X, pos.Y + 20, System.Drawing.Color.Black, ((obj.EndTick - Environment.TickCount) / 1000).ToString("0:00"));
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsEnemy && Data.Config.Item("ENABLEWDHG").GetValue<bool>())
            {
                int level = 1;
                Vector3 pos = args.End;
                switch (args.SData.Name.ToLower())
                {
                    case "deceive":
                        {
                            if (args.Start.Distance(args.End) > 400)
                                pos = args.Start + (args.End - args.Start).Normalized() * 400;
                        }
                        break;
                    case "vaynetumble":
                        {
                            if (sender.HasBuff("VayneInquisition"))
                            {
                                pos = args.Start + (args.End - args.Start).Normalized() * 300;
                                level = 2;
                            }
                            else
                                return;
                        }
                        break;
                    case "summonerflash":
                        {
                            if (sender.IsVisible)
                                return;
                        }
                        break;
                    case "jackinbox":
                        {
                            if (args.Start.Distance(args.End) > 425)
                                pos = args.Start + (args.End - args.Start).Normalized() * 425;

                            Data.StealthObjects.Add(new Data._odata { DisplayName = "Shaco Box", Position = pos.To2D(), EndTick = Environment.TickCount + 60000, Radius = 300 });
                            Utility.DelayAction.Add(60000, () => { var obj = Data.StealthObjects.FirstOrDefault(p => p.Position.Distance(pos.To2D()) < float.Epsilon); if (obj != null) Data.StealthObjects.Remove(obj); });

                            return;
                        }
                    default:
                        {
                            if (!Data.StealthSpells.Any(p => p.Item2 == args.SData.Name.ToLower()))
                                return;

                            level = Data.StealthSpells.First(p => p.Item2 == args.SData.Name.ToLower()).Item1;
                        }
                        break;
                }

                lock (Data.StealthPoses)
                    Data.StealthPoses.Add(new Tuple<int, Vector3>(Utils.TickCount, pos));

                lock (Data.PingPoses)
                    Data.PingPoses.Add(pos);

                List<Vector2> path = sender.GetWaypoints();
                var pair = new Tuple<float, List<Vector2>>(path.PathLength() / sender.MoveSpeed, path);

                lock (Data.StealthPaths)
                    Data.StealthPaths.Add(pair);

                Utility.DelayAction.Add((int)(path.PathLength() / sender.MoveSpeed * 1000), () => Data.StealthPaths.Remove(pair));

                AntiStealth.TryDeStealth(pos, level);
            }
        }
    }
}
