using System;
using System.Linq;
using Railgame.Map;
using Railgame.Player;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Railgame.Editor
{
    public static class RailgamePlayerPhysicsValidator
    {
        private const string ScenePath = "Assets/00.main/Map/Scenes/Map_Procedural_Spring.unity";
        private const float Step = 0.02f;

        public static void Validate()
        {
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            try
            {
                (float floorY, float normalSpeed, float waterSpeed) = ValidateWaterFallAndSlowdown();
                (Vector2Int cell, float landedY) = ValidateDirtClimb();
                Debug.Log($"RAILGAME_PLAYER_PHYSICS_OK waterFloorY={floorY:F3} normalSpeed={normalSpeed:F2} waterSpeed={waterSpeed:F2} dirtCell={cell.x},{cell.y} landedY={landedY:F3}");
            }
            finally
            {
                Physics.simulationMode = previousMode;
            }
        }

        private static (float, float, float) ValidateWaterFallAndSlowdown()
        {
            OpenGeneratedScene(out _, out RailgamePlayerController player);
            WaterSlowVolume water = Object.FindFirstObjectByType<WaterSlowVolume>();
            Require(water != null, "Water slowdown volume missing");

            CharacterController controller = player.GetComponent<CharacterController>();
            Vector3 waterCenter = water.GetComponent<Collider>().bounds.center;
            Teleport(controller, waterCenter + Vector3.up * 3f);

            float minimumY = player.transform.position.y;
            for (int frame = 0; frame < 180; frame++)
            {
                StepPlayer(player, Vector2.zero, false);
                minimumY = Mathf.Min(minimumY, player.transform.position.y);
            }

            Require(player.IsInWater, "Player did not enter water slowdown volume");
            Require(minimumY >= -0.05f, $"Player fell below lower safety floor: {minimumY:F3}");
            Require(player.transform.position.y <= 0.12f, $"Player did not settle on lower safety floor: {player.transform.position.y:F3}");
            Require(player.EffectiveMoveSpeed < player.MoveSpeed, "Water did not reduce movement speed");
            return (player.transform.position.y, player.MoveSpeed, player.EffectiveMoveSpeed);
        }

        private static (Vector2Int, float) ValidateDirtClimb()
        {
            OpenGeneratedScene(out ProceduralMapGenerator generator, out RailgamePlayerController player);
            foreach (ResourceSpawnSlot slot in Object.FindObjectsByType<ResourceSpawnSlot>(FindObjectsSortMode.None))
                slot.gameObject.SetActive(false);
            Physics.SyncTransforms();

            CharacterController controller = player.GetComponent<CharacterController>();
            Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };
            foreach (DirtBlock dirt in Object.FindObjectsByType<DirtBlock>(FindObjectsSortMode.None).Take(32))
            foreach (Vector2Int direction in directions)
            {
                Vector3 target = generator.transform.TransformPoint(new Vector3(dirt.Cell.x + 0.5f, 2f, dirt.Cell.y + 0.5f));
                Vector3 start = generator.transform.TransformPoint(new Vector3(dirt.Cell.x + 0.5f + direction.x, 3f, dirt.Cell.y + 0.5f + direction.y));
                if (!Physics.Raycast(start, Vector3.down, out RaycastHit ground, 3f) ||
                    ground.collider.GetComponentInParent<DirtBlock>() != null || Mathf.Abs(ground.point.y - 1f) > 0.12f)
                    continue;

                Teleport(controller, ground.point + Vector3.up * 0.04f);
                for (int frame = 0; frame < 8; frame++)
                    StepPlayer(player, Vector2.zero, false);

                for (int frame = 0; frame < 100; frame++)
                {
                    Vector3 delta = target - player.transform.position;
                    Vector2 input = new(delta.x, delta.z);
                    if (input.magnitude > 0.15f)
                        input = input.normalized * 0.35f;
                    else
                        input = Vector2.zero;
                    StepPlayer(player, input, frame == 0);
                }

                float planarDistance = Vector2.Distance(new Vector2(player.transform.position.x, player.transform.position.z),
                    new Vector2(target.x, target.z));
                if (Mathf.Abs(player.transform.position.y - 2f) <= 0.12f && planarDistance <= 0.5f)
                    return (dirt.Cell, player.transform.position.y);
            }

            throw new InvalidOperationException("Player could not jump onto any generated 1m dirt block.");
        }

        private static void OpenGeneratedScene(out ProceduralMapGenerator generator, out RailgamePlayerController player)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            generator = Object.FindFirstObjectByType<ProceduralMapGenerator>();
            player = Object.FindFirstObjectByType<RailgamePlayerController>();
            Require(generator != null && player != null, "Generated map or player missing");
            generator.GenerateNow();
            Physics.SyncTransforms();
        }

        private static void Teleport(CharacterController controller, Vector3 position)
        {
            controller.enabled = false;
            controller.transform.position = position;
            controller.enabled = true;
            Physics.SyncTransforms();
        }

        private static void StepPlayer(RailgamePlayerController player, Vector2 input, bool jump)
        {
            player.SimulateInput(input, jump, Step);
            Physics.SyncTransforms();
            Physics.Simulate(Step);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
