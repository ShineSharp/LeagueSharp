using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LeagueSharp;
using LeagueSharp.Common;
using ShineCommon;

namespace ShineSharp.Champions
{
    public class Sivir : BaseChamp
    {
        private TargetedSpellEvader m_targetedEvader;
        public Sivir()
            : base ("Sivir")
        {

        }

        public override void CreateConfigMenu()
        {
            combo = new Menu("Combo", "Combo");
            combo.AddItem(new MenuItem("CUSEQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("CUSEW", "Use W").SetValue(true));


            harass = new Menu("Harass", "Harass");
            harass.AddItem(new MenuItem("HUSEQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("HUSEW", "Use W").SetValue(true));
            harass.AddItem(new MenuItem("HMANA", "Min. Mana Percent").SetValue(new Slider(50, 0, 100)));

            laneclear = new Menu("LaneClear", "LaneClear");
            laneclear.AddItem(new MenuItem("LUSEW", "Use W").SetValue(true));
            laneclear.AddItem(new MenuItem("LMINW", "Min. Minions To W In Range").SetValue(new Slider(5, 2, 12)));
            laneclear.AddItem(new MenuItem("LMANA", "Min. Mana Percent").SetValue(new Slider(50, 0, 100)));

            misc = new Menu("Misc", "Misc");
            misc.AddItem(new MenuItem("MAUTOQIMMO", "Auto Q Immobile Target").SetValue(true));
            misc.AddItem(new MenuItem("MANTIGAPR", "Anti Gap Closer With R").SetValue(false));

            m_evader = new Evader(out evade, EvadeMethods.SpellShield);
            m_targetedEvader = new TargetedSpellEvader(TargetedSpell_Evade, misc);

            Config.AddSubMenu(combo);
            Config.AddSubMenu(harass);
            Config.AddSubMenu(laneclear);
            Config.AddSubMenu(evade);
            Config.AddSubMenu(misc);
            Config.AddToMainMenu();

            OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.Combo] += Combo;
            OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.Mixed] += Harass;
            OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.LaneClear] += LaneClear;
        }
        
        public override void SetSpells()
        {
            Spells[Q] = new Spell(SpellSlot.Q, 1200f);
            Spells[Q].SetSkillshot(0.5f, 90f, 2000f, false, SkillshotType.SkillshotLine);

            Spells[W] = new Spell(SpellSlot.W, 500f);

            Spells[E] = new Spell(SpellSlot.E, 0f);

            Spells[R] = new Spell(SpellSlot.R, 0f);

            m_evader.SetEvadeSpell(Spells[E]);
        }

        public void Combo()
        {
            if (Spells[Q].IsReady() && Config.Item("CUSEQ").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(Spells[Q].Range, TargetSelector.DamageType.Physical);
                if (t != null)
                    CastSkillshot(t, Spells[Q]);
            }
        }

        public void Harass()
        {
            if (ObjectManager.Player.ManaPercent < Config.Item("HMANA").GetValue<Slider>().Value)
                return;

            if (Spells[Q].IsReady() && Config.Item("HUSEQ").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(Spells[Q].Range, TargetSelector.DamageType.Physical);
                if (t != null)
                    CastSkillshot(t, Spells[Q]);
            }
        }

        public void LaneClear()
        {
            if (!Spells[W].IsReady() || ObjectManager.Player.ManaPercent < Config.Item("LMANA").GetValue<Slider>().Value || !Config.Item("LUSEW").GetValue<bool>())
                return;

            if (MinionManager.GetMinions(Spells[W].Range * 1.5f, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.None).Count() >= Config.Item("LMINW").GetValue<Slider>().Value)
                Spells[W].Cast();
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (Config.Item("HUSEW").GetValue<bool>() || Config.Item("CUSEW").GetValue<bool>())
                if (Spells[W].IsReady() && (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && Config.Item("HMANA").GetValue<Slider>().Value >= (ObjectManager.Player.Mana / ObjectManager.Player.MaxMana * 100))))
                    Spells[W].Cast();
        }

        public override void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Spells[R].IsReady() && gapcloser.End.Distance(ObjectManager.Player.ServerPosition) <= 300 && Config.Item("MANTIGAPR").GetValue<bool>())
                Spells[R].Cast();
        }

        public override void Obj_AI_Base_OnBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffAddEventArgs args)
        {
            if (sender.IsEnemy && sender.IsChampion() && ShineCommon.Utility.IsImmobileTarget(sender as Obj_AI_Hero) && Spells[Q].IsInRange(sender) && Config.Item("MAUTOQIMMO").GetValue<bool>())
                if (Spells[Q].IsReady())
                    Spells[Q].Cast(sender.ServerPosition);
        }

        private void TargetedSpell_Evade(DetectedTargetedSpellArgs data)
        {
            if (Spells[E].IsReady())
            {
                if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo || !m_targetedEvader.DisableInComboMode)
                {
                    Spells[E].Cast();
                }
            }
        }
    }
}
