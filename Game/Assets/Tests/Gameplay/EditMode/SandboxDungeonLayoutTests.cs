using System.Collections.Generic;
using NUnit.Framework;
using Sandsunder.Domain;
using Sandsunder.Simulation;

namespace Sandsunder.Tests.Gameplay
{
    public sealed class SandboxDungeonLayoutTests
    {
        [Test]
        public void SameSeedAndSurfaceCell_ProduceSamePathAndStateHash()
        {
            GridCell surfaceCell = new GridCell(13, 7);
            SandboxDungeonLayout first = SandboxDungeonLayout.Create(0xD16A5EUL, surfaceCell);
            SandboxDungeonLayout second = SandboxDungeonLayout.Create(0xD16A5EUL, surfaceCell);

            Assert.That(second.TemplateIndex, Is.EqualTo(first.TemplateIndex));
            Assert.That(second.OrderedPathCells, Is.EqualTo(first.OrderedPathCells));
            Assert.That(second.OrderedSegments, Is.EqualTo(first.OrderedSegments));
            Assert.That(second.ComputeStateHash(), Is.EqualTo(first.ComputeStateHash()));
        }

        [Test]
        public void SeedAndSurfaceCoordinateSelection_ReachesEveryVersionedTemplate()
        {
            var selectedTemplates = new HashSet<int>();
            for (ulong seed = 0; seed < 32; seed++)
            {
                for (int coordinate = -8; coordinate <= 8; coordinate++)
                {
                    selectedTemplates.Add(SandboxDungeonLayout.SelectTemplateIndex(
                        seed,
                        new GridCell(coordinate, 3 - coordinate)));
                }
            }

            Assert.That(selectedTemplates, Has.Count.EqualTo(SandboxDungeonLayout.TemplateCount));
        }

        [Test]
        public void EveryTemplate_IsContiguousUniqueAndConfined()
        {
            Dictionary<int, SandboxDungeonLayout> layouts = FindAllTemplates();
            Assert.That(layouts, Has.Count.EqualTo(SandboxDungeonLayout.TemplateCount));

            foreach (SandboxDungeonLayout layout in layouts.Values)
            {
                Assert.That(layout.TryValidate(out string failure), Is.True, failure);
                Assert.That(layout.OrderedSegments, Has.Count.EqualTo(layout.OrderedPathCells.Count - 1));

                var uniqueCells = new HashSet<GridCell>();
                for (int index = 0; index < layout.OrderedPathCells.Count; index++)
                {
                    GridCell cell = layout.OrderedPathCells[index];
                    Assert.That(uniqueCells.Add(cell), Is.True, $"Template {layout.TemplateIndex} repeats {cell}.");
                    Assert.That(System.Math.Abs((long)cell.X), Is.LessThanOrEqualTo(SandboxDungeonLayout.MaximumLocalCoordinateMagnitude));
                    Assert.That(System.Math.Abs((long)cell.Y), Is.LessThanOrEqualTo(SandboxDungeonLayout.MaximumLocalCoordinateMagnitude));
                    Assert.That(layout.ContainsWalkableCell(cell), Is.True);

                    if (index == 0)
                        continue;

                    GridCell previous = layout.OrderedPathCells[index - 1];
                    long distance = System.Math.Abs((long)cell.X - previous.X)
                        + System.Math.Abs((long)cell.Y - previous.Y);
                    Assert.That(distance, Is.EqualTo(1),
                        $"Template {layout.TemplateIndex} breaks between {previous} and {cell}.");

                    SandboxDungeonSegment segment = layout.OrderedSegments[index - 1];
                    Assert.That(segment.FromCell, Is.EqualTo(previous));
                    Assert.That(segment.ToCell, Is.EqualTo(cell));
                }
            }
        }

        [Test]
        public void ChestIsTerminalAndReturnUsesEntrance()
        {
            foreach (SandboxDungeonLayout layout in FindAllTemplates().Values)
            {
                Assert.That(layout.EntranceCell, Is.EqualTo(layout.OrderedPathCells[0]));
                Assert.That(layout.ReturnCell, Is.EqualTo(layout.EntranceCell));
                Assert.That(layout.ExitCell, Is.EqualTo(layout.EntranceCell));
                Assert.That(layout.ChestEndpointCell, Is.EqualTo(layout.OrderedPathCells[layout.OrderedPathCells.Count - 1]));
                Assert.That(layout.ChestEndpointCell, Is.Not.EqualTo(layout.EntranceCell));

                int adjacentWalkableCells = 0;
                foreach (GridCell cell in layout.OrderedPathCells)
                {
                    long distance = System.Math.Abs((long)cell.X - layout.ChestEndpointCell.X)
                        + System.Math.Abs((long)cell.Y - layout.ChestEndpointCell.Y);
                    if (distance == 1)
                        adjacentWalkableCells++;
                }

                Assert.That(adjacentWalkableCells, Is.EqualTo(1),
                    $"Template {layout.TemplateIndex} chest endpoint must have exactly one tunnel connection.");
            }
        }

        [Test]
        public void StateHashIncludesSurfaceEntranceIdentity()
        {
            const ulong seed = 991UL;
            GridCell firstSurfaceCell = new GridCell(4, 5);
            int template = SandboxDungeonLayout.SelectTemplateIndex(seed, firstSurfaceCell);

            GridCell secondSurfaceCell = FindDifferentCellWithTemplate(seed, template, firstSurfaceCell);
            SandboxDungeonLayout first = SandboxDungeonLayout.Create(seed, firstSurfaceCell);
            SandboxDungeonLayout second = SandboxDungeonLayout.Create(seed, secondSurfaceCell);

            Assert.That(second.TemplateIndex, Is.EqualTo(first.TemplateIndex));
            Assert.That(second.OrderedPathCells, Is.EqualTo(first.OrderedPathCells));
            Assert.That(second.ComputeStateHash(), Is.Not.EqualTo(first.ComputeStateHash()));
        }

        private static Dictionary<int, SandboxDungeonLayout> FindAllTemplates()
        {
            var layouts = new Dictionary<int, SandboxDungeonLayout>();
            for (ulong seed = 0; seed < 256 && layouts.Count < SandboxDungeonLayout.TemplateCount; seed++)
            {
                SandboxDungeonLayout layout = SandboxDungeonLayout.Create(seed, new GridCell(11, -4));
                layouts[layout.TemplateIndex] = layout;
            }

            return layouts;
        }

        private static GridCell FindDifferentCellWithTemplate(ulong seed, int template, GridCell excluded)
        {
            for (int y = -16; y <= 16; y++)
            {
                for (int x = -16; x <= 16; x++)
                {
                    GridCell candidate = new GridCell(x, y);
                    if (!candidate.Equals(excluded)
                        && SandboxDungeonLayout.SelectTemplateIndex(seed, candidate) == template)
                        return candidate;
                }
            }

            Assert.Fail($"Could not find a second surface cell for template {template}.");
            return default;
        }
    }
}
