using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Armed Boats", "Wujaszkun", "2.07.04")]
    [Description("Armament for scrap transport helicopter and Minicopter")]
    internal class ArmedBoats : RustPlugin
    {
        private List<RHIB> boatList = new List<RHIB>();
        public static ArmedBoats instance;

        [ChatCommand("armboat")]
        private void ArmTransportCopters(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin)
            {
                //var target = RaycastAll<RHIB>(player.eyes.HeadRay());
                //target.gameObject.AddComponent<BoatArmament>();
                ReloadBoatInformation();
                ArmBoats();
            }
        }
        private void OnServerInitialized()
        {
            instance = this;
            ReloadBoatInformation();
            ArmBoats();
        }
        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity.gameObject.GetComponent<RHIB>() != null)
            {
                ReloadBoatInformation();
                ArmBoats();
            }
        }
        private void ReloadBoatInformation()
        {
            boatList = new List<RHIB>(GameObject.FindObjectsOfType<RHIB>());
        }
        private void ArmBoats()
        {
            foreach (var rhib in boatList)
            {
                rhib.gameObject.AddComponent<BoatArmament>();
                Puts($"RHIB at {rhib.transform.position} has been armed");
            }
        }

        private void Unload()
        {
            foreach (var boat in GameObject.FindObjectsOfType<BoatArmament>())
            {
                try
                {
                    if (boat != null)
                    {
                        boat.DespawnAllEntities();
                        GameObject.Destroy(boat);
                    }
                }
                catch { }
            }
        }

        private RHIB RaycastAll<T>(Ray ray) where T : RHIB
        {
            var hits = Physics.RaycastAll(ray);
            GamePhysics.Sort(hits);
            var distance = 100f;
            object target = false;
            foreach (var hit in hits)
            {
                var ent = hit.GetEntity();
                if (ent is T && hit.distance < distance)
                {
                    target = ent;
                    break;
                }
            }
            return target as RHIB;
        }
        class BoatArmament : MonoBehaviour
        {
            private RHIB mainBoat;
            private List<BaseEntity> spawnedEntities = new List<BaseEntity>();
            private BasePlayer shooter;
            private AutoTurret turret;

            private void Awake()
            {
                mainBoat = gameObject.GetComponent<RHIB>();
                SpawnGuns();
            }

            private void FixedUpdate()
            {
                if (turret != null)
                {
                    turret.SetTarget(null);
                    shooter = mainBoat.mountPoints[4].mountable.GetMounted();

                    if (shooter != null)
                    {
                        turret.aimDir = shooter.eyes.HeadRay().direction;
                        turret?.SendAimDir();
                        turret?.UpdateAiming();
                    }
                    else
                    {
                        turret.aimDir = mainBoat.transform.forward;
                        turret?.SendAimDir();
                        turret?.UpdateAiming();
                    }
                }
            }

            private Vector3 FindTarget(BasePlayer player)
            {
                RaycastHit hitInfo;

                if (!UnityEngine.Physics.Raycast(player.eyes.HeadRay(), out hitInfo, Mathf.Infinity, -1063040255))
                {
                }
                Vector3 hitpoint = hitInfo.point;
                return hitpoint;
            }

            private void SpawnGuns()
            {
                var rotation = mainBoat.transform.rotation;
                var position = mainBoat.transform.position;

                BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/npc/autoturret/autoturret_deployed.prefab", position, rotation, true);

                entity.SetParent(mainBoat, 0);
                entity.transform.localPosition = new Vector3(0f, 1.2f, 4.2f);
                entity.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                entity?.Spawn();

                turret = entity.GetComponent<AutoTurret>();
                turret.SetPeacekeepermode(true);
                turret.InitializeControl(null);
                turret.UpdateFromInput(100, 0);
                turret.isLootable = false;
                turret.dropChance = 0;

                spawnedEntities.Add(entity);

                turret.inventory.Clear();

                ItemManager.CreateByName("lmg.m249", 1).MoveToContainer(turret.inventory, 0);
                ItemManager.CreateByName("ammo.rifle", 1000).MoveToContainer(turret.inventory, 1);

                turret.UpdateAttachedWeapon();
                turret.Reload();
            }

            public void DespawnAllEntities()
            {
                foreach (var entity in spawnedEntities)
                {
                    try
                    {
                        entity.Kill();
                        spawnedEntities.Remove(entity);
                    }
                    catch { }
                }
            }
            internal void GetInput(BasePlayer player, InputState input)
            {
                if (player == shooter && input.IsDown(BUTTON.FIRE_PRIMARY))
                {
                    FireTurretsGuns(player, turret);
                }
            }

            public void FireTurretsGuns(BasePlayer player, AutoTurret turret)
            {
                if (turret.IsOnline() == true)
                {
                    if (turret.GetAttachedWeapon().AmmoFraction() <= 0)
                    {
                        //nextShootTime = Time.time + turret.GetAttachedWeapon().GetReloadDuration();
                        turret.GetAttachedWeapon().TopUpAmmo();
                        //canGunShoot = false;
                    }
                    turret.FireAttachedGun(Vector3.zero, ConVar.PatrolHelicopter.bulletAccuracy);
                }
            }
        }
        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            try
            {
                player.GetMountedVehicle().GetComponent<BoatArmament>().GetInput(player, input);
            }
            catch { }
        }
    }
}