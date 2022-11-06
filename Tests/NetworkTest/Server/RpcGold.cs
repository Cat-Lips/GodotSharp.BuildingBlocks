using System.Diagnostics;
using Godot;
using GodotSharp.BuildingBlocks;
using static NetworkTest.Constants;

namespace NetworkTest
{
    [SceneTree]
    public partial class RpcGold : Node
    {
        private const bool log = false;

        private const float SafeCaptureTime = .5f;
        private static readonly PackedScene GoldBar = App.LoadScene<GoldBar>();
        private static readonly PackedScene FinishLine = App.LoadScene<FinishLine>();

        private GoldBar gold;
        private FinishLine finish;
        private Action<Node> Spawn;
        private Action<Action<Vector3>> OnSpawnPoint;

        public Vehicle VehicleWithGold { get; private set; }

        private IReadOnlyDictionary<int, Player> Players;
        private IReadOnlyDictionary<StringName, int> Drivers;
        private IReadOnlyDictionary<StringName, Vehicle> Vehicles;

        public void Initialise(
            IReadOnlyDictionary<StringName, int> drivers,
            IReadOnlyDictionary<int, Player> players,
            IReadOnlyDictionary<StringName, Vehicle> vehicles)
        {
            Drivers = drivers;
            Players = players;
            Vehicles = vehicles;
        }

        public void Start(Action<Node> spawn, Action<Action<Vector3>> spawnPointGenerator)
        {
            Debug.Assert(this.MultiplayerServer());
            if (!this.MultiplayerServer()) return;

            Spawn = spawn;
            OnSpawnPoint = spawnPointGenerator;
            OnSpawnPoint(SpawnGold);
        }

        private void SpawnGold(Vector3 pos)
        {
            Debug.Assert(gold is null);
            Debug.Assert(finish is null);
            Debug.Assert(VehicleWithGold is null);

            gold = GoldBar.Instantiate<GoldBar>();
            gold.Position = pos + UpBy1;
            Spawn(gold);

            Log.If(log, $"{gold.Name} spawned at {gold.Position}");

            gold.BodyEntered += OnGoldEntered;

            void OnGoldEntered(Node body)
            {
                if (body is Vehicle vehicle && TryCaptureGold(vehicle, true))
                {
                    gold.BodyEntered -= OnGoldEntered;
                    OnSpawnPoint(SpawnFinish);
                }
            }
        }

        private void SpawnFinish(Vector3 pos)
        {
            Debug.Assert(gold is not null);
            Debug.Assert(finish is null);
            Debug.Assert(VehicleWithGold is not null);

            finish = FinishLine.Instantiate<FinishLine>();
            finish.LookAtFromPosition(pos, gold.Position with { Y = pos.Y });
            Spawn(finish);

            Log.If(log, $"{finish.Name} spawned at {finish.Position}");

            finish.RibbonArea.AreaEntered += OnRibbonEntered;

            void OnRibbonEntered(Area3D area)
            {
                if (area is GoldBar gold)
                {
                    finish.RibbonArea.AreaEntered -= OnRibbonEntered;
                    DeliverGold();
                }
            }
        }

        private bool TryCaptureGold(Vehicle vehicle, bool firstCapture = false)
        {
            Debug.Assert(gold is not null);
            Debug.Assert(firstCapture == finish is null);
            Debug.Assert(firstCapture == VehicleWithGold is null);

            // First capture can be taken by any vehicle
            // Steals must be made by player driver
            if (!VehicleHasDriver(vehicle.Name, out var driver) && !firstCapture) return false;
            Debug.Assert(driver is null || driver.VehicleName == vehicle.Name);

            if (driver is null)
            {
                Debug.Assert(firstCapture);
                Debug.Assert(VehicleWithGold is null);
                Log.If(log, $"{gold.Name} first captured by {vehicle.Name} (no driver)");
            }
            else
            {
                Log.If(log, $"{gold.Name} {(firstCapture ? "first " : "")}captured by {driver.PlayerName} in {driver.VehicleName}");

                if (VehicleWithGold is not null && VehicleHasDriver(VehicleWithGold.Name, out var oldDriver))
                    oldDriver.Status.HasGold = false;
                driver.Status.HasGold = true;

                UpdateScore();
            }

            VehicleWithGold = vehicle;
            gold.VehicleName = vehicle.Name;
            SafeCaptureTimer(OnSafeCaptureTimeout);
            return true;

            void UpdateScore()
            {
                if (firstCapture)
                    ++driver.Status.Captures;
                else
                    ++driver.Status.Steals;
            }

            void OnSafeCaptureTimeout()
            {
                vehicle.BodyEntered += OnVehicleEntered;
                gold.TreeExiting += OnGoldCaptured;

                void OnVehicleEntered(Node body)
                {
                    if (body is Vehicle otherVehicle && TryCaptureGold(otherVehicle))
                        OnGoldCaptured();
                }

                void OnGoldCaptured()
                {
                    gold.TreeExiting -= OnGoldCaptured;
                    vehicle.BodyEntered -= OnVehicleEntered;
                }
            };
        }

        private void DeliverGold()
        {
            Debug.Assert(gold is not null);
            Debug.Assert(finish is not null);
            Debug.Assert(VehicleWithGold is not null);

            // Must be player driver to deliver gold
            if (!VehicleHasDriver(VehicleWithGold.Name, out var driver)) return;

            Log.If(log, $"{gold.Name} delivered to {finish.Name} by {driver.PlayerName} in {driver.VehicleName}");

            UpdateScore();
            VehicleWithGold = null;
            driver.Status.HasGold = false;

            gold.DetachFromParent(free: true);
            gold = null; // TODO: Fancy FX (pop, explode, cheer, etc)

            finish.Rpc(nameof(finish.PlaySnipAnim));
            finish.OnCaptureComplete(OnCaptureComplete);

            void UpdateScore()
                => ++driver.Status.Deliveries;

            void OnCaptureComplete()
            {
                finish.DetachFromParent(free: true);
                finish = null; // TODO: Fancy FX (fade out, sink/shrink, dissolve, etc)

                OnSpawnPoint(SpawnGold);
            }
        }

        private void SafeCaptureTimer(Action action)
            => GetTree().CreateTimer(SafeCaptureTime).Timeout += action;

        private bool VehicleHasDriver(StringName vehicleName, out Player driver)
        {
            if (!Drivers.TryGetValue(vehicleName, out var driverId))
            {
                driver = null;
                return false;
            }

            if (!Players.TryGetValue(driverId, out driver))
            {
                Log.Warn($"Unknown Player [{Log.Var(driverId)}]");
                return false;
            }

            return true;
        }
    }
}
