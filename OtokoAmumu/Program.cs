using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace OtokoAmumu
{
    internal class Program
    {
        private static Orbwalking.Orbwalker _orbwalker;

        private static Spell _q, _w, _e, _r;

        private static Menu _menu;

        private static Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        private static void Main()
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }


        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Amumu")
            {
                return;
            }


            //Spells
            _q = new Spell(SpellSlot.Q, 1100);
            _w = new Spell(SpellSlot.W, 300);
            _e = new Spell(SpellSlot.E, 350);
            _r = new Spell(SpellSlot.R, 550);
            _q.SetSkillshot(0.25f, 90f, 2000f, true, SkillshotType.SkillshotLine);

            //Menu
            _menu = new Menu("Amumu", Player.ChampionName, true);
            Menu orbwalkerMenu = _menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            Menu ts = _menu.AddSubMenu(new Menu("Target Selector", "Target Selector"));
            TargetSelector.AddToMenu(ts);

            //Combo
            Menu spellMenu = _menu.AddSubMenu(new Menu("Combo", "Combo"));
            spellMenu.AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
            spellMenu.AddItem(new MenuItem("useW", "Use W").SetValue(true));
            spellMenu.AddItem(new MenuItem("useE", "Use E").SetValue(true));
            spellMenu.AddItem(new MenuItem("useR", "Use R").SetValue(true));
            spellMenu.AddItem(new MenuItem("ComboQHitChance", "Q HitChance").SetValue(new Slider(3, 1, 4)));
            spellMenu.AddItem(new MenuItem("numR", "Enemies in range to use ultimate").SetValue(new Slider(3, 1, 5)));
            _menu.AddToMainMenu();

            //LaneClear
            Menu laneClear = _menu.AddSubMenu(new Menu("Lane", "Lane Clear"));
            laneClear.AddItem(new MenuItem("useWL", "Use W for lane clear").SetValue(true));
            laneClear.AddItem(new MenuItem("useEL", "Use E for lane clear").SetValue(true));
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;

            //Drawing
            Menu drawing = _menu.AddSubMenu(new Menu("Drawing", "Drawing"));
            drawing.AddItem(new MenuItem("QR", "Draw Q Range").SetValue(true));
            drawing.AddItem(new MenuItem("WR", "Draw W Range").SetValue(true));
            drawing.AddItem(new MenuItem("ER", "Draw E Range").SetValue(true));
            drawing.AddItem(new MenuItem("RR", "Draw R Range").SetValue(true));
            drawing.AddItem(new MenuItem("Ready", "Draw skill range if ready").SetValue(true));
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }

            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                BandageToss();
                Despair();
                Tantrum();
                CurseofTheSadMummy();
            }

            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                BandageToss();
                Despair();
                Tantrum();
            }

            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                DespairLaneClear();
                TantrumLaneClear();
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            bool ready = _menu.Item("Ready").GetValue<bool>();
            if (Player.IsDead)
            {
                return;
            }
            if (!ready)
            {
                if (_menu.Item("QR").GetValue<bool>())
                {
                    Render.Circle.DrawCircle(Player.Position, _q.Range, System.Drawing.Color.Crimson);
                }
                if (_menu.Item("WR").GetValue<bool>())
                {
                    Render.Circle.DrawCircle(Player.Position, _w.Range, System.Drawing.Color.AliceBlue);
                }
                if (_menu.Item("ER").GetValue<bool>())
                {
                    Render.Circle.DrawCircle(Player.Position, _e.Range, System.Drawing.Color.Gold);
                }
                if (_menu.Item("RR").GetValue<bool>())
                {
                    Render.Circle.DrawCircle(Player.Position, _r.Range, System.Drawing.Color.Green);
                }
            }

            if (ready)
            {
                if ((Player.Spellbook.CanUseSpell(SpellSlot.Q) == SpellState.Ready))
                {
                    Render.Circle.DrawCircle(Player.Position, _q.Range, System.Drawing.Color.Crimson);
                }
                if ((Player.Spellbook.CanUseSpell(SpellSlot.E) == SpellState.Ready))
                {
                    Render.Circle.DrawCircle(Player.Position, _e.Range, System.Drawing.Color.Gold);
                }
                if ((Player.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Ready))
                {
                    Render.Circle.DrawCircle(Player.Position, _r.Range, System.Drawing.Color.Green);
                }
            }
        }

        private static void BandageToss()
        {
            if (!_menu.Item("useQ").GetValue<bool>())
            {
                return;
            }
            if (!_q.IsReady())
            {
                return;
            }
            Obj_AI_Hero target = TargetSelector.GetTarget(_q.Range, _q.DamageType);
            PredictionOutput predictionOutput = _q.GetPrediction(target);
            if (predictionOutput.Hitchance >= (HitChance)(_menu.Item("ComboQHitChance").GetValue<Slider>().Value + 2))
            {
                Vector3 predictedPosition = predictionOutput.CastPosition;
                _q.Cast(predictedPosition);
            }
        }

        private static void Despair()
        {
            if (!_menu.Item("useW").GetValue<bool>())
            {
                return;
            }
            int y =
                ObjectManager.Get<Obj_AI_Hero>()
                    .Count(
                        x => !x.IsDead && x.IsEnemy && _w.IsInRange(x.ServerPosition));
            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).ToggleState == 1 && y > 0)
            {
                _w.Cast();
            }
            if (y == 0 && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).ToggleState == 2)
            {
                _w.Cast();
            }
        }

        private static void DespairLaneClear()
        {
            if (!_menu.Item("useWL").GetValue<bool>())
            {
                return;
            }
            if (!_w.IsReady())
            {
                return;
            }
            int z = MinionManager.GetMinions(_w.Range, MinionTypes.All, MinionTeam.NotAlly).Count;
            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).ToggleState == 1 &&
                (z > 2 || MinionManager.GetMinions(_w.Range, MinionTypes.All, MinionTeam.Neutral).Count > 0))
            {
                _w.Cast();
            }
            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).ToggleState == 2 && z == 0)
            {
                _w.Cast();
            }
        }

        private static void Tantrum()
        {
            if (!_menu.Item("useE").GetValue<bool>())
            {
                return;
            }
            if (!_e.IsReady())
            {
                return;
            }
            int y =
                ObjectManager.Get<Obj_AI_Hero>()
                    .Count(
                        x => !x.IsDead && x.IsEnemy && _e.IsInRange(x.ServerPosition));
            if (y >= 1)
            {
                _e.Cast();
            }
        }

        private static void TantrumLaneClear()
        {
            if (!_menu.Item("useEL").GetValue<bool>())
            {
                return;
            }
            if (!_e.IsReady())
            {
                return;
            }
            int z = MinionManager.GetMinions(_e.Range, MinionTypes.All, MinionTeam.NotAlly).Count;
            if (z > 2)
            {
                _e.Cast();
            }
            if (z > 2 || MinionManager.GetMinions(_e.Range, MinionTypes.All, MinionTeam.Neutral).Count > 0)
            {
                _e.Cast();
            }
        }

        private static void CurseofTheSadMummy()
        {
            if (!_menu.Item("useR").GetValue<bool>())
            {
                return;
            }

            if (!_r.IsReady())
            {
                return;
            }
            int y =
                ObjectManager.Get<Obj_AI_Hero>()
                    .Count(x => x.IsEnemy && _r.IsInRange(Prediction.GetPrediction(x, 250, 550).UnitPosition));
            int sliderValue = _menu.Item("numR").GetValue<Slider>().Value;
            if (y >= sliderValue)
            {
                _r.Cast();
            }
        }
    }
}

