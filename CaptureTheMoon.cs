using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using SpaceEngineers.ObjectBuilders;
using SpaceEngineers.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using System.Xml;
using IMyLargeGatlingTurret = SpaceEngineers.Game.ModAPI.IMyLargeGatlingTurret;
using IMyLargeMissileTurret = SpaceEngineers.Game.ModAPI.IMyLargeMissileTurret;
using IMyLargeInteriorTurret = SpaceEngineers.Game.ModAPI.IMyLargeInteriorTurret;
using IMySafeZoneBlock = SpaceEngineers.Game.ModAPI.IMySafeZoneBlock;
using ObjectBuilders.SafeZone;
using Sandbox.Game.Components;
using IMyGravityGenerator = SpaceEngineers.Game.ModAPI.IMyGravityGenerator;

namespace CaptureTheMoon
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MainLogic : MySessionComponentBase
    {
        const int MIN_PLAYERS_FOR_ATTACK = 1;
        BaseWaypoint targetBase = null;

        double TimeOfLastAttack = 0;
        bool attackImminent = false;
        const int MIN_TIME_BETWEEN_ATTACKS = 60;

        int Minutes = 0;

        Random rand = new Random();

        public override void BeforeStart()
        {
            MyAPIGateway.Utilities.MessageEntered += HandleMessage;
            Minutes = (int)MyAPIGateway.Session.ElapsedPlayTime.TotalMinutes;
            //MyAPIGateway.Utilities.ShowMessage("WORLD", "M " + Minutes);
        }

        public override void UpdateBeforeSimulation()
        {
            if ((int)MyAPIGateway.Session.ElapsedPlayTime.TotalMinutes - Minutes >= 1)
            {
                //MyAPIGateway.Utilities.ShowMessage("WORLD", "Minute");
                Minutes = (int)MyAPIGateway.Session.ElapsedPlayTime.TotalMinutes;
                runOnceAMinute();
            }
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.MessageEntered -= HandleMessage;
        }

        void HandleMessage(String message, ref bool sendToOthers)
        {
            //sendToOthers = true;
            if (message.Contains("input"))
            {
                MyAPIGateway.Utilities.SendMessage("TEST 2!");
                //MyAPIUtilities.Static.EnterMessage("TEST 3!", ref sendToOthers);
                MyAPIGateway.Utilities.ShowMessage("WORLD", "HELLO");
            }
            else if (message.Contains("rep"))
            {
                MyAPIGateway.Utilities.ShowMessage("WORLD", "Repair");
                IMyEntity e = MyAPIGateway.Entities.GetEntityByName("Rep");
                if (e.GetType().Name.Equals("MyCubeGrid") &&
                        (e as IMyCubeGrid).IsStatic &&
                        MyAPIGateway.Session.IsServer)
                {
                    //(e as IMyCubeGrid).ChangeGridOwnership(MyAPIGateway.Session.Factions.TryGetFactionByTag("SPRT").FounderId, (MyOwnershipShareModeEnum)1);

                    List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                    (e as IMyCubeGrid).GetBlocks(blocks);
                    foreach (IMySlimBlock b in blocks)
                    {
                        if (b.MaxIntegrity > b.Integrity)
                        {
                            MyAPIGateway.Utilities.ShowMessage("WORLD", "Block");
                            MyAPIGateway.Utilities.ShowMessage("WORLD", "Owner: " + b.OwnerId);
                            List<IMyPlayer> p = new List<IMyPlayer>();
                            MyAPIGateway.Players.GetPlayers(p);
                            //b.IncreaseMountLevel(b.MaxIntegrity - b.Integrity, (e as IMyCubeGrid).BigOwners.First());//MyAPIGateway.Session.Factions.TryGetFactionByTag("FSTC").FounderId);// p.First().IdentityId);// SPRT
                            
                            if (b.OwnerId == 0)
                            {
                                var gridOwnerList = (e as IMyCubeGrid).BigOwners;
                                var ownerCnt = gridOwnerList.Count;
                                var gridOwner = 0L;

                                if (gridOwnerList[0] != 0) gridOwner = gridOwnerList[0];
                                else if (ownerCnt > 1) gridOwner = gridOwnerList[1];

                                if (gridOwner != 0)
                                {
                                    MyAPIGateway.Utilities.ShowMessage("WORLD", "b owner: " + b.OwnerId);
                                    MyAPIGateway.Utilities.ShowMessage("WORLD", "g owner: " + gridOwner);
                                    b.IncreaseMountLevel(b.MaxIntegrity - b.Integrity, gridOwner);
                                }
                            }
                            else
                            {
                                MyAPIGateway.Utilities.ShowMessage("WORLD", "b owner: " + b.OwnerId);
                                b.IncreaseMountLevel(b.MaxIntegrity - b.Integrity, b.OwnerId);
                            }
                        }
                    }
                }
            }
            //when I call OwnerId on an armor block I keep getting 0 even if I use ChangeGridOwnership or UpdateOwnership beforehand.
            //Im trying to use IncreaseMountLevel to repair blocks but its not working unless I use myself as the welderOwnerPlayerId and have Enable Creative Mode Tools on. Using the NPCs such as First Colonist doesnt work even if they own the block
            else if (message.Contains("tran"))
            {
                IMyEntity e = MyAPIGateway.Entities.GetEntityByName("Rep");
                (e as IMyCubeGrid).ChangeGridOwnership(MyAPIGateway.Session.Factions.TryGetFactionByTag("SPRT").FounderId, (MyOwnershipShareModeEnum)1);

                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                (e as IMyCubeGrid).GetBlocks(blocks);
                foreach (IMySlimBlock b in blocks)
                {
                    if (b.FatBlock != null)
                        MyAPIGateway.Utilities.ShowMessage("WORLD", "Block Id: " + b.FatBlock.EntityId);
                    MyAPIGateway.Utilities.ShowMessage("WORLD", "Owner: " + b.OwnerId);
                }
            }
            else if (message.Contains("TEST 3!"))
            {
                MyAPIGateway.Utilities.ShowNotification("ENTERED!");
            }
            else if (message.Contains("show"))
            {
                var Pirates = MyAPIGateway.Session.Factions.TryGetFactionByTag("SPRT");
                MyAPIGateway.Utilities.ShowNotification(Pirates.Name);
            }
            else if (message.Contains("trigger"))
            {
                TimeOfLastAttack -= 260;
                MyAPIGateway.Utilities.ShowMessage("WORLD", "attack triggered");
            }
            else if (message.Contains("num players"))
            {
                MyAPIGateway.Utilities.ShowMessage("WORLD", "Num Players: " +  MyAPIGateway.Multiplayer.Players.Count.ToString());
            }
            else if (message.Contains("bal"))
            {
                List<IMyPlayer> list = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(list);
                IMyPlayer test = list.First();
                long balance = 0;
                test.TryGetBalanceInfo(out balance);
                MyAPIGateway.Utilities.ShowMessage("WORLD", "Balance = " + balance);
            }
            else if (message.Contains("cost"))
            {
                //MyAPIGateway.Utilities.ShowMessage("WORLD", CreditCosts.costs.Find(x => x.TypeId.Contains("ore") && x.SubTypeId.Contains("iron")).Cost.ToString());
            }
            else if (message.Contains("ents"))
            {
                MyAPIGateway.Utilities.ShowMessage("WORLD", "Num Control Points = " + EntityContainer.bases.Count);
            }
        }

        void runOnceAMinute()
        {
            IncomePerMinute();
            checkSendAttack();
        }

        void IncomePerMinute()
        {
            if (EntityContainer.bases.Count > 0)
            {
                long incomePerMinute = 10;
                foreach (BaseWaypoint b in EntityContainer.bases)
                {
                    if (b.owner?.Tag.Contains("FSTC") == true)
                    {
                        incomePerMinute += b.tier;
                    }
                }
                MyAPIGateway.Session.Factions.TryGetFactionByTag("FSTC")?.
                    RequestChangeBalance(incomePerMinute);
            }

            long test;
            MyAPIGateway.Session.Factions.TryGetFactionByTag("FSTC").TryGetBalanceInfo(out test);
            MyAPIGateway.Utilities.ShowMessage("WORLD", "Faction Balance: " + test);
        }

        void checkSendAttack()
        {
            if (MyAPIGateway.Multiplayer.Players.Count < MIN_PLAYERS_FOR_ATTACK || 
                !EntityContainer.bases.Any(x => x.owner.Tag == "FSTC") ||
                EntityContainer.enemyMainBase.owner.Tag.Equals("FSTC"))
            {
                TimeOfLastAttack = MyAPIGateway.Session.ElapsedPlayTime.TotalMinutes;
                attackImminent = false;
            }
            else if (rand.Next(100) + 1 + 
                (int)((MyAPIGateway.Session.ElapsedPlayTime.TotalMinutes - 
                TimeOfLastAttack - MIN_TIME_BETWEEN_ATTACKS) / 2) >= 100)
            {
                TimeOfLastAttack = MyAPIGateway.Session.ElapsedPlayTime.TotalMinutes;
                int tier = 0;
                foreach (BaseWaypoint b in EntityContainer.bases)
                {
                    if (b.owner.Tag == "FSTC" && b.tier > tier)
                    {
                        tier = b.tier;
                        targetBase = b;
                    }
                }
                MyAPIGateway.Utilities.ShowMessage("WORLD", "Enemy assault in 5 minutes. Target: " + 
                    targetBase.name);
                attackImminent = true;
            }
            else if (attackImminent && TimeOfLastAttack + 0 <= MyAPIGateway.Session.ElapsedPlayTime.TotalMinutes)
            {
                MyAPIGateway.Utilities.ShowMessage("WORLD", "Enemy assault is on its way to " + targetBase.name);
                attackImminent = false;
                TimeOfLastAttack = MyAPIGateway.Session.ElapsedPlayTime.TotalMinutes;
                EntityContainer.sendAttack(targetBase);
            }
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_RemoteControl), false)]
    public class EnemyShipRC : MyGameLogicComponent
    {
        public IMyRemoteControl RC;

        private List<Vector3D> waypointVectors = new List<Vector3D>();

        private int currentWaypoint = 0;

        BaseWaypoint targetBase;

        bool needsInit = true;

        bool large = false;
        bool medium = false;
        bool small = false;

        MyPositionComponentBase pos;

        IMyPlayer targetPlayer = null;

        bool isServer = false;

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            if (isServer)
            {
                if (small && needsInit)
                {
                    needsInit = false;
                    
                    RC.AddWaypoint(pos.GetPosition() + pos.WorldMatrixRef.Up * 100, "Waypoint");
                    RC.SetAutoPilotEnabled(true);
                }
                else if (small && (pos.GetPosition() - RC.GetPosition()).Length() < 750)
                {
                    targetPlayer = getClosestPlayer();
                    if (targetPlayer != null)
                    {
                        RC.ClearWaypoints();
                        if ((pos.GetPosition() - targetPlayer.GetPosition()).Length() < 500)
                        {
                            RC.AddWaypoint(targetPlayer.GetPosition() + pos.WorldMatrixRef.Up * 50, "Waypoint");
                        }
                        else
                        {
                            RC.AddWaypoint(pos.GetPosition() + pos.WorldMatrixRef.Up * 100, "Waypoint");
                        }
                        RC.SetAutoPilotEnabled(true);
                    }
                }
                else if (medium && needsInit)
                {
                    needsInit = false;

                    int offset = 200;

                    Vector3D waypoint = pos.GetPosition() + pos.WorldMatrixRef.Up * offset;
                    Vector3D rightForward = pos.WorldMatrixRef.Forward + pos.WorldMatrixRef.Right;
                    rightForward.Normalize();
                    Vector3D rightBack = -pos.WorldMatrixRef.Forward + pos.WorldMatrixRef.Right;
                    rightBack.Normalize();

                    waypointVectors.Add(waypoint + pos.WorldMatrixRef.Forward * offset);
                    waypointVectors.Add(waypoint + rightForward * offset);
                    waypointVectors.Add(waypoint + pos.WorldMatrixRef.Right * offset);
                    waypointVectors.Add(waypoint + rightBack * offset);
                    waypointVectors.Add(waypoint + -pos.WorldMatrixRef.Forward * offset);
                    waypointVectors.Add(waypoint + -rightForward * offset);
                    waypointVectors.Add(waypoint + -pos.WorldMatrixRef.Right * offset);
                    waypointVectors.Add(waypoint + -rightBack * offset);

                    RC.AddWaypoint(waypointVectors[currentWaypoint++], "Waypoint " + currentWaypoint);

                    RC.SetAutoPilotEnabled(true);
                }
                else if (medium && (RC.CurrentWaypoint.Coords - RC.GetPosition()).Length() < 20)
                {
                    RC.ClearWaypoints();
                    RC.AddWaypoint(waypointVectors[currentWaypoint++], "Waypoint " + currentWaypoint);
                    if (currentWaypoint >= 7) currentWaypoint = 0;
                    RC.SetAutoPilotEnabled(true);
                }
                else if (large && needsInit)
                {
                    try
                    {
                        RC.AddWaypoint(pos.GetPosition() +
                        pos.WorldMatrixRef.Up * 300,
                        "Waypoint");
                        RC.SetAutoPilotEnabled(true);
                    }
                    catch (FormatException e)
                    {
                        MyAPIGateway.Utilities.ShowMessage("WORLD", "Format Exception: " + e);
                    }
                }
            }
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            RC = Entity as IMyRemoteControl;
            if (RC != null && MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds > 5)
            {
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
                //MyAPIGateway.Utilities.ShowMessage("WORLD", "RC CName: " + RC.CubeGrid?.CustomName);

                targetBase = EntityContainer.baseUnderAttack;

                if (targetBase != null)
                {
                    MyAPIGateway.Utilities.ShowMessage("WORLD", "Target Base not null");
                    pos = targetBase.WP.PositionComp;
                    if (RC.CubeGrid.CustomName.Contains("Phoenix"))
                    {
                        large = true;
                    }
                    else if (RC.CubeGrid.CustomName.Contains("Braumus with RC"))
                    {
                        medium = true;
                    }
                    else if (RC.CubeGrid.CustomName.Contains("Valkyrie with RC"))
                    {
                        small = true;
                    }
                }
                if (MyAPIGateway.Session.IsServer) isServer = true;
            }
        }

        IMyPlayer getClosestPlayer()
        {
            Vector3D shipPosition = RC.GetPosition();
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Multiplayer.Players.GetPlayers(players);
            IMyPlayer closestPlayer = null;
            double dist = double.MaxValue;
            foreach (IMyPlayer player in players)
            {
                double tempDist = (shipPosition - player.GetPosition()).Length();
                if (tempDist < dist)
                {
                    dist = tempDist;
                    closestPlayer = player;
                }
            }
            return closestPlayer;
        }
    }

    public static class EntityContainer
    {
        public static List<BaseWaypoint> bases = new List<BaseWaypoint>();

        public static BaseWaypoint enemyMainBase = new BaseWaypoint();

        public static BaseWaypoint baseUnderAttack = new BaseWaypoint();

        const int LARGE_SHIP_SPAWN_OFFSET = 300;
        const int MEDIUM_SHIP_SPAWN_OFFSET = 200;
        const int SMALL_SHIP_SPAWN_OFFSET = 100;

        public static void sendAttack(BaseWaypoint targetBase)
        {
            baseUnderAttack = targetBase;
            if (MyAPIGateway.Session.IsServer)
            {
                int attackPoints = 1 + (int)MyAPIGateway.Multiplayer.Players.Count * 2 + targetBase.tier * 10;
                int i = 0;
                Vector3D a = targetBase.WP.WorldMatrix.Up;
                Vector3D b = EntityContainer.enemyMainBase.WP.PositionComp.GetPosition() -
                    targetBase.WP.PositionComp.GetPosition();
                Vector3D proj = b - ((b.Dot(a) / a.Dot(a)) * a);
                proj.Normalize();
                int spawnDist = 9000;
                try
                {
                    while (attackPoints > 0)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            if (attackPoints > 0)
                            {
                                MyAPIGateway.Utilities.ShowMessage("WORLD", "Spawning small ship");
                                MyVisualScriptLogicProvider.SpawnPrefab("Valkyrie with RC",
                                targetBase.WP.PositionComp.GetPosition() +
                                targetBase.WP.WorldMatrix.Up * SMALL_SHIP_SPAWN_OFFSET +
                                targetBase.WP.WorldMatrix.Forward * j * SMALL_SHIP_SPAWN_OFFSET +
                                targetBase.WP.WorldMatrix.Right * i * SMALL_SHIP_SPAWN_OFFSET +
                                proj * (spawnDist / targetBase.tier),
                                targetBase.WP.WorldMatrix.Right,
                                targetBase.WP.WorldMatrix.Up,
                                MyAPIGateway.Session.Factions.TryGetFactionByTag("SPRT").FounderId,
                                "Valkyrie");
                                attackPoints -= 1;
                            }
                        }
                        for (int j = 0; j < 2; j++)
                        {
                            if (attackPoints >= 5)
                            {
                                MyAPIGateway.Utilities.ShowMessage("WORLD", "Spawning medium ship");
                                MyVisualScriptLogicProvider.SpawnPrefab("Braumus with RC",
                                targetBase.WP.PositionComp.GetPosition() +
                                targetBase.WP.WorldMatrix.Up * MEDIUM_SHIP_SPAWN_OFFSET +
                                targetBase.WP.WorldMatrix.Forward * j * MEDIUM_SHIP_SPAWN_OFFSET +
                                targetBase.WP.WorldMatrix.Right * i * MEDIUM_SHIP_SPAWN_OFFSET +
                                proj * spawnDist,
                                targetBase.WP.WorldMatrix.Right,
                                targetBase.WP.WorldMatrix.Up,
                                MyAPIGateway.Session.Factions.TryGetFactionByTag("SPRT").FounderId,
                                "Braumus");
                                attackPoints -= 5;
                            }
                        }
                        if (attackPoints > 10 && i == 0)
                        {
                            MyAPIGateway.Utilities.ShowMessage("WORLD", "Spawning large ship");
                            MyVisualScriptLogicProvider.SpawnPrefab("Phoenix with RC",
                                targetBase.WP.PositionComp.GetPosition() +
                                targetBase.WP.WorldMatrix.Up * LARGE_SHIP_SPAWN_OFFSET +
                                targetBase.WP.WorldMatrix.Right * i * LARGE_SHIP_SPAWN_OFFSET +
                                proj * spawnDist,
                                targetBase.WP.WorldMatrix.Right,
                                targetBase.WP.WorldMatrix.Up,
                                MyAPIGateway.Session.Factions.TryGetFactionByTag("SPRT").FounderId,
                                "Phoenix");
                            attackPoints -= 10;
                        }
                        i++;
                    }
                }
                catch (Exception e)
                {
                    MyAPIGateway.Utilities.ShowMessage("WORLD", "ERROR: " + e);
                }
            }
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Waypoint), false)]
    public class BaseWaypoint : MyGameLogicComponent
    {
        public MyWaypoint WP;

        public int tier;

        public string name;

        public IMyFaction owner = null;

        int timerStart = (int)MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds;
        bool isDefended = true;

        bool test = false;

        int timeOfLastQRF = -31;

        const int BASE_RADIUS = 500;

        int numInitOwnerAttempts = 0;

        const int TIME_TO_LOOSE_BASE = 10;
        
        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            if (owner == null) initOwner();
            else refreshNearbyEntities();
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            WP = Entity as MyWaypoint;
            if (WP != null)
            {
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
                //MyAPIGateway.Utilities.ShowMessage("WORLD", "WP HELLO: " + WP?.Name);
                string[] vars = WP.Name.Split(',');
                if (vars.Count() == 2)
                {
                    try
                    {
                        tier = int.Parse(vars[0]);
                        //MyAPIGateway.Utilities.ShowMessage("WORLD", "WP Parsed. Tier = " + tier);
                    }
                    catch (FormatException e)
                    {
                        tier = 0;
                        MyAPIGateway.Utilities.ShowMessage("WORLD", "WP name parse exception. tier = 0");
                    }
                    if (tier == 4) EntityContainer.enemyMainBase = this;
                    name = vars[1];
                }
                EntityContainer.bases.Add(this);
            }
        }

        public override void Close()
        {
            if (WP != null) EntityContainer.bases.Remove(this);
            base.Close();
        }

        void initOwner()
        {
            BoundingSphereD s = new BoundingSphereD(WP.PositionComp.GetPosition(), BASE_RADIUS);
            foreach (IMyEntity et in MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref s))
            {
                if (et.GetType().Name.Equals("MyCubeGrid") &&
                        (et as IMyCubeGrid).IsStatic &&
                        (et as IMyCubeGrid).BigOwners.Count > 0)
                {
                    long id = (et as IMyCubeGrid).BigOwners.First();
                    owner = MyAPIGateway.Session.Factions.TryGetPlayerFaction(id);
                    //MyAPIGateway.Utilities.ShowMessage("WORLD", "WP Owner: " + owner.Tag);
                    return;
                }
            }
            numInitOwnerAttempts++;
            if (numInitOwnerAttempts > 5) owner = MyAPIGateway.Session.Factions.TryGetFactionByTag("SPRT");
        }


        private void refreshNearbyEntities()
        {
            bool player = false;
            bool turret = false;

            BoundingSphereD s = new BoundingSphereD(WP.PositionComp.GetPosition(), BASE_RADIUS);
            foreach (IMyEntity e in MyAPIGateway.Entities.GetEntitiesInSphere(ref s))
            {
                string entityType = e.GetType().Name;
                if (entityType.Equals("MyLargeGatlingTurret") || 
                    entityType.Equals("MyLargeMissileTurret") ||
                    entityType.Equals("MyLargeInteriorTurret"))
                {
                    //MyAPIGateway.Utilities.ShowMessage("WORLD", "Turret inside");
                    IMyLargeTurretBase t = e as IMyLargeTurretBase;
                    if (t.IsWorking && t.GetOwnerFactionTag().Equals("SPRT"))
                    {
                        turret = true;
                    }
                }
                else if (entityType.Equals("MyCharacter"))
                {
                    player = true;
                    //MyAPIGateway.Utilities.ShowMessage("WORLD", "Player at: " + name);
                }
            }

            if (owner.Tag.Equals("SPRT") && !turret && player)
            {
                MyAPIGateway.Utilities.ShowMessage("WORLD", name + " has been taken");
                foreach (IMyEntity et in MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref s))
                {
                    if (et.GetType().Name.Equals("MyCubeGrid") &&
                        (et as IMyCubeGrid).IsStatic)
                    {
                        //MyAPIGateway.Utilities.ShowMessage("WORLD", "Owning: " + (et as IMyCubeGrid).CustomName);
                        if (MyAPIGateway.Session.IsServer)
                        {
                            (et as IMyCubeGrid).ChangeGridOwnership(MyAPIGateway.Session.Factions.TryGetFactionByTag("FSTC").FounderId, (MyOwnershipShareModeEnum)1);
                        }
                    }
                }
                owner = MyAPIGateway.Session.Factions.TryGetFactionByTag("FSTC");
                isDefended = true;
            }
            else if (owner.Tag.Equals("SPRT") && turret && player &&
                timeOfLastQRF + 30 < (int)MyAPIGateway.Session.ElapsedPlayTime.TotalMinutes &&
                EntityContainer.enemyMainBase.owner.Tag.Equals("SPRT"))
            {
                MyAPIGateway.Utilities.ShowMessage("WORLD", "QRF incoming at " + name);
                timeOfLastQRF = (int)MyAPIGateway.Session.ElapsedPlayTime.TotalMinutes;
                EntityContainer.sendAttack(this);
            }
            else if (owner.Tag.Equals("FSTC") && turret && !player && isDefended)
            {
                MyAPIGateway.Utilities.ShowMessage("WORLD", "No defenders at " + name + ", base is now unsecure");
                isDefended = false;
                timerStart = (int)MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds;
            }
            else if (owner.Tag.Equals("FSTC") && turret && !player && !isDefended &&
                timerStart + TIME_TO_LOOSE_BASE <= (int)MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds)
            {
                MyAPIGateway.Utilities.ShowMessage("WORLD", name + " has been lost");
                foreach (IMyEntity et in MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref s))
                {
                    if (et.GetType().Name.Equals("MyCubeGrid") &&
                        (et as IMyCubeGrid).IsStatic &&
                        MyAPIGateway.Session.IsServer)
                    {
                        if (MyAPIGateway.Session.IsServer)
                        {
                            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                            (et as IMyCubeGrid).GetBlocks(blocks);
                            foreach (IMySlimBlock b in blocks)
                            {
                                if (b.MaxIntegrity > b.Integrity)
                                {
                                    if (b.FatBlock != null) MyAPIGateway.Utilities.ShowMessage("WORLD", "Block Id: " + b.FatBlock.EntityId);

                                    MyAPIGateway.Utilities.ShowMessage("WORLD", "Repairing Block");
                                    MyAPIGateway.Utilities.ShowMessage("WORLD", "Owner: " + b.OwnerId);
                                    b.IncreaseMountLevel(b.MaxIntegrity - b.Integrity, MyAPIGateway.Session.Factions.TryGetFactionByTag("FSTC").FounderId);//, null, b.MaxDeformation);//p.First().IdentityId);//  p.First().IdentityId);//, null, b.MaxDeformation);
                                }
                                if (b.HasDeformation) b.FixBones(0.0f, b.MaxDeformation);
                            }
                            List<IMyPlayer> p = new List<IMyPlayer>();
                            MyAPIGateway.Players.GetPlayers(p);
                            MyAPIGateway.Utilities.ShowMessage("WORLD", "Me? " + MyAPIGateway.Session.Factions.TryGetFactionByTag("FSTC").FounderId);
                            MyAPIGateway.Utilities.ShowMessage("WORLD", "Me: " + p.First().IdentityId);
                            (et as IMyCubeGrid).ChangeGridOwnership(MyAPIGateway.Session.Factions.TryGetFactionByTag("SPRT").FounderId, (MyOwnershipShareModeEnum)1);//ChangeGridOwnership
                        }
                    }
                }
                owner = MyAPIGateway.Session.Factions.TryGetFactionByTag("SPRT");
                //test = true;
            }
            else if (owner.Tag.Equals("FSTC") && turret && !player && !isDefended &&
                timerStart + TIME_TO_LOOSE_BASE > (int)MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds)
            {
                MyAPIGateway.Utilities.ShowMessage("WORLD", name + " will be lost in " + 
                    (timerStart + TIME_TO_LOOSE_BASE - (int)MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds) + " seconds");
            }
            else if (owner.Tag.Equals("FSTC") && (player || !turret) && !isDefended)
            {
                MyAPIGateway.Utilities.ShowMessage("WORLD", name + " resecured");
                isDefended = true;
            }
        }
    }
}