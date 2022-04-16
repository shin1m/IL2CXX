using BepuUtilities;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Web;

namespace DemoUtilities
{
    using KeySet = HashSet<string>;
    using MouseButtonSet = HashSet<long>;
    public class Input
    {
        //You could use GetState-like stuff to avoid the need to explicitly grab these, but shrug. This keeps events localized to just the window, and we can do a little logic of our own.
        private readonly KeySet anyDownedKeys = new(8);
        private readonly KeySet downedKeys = new(8);
        private readonly KeySet previousDownedKeys = new(8);
        private readonly MouseButtonSet anyDownedButtons = new(8);
        private readonly MouseButtonSet downedButtons = new(8);
        private readonly MouseButtonSet previousDownedButtons = new(8);
        public readonly List<char> TypedCharacters = new(32);

        /// <summary>
        /// Forces the mouse to stay at the center of the screen by recentering it on every flush.
        /// </summary>
        public bool MouseLocked;

        /// <summary>
        /// Gets or sets the mouse position in window coordinates without changing the net mouse delta.
        /// </summary>
        public Int2 MousePosition { get; private set; }

        /// <summary>
        /// Gets the change in mouse position since the previous flush.
        /// </summary>
        public Int2 MouseDelta;

        /// <summary>
        /// Gets the amount of upward mouse wheel scrolling since the last flush regardless of how much downward scrolling occurred.
        /// </summary>
        public float ScrolledUp { get; private set; }
        /// <summary>
        /// Gets the amount of downward mouse wheel scrolling since the last flush regardless of how much upward scrolling occurred.
        /// </summary>
        public float ScrolledDown { get; private set; }
        /// <summary>
        /// Gets the mouse wheel scroll delta since the last flush.
        /// </summary>
        public float ScrollDelta => ScrolledUp + ScrolledDown;

        public void KeyPress(KeyboardEventArgs e)
        {
            if (e.Key.Length == 1) TypedCharacters.Add(e.Key[0]);
        }

        public void MouseWheel(WheelEventArgs e)
        {
            if (e.DeltaY > 0)
                ScrolledUp += (float)e.DeltaY;
            else
                ScrolledDown += (float)e.DeltaY;
        }

        public void MouseDown(MouseEventArgs e)
        {
            anyDownedButtons.Add(e.Button);
            downedButtons.Add(e.Button);
        }
        public void MouseUp(MouseEventArgs e) => downedButtons.Remove(e.Button);
        public void MouseMove(MouseEventArgs e) => MousePosition = new((int)e.ClientX, (int)e.ClientY);
        public void PointerMove(int dx, int dy) => MouseDelta = new(MouseDelta.X + dx, MouseDelta.Y + dy);

        public void KeyDown(KeyboardEventArgs e)
        {
            anyDownedKeys.Add(e.Code);
            downedKeys.Add(e.Code);
        }
        public void KeyUp(KeyboardEventArgs e) => downedKeys.Remove(e.Code);

        /// <summary>
        /// Gets whether a key is currently pressed according to the latest event processing call.
        /// </summary>
        /// <param name="key">Key to check.</param>
        /// <returns>True if the key was pressed in the latest event processing call, false otherwise.</returns>
        public bool IsDown(string key) => downedKeys.Contains(key);

        /// <summary>
        /// Gets whether a key was down at the time of the previous flush.
        /// </summary>
        /// <param name="key">Key to check.</param>
        /// <returns>True if the key was down at the time of the previous flush, false otherwise.</returns>
        public bool WasDown(string key) => previousDownedKeys.Contains(key);

        /// <summary>
        /// Gets whether a down event occurred at any point between the previous flush and up to the last event process call for a key that was not down in the previous flush.
        /// </summary>
        /// <param name="key">Key to check.</param>
        /// <returns>True if the key was pressed in the latest event processing call, false otherwise.</returns>
        public bool WasPushed(string key) => !previousDownedKeys.Contains(key) && anyDownedKeys.Contains(key);

        /// <summary>
        /// Gets whether a button is currently pressed according to the latest event processing call.
        /// </summary>
        /// <param name="button">Button to check.</param>
        /// <returns>True if the button was pressed in the latest event processing call, false otherwise.</returns>
        public bool IsDown(long button) => downedButtons.Contains(button);

        /// <summary>
        /// Gets whether a button was down at the time of the previous flush.
        /// </summary>
        /// <param name="mouseButton">Button to check.</param>
        /// <returns>True if the button was down at the time of the previous flush, false otherwise.</returns>
        public bool WasDown(long mouseButton) => previousDownedButtons.Contains(mouseButton);

        /// <summary>
        /// Gets whether a down event occurred at any point between the previous flush and up to the last event process call for a button that was not down in the previous flush.
        /// </summary>
        /// <param name="button">Button to check.</param>
        /// <returns>True if the button was pressed in the latest event processing call, false otherwise.</returns>
        public bool WasPushed(long button) => !previousDownedButtons.Contains(button) && anyDownedButtons.Contains(button);

        public void End()
        {
            anyDownedKeys.Clear();
            anyDownedButtons.Clear();
            previousDownedKeys.Clear();
            previousDownedKeys.UnionWith(downedKeys);
            previousDownedButtons.Clear();
            previousDownedButtons.UnionWith(downedButtons);
            ScrolledDown = ScrolledUp = 0;
            TypedCharacters.Clear();
            MouseDelta = default;
        }
    }
}
