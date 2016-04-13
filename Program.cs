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

        public static Spell.Skillshot Q, W;

        public static Spell.Active R;

        public static Spell.Targeted SmiteSpell;

        public static Menu ComboMenu { get; private set; }

        public static Menu HarassMenu { get; private set; }

        public static Menu MiscMenu { get; private set; }

        public static Menu KSMenu { get; private set; }

        public static Menu JungleMenu { get; private set; }

        public static Menu LaneMenu { get; private set; }

        private static bool Valortome { get { return R.Name == "QuinnR"; } }

        public static AIHeroClient lastTarget;

        public static Vector3 predictedPos;

        public static float lastSeen = Game.Time;

        private static Menu quinnMenu;

        public static readonly string[] SmiteableUnits =
{
            "SRU_Red", "SRU_Blue", "SRU_Dragon", "SRU_Baron",
            "SRU_Gromp", "SRU_Murkwolf", "SRU_Razorbeak",
            "SRU_Krug", "Sru_Crab"
        };

        private static readonly int[] SmiteRed = { 3715, 1415, 1414, 1413, 1412 };

        private static readonly int[] SmiteBlue = { 3706, 1403, 1402, 1401, 1400 };


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
                ComboMenu.Add("youmus", new CheckBox("Use Yoummu"));
                ComboMenu.Add("useitems", new CheckBox("Use Other Items"));
                ComboMenu.Add("useSlowSmite", new CheckBox("KS with Blue Smite"));
                ComboMenu.Add("comboWithDuelSmite", new CheckBox("Combo with Red Smite"));
  
                HarassMenu = quinnMenu.AddSubMenu("Harass");
                HarassMenu.AddGroupLabel("Harass Settings");
                HarassMenu.Add("useQ", new CheckBox("Use Q"));

                LaneMenu = quinnMenu.AddSubMenu("LaneCLear");
                LaneMenu.AddGroupLabel("LaneCLear Settings");
                LaneMenu.Add("UseQlc", new CheckBox("Use Q"));
                LaneMenu.Add("UseElc", new CheckBox("Use E"));
                LaneMenu.AddSeparator();
                LaneMenu.Add("lccount", new Slider("Min minions for Q", 3, 1, 5));
                LaneMenu.Add("lanem", new Slider("Minimum mana %", 20, 0, 100));

                JungleMenu = quinnMenu.AddSubMenu("Jungleclear");
                JungleMenu.AddGroupLabel("Jungleclear Settings");
                JungleMenu.Add("UseQjg", new CheckBox("Use Q"));
                JungleMenu.Add("UseEjg", new CheckBox("Use E"));
                JungleMenu.Add("jgMana", new Slider("Minimum mana %", 20, 0, 100));
                JungleMenu.AddSeparator();
                JungleMenu.Add("smiteActive",
                new KeyBind("Smite Active (toggle)", true, KeyBind.BindTypes.PressToggle, 'H'));
                JungleMenu.AddSeparator();
                JungleMenu.AddSeparator();
                JungleMenu.AddGroupLabel("Camps");
                JungleMenu.AddLabel("Epics");
                JungleMenu.Add("SRU_Baron", new CheckBox("Baron"));
                JungleMenu.Add("SRU_Dragon", new CheckBox("Dragon"));
                JungleMenu.AddLabel("Buffs");
                JungleMenu.Add("SRU_Blue", new CheckBox("Blue"));
                JungleMenu.Add("SRU_Red", new CheckBox("Red"));
                JungleMenu.AddLabel("Small Camps");
                JungleMenu.Add("SRU_Gromp", new CheckBox("Gromp", false));
                JungleMenu.Add("SRU_Murkwolf", new CheckBox("Murkwolf", false));
                JungleMenu.Add("SRU_Krug", new CheckBox("Krug", false));
                JungleMenu.Add("SRU_Razorbeak", new CheckBox("Razerbeak", false));
                JungleMenu.Add("Sru_Crab", new CheckBox("Skuttles", false));

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
                W = new Spell.Skillshot(SpellSlot.W, 2100, SkillShotType.Circular, 0, 5000, 300);
                E = new Spell.Targeted(SpellSlot.E, (int)675f);
                R = new Spell.Active(SpellSlot.R, 550);



                Game.OnUpdate += OnUpdate;
                Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
                Interrupter.OnInterruptableSpell += Interrupt;
                Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
                Orbwalker.OnPostAttack += AfterAA;
            Game.OnUpdate += SmiteEvent;
        }


 


        private static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            try
            {
                if (MiscMenu["antiG"].Cast<CheckBox>().CurrentValue && E.IsReady())
                {
                    if (e.Sender.IsValidTarget(E.Range))
                    {
                        E.Cast(e.Sender);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
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

                if (Valortome && ComboMenu["useQ"].Cast<CheckBox>().CurrentValue && Q.IsReady())
                {
                    var prediction = Q.GetPrediction(target);
                    if (prediction.HitChance >= HitChance.High)
                    {
                        Q.Cast(prediction.CastPosition);
                    }

                    if (ComboMenu["youmus"].Cast<CheckBox>().CurrentValue)
                    {
                        UseItems2(target);
                    }

                    if (ComboMenu["useitems"].Cast<CheckBox>().CurrentValue)
                    {
                        UseItems(target);
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
            var allMinions = EntityManager.MinionsAndMonsters.Get(
                EntityManager.MinionsAndMonsters.EntityType.Minion,
                EntityManager.UnitTeam.Enemy,
                ObjectManager.Player.Position,
                Q.Range,
                false);
            if (allMinions == null)
            {
                return;
            }

            foreach (var minion in allMinions)
            {
                if (LaneMenu["UseQlc"].Cast<CheckBox>().CurrentValue && Q.IsReady())
                {
                    allMinions.Any();
                    {
                        var fl = EntityManager.MinionsAndMonsters.GetLineFarmLocation(allMinions, 100, (int)Q.Range);
                        if (fl.HitNumber >= LaneMenu["lccount"].Cast<Slider>().CurrentValue)
                        {
                            Q.Cast(minion);
                        }

                        if (LaneMenu["UseElc"].Cast<CheckBox>().CurrentValue && E.IsReady())
                        {
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
      }

        private static void JungleClear()

        {
            var minion =
                EntityManager.MinionsAndMonsters.GetJungleMonsters()
                    .Where(x => x.IsValidTarget(W.Range))
                    .OrderByDescending(x => x.MaxHealth)
                    .FirstOrDefault(x => x != null);
            if (minion == null)
            {
                return;
            }

            if (Q.IsReady() && minion.IsValidTarget(Q.Range) && JungleMenu["UseQjg"].Cast<CheckBox>().CurrentValue)
            {
                Q.Cast(minion);
            }


            if (E.IsReady() && minion.IsValidTarget(E.Range) && JungleMenu["UseEjg"].Cast<CheckBox>().CurrentValue)
            {
                E.Cast(minion);
            }
                                                            
        }


        private static void AutoE()
        {
            foreach (var enemy in EntityManager.Heroes.Enemies.Where(a => a.IsValidTarget(2100)))
            {
                if (enemy != null)
                {
                    var tpred = W.GetPrediction(enemy);
                    var tpredcast = tpred.CastPosition.To2D();
                    var flags = NavMesh.GetCollisionFlags(tpredcast);

                    if (flags.HasFlag(CollisionFlags.Grass) && !enemy.VisibleOnScreen)
                    {
                        W.Cast(tpredcast.To3D());
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

                if (ComboMenu["UseW"].Cast<CheckBox>().CurrentValue)
                {
                    AutoE();
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


        public static void SetSmiteSlot()
        {
            SpellSlot smiteSlot;
            if (SmiteBlue.Any(x => myHero.InventoryItems.FirstOrDefault(a => a.Id == (ItemId)x) != null))
                smiteSlot = myHero.GetSpellSlotFromName("s5_summonersmiteplayerganker");
            else if (
                SmiteRed.Any(
                    x => myHero.InventoryItems.FirstOrDefault(a => a.Id == (ItemId)x) != null))
                smiteSlot = myHero.GetSpellSlotFromName("s5_summonersmiteduel");
            else
                smiteSlot = myHero.GetSpellSlotFromName("summonersmite");
            SmiteSpell = new Spell.Targeted(smiteSlot, 500);
        }

        public static int GetSmiteDamage()
        {
            var level = myHero.Level;
            int[] smitedamage =
            {
                20*level + 370,
                30*level + 330,
                40*level + 240,
                50*level + 100
            };
            return smitedamage.Max();
        }

        private static void SmiteEvent(EventArgs args)
        {
            SetSmiteSlot();
            if (!SmiteSpell.IsReady() || myHero.IsDead) return;
            if (JungleMenu["smiteActive"].Cast<KeyBind>().CurrentValue)
            {
                var unit =
                    EntityManager.MinionsAndMonsters.Monsters
                        .Where(
                            a =>
                                SmiteableUnits.Contains(a.BaseSkinName) && a.Health < GetSmiteDamage() &&
                                JungleMenu[a.BaseSkinName].Cast<CheckBox>().CurrentValue)
                        .OrderByDescending(a => a.MaxHealth)
                        .FirstOrDefault();

                if (unit != null)
                {
                    SmiteSpell.Cast(unit);
                    return;
                }
            }
            if (ComboMenu["useSlowSmite"].Cast<CheckBox>().CurrentValue &&
                SmiteSpell.Handle.Name == "s5_summonersmiteplayerganker")
            {
                foreach (
                    var target in
                        EntityManager.Heroes.Enemies
                            .Where(h => h.IsValidTarget(SmiteSpell.Range) && h.Health <= 20 + 8 * myHero.Level))
                {
                    SmiteSpell.Cast(target);
                    return;
                }
            }
            if (ComboMenu["comboWithDuelSmite"].Cast<CheckBox>().CurrentValue &&
                SmiteSpell.Handle.Name == "s5_summonersmiteduel" &&
                Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                foreach (
                    var target in
                        EntityManager.Heroes.Enemies
                            .Where(h => h.IsValidTarget(SmiteSpell.Range)).OrderByDescending(TargetSelector.GetPriority)
                    )
                {
                    SmiteSpell.Cast(target);
                    return;
                }
            }
        }

        internal static void UseItems2(Obj_AI_Base target)
        {
            var RivenServerPosition = myHero.ServerPosition.To2D();
            var targetServerPosition = target.ServerPosition.To2D();

            if (Item.CanUseItem(ItemId.Youmuus_Ghostblade) && myHero.GetAutoAttackRange() > myHero.Distance(target))
            {
                Item.UseItem(ItemId.Youmuus_Ghostblade);
            }
        }

        internal static void UseItems(Obj_AI_Base target)
        {
            var RivenServerPosition = myHero.ServerPosition.To2D();
            var targetServerPosition = target.ServerPosition.To2D();

            if (Item.CanUseItem(ItemId.Ravenous_Hydra_Melee_Only) && 400 > myHero.Distance(target))
            {
                Item.UseItem(ItemId.Ravenous_Hydra_Melee_Only);
            }
            if (Item.CanUseItem(ItemId.Tiamat_Melee_Only) && 400 > myHero.Distance(target))
            {
                Item.UseItem(ItemId.Tiamat_Melee_Only);
            }
            if (Item.CanUseItem(ItemId.Titanic_Hydra) && 400 > myHero.Distance(target))
            {
                Item.UseItem(ItemId.Titanic_Hydra);
            }
            if (Item.CanUseItem(ItemId.Blade_of_the_Ruined_King) && 550 > myHero.Distance(target))
            {
                Item.UseItem(ItemId.Blade_of_the_Ruined_King);
            }

            if (Item.CanUseItem(ItemId.Bilgewater_Cutlass) && 550 > myHero.Distance(target))
            {
                Item.UseItem(ItemId.Bilgewater_Cutlass);
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