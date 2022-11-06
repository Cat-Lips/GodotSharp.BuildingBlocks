using Godot;

namespace GodotSharp.BuildingBlocks
{
    public static class CameraCore
    {
        private static readonly float CameraClampX = Mathf.DegToRad(80);

        public static void ProcessSelect(this Camera3D camera, Action<CollisionObject3D> onObjectSelected)
        {
            if (onObjectSelected is null) return;

            var screenPos = camera.GetViewport().GetMousePosition();
            var rayStart = camera.ProjectRayOrigin(screenPos);
            var rayNormal = camera.ProjectRayNormal(screenPos);

            camera.CastRay(rayStart, rayNormal, 1000, onObjectSelected);

        }

        public static void ProcessAsChaseCamera(this Camera3D camera, RigidBody3D target, Vector3 offset, float speed, float delta)
            => camera.ProcessAsFollowCamera(target, offset, speed, delta); // TODO

        public static void ProcessAsFollowCamera(this Camera3D camera, Node3D target, Vector3 offset, float speed, float delta)
        {
            var sourceTransform = camera.GlobalTransform;
            var targetTransform = target.GlobalTransform.TranslatedLocal(offset); // TODO:  Offset from direction of travel
            camera.GlobalTransform = sourceTransform.InterpolateWith(targetTransform, speed * delta);
            camera.LookAt(target.GlobalPosition);
        }

        public static void ProcessAsFreeLookCamera(this Camera3D camera, float speed, float delta)
        {
            if (Input.IsActionPressed(MyInput.Run))
                speed *= 10;

            var (x, z) = Input.GetVector(MyInput.Left, MyInput.Right, MyInput.Forward, MyInput.Back) * speed * delta;
            var y = Input.GetAxis(MyInput.Down, MyInput.Up) * speed * delta;
            camera.TranslateObjectLocal(new(x, y, z));
        }

        public static void ProcessInputAsFreeLookCamera(this Camera3D camera, InputEvent e, float sensitivity = 0.05f)
        {
            if (e is InputEventMouseMotion motion)
            {
                var (yaw, pitch) = -motion.Relative * sensitivity;
                camera.RotateY(Mathf.DegToRad(yaw));
                camera.RotateObjectLocal(new(1, 0, 0), Mathf.DegToRad(pitch));
                ClampCameraRotation();

                void ClampCameraRotation()
                {
                    var rot = camera.Rotation;
                    rot.X = Math.Clamp(rot.X, -CameraClampX, CameraClampX);
                    rot.Z = 0;
                    camera.Rotation = rot;
                }
            }
        }
    }
}
