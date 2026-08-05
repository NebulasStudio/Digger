using System.Reflection;
using NUnit.Framework;
using Sandsunder.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sandsunder.Tests.Gameplay
{
    public sealed class SandboxPresentationTests
    {
        [Test]
        public void PixelProxy_ReusesCachedSpriteForSameKindAndColor()
        {
            Color color = new(0.31f, 0.71f, 0.82f, 1f);

            Sprite first = PrototypePixelArt.GetCachedSprite(PrototypePixelKind.Projectile, color);
            int countAfterFirst = PrototypePixelArt.CachedSpriteCount;
            Sprite second = PrototypePixelArt.GetCachedSprite(PrototypePixelKind.Projectile, color);

            Assert.That(second, Is.SameAs(first));
            Assert.That(PrototypePixelArt.CachedSpriteCount, Is.EqualTo(countAfterFirst));
        }

        [Test]
        public void ActorVisual_SeparatesPhysicalRootFromBodyShadowAndWeapon()
        {
            GameObject actor = new("Sandbox Actor Test");
            SpriteRenderer physicalRenderer = actor.AddComponent<SpriteRenderer>();
            Sprite body = PrototypePixelArt.GetCachedSprite(PrototypePixelKind.Player, Color.cyan);
            Sprite weapon = PrototypePixelArt.GetCachedSprite(PrototypePixelKind.Projectile, Color.yellow);
            SandboxActorVisual visual = actor.AddComponent<SandboxActorVisual>();

            visual.Configure(body, null, weapon, null, null, isHostile: false);

            Assert.That(visual.VisualRoot, Is.Not.Null);
            Assert.That(visual.VisualRoot.parent, Is.SameAs(actor.transform));
            Assert.That(visual.BodyRenderer.transform, Is.Not.SameAs(actor.transform));
            Assert.That(visual.WeaponRenderer.transform, Is.Not.SameAs(actor.transform));
            Assert.That(visual.BodyRenderer.sprite, Is.SameAs(body));
            Assert.That(visual.WeaponRenderer.sprite, Is.SameAs(weapon));
            Assert.That(physicalRenderer.enabled, Is.False);

            Object.DestroyImmediate(actor);
        }

        [Test]
        public void ActorVisual_RehydratesRuntimeOnlyRigReferencesAfterDeserialization()
        {
            GameObject actor = new("Sandbox Actor Deserialization Test");
            actor.AddComponent<SpriteRenderer>();
            SandboxActorVisual visual = actor.AddComponent<SandboxActorVisual>();
            visual.Configure(
                PrototypePixelArt.GetCachedSprite(PrototypePixelKind.Player, Color.cyan),
                null,
                null,
                null,
                null,
                isHostile: false);

            const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(SandboxActorVisual).GetField("bodyRoot", PrivateInstance)?.SetValue(visual, null);
            typeof(SandboxActorVisual).GetField("weaponRoot", PrivateInstance)?.SetValue(visual, null);

            Assert.DoesNotThrow(() => actor.SendMessage("LateUpdate"));
            Assert.That(visual.BodyRenderer.transform.parent.name, Is.EqualTo("VisualRoot"));
            Assert.That(visual.WeaponRenderer.transform.parent.name, Is.EqualTo("VisualRoot"));

            Object.DestroyImmediate(actor);
        }

        [Test]
        public void CameraRig_UsesSandboxFovAndClampsTargetInsideBounds()
        {
            GameObject target = new("Camera Target");
            target.transform.position = new Vector3(100f, 100f, 0f);
            GameObject cameraObject = new("Sandbox Camera Test");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.aspect = 16f / 9f;
            OrthographicCameraFollow follow = cameraObject.AddComponent<OrthographicCameraFollow>();
            follow.Configure(
                target.transform,
                null,
                sharpness: 14f,
                minBounds: new Vector2(-12f, -8f),
                maxBounds: new Vector2(12f, 8f),
                configuredAimLookAhead: 1.2f);

            Vector2 desired = follow.CalculateDesiredPosition(includeShake: false);

            Assert.That(camera.orthographic, Is.True);
            Assert.That(camera.orthographicSize, Is.EqualTo(5.4f).Within(0.001f));
            Assert.That(desired.x, Is.LessThanOrEqualTo(12f - camera.orthographicSize * camera.aspect + 0.063f));
            Assert.That(desired.y, Is.LessThanOrEqualTo(8f - camera.orthographicSize + 0.063f));

            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void ProjectileVisual_UsesCachedCoreAndAddsTrailWithoutNewProxyPerShot()
        {
            Color color = new(0.91f, 0.38f, 0.24f, 1f);
            GameObject firstObject = new("Projectile Visual A");
            GameObject secondObject = new("Projectile Visual B");
            firstObject.transform.position = new Vector3(0f, 4f, 0f);
            secondObject.transform.position = new Vector3(0f, -4f, 0f);
            SandboxProjectileVisual first = firstObject.AddComponent<SandboxProjectileVisual>();
            SandboxProjectileVisual second = secondObject.AddComponent<SandboxProjectileVisual>();

            first.Configure(null, color, Vector2.right, 0f, hostile: true);
            second.Configure(null, color, Vector2.right, 0f, hostile: true);

            Assert.That(first.CoreRenderer.sprite, Is.SameAs(second.CoreRenderer.sprite));
            Assert.That(first.Trail, Is.Not.Null);
            Assert.That(second.Trail, Is.Not.Null);
            Assert.That(firstObject.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(first.CoreRenderer.transform, Is.Not.SameAs(firstObject.transform));
            Assert.That(first.CoreRenderer.sortingOrder, Is.LessThan(second.CoreRenderer.sortingOrder));

            Object.DestroyImmediate(firstObject);
            Object.DestroyImmediate(secondObject);
        }

        [Test]
        public void DigFeedback_AnimatesVisualChildWithoutChangingColliderRootScale()
        {
            GameObject node = new("Dig Visual Test");
            node.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            node.AddComponent<BoxCollider2D>();
            SpriteRenderer renderer = node.AddComponent<SpriteRenderer>();
            renderer.sprite = PrototypePixelArt.GetCachedSprite(PrototypePixelKind.DigNode, Color.yellow);
            SandboxDigVisual visual = node.AddComponent<SandboxDigVisual>();
            visual.Configure(renderer.sprite, null, null);
            Vector3 physicalScale = node.transform.localScale;

            visual.PlayStrike(strikesRemaining: 2);

            Assert.That(node.transform.localScale, Is.EqualTo(physicalScale));
            Assert.That(node.transform.Find("VisualRoot"), Is.Not.Null);
            Assert.That(renderer.enabled, Is.False);

            Object.DestroyImmediate(node);
        }

        [Test]
        public void GameplayLab_ContainsCompleteVisualSandboxRig()
        {
            const string ScenePath = "Assets/Scenes/GameplayLab.unity";
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                GameObject arena = System.Array.Find(roots, root => root.name == "Arena");
                GameObject player = System.Array.Find(roots, root => root.name == "Player");
                GameObject spitters = System.Array.Find(roots, root => root.name == "Dune Spitters");
                GameObject digNodes = System.Array.Find(roots, root => root.name == "Dig Nodes");
                GameObject cameraObject = System.Array.Find(roots, root => root.name == "Gameplay Camera");

                Assert.That(arena, Is.Not.Null);
                SpriteRenderer floor = arena.transform.Find("Floor")?.GetComponent<SpriteRenderer>();
                Assert.That(floor, Is.Not.Null);
                Assert.That(floor.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
                Assert.That(floor.sprite, Is.Not.Null);
                Assert.That(floor.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));

                SandboxActorVisual playerVisual = player?.GetComponent<SandboxActorVisual>();
                Assert.That(playerVisual, Is.Not.Null);
                Assert.That(playerVisual.BodyRenderer.sprite, Is.Not.Null);
                Assert.That(player.GetComponent<SpriteRenderer>().enabled, Is.False);
                Assert.That(spitters.GetComponentsInChildren<SandboxActorVisual>(true), Has.Length.EqualTo(3));
                Assert.That(digNodes.GetComponentsInChildren<SandboxDigVisual>(true), Has.Length.EqualTo(6));

                Camera gameplayCamera = cameraObject?.GetComponent<Camera>();
                Assert.That(gameplayCamera, Is.Not.Null);
                Assert.That(gameplayCamera.orthographic, Is.True);
                Assert.That(gameplayCamera.orthographicSize, Is.EqualTo(5.4f).Within(0.001f));
                OrthographicCameraFollow cameraFollow = cameraObject.GetComponent<OrthographicCameraFollow>();
                Assert.That(cameraFollow, Is.Not.Null);
                float previousAspect = gameplayCamera.aspect;
                Vector3 previousPlayerPosition = player.transform.position;
                gameplayCamera.aspect = 16f / 9f;
                player.transform.position = new Vector3(5f, 0f, 0f);
                Assert.That(cameraFollow.CalculateDesiredPosition(includeShake: false).x, Is.GreaterThan(0.5f));
                player.transform.position = previousPlayerPosition;
                gameplayCamera.aspect = previousAspect;

                int missingScripts = 0;
                foreach (GameObject root in roots)
                {
                    foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                    {
                        missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject);
                    }
                }

                Assert.That(missingScripts, Is.Zero);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
