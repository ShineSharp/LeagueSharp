using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using ShineCommon;
using SPrediction;
using SharpDX;

namespace MoonDiana 
{
    public class Diana : BaseChamp
    {
        private Obj_AI_Hero m_target = null;
        private int m_misaya_start_tick = 0, m_moon_start_tick = 0;
        private bool m_moon_r_casted = false;
        public Diana()
            : base ("Diana")
        {
            
        }

        public override void CreateConfigMenu()
        {
            combo = new Menu("Combo", "Combo");
            combo.AddItem(new MenuItem("CUSEQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("CUSEW", "Use W").SetValue(true));
            combo.AddItem(new MenuItem("CUSEE", "Use E").SetValue(true));
            //
            ult = new Menu("R Settings", "rsettings");
            ult.AddItem(new MenuItem("CUSER", "Use R").SetValue(true));
            ult.AddItem(new MenuItem("CUSERTOWER", "Dont Use R if Enemy is Under Tower").SetValue(false));
            ult.AddItem(new MenuItem("CUSERMETHOD", "R Method").SetValue<StringList>(new StringList(new string[] { "Use Smart R", "Use Only R To Moonlight Debuffed", "Use R Always" }, 0)));
            //
            combo.AddSubMenu(ult);

            harass = new Menu("Harass", "Harass");
            harass.AddItem(new MenuItem("HUSEQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("HUSEW", "Use W").SetValue(true));
            harass.AddItem(new MenuItem("HUSEE", "Use E").SetValue(false));
            harass.AddItem(new MenuItem("HUSER", "Use R if Moonlight Debuffed").SetValue(true));
            harass.AddItem(new MenuItem("HMANA", "Min. Mana Percent").SetValue(new Slider(50, 100, 0)));

            laneclear = new Menu("LaneClear/JungleClear", "LaneClear");
            laneclear.AddItem(new MenuItem("LUSEQ", "Use Q").SetValue(true));
            laneclear.AddItem(new MenuItem("LUSEW", "Use W").SetValue(true));
            laneclear.AddItem(new MenuItem("LMINC", "Min. Minions to use Q & W").SetValue<Slider>(new Slider(3, 1, 6)));
            laneclear.AddItem(new MenuItem("LMANA", "Min. Mana Percent").SetValue(new Slider(50, 100, 0)));

            misc = new Menu("Misc", "Misc");
            misc.AddItem(new MenuItem("MMISAYA", "Misaya Combo Key").SetValue<KeyBind>(new KeyBind('T', KeyBindType.Press)))
                    .ValueChanged += (s, ar) =>
                    {
                        if (!ar.GetNewValue<KeyBind>().Active)
                        {
                            m_target = null;
                            m_misaya_start_tick = 0;
                        }
                    };
            misc.AddItem(new MenuItem("MMISAYADR", "Misaya Combo Dont Use 2nd R").SetValue(false));
            misc.AddItem(new MenuItem("MMOON", "Moon Combo Key").SetValue<KeyBind>(new KeyBind('G', KeyBindType.Press)))
                    .ValueChanged += (s, ar) =>
                    {
                        if (!ar.GetNewValue<KeyBind>().Active)
                        {
                            m_target = null;
                            m_moon_start_tick = 0;
                            m_moon_r_casted = false;
                        }
                    };
            misc.AddItem(new MenuItem("MFLEE", "Flee Key").SetValue<KeyBind>(new KeyBind('A', KeyBindType.Press)));
            misc.AddItem(new MenuItem("MINTERRUPTE", "Use E For Interrupt").SetValue(true));
            misc.AddItem(new MenuItem("MINTERRUPTRE", "Use R->E to Interrupt Important Spells").SetValue(true));
            misc.AddItem(new MenuItem("MGAPCLOSEW", "Use W For Gapcloser").SetValue(true));
            misc.AddItem(new MenuItem("MLXORBWALKER", "Use LXOrbwalker").SetValue(false))
                        .ValueChanged += (s, ar) =>
                        {
                            if (ar.GetNewValue<bool>())
                            {
                                Orbwalker.Disable();
                                OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.Combo] -= Combo;
                                OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.Mixed] -= Harass;
                                OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.LaneClear] -= LaneClear;

                                OrbwalkingFunctions[(int)LXOrbwalker.Mode.Combo] += Combo;
                                OrbwalkingFunctions[(int)LXOrbwalker.Mode.Harass] += Harass;
                                OrbwalkingFunctions[(int)LXOrbwalker.Mode.LaneClear] += LaneClear;
                                LXOrbwalker.Enable();
                            }
                            else
                            {
                                LXOrbwalker.Disable();
                                OrbwalkingFunctions[(int)LXOrbwalker.Mode.Combo] -= Combo;
                                OrbwalkingFunctions[(int)LXOrbwalker.Mode.Harass] -= Harass;
                                OrbwalkingFunctions[(int)LXOrbwalker.Mode.LaneClear] -= LaneClear;

                                OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.Combo] += Combo;
                                OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.Mixed] += Harass;
                                OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.LaneClear] += LaneClear;
                                Orbwalker.Enable();
                            }
                        };
            misc.AddItem(new MenuItem("MKILLABLEDRAW", "Disable Notifier Drawings").SetValue(false));
            LXOrbwalker.AddToMenu(misc.SubMenu("LXOrbwalker Settings"));

