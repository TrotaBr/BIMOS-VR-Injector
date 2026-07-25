using BimosVrInjector.Core.Abstractions;
using UnityEngine;

namespace BimosVrInjector.Mod.Runtime
{
    internal sealed class CubeRigSpawner : ILiveRigSpawner
    {
        public const string RigName = "BIMOS_Rig_Placeholder";

        private GameObject? _current;

        public void Spawn(float[] pos, float[] rotEuler, float[] scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = RigName;
            go.transform.position = new Vector3(pos[0], pos[1], pos[2]);
            go.transform.rotation = Quaternion.Euler(rotEuler[0], rotEuler[1], rotEuler[2]);
            go.transform.localScale = new Vector3(scale[0], scale[1], scale[2]);

            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Forward";
            nose.transform.SetParent(go.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0f, 0.6f);
            nose.transform.localScale = new Vector3(0.25f, 0.25f, 0.5f);

            _current = go;
        }

        public void DespawnExisting()
        {
            if (_current != null)
            {
                Object.Destroy(_current);
                _current = null;
            }

            var stray = GameObject.Find(RigName);
            if (stray != null)
                Object.Destroy(stray);
        }

        public GameObject? Current => _current;
    }
}
