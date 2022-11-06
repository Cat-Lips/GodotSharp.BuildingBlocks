using Godot;

namespace GodotSharp.BuildingBlocks
{
    [Tool]
    public partial class VehicleFeatures : Resource
    {
        public enum VehicleDriveType
        {
            AllWheelDrive,
            RearWheelDrive,
            FrontWheelDrive,
        }

        [Export, Notify]
        public VehicleDriveType DriveType
        {
            get => _driveType.Get();
            set => _driveType.Set(value);
        }

        public VehicleFeatures()
            => ResourceName = nameof(VehicleFeatures);

        internal VehicleFeatures Initialise(VehicleWheel3D[] allWheels)
        {
            var rearWheels = allWheels.Where(x => x.Name.ToString().ToLower().Contains("rear")).ToArray();
            var frontWheels = allWheels.Where(x => x.Name.ToString().ToLower().Contains("front")).ToArray();

            DriveType = GetDriveType();
            DriveTypeChanged += SetDriveType;

            return this;

            VehicleDriveType GetDriveType()
            {
                return IsAllWheelDrive() ? VehicleDriveType.AllWheelDrive
                    : IsRearWheelDrive() ? VehicleDriveType.RearWheelDrive
                    : IsFrontWheelDrive() ? VehicleDriveType.FrontWheelDrive
                    : (VehicleDriveType)(-1);

                bool IsAllWheelDrive() => allWheels.All(x => x.UseAsTraction);
                bool IsRearWheelDrive() => rearWheels.All(x => x.UseAsTraction);
                bool IsFrontWheelDrive() => frontWheels.All(x => x.UseAsTraction);
            }

            void SetDriveType()
            {
                switch (DriveType)
                {
                    case VehicleDriveType.AllWheelDrive:
                        allWheels.ForEach(x => x.UseAsTraction = true);
                        break;
                    case VehicleDriveType.RearWheelDrive:
                        rearWheels.ForEach(x => x.UseAsTraction = true);
                        allWheels.Except(rearWheels).ForEach(x => x.UseAsTraction = false);
                        break;
                    case VehicleDriveType.FrontWheelDrive:
                        frontWheels.ForEach(x => x.UseAsTraction = true);
                        allWheels.Except(frontWheels).ForEach(x => x.UseAsTraction = false);
                        break;
                }
            }
        }
    }
}
