using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skinner
{
    [AddComponentMenu("")]
    public class CullingStateController : MonoBehaviour
    {
        public Renderer target { get; set; }

        private void OnPreCull()
        {
            target.enabled = true;
        }

        private void OnPostRender()
        {
            target.enabled = false;
        }
    }
}