            Config.AddSubMenu(combo);
            Config.AddSubMenu(harass);
            Config.AddSubMenu(laneclear);
            Config.AddSubMenu(misc);
            Config.AddToMainMenu();

            BeforeOrbWalking += BeforeOrbwalk;
            BeforeDrawing += BeforeDraw;
            if (!LXOrbwalkerEnabled)
            {
                OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.Combo] += Combo;
                OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.Mixed] += Harass;
                OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.LaneClear] += LaneClear;
            }
            else
            {
                OrbwalkingFunctions[(int)LXOrbwalker.Mode.Combo] += Combo;
                OrbwalkingFunctions[(int)LXOrbwalker.Mode.Harass] += Harass;
                OrbwalkingFunctions[(int)LXOrbwalker.Mode.LaneClear] += LaneClear;
                LXOrbwalker.Enable();
            }
        }

        public override void SetSpells()
        {
            Spells[Q] = new Spell(SpellSlot.Q, 830f);
            Spells[Q].SetSkillshot(0.5f, 195f, 1600f, false, SkillshotType.SkillshotLine);

            Spells[W] = new Spell(SpellSlot.W, 200f);

            Spells[E] = new Spell(SpellSlot.E, 350f);

            Spells[R] = new Spell(SpellSlot.R, 825f);
        }

        public void BeforeOrbwalk()
        {
            if (Config.Item("MMISAYA").GetValue<KeyBind>().Active)
                Misaya();
            else if (Config.Item("MMOON").GetValue<KeyBind>().Active)
                Moon();
            else if (Config.Item("MFLEE").GetValue<KeyBind>().Active)
                Flee();
        }

        //r q w r e
        public void Misaya()
        {
            if (m_target == null && Spells[Q].IsReady() && Spells[R].IsReady())
                m_target = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Magical);

            if (!LXOrbwalkerEnabled)
                Orbwalking.Orbwalk(m_target, Game.CursorPos);
            else
                LXOrbwalker.Orbwalk(Game.CursorPos, m_target);

            if (m_target != null)
            {
                if (m_target.ServerPosition.Distance(ObjectManager.Player.ServerPosition) <= 810f)
                {
                    if (m_misaya_start_tick == 0) //begin combo
                    {
                        m_misaya_start_tick = Environment.TickCount;
                        Spells[R].CastOnUnit(m_target);
                    }
                }

                if (m_misaya_start_tick != 0)
                {
                    Spells[Q].Cast(m_target.ServerPosition);
                    if (!m_target.IsDead && Spells[W].IsInRange(m_target))
                        Spells[W].Cast();
                    if (!Config.Item("MMISAYADR").GetValue<bool>())
                    {
                        if (!m_target.IsDead)
                            Spells[R].CastOnUnit(m_target);
                        if (!m_target.IsDead && Spells[E].IsInRange(m_target))
                            Spells[E].Cast();
                    }
                    else
                    {
                        if (HasMoonlight(m_target))
                        {
                            if (!m_target.IsDead)
                                Spells[R].CastOnUnit(m_target);
                            if (!m_target.IsDead && Spells[E].IsInRange(m_target))
                                Spells[E].Cast();
                        }
                    }
                }
            }
        }

        public void Moon()
        {
            if (m_target == null && ComboReady())
                m_target = TargetSelector.GetTarget(2000, TargetSelector.DamageType.Magical);

            if (!LXOrbwalkerEnabled)
                Orbwalking.Orbwalk(m_target, Game.CursorPos);
            else
                LXOrbwalker.Orbwalk(Game.CursorPos, m_target);

            if (m_target != null)
            {
                var minion = MinionManager.GetMinions(Spells[R].Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.None).Where(p => p.HasBuff("dianamoonlight")).OrderByDescending(q => q.ServerPosition.Distance(ObjectManager.Player.ServerPosition)).OrderBy(r => r.ServerPosition.Distance(Game.CursorPos)).FirstOrDefault();
                if (minion == null)
                {
                    minion = MinionManager.GetMinions(Spells[Q].Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.None).Where(p => p.Health > Spells[Q].GetDamage(p)).OrderByDescending(q => q.ServerPosition.Distance(ObjectManager.Player.ServerPosition)).ThenByDescending(t => t.Health).FirstOrDefault();
                    if (minion != null && !m_moon_r_casted)
                    {
                        Spells[Q].Cast(minion.ServerPosition);
                        m_moon_start_tick = Environment.TickCount;
                        m_moon_r_casted = false;
                    }
                }

                if (minion != null)
                {
                    if (minion.ServerPosition.Distance(m_target.ServerPosition) < Spells[E].Range - 10 && Environment.TickCount - m_moon_start_tick > 500)
                    {
                        if (Spells[E].IsReady() && !m_moon_r_casted && Spells[R].IsInRange(minion)) //because r->e combo 
                        {
                            Spells[R].CastOnUnit(minion);
                            m_moon_r_casted = true;
                        }
                    }
                }


                if (m_moon_r_casted)
                {
                    if (Spells[E].IsReady() && !m_target.IsDead)
                        Spells[E].Cast();
                    if (Spells[W].IsReady() && !m_target.IsDead)
                        Spells[W].Cast();

                    if (Spells[Q].IsReady() && !m_target.IsDead)
                        Spells[Q].SPredictionCastArc(m_target, HitChance.High, 100);

                    if (Spells[R].IsReady() && !m_target.IsDead && HasMoonlight(m_target))
                        Spells[R].Cast();
                    m_moon_start_tick = 0;
                }
            }
        }

        public void Flee()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            var minion = MinionManager.GetMinions(Spells[R].Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.None).OrderByDescending(q => q.ServerPosition.Distance(ObjectManager.Player.ServerPosition)).Where(p => p.HasBuff("dianamoonlight")).FirstOrDefault();
            if (minion == null)
            {
                minion = MinionManager.GetMinions(Spells[Q].Range - 20, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.None).Where(p => p.Health > Spells[Q].GetDamage(p)).OrderByDescending(q => q.ServerPosition.Distance(ObjectManager.Player.ServerPosition)).FirstOrDefault();
                if (minion != null)
                    Spells[Q].Cast(minion.ServerPosition);
            }

            if (minion != null)
                Spells[R].CastOnUnit(minion);
        }

        public void Combo()
        {
            if (m_target != null && m_target.IsDead)
                m_target = null;

            if (Spells[Q].IsReady() && Config.Item("CUSEQ").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(Spells[Q].Range, TargetSelector.DamageType.Magical);
                if (t != null)
                    Spells[Q].SPredictionCastArc(t, HitChance.High);
            }

            if (m_target == null)
                m_target = HeroManager.Enemies.Where(p => HasMoonlight(p) && p.ServerPosition.Distance(ObjectManager.Player.ServerPosition) <= 900).OrderByDescending(q => TargetSelector.GetPriority(q)).FirstOrDefault();

            if (Config.Item("CUSER").GetValue<bool>())
            {
                if (m_target != null && Config.Item("CUSERMETHOD").GetValue<StringList>().SelectedIndex != 2 && Spells[R].IsReady())
                {
                    if (Spells[R].IsInRange(m_target) && ((m_target.UnderTurret() && !Config.Item("CUSERTOWER").GetValue<bool>()) || !m_target.UnderTurret()))
                    {
                        Spells[R].CastOnUnit(m_target);
                        if (!m_target.IsDead && Spells[W].IsReady() && Config.Item("CUSEW").GetValue<bool>()) //overkill check
                            Spells[W].Cast();

                        if (!m_target.IsDead) //overkill check
                        {
                            if (Config.Item("CUSERMETHOD").GetValue<StringList>().SelectedIndex != 1)
                            {
                                if (Spells[R].IsReady() && ((m_target.UnderTurret() && !Config.Item("CUSERTOWER").GetValue<bool>()) || !m_target.UnderTurret()))
                                {
                                    Spells[R].CastOnUnit(m_target);
                                    m_target = null;
                                }
                            }
                            if(Spells[E].IsReady() && Config.Item("CUSEE").GetValue<bool>())
                                Spells[E].Cast();
                        }
                        else
                            m_target = null;
                        return;
                    }
                    m_target = null;
                }

                if (m_target == null && (Config.Item("CUSERMETHOD").GetValue<StringList>().SelectedIndex == 2 || Config.Item("CUSERMETHOD").GetValue<StringList>().SelectedIndex == 0))
                {
                    var t = TargetSelector.GetTarget(Spells[R].Range, TargetSelector.DamageType.Magical);
                    if (t != null)
                    {
                        if (Config.Item("CUSERMETHOD").GetValue<StringList>().SelectedIndex == 2 || (Config.Item("CUSERMETHOD").GetValue<StringList>().SelectedIndex == 0 && CalculateDamageR(t) >= t.Health))
                        {
                            m_target = t;
                            if(((m_target.UnderTurret() && !Config.Item("CUSERTOWER").GetValue<bool>()) || !m_target.UnderTurret()))
                                Spells[R].CastOnUnit(t);
                        }
                    }
                }
            }

            {
                var t = m_target == null ? TargetSelector.GetTarget(Spells[E].Range, TargetSelector.DamageType.Magical) : m_target;
                if (t != null)
                {
                    if (!t.IsDead)
                    {
                        if (Spells[W].IsReady() && !Spells[E].IsReady() && Spells[W].IsInRange(t) && Config.Item("CUSEW").GetValue<bool>())
                            Spells[W].Cast();

                        if (!t.IsDead && Spells[E].IsReady() && Spells[W].IsReady() && Spells[E].IsInRange(t))
                        {
                            if (Config.Item("CUSEW").GetValue<bool>())
                                Spells[W].Cast();
                            if (Config.Item("CUSEE").GetValue<bool>())
                                Spells[E].Cast();
                        }
                    }
                    m_target = null;
                }
            }
        }

        public void Harass()
        {
            if (ObjectManager.Player.ManaPercent < Config.Item("HMANA").GetValue<Slider>().Value)
                return;

            if (Spells[Q].IsReady() && Config.Item("HUSEQ").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(Spells[Q].Range, TargetSelector.DamageType.Magical);
                if (t != null)
                    Spells[Q].SPredictionCastArc(t, HitChance.High, 100);
            }

            if (m_target == null)
                m_target = HeroManager.Enemies.Where(p => HasMoonlight(p) && p.ServerPosition.Distance(ObjectManager.Player.ServerPosition) <= 900).OrderByDescending(q => TargetSelector.GetPriority(q)).FirstOrDefault();

            if (Config.Item("HUSER").GetValue<bool>())
            {
                if (m_target != null)
                {
                    if (m_target.ServerPosition.CountEnemiesInRange(600) == 1 && Spells[R].IsInRange(m_target) && HasMoonlight(m_target) && !m_target.UnderTurret())
                    {
                        Spells[R].CastOnUnit(m_target);

                        if (!m_target.IsDead && Spells[W].IsReady() && Config.Item("HUSEW").GetValue<bool>()) //overkill check
                            Spells[W].Cast();

                        if (!m_target.IsDead && Spells[E].IsReady() && Config.Item("HUSEE").GetValue<bool>()) //overkill check
                            Spells[E].Cast();

                        m_target = null;
                        return;
                    }
                    m_target = null;
                }
            }

            if (m_target == null)
            {
                var t = TargetSelector.GetTarget(Spells[E].Range, TargetSelector.DamageType.Magical);
                if (t != null)
                {
                    if (Spells[W].IsReady() && !Spells[E].IsReady() && Spells[W].IsInRange(t) && Config.Item("HUSEW").GetValue<bool>())
                        Spells[W].Cast();

                    if (!t.IsDead && Spells[E].IsReady() && Spells[W].IsReady() && Spells[E].IsInRange(t))
                    {
                        if (Config.Item("HUSEW").GetValue<bool>())
                            Spells[W].Cast();
                        if (Config.Item("HUSEE").GetValue<bool>())
                            Spells[E].Cast();
                    }
                }
            }
        }

        public void LaneClear()
        {
            if (ObjectManager.Player.ManaPercent < Config.Item("LMANA").GetValue<Slider>().Value)
                return;

            if (Spells[Q].IsReady() && Config.Item("LUSEQ").GetValue<bool>())
            {
                var farm = MinionManager.GetBestCircularFarmLocation(MinionManager.GetMinions(Spells[Q].Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.None).Select(p => p.ServerPosition.To2D()).ToList(), Spells[Q].Width, Spells[Q].Range);
                if (farm.MinionsHit >= Config.Item("LMINC").GetValue<Slider>().Value)
                    Spells[Q].Cast(farm.Position);

                var jungle_minion = MinionManager.GetMinions(Spells[Q].Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault();
                if (jungle_minion != null)
                    Spells[Q].Cast(jungle_minion.ServerPosition);
            }

            if (Spells[W].IsReady() && Config.Item("LUSEW").GetValue<bool>())
            {
                if (MinionManager.GetMinions(Spells[W].Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.None).Count >= Config.Item("LMINC").GetValue<Slider>().Value)
                    Spells[W].Cast();

                var jungle_minion = MinionManager.GetMinions(Spells[W].Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault();
                if (jungle_minion != null)
                    Spells[W].Cast();
            }
        }

        public override void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.Item("MGAPCLOSEW").GetValue<bool>() && gapcloser.End.Distance(ObjectManager.Player.ServerPosition) <= 300)
                if (Spells[W].IsReady())
                    Spells[W].Cast();
        }

        public override void Interrupter_OnPossibleToInterrupt(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (Config.Item("MINTERRUPTE").GetValue<bool>() && Spells[E].IsInRange(sender))
                Spells[E].Cast();

            if (Config.Item("MINTERRUPTRE").GetValue<bool>() && Spells[R].IsInRange(sender) && sender.IsChannelingImportantSpell())
            {
                if (Spells[R].IsReady() && Spells[E].IsReady())
                {
                    Spells[R].CastOnUnit(sender);
                    Spells[E].Cast();
                }
            }
        }

        public void BeforeDraw()
        {
            if (!Config.Item("MKILLABLEDRAW").GetValue<bool>())
            {
                if (ComboReady())
                    Text.DrawText(null, "Misaya & Moon Combo Is Ready", (int)(ObjectManager.Player.HPBarPosition.X + ObjectManager.Player.BoundingRadius / 2 - 10), (int)(ObjectManager.Player.HPBarPosition.Y - 20), SharpDX.Color.Red);

                if (m_target != null)
                {
                    if (Config.Item("MMISAYA").GetValue<KeyBind>().Active)
                        Text.DrawText(null, "Misaya Combo Target", (int)(m_target.HPBarPosition.X + m_target.BoundingRadius / 2 - 10), (int)(m_target.HPBarPosition.Y), SharpDX.Color.Yellow);
                    else if (Config.Item("MMOON").GetValue<KeyBind>().Active)
                        Text.DrawText(null, "Moon Combo Target", (int)(m_target.HPBarPosition.X + m_target.BoundingRadius / 2 - 10), (int)(m_target.HPBarPosition.Y), SharpDX.Color.Yellow);
                }

                foreach (var enemy in HeroManager.Enemies)
                {
                    if (!enemy.IsDead && enemy.Health < CalculateComboDamage(enemy))
                        Drawing.DrawText((int)(enemy.HPBarPosition.X + enemy.BoundingRadius / 2 - 10), (int)(enemy.HPBarPosition.Y - 20), System.Drawing.Color.Red, "Killable");

                    if (enemy.HasBuff("dianamoonlight"))
                    {
                        var buff = enemy.GetBuff("dianamoonlight");
                        string time = ((int)Math.Floor(buff.EndTime - Game.Time)).ToString();
                        var debuff_pos = Drawing.WorldToScreen(enemy.Position);
                        Drawing.DrawText((int)debuff_pos.X - 20, (int)debuff_pos.Y + 35, System.Drawing.Color.Red, time);
                    }
                }
            }
        }

        public bool HasMoonlight(Obj_AI_Hero t)
        {
            return t.HasBuff("dianamoonlight");
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override double CalculateDamageW(Obj_AI_Hero target)
        {
            if (Config.Item("CUSEW").GetValue<bool>() && Spells[W].IsReady())
                return ObjectManager.Player.GetSpellDamage(target, SpellSlot.W) * 3;

            return 0.0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override double CalculateDamageE(Obj_AI_Hero target)
        {
            return 0.0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override double CalculateDamageR(Obj_AI_Hero target)
        {
            if (Config.Item("CUSER").GetValue<bool>() && Spells[R].IsReady())
                return ObjectManager.Player.GetSpellDamage(target, SpellSlot.R) * 2;

            return 0.0;
        }

        public override void Game_OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling() || args == null)
                return;

            if (BeforeOrbWalking != null) BeforeOrbWalking();

            if (!LXOrbwalkerEnabled && Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.None && OrbwalkingFunctions[(int)Orbwalker.ActiveMode] != null)
                OrbwalkingFunctions[(int)Orbwalker.ActiveMode]();
            else if (LXOrbwalkerEnabled && LXOrbwalker.CurrentMode != LXOrbwalker.Mode.None && OrbwalkingFunctions[(int)LXOrbwalker.CurrentMode] != null)
                OrbwalkingFunctions[(int)LXOrbwalker.CurrentMode]();
        }

        private bool LXOrbwalkerEnabled
        {
            get { return Config.Item("MLXORBWALKER").GetValue<bool>(); }
        }
    }
}
