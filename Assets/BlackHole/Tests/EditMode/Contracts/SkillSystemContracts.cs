using System;
using System.Collections.Generic;

namespace BlackHole.Core.Tests
{
    internal static class SkillSystemContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Skills.LoadIndependentlyOfGameContent", LoadIndependentlyOfGameContent);
            yield return new Contract("Skills.RejectInvalidStandaloneContent", RejectInvalidStandaloneContent);
            yield return new Contract("Stats.AddAndRateAreOrderIndependent", AddAndRateAreOrderIndependent);
            yield return new Contract("Stats.RejectUnknownOperationAndNonFinite", RejectUnknownOperationAndNonFinite);
            yield return new Contract("Stats.IntegerAndOperationConstraints", IntegerAndOperationConstraints);
            yield return new Contract("Graph.CycleAndEitherNeighbor", CycleAndEitherNeighbor);
            yield return new Contract("Graph.RejectInvalidConnections", RejectInvalidConnections);
            yield return new Contract("Graph.UnlockCannotBeBypassed", UnlockCannotBeBypassed);
            yield return new Contract("Loadout.NodeOrderDoesNotChangeBattle", NodeOrderDoesNotChangeBattle);
            yield return new Contract("Loadout.FourPlayersRemainIndependent", FourPlayersRemainIndependent);
            yield return new Contract("Layout.IsSeparateFromPurchase", LayoutIsSeparateFromPurchase);
            yield return new Contract("Stats.NewSkillNeedsNoUpgradeBranch", NewSkillNeedsNoUpgradeBranch);
        }

        private static void LoadIndependentlyOfGameContent()
        {
            SkillLoadResult result = SkillContentLoader.Load(
                new[] { TestContent.Laser("laser", 2, 3, 0.5f, 0.2f),
                    new SkillData { Id = TestContent.SkillId, Kind = "Breaker", Radius = 1, Interval = 1, Damage = 2 } },
                new[] { TestContent.SkillId });
            Expect.True(result.Succeeded, "다른 gameplay 필드 없이 스킬만 검증한다.");
            Expect.Equal(0, result.Diagnostics.Count);
            Expect.True(result.Content.TryGetSkill("laser", out PassiveSkillDefinition laser), "ID로 Skill을 찾는다.");
            Expect.Near(3, laser.ComputeStats(Array.Empty<StatModifier>())["Damage"]);
            Expect.Equal(TestContent.SkillId, result.Content.StartingSkills[0]);
            Expect.True(!result.Content.TryGetSkill("missing", out _), "없는 ID는 찾지 못한다.");
        }

        private static void RejectInvalidStandaloneContent()
        {
            SkillData invalid = new SkillData { Id = "broken", Kind = "Breaker", Radius = 1, Interval = -1, Damage = 2 };
            SkillLoadResult result = SkillContentLoader.Load(
                new[] { TestContent.Laser("same", 1, 2, 1, 0.2f),
                    TestContent.Laser("same", 1, 2, 1, 0.2f), invalid },
                new[] { "absent", "absent" });
            Expect.True(!result.Succeeded && result.Content == null, "오류가 있는 스킬 묶음은 실행할 수 없다.");
            Expect.True(result.Diagnostics.Count >= 3, "수치, 중복 ID, 시작 Skill 참조를 검사한다.");
            Expect.True(!SkillContentLoader.Load(null, null).Succeeded, "필수 목록 누락을 거부한다.");
        }

        private static void AddAndRateAreOrderIndependent()
        {
            var definition = new BreakerSkillDefinition("breaker", new BreakerStats(1, 1, 10));
            var modifiers = new[] { new StatModifier("Damage", StatOperation.Rate, 0.5f),
                new StatModifier("Damage", StatOperation.Add, 2), new StatModifier("Damage", StatOperation.Rate, 0.25f) };
            Expect.Near(21, definition.ComputeStats(modifiers)["Damage"]);
            Array.Reverse(modifiers);
            Expect.Near(21, definition.ComputeStats(modifiers)["Damage"]);
            Expect.Near(10, definition.BaseStats.Damage);
            Expect.Throws<ArgumentException>(() => definition.ComputeStats(new[] { new StatModifier("Width", StatOperation.Add, 1) }));
            Expect.Throws<ArgumentOutOfRangeException>(() => definition.ComputeStats(new[] { new StatModifier("AttackSpeed", StatOperation.Rate, -1) }));
        }

        private static void RejectUnknownOperationAndNonFinite()
        {
            ContentData data = Market();
            data.Upgrades[0].Effects[0].Operation = "0";
            TestContent.HasDiagnostic(ContentLoader.Load(data), "Upgrades[root].Effects[0].Operation", "Add");
            data.Upgrades[0].Effects[0].Operation = "Add";
            data.Upgrades[0].Effects[0].Value = float.NaN;
            Expect.True(!ContentLoader.Load(data).Succeeded, "NaN 보정은 실패해야 한다.");
        }

        private static void IntegerAndOperationConstraints()
        {
            var catalog = new StatCatalog(new StatDefinition("Chains", "연쇄 수", "count", 1, 10, StatValueKind.Integer, StatOperation.Add));
            var initial = new Dictionary<string, float> { ["Chains"] = 3 };
            Expect.Near(5, catalog.Compute(initial, new[] { new StatModifier("Chains", StatOperation.Add, 2) })["Chains"]);
            Expect.Throws<ArgumentException>(() => catalog.Compute(initial, new[] { new StatModifier("Chains", StatOperation.Rate, 1) }));
            Expect.Throws<ArgumentOutOfRangeException>(() => catalog.Compute(initial, new[] { new StatModifier("Chains", StatOperation.Add, 0.5f) }));
        }

        private static ContentData Market()
        {
            ContentData data = TestContent.Data();
            data.Enemies[0].MaxHealth = 1;
            data.Enemies[0].Gold = 100;
            data.Enemies[0].MoveSpeed = 0.0001f;
            data.Upgrades = new List<UpgradeData> {
                TestContent.Upgrade("root", 1, null, TestContent.Stat("Damage", "Add", 2, TestContent.SkillId)),
                TestContent.Upgrade("left", 1, "root", TestContent.Stat("Damage", "Rate", 0.5f, TestContent.SkillId)),
                TestContent.Upgrade("right", 1, "root", TestContent.Stat("Damage", "Rate", 0.25f, TestContent.SkillId)),
                TestContent.Upgrade("end", 1, "left", TestContent.Stat("AttackSpeed", "Rate", 1, TestContent.SkillId)) };
            data.Upgrades[3].Connections.Add("right");
            return data;
        }

        private static PlayerState Earn(GameContent content, PlayerId id)
        {
            var state = new PlayerState(id);
            GameSession battle = SessionAssembler.CreateBattle(content, new[] { state });
            battle.SetAimPoint(id, new Point2(3, 0));
            battle.Advance(0.01f);
            battle.Stop();
            Expect.Equal(100, state.Gold);
            return state;
        }

        private static void CycleAndEitherNeighbor()
        {
            GameContent content = TestContent.Load(Market());
            foreach (string neighbor in new[] { "left", "right" })
            {
                PlayerState state = Earn(content, TestContent.First);
                Expect.Equal(UpgradeNodeState.Hidden, UpgradePurchase.GetState(state, content, "end"));
                Expect.Equal(PurchaseResult.Purchased, UpgradePurchase.TryPurchase(state, content, "root"));
                Expect.Equal(PurchaseResult.Purchased, UpgradePurchase.TryPurchase(state, content, neighbor));
                Expect.Equal(UpgradeNodeState.Purchasable, UpgradePurchase.GetState(state, content, "end"));
                Expect.Equal(PurchaseResult.Purchased, UpgradePurchase.TryPurchase(state, content, "end"));
                Expect.Equal(UpgradeNodeState.Owned, UpgradePurchase.GetState(state, content, "end"));
            }
            var empty = new PlayerState(TestContent.First);
            Expect.Equal(UpgradeNodeState.Revealed, UpgradePurchase.GetState(empty, content, "root"));
        }

        private static void RejectInvalidConnections()
        {
            foreach (string target in new[] { "ghost", "end", "left" })
            {
                ContentData data = Market();
                data.Upgrades[3].Connections.Add(target);
                Expect.True(!ContentLoader.Load(data).Succeeded, "오타·자기 연결·중복 선을 거부한다.");
            }
            ContentData disconnected = Market();
            disconnected.Upgrades[0].IsStart = false;
            Expect.True(!ContentLoader.Load(disconnected).Succeeded, "시작점 없는 그래프를 거부한다.");
        }

        private static void UnlockCannotBeBypassed()
        {
            ContentData data = Market();
            data.Skills.Add(TestContent.Laser(TestContent.LaserId, 1, 5, 1, 0.2f));
            data.Upgrades[1].Effects = new List<UpgradeEffectData> { new UpgradeEffectData { Kind = "SkillUnlock", Target = TestContent.LaserId } };
            data.Upgrades[3].Effects = new List<UpgradeEffectData> { TestContent.Stat("Width", "Add", 1, TestContent.LaserId) };
            Expect.True(!ContentLoader.Load(data).Succeeded, "right 경로는 해금을 우회한다.");
            data.Upgrades[3].Connections.Remove("right");
            Expect.True(ContentLoader.Load(data).Succeeded, "모든 경로가 해금을 지나면 통과한다.");
        }

        private static void NodeOrderDoesNotChangeBattle()
        {
            ContentData data = Market();
            GameContent first = TestContent.Load(data);
            PlayerState a = Earn(first, TestContent.First);
            foreach (string id in new[] { "root", "left", "right", "end" }) UpgradePurchase.TryPurchase(a, first, id);
            data.Upgrades.Reverse();
            GameContent second = TestContent.Load(data);
            PlayerState b = Earn(second, TestContent.First);
            foreach (string id in new[] { "root", "right", "end", "left" }) UpgradePurchase.TryPurchase(b, second, id);
            GameSession one = SessionAssembler.CreateBattle(first, new[] { a });
            GameSession two = SessionAssembler.CreateBattle(second, new[] { b });
            Expect.Near(5.25f, TestContent.Breaker(one.World.Players[0]).Stats.Damage);
            Expect.Near(TestContent.Breaker(one.World.Players[0]).Stats.Damage, TestContent.Breaker(two.World.Players[0]).Stats.Damage);
            one.SetAimPoint(a.Id, new Point2(3, 0)); two.SetAimPoint(b.Id, new Point2(3, 0));
            for (int i = 0; i < 200; i++) { one.Advance(0.1f); two.Advance(0.1f); }
            Expect.Equal(a.Gold, b.Gold);
            Expect.Equal(TestContent.Breaker(one.World.Players[0]).TickCount, TestContent.Breaker(two.World.Players[0]).TickCount);
        }

        private static void FourPlayersRemainIndependent()
        {
            ContentData data = Market();
            GameContent rewarded = TestContent.Load(data);
            var states = new PlayerState[4];
            for (int i = 0; i < 4; i++) states[i] = Earn(rewarded, new PlayerId(i + 1));
            UpgradePurchase.TryPurchase(states[0], rewarded, "root");
            data.Enemies[0].Gold = 0;
            GameSession battle = SessionAssembler.CreateBattle(TestContent.Load(data), states);
            Expect.Near(3, TestContent.Breaker(battle.World.Players[0]).Stats.Damage);
            for (int i = 1; i < 4; i++) Expect.Near(1, TestContent.Breaker(battle.World.Players[i]).Stats.Damage);
        }

        private static void LayoutIsSeparateFromPurchase()
        {
            GameContent content = TestContent.Load(Market());
            var layout = new UpgradeLayout();
            for (int i = 0; i < content.Upgrades.Count; i++) layout.Nodes.Add(new UpgradeNodeDisplay { NodeId = content.Upgrades[i].Id, X = i });
            Expect.Equal(0, layout.Validate(content).Count);
            layout.Nodes[1].X = 0;
            Expect.Equal(1, layout.Validate(content).Count);
            PlayerState state = Earn(content, TestContent.First);
            Expect.Equal(PurchaseResult.Purchased, UpgradePurchase.TryPurchase(state, content, "root"));
        }

        // 테스트용 새 종류. 노드·그래프·Loadout에는 이 종류를 위한 분기가 없다.
        private sealed class PulseDefinition : PassiveSkillDefinition
        {
            public PulseDefinition() : base("pulse") { }
            public override StatCatalog Stats { get; } = new StatCatalog(new StatDefinition("Power", "힘", "units", 1, 100));
            public override IReadOnlyDictionary<string, float> BaseValues => new Dictionary<string, float> { ["Power"] = 2 };
            internal override PassiveSkill Create(Player owner, StatValues values) => new Pulse(this, owner, values["Power"]);
        }
        private sealed class Pulse : PassiveSkill
        {
            public float Power { get; }
            public Pulse(PassiveSkillDefinition definition, Player owner, float power) : base(definition, owner) { Power = power; }
            internal override void Advance(float delta, World world) { }
        }
        private static void NewSkillNeedsNoUpgradeBranch()
        {
            var definition = new PulseDefinition();
            var effect = new UpgradeEffect(UpgradeEffectKind.SkillStat, 3, definition, modifier: new StatModifier("Power", StatOperation.Add, 3));
            var node = new UpgradeNodeDefinition("power", 1, new[] { effect });
            GameContent baseline = TestContent.Load(Market());
            var content = new GameContent(baseline.TimeLimit, baseline.Hq, baseline.Enemies, baseline.Spawn,
                baseline.StartSupply, baseline.Growth, new PassiveSkillDefinition[] { definition }, new[] { "pulse" }, new[] { node },
                new UpgradeGraph(new[] { node }, new[] { "power" }, Array.Empty<UpgradeEdge>()));
            var state = new PlayerState(TestContent.First);
            state.EarnGold(1);
            UpgradePurchase.TryPurchase(state, content, "power");
            var battle = SessionAssembler.CreateBattle(content, new[] { state });
            Expect.Near(5, ((Pulse)battle.World.Players[0].Skills[0]).Power);
        }
    }
}
