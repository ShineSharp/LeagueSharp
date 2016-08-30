namespace Taliyah
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;
    using LeagueSharp.SDK.Enumerations;
    using SharpDX;

    using Menu = LeagueSharp.SDK.UI.Menu;
    using SkillshotType = LeagueSharp.SDK.Enumerations.SkillshotType;
    using Spell = LeagueSharp.SDK.Spell;


    class Program
    {
        private static Menu main_menu;
        private static Spell Q, W, E;
        private static Vector3 lastE;
        private static int lastETick = Environment.TickCount;
        private static bool Q5x = true;
        private static bool EWCasting = false;
        private static GameObject selectedGObj = null;
        private static bool pull_push_enemy = false;
        static void Main(string[] args)
        {
            Bootstrap.Init();
            Events.OnLoad += OnLoad;
        }

        private static void OnLoad(object sender, EventArgs e)
        {
            if (ObjectManager.Player.ChampionName != "Taliyah")
                return;

            main_menu = new Menu("taliyah", "Taliyah", true);

            Menu combo = new Menu("taliyah.combo", "Combo");
            combo.Add(new MenuBool("taliyah.combo.useq", "Use Q", true, ObjectManager.Player.ChampionName));
            combo.Add(new MenuBool("taliyah.combo.usew", "Use W", true, ObjectManager.Player.ChampionName));
            combo.Add(new MenuBool("taliyah.combo.usee", "Use E", true, ObjectManager.Player.ChampionName));
            main_menu.Add(combo);

            Menu harass = new Menu("taliyah.harass", "Harass");
            harass.Add(new MenuBool("taliyah.harass.useq", "Use Q", true, ObjectManager.Player.ChampionName));
            harass.Add(new MenuSlider("taliyah.harass.manaperc", "Min. Mana", 40, 0, 100, ObjectManager.Player.ChampionName));
            main_menu.Add(harass);

            Menu laneclear = new Menu("taliyah.laneclear", "LaneClear");
            laneclear.Add(new MenuBool("taliyah.laneclear.useq", "Use Q", true, ObjectManager.Player.ChampionName));
            laneclear.Add(new MenuBool("taliyah.laneclear.useew", "Use EW", false, ObjectManager.Player.ChampionName));
            laneclear.Add(new MenuSlider("taliyah.laneclear.minq", "Min. Q Hit", 3, 1, 6, ObjectManager.Player.ChampionName));
            laneclear.Add(new MenuSlider("taliyah.laneclear.minew", "Min. EW Hit", 5, 1, 6, ObjectManager.Player.ChampionName));
            laneclear.Add(new MenuSlider("taliyah.laneclear.manaperc", "Min. Mana", 40, 0, 100, ObjectManager.Player.ChampionName));
            main_menu.Add(laneclear);

            Menu drawing = new Menu("taliyah.drawing", "Drawings");
            drawing.Add(new MenuBool("taliyah.drawing.drawq", "Draw Q", true, ObjectManager.Player.ChampionName));
            drawing.Add(new MenuBool("taliyah.drawing.draww", "Draw W", true, ObjectManager.Player.ChampionName));
            drawing.Add(new MenuBool("taliyah.drawing.drawe", "Draw E", true, ObjectManager.Player.ChampionName));
            drawing.Add(new MenuBool("taliyah.drawing.drawr", "Draw R Minimap", true, ObjectManager.Player.ChampionName));
            main_menu.Add(drawing);

            main_menu.Add(new MenuBool("taliyah.onlyq5", "Only cast 5x Q", false, ObjectManager.Player.ChampionName));
            main_menu.Add(new MenuBool("taliyah.antigap", "Auto E to Gapclosers", true, ObjectManager.Player.ChampionName));
            main_menu.Add(new MenuBool("taliyah.interrupt", "Auto W to interrupt spells", true, ObjectManager.Player.ChampionName));
            main_menu.Add(new MenuKeyBind("taliyah.pullenemy", "Pull Selected Target", System.Windows.Forms.Keys.T, KeyBindType.Press, ObjectManager.Player.ChampionName));
            main_menu.Add(new MenuKeyBind("taliyah.pushenemy", "Push Selected Target", System.Windows.Forms.Keys.G, KeyBindType.Press, ObjectManager.Player.ChampionName));
            main_menu.Attach();
      
            Q = new Spell(SpellSlot.Q, 900f);
            Q.SetSkillshot(0f, 60f, Q.Instance.SData.MissileSpeed, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 800f);
            W.SetSkillshot(1f, 50f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 700f);
            E.SetSkillshot(0.25f, 150f, 2000f, false, SkillshotType.SkillshotLine);

            Game.OnWndProc += Game_OnWndProc;
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            Events.OnGapCloser += Events_OnGapCloser;
            Events.OnInterruptableTarget += Events_OnInterruptableTarget;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Drawing.OnEndScene += Drawing_OnEndScene;
        }

        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (main_menu["taliyah.drawing"]["taliyah.drawing.drawr"].GetValue<MenuBool>().Value && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).IsReady())
            {
                float radius = 3000f + 1500f * (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level - 1);
                var pointList = new List<Vector3>();
                for (var i = 0; i < 23; i++)
                {
                    var angle = i * Math.PI * 2 / 23;
                    pointList.Add(
                        new Vector3(
                            ObjectManager.Player.ServerPosition.X + radius * (float)Math.Cos(angle), ObjectManager.Player.ServerPosition.Y + radius * (float)Math.Sin(angle),
                            ObjectManager.Player.ServerPosition.Z));
                }

                for (var i = 0; i < pointList.Count; i++)
                {
                    var a = pointList[i];
                    var b = pointList[i == pointList.Count - 1 ? 0 : i + 1];

                    var aonScreen = Drawing.WorldToMinimap(a);
                    var bonScreen = Drawing.WorldToMinimap(b);

                    Drawing.DrawLine(aonScreen.X, aonScreen.Y, bonScreen.X, bonScreen.Y, 1, System.Drawing.Color.White);
                }

            }
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint)WindowsMessages.LBUTTONDOWN)
            {
                selectedGObj = ObjectManager.Get<Obj_AI_Base>().Where(p => p.IsValid && !p.IsMe && !p.IsDead && p.Distance(Game.CursorPos.ToVector2()) < 200 && p.IsAlly).FirstOrDefault();
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (main_menu["taliyah.drawing"]["taliyah.drawing.drawq"].GetValue<MenuBool>().Value)
                Drawing.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Red);

            if (main_menu["taliyah.drawing"]["taliyah.drawing.draww"].GetValue<MenuBool>().Value)
                Drawing.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Green);

            if (main_menu["taliyah.drawing"]["taliyah.drawing.drawe"].GetValue<MenuBool>().Value)
                Drawing.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Blue);
        }

        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.Slot == SpellSlot.E)
                lastETick = Environment.TickCount;
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.IsAlly && sender.Name == "Taliyah_Base_Q_aoe_bright.troy")
                Q5x = false;
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.IsAlly && sender.Name == "Taliyah_Base_Q_aoe_bright.troy")
                Q5x = true;
        }

        private static void Combo()
        {
            if (W.Instance.Name == "TaliyahWNoClick")
            {
                //
            }
            else
            {
                if (W.IsReady()) //killable W
                {
                    var target = W.GetTarget();
                    if (target != null && target.Health < WDamage(target) - 50)
                    {
                        var pred = W.GetPrediction(target);
                        if (pred.Hitchance >= HitChance.High)
                            W.Cast(pred.UnitPosition, ObjectManager.Player.ServerPosition);
                    }

                }

                if (!EWCasting)
                {
                    if (E.IsReady() && main_menu["taliyah.combo"]["taliyah.combo.usee"].GetValue<MenuBool>().Value)
                    {
                        if (W.IsReady() && main_menu["taliyah.combo"]["taliyah.combo.usew"].GetValue<MenuBool>().Value)
                        {
                            //e w combo
                            var target = W.GetTarget();
                            if (target != null)
                            {
                                var pred = W.GetPrediction(target);
                                if (pred.Hitchance >= HitChance.High)
                                {
                                    lastE = ObjectManager.Player.ServerPosition;
                                    E.Cast(ObjectManager.Player.ServerPosition.ToVector2() + (pred.CastPosition.ToVector2() - ObjectManager.Player.ServerPosition.ToVector2()).Normalized() * (E.Range - 200));
                                    DelayAction.Add(250, () => { W.Cast(pred.UnitPosition, lastE); EWCasting = false; });
                                    EWCasting = true;
                                }
                            }
                            return;
                        }
                        else
                        {
                            var target = E.GetTarget();
                            if (target != null)
                            {
                                E.Cast(target);
                                lastE = ObjectManager.Player.ServerPosition;
                            }
                        }
                    }
                }
                if (W.IsReady() && main_menu["taliyah.combo"]["taliyah.combo.usew"].GetValue<MenuBool>().Value && !EWCasting)
                {
                    var target = W.GetTarget();
                    if (target != null)
                    {
                        var pred = W.GetPrediction(target);
                        if (pred.Hitchance >= HitChance.High)
                            W.Cast(pred.UnitPosition, pred.UnitPosition);
                    }
                }
            }
            var q_target = Q.GetTarget();
            if (q_target != null && main_menu["taliyah.combo"]["taliyah.combo.useq"].GetValue<MenuBool>().Value && (!main_menu["taliyah.onlyq5"].GetValue<MenuBool>().Value || Q5x))
                Q.Cast(q_target);

        }

        private static void Harass()
        {
            if (ObjectManager.Player.ManaPercent < main_menu["taliyah.harass"]["taliyah.harass.manaperc"].GetValue<MenuSlider>().Value)
                return;

            if (main_menu["taliyah.harass"]["taliyah.harass.useq"].GetValue<MenuBool>().Value)
            {
                var target = Q.GetTarget();
                if (target != null)
                    Q.Cast(target);
            }
        }

        private static void LaneClear()
        {
            if (ObjectManager.Player.ManaPercent < main_menu["taliyah.laneclear"]["taliyah.laneclear.manaperc"].GetValue<MenuSlider>().Value)
                return;
            
            if (main_menu["taliyah.laneclear"]["taliyah.laneclear.useq"].GetValue<MenuBool>().Value && Q.IsReady())
            {
                var farm = Q.GetCircularFarmLocation(ObjectManager.Get<Obj_AI_Minion>().Where(p => p.IsEnemy && p.DistanceToPlayer() < Q.Range).ToList());
                if (farm.MinionsHit >= main_menu["taliyah.laneclear"]["taliyah.laneclear.minq"].GetValue<MenuSlider>().Value)
                    Q.Cast(farm.Position);
            }

            if (main_menu["taliyah.laneclear"]["taliyah.laneclear.useew"].GetValue<MenuBool>().Value && W.IsReady() && E.IsReady())
            {
                var farm = W.GetCircularFarmLocation(ObjectManager.Get<Obj_AI_Minion>().Where(p => p.IsEnemy && p.DistanceToPlayer() < W.Range).ToList());
                if (farm.MinionsHit >= main_menu["taliyah.laneclear"]["taliyah.laneclear.minew"].GetValue<MenuSlider>().Value)
                {
                    E.Cast(farm.Position);
                    lastE = ObjectManager.Player.ServerPosition;
                    if (W.Instance.Name == "TaliyahW")
                        W.Cast(farm.Position, lastE.ToVector2());
                }
            }

        }

        private static void CheckKeyBindings()
        {
            if (main_menu["taliyah.pullenemy"].GetValue<MenuKeyBind>().Active || main_menu["taliyah.pushenemy"].GetValue<MenuKeyBind>().Active)
            {
                Variables.Orbwalker.Orbwalk();
                if (!pull_push_enemy && Variables.TargetSelector.Selected.Target != null && Variables.TargetSelector.Selected.Target.IsValidTarget(W.Range))
                {
                    Vector3 push_position = ObjectManager.Player.ServerPosition;

                    if (main_menu["taliyah.pushenemy"].GetValue<MenuKeyBind>().Active)
                    {
                        if (selectedGObj != null && selectedGObj.Distance(ObjectManager.Player) < 1000)
                            push_position = selectedGObj.Position;
                        else
                            push_position = Variables.TargetSelector.Selected.Target.ServerPosition + (Variables.TargetSelector.Selected.Target.ServerPosition - ObjectManager.Player.ServerPosition).Normalized() * 50f;
                    }
                    var pred = W.GetPrediction(Variables.TargetSelector.Selected.Target);
                    if (pred.Hitchance >= HitChance.High)
                    {
                        pull_push_enemy = true;
                        W.Cast(pred.UnitPosition, push_position);
                        DelayAction.Add(250, () => pull_push_enemy = false);
                    }
                }
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            switch (Variables.Orbwalker.ActiveMode)
            {
                case OrbwalkingMode.Combo:
                    Combo();
                    break;
                case OrbwalkingMode.Hybrid:
                    Harass();
                    break;
                case OrbwalkingMode.LaneClear:
                    LaneClear();
                    break;
                case OrbwalkingMode.None:
                    CheckKeyBindings();
                    break;
            }
        }

        private static void Events_OnInterruptableTarget(object sender, Events.InterruptableTargetEventArgs e)
        {
            if (main_menu["taliyah.interrupt"].GetValue<MenuBool>().Value)
            {
                if (e.Sender.DistanceToPlayer() < W.Range)
                    W.Cast(e.Sender.ServerPosition, ObjectManager.Player.ServerPosition);
            }
        }

        private static void Events_OnGapCloser(object sender, Events.GapCloserEventArgs e)
        {
            if (main_menu["taliyah.antigap"].GetValue<MenuBool>().Value)
            {
                if (e.End.DistanceToPlayer() < E.Range)
                    E.Cast(e.Sender.ServerPosition);
            }
        }

        private static float WDamage(Obj_AI_Base target)
        {
            return (float)ObjectManager.Player.CalculateDamage(target, DamageType.Magical, new int[] { 60, 80, 100, 120, 140 }[W.Level - 1] + ObjectManager.Player.TotalMagicalDamage * 0.4f);
        }
    }
}
