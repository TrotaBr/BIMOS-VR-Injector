using UnityEngine;

namespace BimosVrInjector.Mod.Runtime
{
    internal static class CameraUtil
    {
        public static Camera? GetActiveCamera()
        {
            var cam = Camera.main;
            if (cam != null) return cam;

            cam = Camera.current;
            if (cam != null) return cam;

            var all = Object.FindObjectsOfType<Camera>();
            Camera? any = null;
            foreach (var c in all)
            {
                if (c == null) continue;
                any = c;
                if (c.enabled && c.isActiveAndEnabled)
                    return c;
            }
            return any;
        }
    }
}
