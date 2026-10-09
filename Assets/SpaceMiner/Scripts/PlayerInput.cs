using System;
using UnityEngine;
namespace SpaceMiner
{
    public enum CameraAction { Forward, Backward, Left, Right, Up, Down, Reset, Overview, Focus, ToggleHud, Technology }
    [Serializable] public sealed class CameraBindings
    {
        public KeyCode[] Keys = { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.E, KeyCode.Q, KeyCode.R, KeyCode.B, KeyCode.F, KeyCode.H, KeyCode.T };
        public KeyCode Get(CameraAction action) => Keys[(int)action];
        public static bool Allowed(KeyCode key) => key >= KeyCode.A && key <= KeyCode.Z || key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9 || key == KeyCode.UpArrow || key == KeyCode.DownArrow || key == KeyCode.LeftArrow || key == KeyCode.RightArrow;
        public bool TryAssign(CameraAction action, KeyCode key)
        {
            if (!Allowed(key)) return false;
            int index = (int)action, other = Array.IndexOf(Keys, key);
            if (other >= 0 && other != index) Keys[other] = Keys[index];
            Keys[index] = key; return true;
        }
        public void Validate()
        {
            var defaults = new CameraBindings().Keys;
            if(Keys!=null&&Keys.Length==defaults.Length-1){
                var migrated=new KeyCode[defaults.Length];Array.Copy(Keys,migrated,Keys.Length);
                // Preserve older custom bindings, including a camera action already on T.
                migrated[defaults.Length-1]=Array.IndexOf(Keys,KeyCode.T)<0?KeyCode.T:Array.Find((KeyCode[])Enum.GetValues(typeof(KeyCode)),key=>Allowed(key)&&Array.IndexOf(Keys,key)<0);
                Keys=migrated;
            }
            if (Keys == null || Keys.Length != defaults.Length) { Keys = defaults; return; }
            for (int i=0;i<Keys.Length;i++) if (!Allowed(Keys[i]) || Array.IndexOf(Keys,Keys[i]) != i) { Keys = defaults; return; }
        }
    }
    public static class PlayerInput
    {
        public static bool Held(CameraAction action) => Input.GetKey(SettingsStore.Current.Controls.Bindings.Get(action));
        public static bool Pressed(CameraAction action) => Input.GetKeyDown(SettingsStore.Current.Controls.Bindings.Get(action));
    }
}
