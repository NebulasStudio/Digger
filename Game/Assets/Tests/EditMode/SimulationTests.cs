using System;
using System.Linq;
using NUnit.Framework;
using Sandsunder.Domain;
using Sandsunder.Simulation;

namespace Sandsunder.Tests
{
    public sealed class SimulationTests
    {
        [Test]
        public void SeededRngAndDigGridAreDeterministic()
        {
            var first = new DeterministicRng(12345);
            var second = new DeterministicRng(12345);
            var sequenceA = Enumerable.Range(0, 64).Select(_ => first.NextUInt()).ToArray();
            var sequenceB = Enumerable.Range(0, 64).Select(_ => second.NextUInt()).ToArray();

            Assert.That(sequenceB, Is.EqualTo(sequenceA));

            var loot = new[] { "weapon.shovel", "heal.small", "utility.dash" };
            var gridA = new DigGrid(8, 8, 9981, loot);
            var gridB = new DigGrid(8, 8, 9981, loot);
            for (var y = 0; y < 8; y++)
            for (var x = 0; x < 8; x++)
                Assert.That(gridB.Dig(new GridCell(x, y)).RevealedLootId,
                    Is.EqualTo(gridA.Dig(new GridCell(x, y)).RevealedLootId));
        }

        [Test]
        public void SameSeedAndCommandsProduceSameReplayChecksum()
        {
            var first = CreateMatch();
            var second = CreateMatch();

            foreach (var match in new[] { first, second })
            {
                var player = AddPlayer(match, 1);
                AddPlayer(match, 2);
                match.AdvanceToTick(3);
                match.AwardRitualSeal(player, "outer");
                match.AdvanceToTick(7);
                match.RecordMilestone(player, "objective:test");
            }

            Assert.That(SimulationStateHasher.Compute(second, CreateGrid()), Is.EqualTo(SimulationStateHasher.Compute(first, CreateGrid())));
        }

        [Test]
        public void ReplayChecksumDetectsHiddenObjectiveDivergence()
        {
            var first = CreateMatch();
            var second = CreateMatch();
            var firstPlayer = AddPlayer(first, 1);
            var secondPlayer = AddPlayer(second, 1);
            AddPlayer(first, 2);
            AddPlayer(second, 2);
            first.AwardRitualSeal(firstPlayer, "outer");
            second.AwardRitualSeal(secondPlayer, "elite");

            Assert.That(first.Players.First().ObjectiveMilestones, Is.EqualTo(second.Players.First().ObjectiveMilestones));
            Assert.That(SimulationStateHasher.Compute(first, CreateGrid()), Is.Not.EqualTo(SimulationStateHasher.Compute(second, CreateGrid())));
        }

        [Test]
        public void ReplayChecksumAlwaysIncludesDigGrid()
        {
            var match = CreateMatch();
            AddPlayer(match, 1);
            AddPlayer(match, 2);
            var untouched = CreateGrid();
            var dug = CreateGrid();
            dug.Dig(new GridCell(0, 0));

            Assert.That(SimulationStateHasher.Compute(match, untouched), Is.Not.EqualTo(SimulationStateHasher.Compute(match, dug)));
            Assert.Throws<ArgumentNullException>(() => SimulationStateHasher.Compute(match, null));
        }

