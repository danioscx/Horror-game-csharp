using Godot;

namespace Horror.Player.Scripts {

    public class Character : KinematicBody {
        private const float Gravity = -9.8f;
        private const float MotionInterpolateSpeed = 10.0f;
        private const float CameraRotationSpeed = 0.001f;
        private Transform _rootMotion;
        private Vector3 _velocity = Vector3.Zero;
        private Vector2 _motion;
        private Transform _orientation;

        private float _cameraXRoot;

        private Spatial _model;
        private Spatial _cameraRoot;
        private Spatial _cameraBase;
        private Camera _cameraView;
        private AnimationTree _animationTree;

        public override void _Ready() {
            base._Ready();
            _model = GetNode<Spatial>("Player");
            _cameraRoot = GetNode<Spatial>("CameraRoot");
            _cameraBase = GetNode<Spatial>("CameraRoot/CameraBase");
            _cameraView = GetNode<Camera>("CameraRoot/CameraBase/SpringArm/Camera");
            _animationTree = GetNode<AnimationTree>("AnimationTree");
            Input.SetMouseMode(Input.MouseMode.Captured);

            _orientation = _model.GlobalTransform;
            _orientation.origin = Vector3.Zero;
        }

        public override void _PhysicsProcess(float delta) {
            base._PhysicsProcess(delta);
            var motionTarget = new Vector2(
                Input.GetActionStrength("left") - Input.GetActionStrength("right"),
                Input.GetActionStrength("back") - Input.GetActionStrength("front")
            );
            motionTarget.Normalized();
            _motion = _motion.LinearInterpolate(motionTarget, MotionInterpolateSpeed * delta);
            var cameraBasis = _cameraView.GlobalTransform.basis;

            var cameraX = cameraBasis.x;
            var cameraZ = cameraBasis.z;

            cameraX.y = 0;
            cameraX.Normalized();
            cameraZ.y = 0;
            cameraZ.Normalized();

            if (motionTarget.Length() > 0) {
                var target = cameraX * _motion.x - cameraZ * _motion.y;
                if (target.Length() > 0.001) {
                    var qFrom = new Quat(_orientation.basis).Normalized();
                    var qTo = new Quat(new Transform().LookingAt(target, Vector3.Up).basis).Normalized();
                    _orientation.basis = new Basis(qFrom.Slerp(qTo, delta * MotionInterpolateSpeed));
                }
                _animationTree.Set("parameters/walk/blend_position", _motion.Length());
                _rootMotion = _animationTree.GetRootMotionTransform();
            }
            else {
                _animationTree.Set("parameters/walk/blend_position", _motion.Length());
                return;
            }
            _orientation *= _rootMotion;
            var hVel = _orientation.origin / delta;
            _velocity.x = hVel.x;
            _velocity.z = hVel.z;
            _velocity.y = Gravity * delta;

            _velocity = MoveAndSlide(_velocity, Vector3.Up);

            _orientation.origin = new Vector3();
            _orientation.Orthonormalized();

            _model.SetTransform(_orientation);
            var modelBasis = _model.GetTransform().basis;
            modelBasis = _orientation.basis;

        }

        public override void _Input(InputEvent @event) {
            base._Input(@event);
            if (@event is InputEventMouseMotion eventMouseMotion) {
                _cameraRoot.RotateY(-eventMouseMotion.Relative.x * CameraRotationSpeed);
                _cameraRoot.Orthonormalize();
                _cameraXRoot += eventMouseMotion.Relative.y * CameraRotationSpeed;
                _cameraXRoot = Mathf.Clamp(_cameraXRoot, Mathf.Deg2Rad(-30), Mathf.Deg2Rad(30));
                _cameraBase.SetRotation(new Vector3(_cameraXRoot, 0, 0));

            }
            if (@event.IsActionPressed("ui_cancel")) {
                Input.SetMouseMode(Input.MouseMode.Visible);
            }
        }
    }
    
}
