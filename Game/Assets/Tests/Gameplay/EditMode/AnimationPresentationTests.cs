using System.Linq;
using NUnit.Framework;
using Sandsunder.Gameplay;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Sandsunder.Tests.Gameplay
{
    public sealed class AnimationPresentationTests
    {
        [Test]
        public void ActorVisual_PreservesHandAnchorAndGroundedShadowForBothFacings()
        {
            GameObject actor = new("Animation Rig Test");
            actor.AddComponent<SpriteRenderer>();
            SandboxActorVisual visual = actor.AddComponent<SandboxActorVisual>();
            Sprite body = PrototypePixelArt.GetCachedSprite(PrototypePixelKind.Player, Color.cyan);
            Sprite weapon = PrototypePixelArt.GetCachedSprite(PrototypePixelKind.Projectile, Color.yellow);

            visual.Configure(body, null, weapon, null, null, isHostile: false);
            visual.SetAimDirection(Vector2.right);
            visual.RefreshAttachmentPose();

            Assert.That(visual.VisualRoot.Find("Shadow").localPosition, Is.EqualTo(new Vector3(0f, -0.15f, 0f)));
            Assert.That(visual.VisualRoot.Find("Weapon").localPosition, Is.EqualTo(new Vector3(0.08f, 0.05f, 0f)));

            visual.PlayFire(Vector2.left);
            visual.RefreshAttachmentPose();

            Assert.That(visual.VisualRoot.Find("Weapon").localPosition, Is.EqualTo(new Vector3(-0.08f, 0.05f, 0f)));
            Assert.That(visual.WeaponRenderer.transform.localPosition.x, Is.LessThanOrEqualTo(0.08f));

            Object.DestroyImmediate(actor);
        }

        [Test]
        public void ActorVisual_AssignsSeparateNomadAndSpitterDrivers()
        {
            GameObject nomad = new("Nomad Animation Driver Test");
            nomad.AddComponent<SpriteRenderer>();
            SandboxActorVisual nomadVisual = nomad.AddComponent<SandboxActorVisual>();
            nomadVisual.Configure(PrototypePixelArt.GetCachedSprite(PrototypePixelKind.Player, Color.cyan), null, null, null, null, false);

            GameObject spitter = new("Spitter Animation Driver Test");
            spitter.AddComponent<SpriteRenderer>();
            SandboxActorVisual spitterVisual = spitter.AddComponent<SandboxActorVisual>();
            spitterVisual.Configure(PrototypePixelArt.GetCachedSprite(PrototypePixelKind.Spitter, Color.red), null, null, null, null, true);

            Assert.That(nomadVisual.VisualRoot.Find("Body").GetComponent<NomadAnimator>(), Is.Not.Null);
            Assert.That(nomadVisual.VisualRoot.Find("Body").GetComponent<SpitterAnimator>(), Is.Null);
            Assert.That(spitterVisual.VisualRoot.Find("Body").GetComponent<SpitterAnimator>(), Is.Not.Null);
            Assert.That(spitterVisual.VisualRoot.Find("Body").GetComponent<NomadAnimator>(), Is.Null);

            Object.DestroyImmediate(nomad);
            Object.DestroyImmediate(spitter);
        }

        [Test]
        public void GeneratedControllers_CoverApprovedNomadAndSpitterStateSets()
        {
            AnimatorController nomad = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                "Assets/Sandsunder/Art/Generated/NomadAnimatorController.controller");
            Assert.That(nomad, Is.Not.Null);

            AnimatorState[] nomadStates = nomad.layers[0].stateMachine.states
                .Select(child => child.state)
                .ToArray();
            string[] expectedNomadStates =
            {
                "Idle", "Walk", "Run", "Roll", "Dig", "StealthCrouch",
                "Melee", "ShootRecoil", "Hurt", "Death"
            };
            Assert.That(nomadStates.Select(state => state.name), Is.EquivalentTo(expectedNomadStates));
            Assert.That(nomadStates, Has.All.Matches<AnimatorState>(state => state.motion != null));
            Assert.That(nomadStates.Single(state => state.name == "Idle").motion.name, Is.EqualTo("Nomad_Idle"));
            Assert.That(nomadStates.Single(state => state.name == "Roll").motion.name, Is.EqualTo("Nomad_Run"));
            Assert.That(nomad.parameters.Select(parameter => parameter.name), Does.Contain("IsRolling"));
            AssertStateSpritesAreWorldScaleSafe(nomadStates);

            AnimatorController spitter = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                "Assets/Sandsunder/Art/Generated/SpitterAnimatorController.controller");
            Assert.That(spitter, Is.Not.Null);
            Assert.That(spitter.layers[0].stateMachine.states.Select(child => child.state.name),
                Is.EquivalentTo(new[] { "Idle", "Charge", "Death" }));
            AssertStateSpritesAreWorldScaleSafe(spitter.layers[0].stateMachine.states.Select(child => child.state));

            GameObject actor = new("Nomad Animator Enabled Test");
            actor.AddComponent<SpriteRenderer>();
            SandboxActorVisual visual = actor.AddComponent<SandboxActorVisual>();
            visual.Configure(PrototypePixelArt.GetCachedSprite(PrototypePixelKind.Player, Color.cyan),
                null, null, null, null, false, nomad);
            Animator runtimeAnimator = visual.VisualRoot.Find("Body").GetComponent<Animator>();
            Assert.That(runtimeAnimator.enabled, Is.True);
            Assert.That(runtimeAnimator.runtimeAnimatorController, Is.SameAs(nomad));
            Object.DestroyImmediate(actor);
        }

        private static void AssertStateSpritesAreWorldScaleSafe(System.Collections.Generic.IEnumerable<AnimatorState> states)
        {
            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
                string.Empty, typeof(SpriteRenderer), "m_Sprite");
            foreach (AnimatorState state in states)
            {
                AnimationClip clip = state.motion as AnimationClip;
                Assert.That(clip, Is.Not.Null, $"State {state.name} must use an AnimationClip.");
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                foreach (ObjectReferenceKeyframe key in keys ?? System.Array.Empty<ObjectReferenceKeyframe>())
                {
                    Sprite sprite = key.value as Sprite;
                    Assert.That(sprite, Is.Not.Null, $"State {state.name} contains a non-sprite frame.");
                    Assert.That(sprite.pixelsPerUnit, Is.EqualTo(32f).Within(0.01f),
                        $"State {state.name} must preserve the authoritative 32 PPU import scale.");
                    Assert.That(sprite.rect.width / sprite.pixelsPerUnit, Is.LessThanOrEqualTo(1.5f),
                        $"State {state.name} has an oversized world-space frame.");
                    Assert.That(sprite.rect.height / sprite.pixelsPerUnit, Is.LessThanOrEqualTo(1.5f),
                        $"State {state.name} has an oversized world-space frame.");
                }
            }
        }
    }
}