        [Test]
        public void MatchTransitionsThroughCenterSuddenDeathAndTimeout()
        {
            var match = CreateMatch(centerTick: 5, endTick: 20);
            AddPlayer(match, 1);
            AddPlayer(match, 2);

            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Preparation));
            match.AdvanceToTick(5);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.CenterOpen));
            match.AdvanceToTick(15);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.SuddenDeath));
            match.AdvanceToTick(20);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Completed));
            Assert.That(match.Outcome.Value.Condition, Is.EqualTo(WinCondition.ObjectiveTimeout));
            Assert.That(match.Outcome.Value.CompletedTick, Is.EqualTo(20));
        }

        [Test]
        public void HiddenLootIsAbsentUntilServerRevealsCell()
        {
            var grid = new DigGrid(4, 4, 7, new[] { "loot.only" }, emptyWeight: 0);
            var cell = new GridCell(2, 1);

            Assert.That(grid.CreatePublicSnapshot().All(view => view.RevealedLootId == null), Is.True);
            var result = grid.Dig(cell);

            Assert.That(result.Changed, Is.True);
            Assert.That(result.NewDepth, Is.EqualTo(1));
            Assert.That(grid.GetPublicCell(cell).RevealedLootId, Is.Not.Null);
        }

        [Test]
        public void MultiLayerDiggingAndSubterraneanTunnelsBehaveAsExpected()
        {
            var grid = new DigGrid(4, 4, 12345, new[] { "key.subterranean", "weapon.ancient" }, emptyWeight: 0);
            var cell = new GridCell(0, 0);

            // First dig: Surface -> Level 1
            var firstResult = grid.Dig(cell);
            Assert.That(firstResult.Changed, Is.True);
            Assert.That(firstResult.NewDepth, Is.EqualTo(1));

            // Second dig: Level 1 -> Level 2 (if special tell exists) or restricted
            var publicCell = grid.GetPublicCell(cell);
            var secondResult = grid.Dig(cell);

            if (publicCell.HasSpecialTell)
            {
                Assert.That(secondResult.Changed, Is.True);
                Assert.That(secondResult.NewDepth, Is.EqualTo(2));
                Assert.That(secondResult.IsTunnel, Is.True);

                // Third dig: Max depth reached
                var thirdResult = grid.Dig(cell);
                Assert.That(thirdResult.Changed, Is.False);
            }
            else
            {
                Assert.That(secondResult.Changed, Is.False);
                Assert.That(secondResult.NewDepth, Is.EqualTo(1));
            }
        }

        [Test]
        public void RitualRaceCompletesOnlyAfterPrerequisitesAndChannel()
        {
            var match = CreateMatch(ritualTicks: 2);
            var player = AddPlayer(match, 1);
            AddPlayer(match, 2);

            match.AwardRitualSeal(player, "outer");
            match.AwardRitualSeal(player, "elite");
            match.AwardRitualSeal(player, "guardian");
            match.ActivateRitualStation(player, 0);
            match.ActivateRitualStation(player, 1);

            match.AdvanceToTick(10);
            Assert.That(match.AdvanceRitualChannel(player), Is.False);
            Assert.That(match.AdvanceRitualChannel(player), Is.False, "Only one channel step is accepted per tick.");
            match.AdvanceToTick(11);
            Assert.That(match.AdvanceRitualChannel(player), Is.True);
            Assert.That(match.Outcome, Is.Null, "Wins are arbitrated at the end of the authoritative tick.");
            match.FinalizeCurrentTick();
            Assert.That(match.Outcome.Value.Condition, Is.EqualTo(WinCondition.RitualRace));
            Assert.That(match.Outcome.Value.Winner, Is.EqualTo(player));
        }

        [Test]
        public void RelicExtractionRequiresGuardianRelicAndExit()
        {
            var match = CreateMatch();
            var player = AddPlayer(match, 1);
            AddPlayer(match, 2);

            match.AdvanceToTick(10);
            Assert.That(match.ClaimRelic(player), Is.False);
            match.DefeatGuardian(player);
            Assert.That(match.ClaimRelic(player), Is.True);
            Assert.That(match.ExtractRelic(player, exitId: 1), Is.True);
            match.FinalizeCurrentTick();
            Assert.That(match.Outcome.Value.Condition, Is.EqualTo(WinCondition.RelicExtraction));
        }

        [Test]
        public void LastPermanentSurvivorWins()
        {
            var match = CreateMatch(centerTick: 5);
            var survivor = AddPlayer(match, 1);
            var defeated = AddPlayer(match, 2);
            match.AdvanceToTick(5);

            Assert.That(match.Eliminate(defeated, "elim:1"), Is.EqualTo(EliminationResult.PermanentlyEliminated));
            match.FinalizeCurrentTick();
            Assert.That(match.Outcome.Value.Winner, Is.EqualTo(survivor));
            Assert.That(match.Outcome.Value.Condition, Is.EqualTo(WinCondition.LastSurvivor));
        }

        [Test]
        public void RespawnIsAvailableOnceAndOnlyBeforeCenterOpens()
        {
            var match = CreateMatch(centerTick: 5);
            var player = AddPlayer(match, 1);
            AddPlayer(match, 2);

            Assert.That(match.Eliminate(player, "elim:first"), Is.EqualTo(EliminationResult.Respawned));
            Assert.That(match.Eliminate(player, "elim:first"), Is.EqualTo(EliminationResult.Ignored));
            Assert.That(match.Players.First(state => state.Id == player).RespawnsRemaining, Is.Zero);
            Assert.That(match.Eliminate(player, "elim:distinct-during-wait"), Is.EqualTo(EliminationResult.Ignored));
            match.AdvanceToTick(2);
            Assert.That(match.Eliminate(player, "elim:distinct-during-wait"), Is.EqualTo(EliminationResult.Ignored));
            Assert.That(match.Eliminate(player, "elim:second"), Is.EqualTo(EliminationResult.PermanentlyEliminated));

            var other = CreateMatch(centerTick: 5);
            var lateDeath = AddPlayer(other, 1);
            AddPlayer(other, 2);
            other.AdvanceToTick(5);
            Assert.That(other.Eliminate(lateDeath, "elim:late"), Is.EqualTo(EliminationResult.PermanentlyEliminated));
        }

        [Test]
        public void AwaitingRespawnIsInactiveAndStillPreventsLastSurvivorWin()
        {
            var match = CreateMatch(centerTick: 5, respawnTicks: 8);
            var waiting = AddPlayer(match, 1);
            var eliminated = AddPlayer(match, 2);
            AddPlayer(match, 3);

            Assert.That(match.Eliminate(waiting, "elim:waiting"), Is.EqualTo(EliminationResult.Respawned));
            var state = match.Players.First(player => player.Id == waiting);
            Assert.That(state.IsAlive, Is.False);
            Assert.That(state.AwaitingRespawn, Is.True);
            Assert.That(state.RespawnAtTick, Is.EqualTo(8));
            Assert.Throws<InvalidOperationException>(() => match.RecordMilestone(waiting, "blocked"));
            Assert.That(match.Eliminate(waiting, "elim:other"), Is.EqualTo(EliminationResult.Ignored));

            match.AdvanceToTick(5);
            match.Eliminate(eliminated, "elim:permanent");
            Assert.That(match.Outcome, Is.Null, "Awaiting respawn remains a Last Survivor contender.");
            match.AdvanceToTick(8);
            Assert.That(state.IsAlive, Is.True);
            Assert.That(state.AwaitingRespawn, Is.False);
            Assert.That(state.RespawnAtTick, Is.EqualTo(-1));
        }

        [Test]
        public void TimeoutUsesMilestonesThenEarliestLatestMilestone()
        {
            var match = CreateMatch(endTick: 20);
            var early = AddPlayer(match, 9);
            var late = AddPlayer(match, 2);

            match.AdvanceToTick(3);
            match.RecordMilestone(early, "objective:first");
            match.AdvanceToTick(7);
            match.RecordMilestone(late, "objective:first");
            match.AdvanceToTick(20);

            Assert.That(match.Outcome.Value.Winner, Is.EqualTo(early));
            Assert.That(match.Outcome.Value.Condition, Is.EqualTo(WinCondition.ObjectiveTimeout));
        }

        [Test]
        public void EliminatedPlayerRemainsEligibleForObjectiveTimeout()
        {
            var match = CreateMatch(centerTick: 5, endTick: 20);
            var leader = AddPlayer(match, 1);
            AddPlayer(match, 2);
            AddPlayer(match, 3);
            match.RecordMilestone(leader, "objective:lead");
            match.AdvanceToTick(5);
            match.Eliminate(leader, "elim:leader");
            match.AdvanceToTick(20);

            Assert.That(match.Outcome.Value.Winner, Is.EqualTo(leader));
            Assert.That(match.Outcome.Value.Condition, Is.EqualTo(WinCondition.ObjectiveTimeout));
        }

        [Test]
        public void ExactTimeoutTieUsesSeededSeatPriority()
        {
            var match = CreateMatch(endTick: 30, mapSeed: 1);
            AddPlayer(match, 10);
            var seededWinner = AddPlayer(match, 20);
            match.AdvanceToTick(30);

            Assert.That(match.Outcome.Value.Winner, Is.EqualTo(seededWinner));
        }

        [Test]
        public void MilestoneIdsAreIdempotent()
        {
            var match = CreateMatch();
            var player = AddPlayer(match, 1);
            AddPlayer(match, 2);

            Assert.That(match.RecordMilestone(player, "ruin:first"), Is.True);
            Assert.That(match.RecordMilestone(player, "ruin:first"), Is.False);
            Assert.That(match.Players.First(state => state.Id == player).ObjectiveMilestones, Is.EqualTo(1));
        }

        [Test]
        public void RitualInterruptWinsInEitherSameTickOrderAndRequiresContinuousTicks()
        {
            var match = CreateMatch(ritualTicks: 2);
            var player = AddPlayer(match, 1);
            AddPlayer(match, 2);
            PrepareRitual(match, player);
            match.AdvanceToTick(10);

            Assert.That(match.AdvanceRitualChannel(player), Is.False);
            Assert.That(match.AdvanceRitualChannel(player, interrupted: true), Is.False);
            Assert.That(match.AdvanceRitualChannel(player), Is.False);
            match.AdvanceToTick(11);
            Assert.That(match.AdvanceRitualChannel(player), Is.False);
            match.AdvanceToTick(12);
            Assert.That(match.AdvanceRitualChannel(player), Is.True);
            match.AdvanceRitualChannel(player, interrupted: true);
            Assert.That(match.FinalizeCurrentTick(), Is.False, "A same-tick interrupt cancels ritual completion.");
            Assert.That(match.Outcome, Is.Null);

            var reverse = CreateMatch(ritualTicks: 2);
            var reversePlayer = AddPlayer(reverse, 1);
            AddPlayer(reverse, 2);
            PrepareRitual(reverse, reversePlayer);
            reverse.AdvanceToTick(10);
            reverse.AdvanceRitualChannel(reversePlayer, interrupted: true);
            Assert.That(reverse.AdvanceRitualChannel(reversePlayer), Is.False);
        }

        [Test]
        public void SameTickVictoryUsesSeededSeatPriorityNotCommandOrder()
        {
            var ritualFirst = CreateSimultaneousVictoryMatch(ritualFirst: true);
            var relicFirst = CreateSimultaneousVictoryMatch(ritualFirst: false);
            var differentSeed = CreateSimultaneousVictoryMatch(ritualFirst: false, mapSeed: 1);

            Assert.That(ritualFirst.Outcome.Value.Winner, Is.EqualTo(relicFirst.Outcome.Value.Winner));
            Assert.That(ritualFirst.Outcome.Value.Condition, Is.EqualTo(relicFirst.Outcome.Value.Condition));
            Assert.That(differentSeed.Outcome.Value.Winner, Is.Not.EqualTo(ritualFirst.Outcome.Value.Winner));
        }

        [Test]
        public void MvpTimingMatchesGddAndPublicIdentityContainsNoSeed()
        {
            var rules = MatchRules.MvpDefault;
            Assert.That(rules.CenterOpenTick, Is.EqualTo(6L * 60 * rules.Tick.TickRate));
            Assert.That(rules.RitualChannelTicks, Is.EqualTo(8 * rules.Tick.TickRate));
            Assert.That(rules.SuddenDeathRitualChannelTicks, Is.EqualTo(5 * rules.Tick.TickRate));
            Assert.That(rules.RespawnDelayTicks, Is.EqualTo(8 * rules.Tick.TickRate));
            Assert.That(typeof(MatchIdentity).GetProperty("MapSeed"), Is.Null);
        }

        private static PlayerId AddPlayer(MatchSimulation match, int id)
        {
            var player = new PlayerId(id);
            match.AddPlayer(player);
            return player;
        }

        private static DigGrid CreateGrid() => new DigGrid(2, 2, 123, new[] { "loot.test" });

        private static void PrepareRitual(MatchSimulation match, PlayerId player)
        {
            match.AwardRitualSeal(player, "outer");
            match.AwardRitualSeal(player, "elite");
            match.AwardRitualSeal(player, "guardian");
            match.ActivateRitualStation(player, 0);
            match.ActivateRitualStation(player, 1);
        }

        private static MatchSimulation CreateSimultaneousVictoryMatch(bool ritualFirst, ulong mapSeed = 987654321)
        {
            var rules = new MatchRules(FixedTickConfig.CompetitiveDefault, 2, 20, 30,
                requiredSeals: 1, requiredStations: 1, ritualChannelTicks: 1,
                suddenDeathRitualChannelTicks: 1);
            var match = new MatchSimulation(
                new AuthoritativeMatchIdentity(new MatchIdentity("tie", "dev", "rules-v1"), mapSeed), rules);
            var ritual = AddPlayer(match, 10);
            var relic = AddPlayer(match, 20);
            match.AwardRitualSeal(ritual, "seal");
            match.ActivateRitualStation(ritual, 0);
            match.AdvanceToTick(2);
            match.DefeatGuardian(relic);
            match.ClaimRelic(relic);
            if (ritualFirst)
            {
                match.AdvanceRitualChannel(ritual);
                match.ExtractRelic(relic, 0);
            }
            else
            {
                match.ExtractRelic(relic, 0);
                match.AdvanceRitualChannel(ritual);
            }
            match.FinalizeCurrentTick();
            return match;
        }

        private static MatchSimulation CreateMatch(
            long centerTick = 10,
            long endTick = 30,
            int ritualTicks = 4,
            int respawnTicks = 2,
            ulong mapSeed = 123)
        {
            var rules = new MatchRules(
                FixedTickConfig.CompetitiveDefault,
                centerTick,
                suddenDeathTick: endTick - 5,
                matchEndTick: endTick,
                ritualChannelTicks: ritualTicks,
                suddenDeathRitualChannelTicks: ritualTicks,
                respawnDelayTicks: respawnTicks);
            return new MatchSimulation(
                new AuthoritativeMatchIdentity(new MatchIdentity("match-test", "dev", "rules-v1"), mapSeed), rules);
        }
    }
}
