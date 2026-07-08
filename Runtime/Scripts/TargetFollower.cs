
using UnityEngine;
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP 
using UdonSharpEditor;
#endif

namespace VRSL
{
    public class TargetFollower : UdonSharpBehaviour
    {
        [Tooltip ("Enable this to follow whoever owns this object. Change the owner of this object to change who this object follows.")]
        public bool followOwner;

        void Update() 
        {
            if (Networking.GetOwner(this.gameObject) != null && followOwner)
            {
                this.transform.position = Networking.GetOwner(this.gameObject).GetPosition();
            }
        }
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            private void OnDrawGizmos()
            {
                #pragma warning disable 0618 //suppressing obsoletion warnings
                this.UpdateProxy(ProxySerializationPolicy.RootOnly);
                #pragma warning restore 0618 //suppressing obsoletion warnings
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(transform.position, 0.25f);
            }
#endif
    }
}


