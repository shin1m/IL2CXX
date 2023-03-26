using DemoUtilities;

namespace Demos
{
    public interface IHoldableBind
    {
        bool IsDown(Input input);
        bool WasPushed(Input input);
        TextBuilder AppendString(TextBuilder text);
    }
    public class HoldableKeyBind : IHoldableBind
    {
        private readonly string key;

        public HoldableKeyBind(string key) => this.key = key;
        public bool IsDown(Input input) => input.IsDown(key);
        public bool WasPushed(Input input) => input.WasPushed(key);
        public TextBuilder AppendString(TextBuilder text) => text.Append(key);
    }
    public class HoldableButtonBind : IHoldableBind
    {
        private readonly long button;

        public HoldableButtonBind(long button) => this.button = button;
        public bool IsDown(Input input) => input.IsDown(button);
        public bool WasPushed(Input input) => input.WasPushed(button);
        public TextBuilder AppendString(TextBuilder text) => text.Append($"Button{button}");
    }
    public class HoldableOrBind : IHoldableBind
    {
        private readonly IHoldableBind x;
        private readonly IHoldableBind y;

        public HoldableOrBind(IHoldableBind x, IHoldableBind y)
        {
            this.x = x;
            this.y = y;
        }
        public bool IsDown(Input input) => x.IsDown(input) || y.IsDown(input);
        public bool WasPushed(Input input) => x.WasPushed(input) || y.WasPushed(input);
        public TextBuilder AppendString(TextBuilder text) => y.AppendString(x.AppendString(text).Append(" or "));
    }

    public interface IInstantBind
    {
        bool WasTriggered(Input input);
        TextBuilder AppendString(TextBuilder text);
    }
    public class InstantKeyBind : IInstantBind
    {
        private readonly string key;

        public InstantKeyBind(string key) => this.key = key;
        public bool WasTriggered(Input input) => input.WasPushed(key);
        public TextBuilder AppendString(TextBuilder text) => text.Append(key);
    }
    public class InstantButtonBind : IInstantBind
    {
        private readonly long button;

        public InstantButtonBind(long button) => this.button = button;
        public bool WasTriggered(Input input) => input.WasPushed(button);
        public TextBuilder AppendString(TextBuilder text) => text.Append($"Button{button}");
    }
    public class InstantWheelScrollUpBind : IInstantBind
    {
        public bool WasTriggered(Input input) => input.ScrolledUp > 0;
        public TextBuilder AppendString(TextBuilder text) => text.Append("Wheel Scroll Up");
    }
    public class InstantWheelScrollDownBind : IInstantBind
    {
        public bool WasTriggered(Input input) => input.ScrolledDown < 0;
        public TextBuilder AppendString(TextBuilder text) => text.Append("Wheel Scroll Down");
    }
    public class InstantOrBind : IInstantBind
    {
        private readonly IInstantBind x;
        private readonly IInstantBind y;

        public InstantOrBind(IInstantBind x, IInstantBind y)
        {
            this.x = x;
            this.y = y;
        }
        public bool WasTriggered(Input input) => x.WasTriggered(input) || y.WasTriggered(input);
        public TextBuilder AppendString(TextBuilder text) => y.AppendString(x.AppendString(text).Append(" or "));
    }

    public struct Controls
    {
        public IHoldableBind MoveForward;
        public IHoldableBind MoveBackward;
        public IHoldableBind MoveLeft;
        public IHoldableBind MoveRight;
        public IHoldableBind MoveUp;
        public IHoldableBind MoveDown;
        public IInstantBind MoveSlower;
        public IInstantBind MoveFaster;
        public IHoldableBind Grab;
        public IHoldableBind GrabRotate;
        public float MouseSensitivity;
        public float CameraSlowMoveSpeed;
        public float CameraMoveSpeed;
        public float CameraFastMoveSpeed;

        public IHoldableBind SlowTimesteps;
        public IInstantBind LockMouse;
        public IInstantBind Exit;
        public IInstantBind ShowConstraints;
        public IInstantBind ShowContacts;
        public IInstantBind ShowBoundingBoxes;
        public IInstantBind ChangeTimingDisplayMode;
        public IInstantBind ChangeDemo;
        public IInstantBind ShowControls;

        public static Controls Default => new Controls
        {
            MoveForward = new HoldableKeyBind("KeyW"),
            MoveBackward = new HoldableKeyBind("KeyS"),
            MoveLeft = new HoldableKeyBind("KeyA"),
            MoveRight = new HoldableKeyBind("KeyD"),
            MoveUp = new HoldableKeyBind("ShiftLeft"),
            MoveDown = new HoldableKeyBind("ControlLeft"),
            MoveSlower = new InstantOrBind(new InstantWheelScrollDownBind(), new InstantKeyBind("KeyY")),
            MoveFaster = new InstantOrBind(new InstantWheelScrollUpBind(), new InstantKeyBind("KeyU")),
            Grab = new HoldableButtonBind(2),
            GrabRotate = new HoldableKeyBind("KeyQ"),
            MouseSensitivity = 1.5e-3f,
            CameraSlowMoveSpeed = 0.5f,
            CameraMoveSpeed = 5,
            CameraFastMoveSpeed = 50,

            SlowTimesteps = new HoldableOrBind(new HoldableButtonBind(1), new HoldableKeyBind("KeyO")),
            LockMouse = new InstantKeyBind("Tab"),
            Exit = new InstantKeyBind("Escape"),
            ShowConstraints = new InstantKeyBind("KeyJ"),
            ShowContacts = new InstantKeyBind("KeyK"),
            ShowBoundingBoxes = new InstantKeyBind("KeyL"),
            ChangeTimingDisplayMode = new InstantKeyBind("F2"),
            ChangeDemo = new InstantOrBind(new InstantKeyBind("Backquote"), new InstantKeyBind("F3")),
            ShowControls = new InstantKeyBind("F1")
        };
    }
}
