using UnityEngine;

namespace BimosRig
{
    public class RotateForever : MonoBehaviour
    {
        public float degreesPerSecond = 90f;

        private void Update()
        {
            transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f);
        }
    }
}
