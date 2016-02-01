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
    public class AntiStealth
    {
        public static bool TryDeStealth(Vector3 pos = new Vector3(), int level = 1)
        {
            if (!TryConsumables(pos))
                return TrySpells(pos, level);

            return true;
        }

        private static bool TryConsumables(Vector3 pos)
        {
            SpellSlot slot = SpellSlot.Unknown;

            //vision ward
            if (Data.Config.Item("USEVISIONWARD").GetValue<bool>())
            {
                if (ObjectManager.Player.Distance(pos) < 1000)
                {
                    if (Items.CanUseItem(2043) && Items.HasItem(2043))
                        slot = ObjectManager.Player.GetSpellSlot("VisionWard");
                    else if ((Items.CanUseItem(3362) && Items.HasItem(3362)))
                        slot = SpellSlot.Trinket;
                }
            }

            //oracle's lens
            if (Data.Config.Item("USEORACLESLENS").GetValue<bool>())
            {
                if (ObjectManager.Player.Distance(pos) < 700)
                {
                    if ((Items.CanUseItem(3364) && Items.HasItem(3364)) || (Items.CanUseItem(3341) && Items.HasItem(3341)))
                        slot = SpellSlot.Trinket;
                }
            }

            //lightbringer active
            if (Data.Config.Item("USELIGHTBRINGER").GetValue<bool>())
            {
                if (Items.CanUseItem(3185) && Items.HasItem(3185))
                {
                    LeagueSharp.Common.Items.UseItem(3185, pos);
                    return true;
                }
            }

            //hextech sweeper
            if (Data.Config.Item("USEHEXTECHSWEEPER").GetValue<bool>())
            {
                if (Items.CanUseItem(3187) && Items.HasItem(3187))
                {
                    LeagueSharp.Common.Items.UseItem(3187, pos);
                    return true;
                }
                else if (Items.CanUseItem(3348) && Items.HasItem(3348))
                {
                    LeagueSharp.Common.Items.UseItem(3348, pos);
                    return true;
                }
            }

            //aram snowball
            if (Data.Config.Item("USESNOWBALL").GetValue<bool>())
            {
                if(Game.MapId.Equals(GameMapId.HowlingAbyss))
                    slot = ObjectManager.Player.GetSpellSlot("summonersnowball");
            }

            if (slot != SpellSlot.Unknown)
                ObjectManager.Player.Spellbook.CastSpell(slot, ObjectManager.Player.ServerPosition);

            return slot != SpellSlot.Unknown;
        }

        private static bool TrySpells(Vector3 pos, int level)
        {
            var spells = Data.AntiStealthSpells.Where(p => p.ChampionName == ObjectManager.Player.ChampionName.ToLower());
            foreach (var spell in spells)
            {
                if (Data.Config.Item(String.Format("USE{0}", spell.Spell.ToString())).GetValue<bool>() && Data.Config.Item(String.Format("DETECT{0}", spell.Spell.ToString())).GetValue<Slider>().Value >= level)
                {
                    if (spell.Spell.IsReady())
                    {
                        if (spell.SelfCast)
                            ObjectManager.Player.Spellbook.CastSpell(spell.Spell);
                        else
                        {
                            if (!ObjectManager.Player.HasBuff("rengarralertsound") && pos.Distance(ObjectManager.Player.ServerPosition) <= spell.SpellRange)
                                ObjectManager.Player.Spellbook.CastSpell(spell.Spell, pos);
                        }
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
