using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SPrediction;
using ShineCommon;

namespace SPredictioner
{
    public class EventHandlers
    {
        private static bool[] handleEvent = { true, true, true, true };
        private static object m_lock = new object();

        

        public static void Game_OnGameLoad(EventArgs args)
        {
            SPredictioner.Initialize();
        }

        public static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                lock (m_lock)
                {
                    SpellSlot slot = ObjectManager.Player.GetSpellSlot(args.SData.Name);
                    if (!ShineCommon.Utility.IsValidSlot(slot))
                        return;

                    if (!handleEvent[(int)slot])
                    {
                        if (SPredictioner.Spells[(int)slot] != null)
                            handleEvent[(int)slot] = true;
                    }
                }
            }
        }

        public static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.Owner.IsMe)
            {
                lock (m_lock)
                {
                    if (SPredictioner.Config.Item("ENABLED").GetValue<bool>() && (SPredictioner.Config.Item("COMBOKEY").GetValue<KeyBind>().Active || SPredictioner.Config.Item("HARASSKEY").GetValue<KeyBind>().Active))
                    {
                        if (!ShineCommon.Utility.IsValidSlot(args.Slot))
                            return;

                        if (SPredictioner.Spells[(int)args.Slot] == null)
                            return;

                        if (!SPredictioner.Config.Item(String.Format("{0}{1}", ObjectManager.Player.ChampionName, args.Slot)).GetValue<bool>())
                            return;

                        if (handleEvent[(int)args.Slot])
                        {
                            args.Process = false;
                            handleEvent[(int)args.Slot] = false;
                            var enemy = args.EndPosition.GetEnemiesInRange(200f).OrderByDescending(p => ShineCommon.Utility.GetPriority(p.ChampionName)).FirstOrDefault();
                            if (enemy == null)
                                enemy = TargetSelector.GetTarget(SPredictioner.Spells[(int)args.Slot].Range, TargetSelector.DamageType.Physical);

                            if (enemy != null)
                                SPredictioner.Spells[(int)args.Slot].SPredictionCast(enemy, ShineCommon.Utility.HitchanceArray[SPredictioner.Config.Item("SPREDHITC").GetValue<StringList>().SelectedIndex]);
                        }
                    }
                }
            }
        }
    }
}
