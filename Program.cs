namespace Quinnz
{
    using System;
    using System.Linq;

    using EloBuddy;
    using EloBuddy.SDK;
    using EloBuddy.SDK.Enumerations;
    using EloBuddy.SDK.Events;
    using EloBuddy.SDK.Menu;
    using EloBuddy.SDK.Menu.Values;
    using EloBuddy.SDK.Rendering;
    using SharpDX;
    using System.Collections.Generic;

    public static class Program
    {

        public static SpellSlot IgniteSlot;

        private static Menu Menu;

        private static AIHeroClient myHero
        {
            get
            {
                return ObjectManager.Player;
            }
        }

        public static Spell.Targeted E;

        public static Spell.Skillshot Q;

        public static Spell.Active R, W, E2;

        public static Menu ComboMenu { get; private set; }

        public static Menu HarassMenu { get; private set; }

        public static Menu MiscMenu { get; private set; }

        public static Menu KSMenu { get; private set; }

        public static Menu JungleMenu { get; private set; }

        public static Menu LaneMenu { get; private set; }


        public static AIHeroClient lastTarget;

        public static Vector3 predictedPos;

        public static float lastSeen = Game.Time;

        private static Menu quinnMenu;

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Game_OnGameLoad;
        }


        public static void Game_OnGameLoad(EventArgs args)
        {
                if (myHero.ChampionName != "Quinn")
                {
                    return;
                }


                quinnMenu = MainMenu.AddMenu("Quinn", "Quinn");
                quinnMenu.AddGroupLabel("Quinn it to WIN it!");
                ComboMenu = quinnMenu.AddSubMenu("Combo");
                ComboMenu.AddGroupLabel("Combo Settings");
                ComboMenu.Add("useQ", new CheckBox("Use Q"));
                ComboMenu.Add("UseW", new CheckBox("Use W when enemy is not visible"));
                ComboMenu.Add("useE", new CheckBox("Use E"));


                HarassMenu = quinnMenu.AddSubMenu("Harass");
                HarassMenu.AddGroupLabel("Harass Settings");
                HarassMenu.Add("useQ", new CheckBox("Use Q"));

                LaneMenu = quinnMenu.AddSubMenu("LaneCLear");
                LaneMenu.AddGroupLabel("LaneCLear Settings");
                LaneMenu.Add("UseQlc", new CheckBox("Use Q"));
                LaneMenu.Add("UseElc", new CheckBox("Use E"));
                LaneMenu.Add("lcount", new Slider("Use Q on {0} Minion (0 = Don't)", 2, 0, 5));
                LaneMenu.Add("lanem", new Slider("Minimum mana", 20, 0, 100));

                JungleMenu = quinnMenu.AddSubMenu("Jungleclear");
                JungleMenu.AddGroupLabel("Jungleclear Settings");
                JungleMenu.Add("UseQjg", new CheckBox("Use Q"));
                JungleMenu.Add("UseEjg", new CheckBox("Use E"));
                JungleMenu.Add("jgMana", new Slider("Minimum mana", 20, 0, 100));

                KSMenu = quinnMenu.AddSubMenu("Killsteal");
                KSMenu.AddGroupLabel("Killsteal Settings");
                KSMenu.Add("ksQ", new CheckBox("Use Q"));

                MiscMenu = quinnMenu.AddSubMenu("Miscellaneous");
                MiscMenu.AddGroupLabel("Misc Settings");
                MiscMenu.Add("antiG", new CheckBox("Use E - Antigapcloser"));
                MiscMenu.Add("interrpt", new CheckBox("Use E - interrupter"));
                MiscMenu.Add("autor", new CheckBox("Use R in Base"));

                IgniteSlot = ObjectManager.Player.GetSpellSlotFromName("summonerdot");

                Q = new Spell.Skillshot(SpellSlot.Q, 1025, SkillShotType.Linear, 0, 750, 210);
                W = new Spell.Active(SpellSlot.W, 2100);
                E = new Spell.Targeted(SpellSlot.E, (int)675f);
                R = new Spell.Active(SpellSlot.R, 0);
                E2 = new Spell.Active(SpellSlot.E, 300);



            Game.OnUpdate += OnUpdate;
                Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
                Interrupter.OnInterruptableSpell += Interrupt;
                Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
                Orbwalker.OnPostAttack += AfterAA;

        }


 


        private static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            try
            {
                if (MiscMenu["antiG"].Cast<CheckBox>().CurrentValue && W.IsReady())
                {
                    if (e.Sender.IsValidTarget(W.Range))
                    {
                        W.Cast(e.Sender);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }


        private static void Wvision()
        {
            if (Player.Instance.IsDead || Player.Instance.IsInvulnerable || !Player.Instance.IsTargetable || Player.Instance.IsZombie || Player.Instance.IsInShopRange())
                return;
            else if (ComboMenu["UseW"].Cast<CheckBox>().CurrentValue && lastTarget != null && W.IsReady() && lastTarget.Position.Distance(Player.Instance) < 600 && Game.Time - lastSeen > 2)           
                    {
                        W.Cast(predictedPos);
                    }
                }
            
       

        private static void AfterAA(AttackableUnit target, EventArgs args)

        {
            if (target.Type == GameObjectType.AIHeroClient)
            {
                var t = target as AIHeroClient;
                if (E.IsReady() && ComboMenu["useE"].Cast<CheckBox>().CurrentValue && t.IsValidTarget(E.Range) && t.CountEnemiesInRange(800) < 3)
                {
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                        E.Cast(t);
                        Orbwalker.ResetAutoAttack();

                }
            }
        }

        private static void DoCombo()
        {
            try
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                if (target == null)
                {
                    return;
                }

                var passiveTarget = EntityManager.Heroes.Enemies.Find(x => x.HasBuff("quinnw") && x.IsValidTarget(Q.Range));
                if (passiveTarget != null)
                {
                    Orbwalker.ForcedTarget = passiveTarget;
                }
                else
                {
                    Orbwalker.ForcedTarget = null;
                }

                if (ComboMenu["useQ"].Cast<CheckBox>().CurrentValue && target.Distance(myHero.Position) < Q.Range && Q.IsReady())
                {
                    var prediction = Q.GetPrediction(target);
                    if (prediction.HitChance >= HitChance.High)
                    {
                        Q.Cast(prediction.CastPosition);
                    }

                 else if (Q.IsReady() && target.Distance(myHero.Position) < E2.Range && IsValorMode)

                    {
                        if (prediction.HitChance >= HitChance.High)
                        {
                            Q.Cast(prediction.CastPosition);
                        }

                        return;
                    }
                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void DoHarass()
        {
            try
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                if (target == null)
                {
                    return;
                }

                var passiveTarget = EntityManager.Heroes.Enemies.Find(x => x.HasBuff("quinnw") && x.IsValidTarget(Q.Range));
                if (passiveTarget != null)
                {
                    Orbwalker.ForcedTarget = passiveTarget;
                }
                else
                {
                    Orbwalker.ForcedTarget = null;
                }

                if (HarassMenu["useQ"].Cast<CheckBox>().CurrentValue && target.Distance(myHero.Position) < Q.Range && Q.IsReady())
                {
                    var prediction = Q.GetPrediction(target);
                    if (prediction.HitChance >= HitChance.High)
                    {
                        Q.Cast(prediction.CastPosition);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void LaneClear()
        {
                if (myHero.ManaPercent > LaneMenu["lanem"].Cast<Slider>().CurrentValue)
                {
                    if (Q.IsReady() && LaneMenu["UseQlc"].Cast<CheckBox>().CurrentValue)
                {
                    var allMinions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.ServerPosition, Q.Range).ToList();

                    if (allMinions != null)
                        if (allMinions.FirstOrDefault().IsValidTarget(Q.Range))
                            if (allMinions.Count >= LaneMenu["lcount"].Cast<Slider>().CurrentValue)
                            {
                                foreach (var minion in allMinions)
                                {
                                    Q.Cast(minion);
                                }

                 if (LaneMenu["UseElc"].Cast<CheckBox>().CurrentValue && E.IsReady())
                {
                    foreach (var minion in allMinions)
                    {
                        if (minion.IsValidTarget())
                        {
                            E.Cast(minion);
                        }
                    }
                }
            }
        }
            }
        }

        private static void JungleClear()
        {
            var Mob = EntityManager.MinionsAndMonsters.GetJungleMonsters(myHero.ServerPosition, E.Range).OrderBy(x => x.MaxHealth).ToList();

            if (myHero.ManaPercent > JungleMenu["jgMana"].Cast<Slider>().CurrentValue)
            {
                if (JungleMenu["UseQjg"].Cast<CheckBox>().CurrentValue && Q.IsReady())
                {
                    foreach (var minion in Mob)
                    {
                        if (minion.IsValidTarget())
                        {
                            Q.Cast(minion);
                        }
                    }
                }

                if (JungleMenu["UseEjg"].Cast<CheckBox>().CurrentValue && E.IsReady())
                {
                    foreach (var minion in Mob)
                    {
                        if (minion.IsValidTarget())
                        {
                            E.Cast(minion);
                        }
                    }
                }
            }
        }

        private static void Interrupt(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            try
            {
                if (MiscMenu["interrpt"].Cast<CheckBox>().CurrentValue && E.IsReady())
                {
                    if (sender.IsValidTarget(E.Range))
                    {
                        E.Cast(sender);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        public static double QDamage(Obj_AI_Base target)
        {
            return (new double[] { 20, 45, 70, 95, 120 }[Q.Level - 1] +
                    ((myHero.MaxHealth - (498.48f + (86f * (myHero.Level - 1f)))) * 0.15f)) *
                   ((target.MaxHealth - target.Health) / target.MaxHealth + 1);
        }

        private static void KsQ()
        {
            try
            {
                foreach (
                    var enemy in EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(Q.Range) && !x.IsDead && !x.IsZombie))
                {
                    if (enemy.IsValidTarget(Q.Range) && QDamage(enemy) > enemy.Health)
                    {
                        var prediction = Q.GetPrediction(enemy);
                        if (prediction.HitChance >= HitChance.Medium)
                        {
                            Q.Cast(prediction.CastPosition);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }


        internal static void useitem2(Obj_AI_Base target)
        {
            var RivenServerPosition = myHero.ServerPosition.To2D();
            var targetServerPosition = target.ServerPosition.To2D();

            if (Item.CanUseItem(ItemId.Youmuus_Ghostblade) && myHero.GetAutoAttackRange() > myHero.Distance(target))
            {
                Item.UseItem(ItemId.Youmuus_Ghostblade);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            try
            {
                if (myHero.IsDead)
                {
                    return;
                }

                {
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) DoCombo(); 
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass)) DoHarass();
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear)) LaneClear();
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear)) JungleClear();


                }

                if (KSMenu["ksQ"].Cast<CheckBox>().CurrentValue)
                {
                    KsQ();
                }

                if (MiscMenu["autor"].Cast<CheckBox>().CurrentValue)
                {
                    autoR();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private static void autoR()
        {
            if (myHero.IsInShopRange() && R.Name == "QuinnR")
            {
                R.Cast();
            }
        }

        private static bool IsValorMode
        {
            get
            {
                return ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "QuinnRFinale";
            }
        }

        static void Orbwalker_OnPreAttack(AttackableUnit ff, Orbwalker.PreAttackArgs args)
        {
            try
            {
                if (!myHero.IsMe)
                {
                    return;
                }
                if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo
                    || Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass)
                {
                    if (!(args.Target is AIHeroClient))
                    {
                        return;
                    }

                    var target = EntityManager.Heroes.Enemies.Find(x => x.HasBuff("quinnw") && x.IsValidTarget(Q.Range));
                    if (target == null)
                    {
                        return;
                    }
                    if (myHero.IsInAutoAttackRange(target))
                    {
                        Orbwalker.ForcedTarget = target;
                    }
                }

                if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.LastHit)
                {
                    var minion = args.Target as Obj_AI_Minion;
                    if (minion != null && minion.HasBuff("quinnw"))
                    {
                        Orbwalker.ForcedTarget = minion;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

    }
}