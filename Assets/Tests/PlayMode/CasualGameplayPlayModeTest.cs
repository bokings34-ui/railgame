using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Railgame.Tests
{
    public sealed class CasualGameplayPlayModeTest : InputTestFixture
    {
        [UnityTest]
        public IEnumerator LobbyToSummerSpawnsChasingEnemy()
        {
            Time.timeScale = 1f;
            InputSystem.AddDevice<Keyboard>();
            SceneManager.LoadScene("Railgame_Lobby");
            yield return null;
            Click("SummerButton");
            while (SceneManager.GetActiveScene().name != "Map_Procedural_Summer") yield return null;
            yield return null;

            Assert.That(Find("Railgame.Player.RailgamePlayerController"), Is.Not.Null);
            Assert.That(Find("Railgame.Player.WaterSlowVolume"), Is.Not.Null);
            Component spawner = Find("Railgame.Enemy.DummyEnemySpawner");
            Object enemy = null;
            float deadline = Time.realtimeSinceStartup + 12f;
            while (enemy == null && Time.realtimeSinceStartup < deadline)
            {
                enemy = Property<Object>(spawner, "SpawnedEnemy");
                yield return null;
            }
            Assert.That(enemy, Is.Not.Null);
            while (Property<object>(enemy, "State").ToString() != "Chasing" && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(Property<object>(enemy, "State").ToString(), Is.EqualTo("Chasing"));
        }

        [UnityTest]
        public IEnumerator LobbyToSpringCoreLoopWorks()
        {
            Time.timeScale = 1f;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            SceneManager.LoadScene("Railgame_Lobby");
            yield return null;

            Assert.That(Find("Railgame.UI.RailgameLobbyController"), Is.Not.Null);
            Click("SettingsButton");
            Component settings = Find("Railgame.UI.RailgameSettingsPanel");
            Assert.That(settings.gameObject.activeSelf, Is.True);
            Invoke(settings, "CancelAndClose");
            Assert.That(settings.gameObject.activeSelf, Is.False);

            Click("SpringButton");
            while (SceneManager.GetActiveScene().name != "Map_Procedural_Spring")
                yield return null;
            yield return null;

            Component player = Find("Railgame.Player.RailgamePlayerController");
            Component spawner = Find("Railgame.Enemy.DummyEnemySpawner");
            Assert.That(player, Is.Not.Null);
            Assert.That(spawner, Is.Not.Null);

            Vector3 playerStart = player.transform.position;
            for (int frame = 0; frame < 20; frame++)
            {
                Invoke(player, "SimulateInput", Vector2.up, false, 0.02f);
                yield return null;
            }
            Assert.That(player.transform.position.z - playerStart.z, Is.GreaterThan(0.5f));

            Object enemy = null;
            float deadline = Time.realtimeSinceStartup + 12f;
            while (enemy == null && Time.realtimeSinceStartup < deadline)
            {
                enemy = Property<Object>(spawner, "SpawnedEnemy");
                yield return null;
            }
            Assert.That(enemy, Is.Not.Null, "Dummy enemy did not spawn.");
            Component enemyComponent = (Component)enemy;
            Vector3 enemyStart = enemyComponent.transform.position;
            while (Property<object>(enemyComponent, "State").ToString() != "Chasing" &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(Property<object>(enemyComponent, "State").ToString(), Is.EqualTo("Chasing"));
            Assert.That(Vector3.Distance(enemyStart, enemyComponent.transform.position), Is.GreaterThan(1f));

            Component generator = Find("Railgame.Map.ProceduralMapGenerator");
            foreach (Component slot in FindAll("Railgame.Map.ResourceSpawnSlot"))
                slot.gameObject.SetActive(false);
            Physics.SyncTransforms();
            CharacterController controller = player.GetComponent<CharacterController>();
            bool climbed = false;
            Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };
            foreach (Component dirtBlock in FindAll("Railgame.Map.DirtBlock"))
            {
                Vector2Int cell = Property<Vector2Int>(dirtBlock, "Cell");
                if (cell != new Vector2Int(13, 118)) continue;
                foreach (Vector2Int direction in directions)
                {
                    Vector3 target = generator.transform.TransformPoint(new Vector3(cell.x + 0.5f, 2f, cell.y + 0.5f));
                    Vector3 rayStart = generator.transform.TransformPoint(new Vector3(cell.x + 0.5f + direction.x, 3f,
                        cell.y + 0.5f + direction.y));
                    if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 3f) ||
                        hit.collider.GetComponentInParent(FindType("Railgame.Map.DirtBlock")) != null ||
                        Mathf.Abs(hit.point.y - 1f) > 0.12f)
                        continue;

                    Teleport(controller, hit.point + Vector3.up * 0.04f);
                    Field(player, "verticalVelocity", 0f);
                    for (int frame = 0; frame < 8; frame++) yield return null;
                    Invoke(player, "SimulateInput", Vector2.zero, false, 0.02f);
                    Assert.That(Property<bool>(player, "IsGrounded"), Is.True, "Player did not settle before jump.");
                    ButtonControl moveKey = direction.x > 0 ? keyboard.aKey : direction.x < 0 ? keyboard.dKey :
                        direction.y > 0 ? keyboard.sKey : keyboard.wKey;
                    Press(keyboard.spaceKey);
                    Press(moveKey);
                    yield return null;
                    Release(keyboard.spaceKey);
                    bool moveReleased = false;
                    float maximumY = player.transform.position.y;
                    float movementDeadline = Time.time + 1.25f;
                    while (Time.time < movementDeadline)
                    {
                        yield return null;
                        maximumY = Mathf.Max(maximumY, player.transform.position.y);
                        float distance = Vector2.Distance(new Vector2(player.transform.position.x, player.transform.position.z),
                            new Vector2(target.x, target.z));
                        if (!moveReleased && distance <= 0.2f)
                        {
                            Release(moveKey);
                            moveReleased = true;
                        }
                    }
                    if (!moveReleased)
                    {
                        Release(moveKey);
                        InputSystem.Update();
                    }
                    float settleDeadline = Time.time + 0.75f;
                    while (Time.time < settleDeadline) yield return null;

                    float planarDistance = Vector2.Distance(new Vector2(player.transform.position.x, player.transform.position.z),
                        new Vector2(target.x, target.z));
                    climbed = Mathf.Abs(player.transform.position.y - 2f) <= 0.12f && planarDistance <= 0.5f;
                    if (!climbed)
                        Debug.Log($"DIRT_CLIMB_DIAGNOSTIC cell={cell} maxY={maximumY:F3} finalY={player.transform.position.y:F3} distance={planarDistance:F3}");
                    if (climbed) break;
                }
                if (climbed) break;
            }
            Assert.That(climbed, Is.True, "Player could not jump onto a generated 1m dirt block.");

            Component water = Find("Railgame.Player.WaterSlowVolume");
            Teleport(controller, water.GetComponent<Collider>().bounds.center + Vector3.up * 3f);
            deadline = Time.realtimeSinceStartup + 8f;
            while ((!Property<bool>(player, "IsInWater") || player.transform.position.y > 0.15f) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(Property<bool>(player, "IsInWater"), Is.True);
            Assert.That(player.transform.position.y, Is.GreaterThanOrEqualTo(-0.05f));
            Assert.That(Property<float>(player, "EffectiveMoveSpeed"), Is.LessThan(Property<float>(player, "MoveSpeed")));

            Component navigation = Find("Railgame.Map.RuntimeNavigationController");
            int updateCount = Property<int>(navigation, "CompletedUpdateCount");
            Component dirt = Find("Railgame.Map.DirtBlock");
            Assert.That((bool)Invoke(dirt, "Mine"), Is.True);
            deadline = Time.realtimeSinceStartup + 12f;
            while (Property<int>(navigation, "CompletedUpdateCount") == updateCount && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(Property<int>(navigation, "CompletedUpdateCount"), Is.GreaterThan(updateCount));
            NavMeshAgent agent = enemyComponent.GetComponent<NavMeshAgent>();
            Assert.That(agent.isOnNavMesh, Is.True);

            Component menu = Find("Railgame.UI.RailgameGameMenuController");
            Press(keyboard.escapeKey);
            InputSystem.Update();
            Assert.That(keyboard.escapeKey.wasPressedThisFrame, Is.True, "Escape test input was not delivered.");
            Invoke(menu, "Update");
            Assert.That(Time.timeScale, Is.Zero, "Escape did not pause gameplay.");
            Release(keyboard.escapeKey);
            InputSystem.Update();
            yield return null;

            Invoke(menu, "Resume");
            Invoke(menu, "OpenShop");
            Assert.That(Time.timeScale, Is.Zero);
            Component shop = Find("Railgame.UI.RailgameShopScreen");
            Click("Offer1");
            Assert.That(Property<int>(shop, "Bolts"), Is.EqualTo(4));
            Invoke(menu, "Resume");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        private static Type FindType(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.GetType(name) is Type type)
                    return type;
            throw new InvalidOperationException($"Type not found: {name}");
        }

        private static Component Find(string typeName)
        {
            Type type = FindType(typeName);
            foreach (Object item in Resources.FindObjectsOfTypeAll(type))
                if (item is Component component && component.gameObject.scene.IsValid())
                    return component;
            return null;
        }

        private static Component[] FindAll(string typeName)
        {
            Type type = FindType(typeName);
            var results = new System.Collections.Generic.List<Component>();
            foreach (Object item in Resources.FindObjectsOfTypeAll(type))
                if (item is Component component && component.gameObject.scene.IsValid())
                    results.Add(component);
            return results.ToArray();
        }

        private static object Invoke(Component target, string method, params object[] args) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.Invoke(target, args);

        private static T Property<T>(Object target, string name) =>
            (T)target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(target);

        private static void Field(Object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);

        private static void Click(string name)
        {
            GameObject button = GameObject.Find(name);
            Assert.That(button, Is.Not.Null, $"Button missing: {name}");
            Component component = button.GetComponent(FindType("UnityEngine.UI.Button"));
            object onClick = component.GetType().GetProperty("onClick")?.GetValue(component);
            onClick?.GetType().GetMethod("Invoke")?.Invoke(onClick, null);
        }

        private static void Teleport(CharacterController controller, Vector3 position)
        {
            controller.enabled = false;
            controller.transform.position = position;
            controller.enabled = true;
            Physics.SyncTransforms();
        }
    }
}
