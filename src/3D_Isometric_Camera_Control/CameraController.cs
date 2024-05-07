using Godot;

namespace TrapZone
{
    public enum EdgePannigState
    {
        None,
        Left,
        Right,
        Up,
        Down
    }
    public partial class CameraController : Node3D
    {
        private readonly float _edgePanningTriggerOffsetValue = 2f;// Value to trigger edge panning

        private readonly float _panningCameraSpeed = 50f; // Speed at which the camera pans

        private readonly float _middleMouseGrabCameraSpeed = 5f;// Speed at which the camera pans when middle mouse button is used


        // Minimum and maximum zoom distances
        private readonly int _zoomMinDistance = 20;
        private readonly int _zoomMaxDistance = 60;

        private readonly int _zoomAmountPerRoll = 10;// Amount by which the zoom changes per roll

        private float _currentCameraSize = 0f;// Current Camera Size used for zoom In/Out

        private float _smoothTimer = 0.0f;// Timer for smooth zoom interpolation

        private float _smoothZoomTime = 1.0f; // Time taken for smooth zoom

        private float _screenRatio;// Ratio of screen dimensions

        private Vector2 _viewPortSize;// Size of the current viewport

        private Vector2 _mouseRelativeVel = Vector2.Zero; // Relative mouse velocity, needed to reset after every frame

        // These variables control zooming, camera rotation, and dragging behavior
        private bool _applyZoomSmooth = false;
        private bool _rotateCamera = false;
        private bool _isDragging = false;

        [Export] private Camera3D _camera; // The camera object to be controlled
        public EdgePannigState pannigState = EdgePannigState.None; // The current panning state
        public override void _Ready()
        {
            _currentCameraSize = _camera.Size;
            _viewPortSize = GetViewport().GetVisibleRect().Size;
            _screenRatio = _viewPortSize.X / _viewPortSize.Y;
            GetViewport().SizeChanged += SizeChanged;
            Input.MouseMode = Input.MouseModeEnum.Confined;
        }
        /// <summary>
        /// called when the node is removed from the scene tree. Handle event unsubscriptions here to avoid memory leaks.
        /// </summary>
        public override void _ExitTree()
        {
            GetViewport().SizeChanged -= SizeChanged;
        }
        /// <summary>
        /// called when the viewport size changes. It updates the size and aspect ratio variables accordingly.
        /// </summary>
        private void SizeChanged()
        {
            _viewPortSize = GetViewport().GetVisibleRect().Size;
            _screenRatio = _viewPortSize.X / _viewPortSize.Y;
        }
        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
            if (_isDragging)
            {
                GlobalPosition -=
                        GlobalTransform.Basis.X * _mouseRelativeVel.X * _middleMouseGrabCameraSpeed * (float)delta +
                        GlobalTransform.Basis.Z * _mouseRelativeVel.Y * _middleMouseGrabCameraSpeed * (float)delta * _screenRatio;
            }
            else
            {
                ApplyPanningStateToCamera(delta);
            }
            if (_applyZoomSmooth)
            {
                ApplyZoom(delta);
            }
            if (_rotateCamera)
            {
                RotateY(-_mouseRelativeVel.X * 0.5f * (float)delta);
            }
            //relative mouse velocity need to be reset after every frame
            _mouseRelativeVel = Vector2.Zero;
        }
        /// <summary>
        /// handles smooth zooming by interpolating the camera size over time.
        /// </summary>
        /// <param name="delta"></param>
        private void ApplyZoom(double delta)
        {
            _smoothTimer += (float)delta;
            if (_smoothTimer < _smoothZoomTime)
            {
                _camera.Size = Mathf.Lerp(_camera.Size, _currentCameraSize, _smoothTimer / _smoothZoomTime);
            }
            else
            {
                _applyZoomSmooth = false;
            }
        }
        /// <summary>
        /// applies the current panning state to the camera, moving it in the corresponding direction based on the panning camera speed.
        /// </summary>
        /// <param name="delta"></param>
        private void ApplyPanningStateToCamera(double delta)
        {
            switch (pannigState)
            {
                case EdgePannigState.None:
                    break;
                case EdgePannigState.Left:
                    GlobalPosition +=
                      GlobalTransform.Basis.X * -_panningCameraSpeed * (float)delta;
                    break;
                case EdgePannigState.Right:
                    GlobalPosition +=
                     GlobalTransform.Basis.X * _panningCameraSpeed * (float)delta;
                    break;
                case EdgePannigState.Up:
                    GlobalPosition +=
                      GlobalTransform.Basis.Z * -_panningCameraSpeed * (float)delta;
                    break;
                case EdgePannigState.Down:
                    GlobalPosition +=
                     GlobalTransform.Basis.Z * _panningCameraSpeed * (float)delta;
                    break;
            }
        }
        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent)
            {
                if (keyEvent.Keycode == Key.Escape && keyEvent.Pressed)
                {
                    Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Confined ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Confined;
                }
            }
            if (@event is InputEventMouseButton mouseButton)
            {
                switch (mouseButton.ButtonIndex)
                {
                    case MouseButton.WheelDown:
                    case MouseButton.WheelUp:
                        {
                            if (!_isDragging)
                            {
                                _currentCameraSize = mouseButton.ButtonIndex == MouseButton.WheelUp ? _currentCameraSize - _zoomAmountPerRoll : _currentCameraSize + _zoomAmountPerRoll;
                                _currentCameraSize = Mathf.Clamp(_currentCameraSize, _zoomMinDistance, _zoomMaxDistance);
                                _smoothTimer = 0;
                                _applyZoomSmooth = true;
                            }
                        }
                        break;
                    case MouseButton.Middle when mouseButton.Pressed:
                        _isDragging = true;
                        break;
                    case MouseButton.Right when mouseButton.Pressed:
                        _rotateCamera = true;
                        break;
                    default:
                        _rotateCamera = false;
                        _isDragging = false;
                        break;
                }
            }
            if (@event is InputEventMouseMotion mouseMotion)
            {
                _mouseRelativeVel = mouseMotion.Relative;
                EdgePanningDetection(mouseMotion.Position);
            }
        }
        /// <summary>
        /// detects if the mouse cursor is near the edges of the screen.
        /// If the cursor is near an edge, it sets the corresponding panning state(Left, Right, Up, Down or None).
        /// </summary>
        /// <param name="mouseMotion"></param>
        private void EdgePanningDetection(Vector2 mouseMotion)
        {
            if (mouseMotion.X < _edgePanningTriggerOffsetValue)
            {
                pannigState = EdgePannigState.Left;
            }
            else if (mouseMotion.X > (_viewPortSize.X - _edgePanningTriggerOffsetValue))
            {
                pannigState = EdgePannigState.Right;
            }
            else if (mouseMotion.Y < _edgePanningTriggerOffsetValue)
            {
                pannigState = EdgePannigState.Up;
            }
            else if (mouseMotion.Y > (_viewPortSize.Y - _edgePanningTriggerOffsetValue))
            {
                pannigState = EdgePannigState.Down;
            }
            else
            {
                pannigState = EdgePannigState.None;
            }
        }
    }
}


