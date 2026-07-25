using BimosVrInjector.Core.Abstractions;
using UnityEngine;

namespace BimosVrInjector.Mod.Runtime
{
    internal interface ILiveRigSpawner : IRigSpawner
    {
        GameObject? Current { get; }
    }
}
